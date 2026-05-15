import datetime
import logging
import math
from concurrent.futures import ThreadPoolExecutor, as_completed
from typing import Any

from .index_names import (
    issue_index_pattern,
    historical_snapshot_index,
    historical_scan_snapshot_index,
    current_snapshot_index,
    current_scan_snapshot_index,
)
from .month_range import iter_months, month_start
from .issue_snapshot_builder import (
    fetch_asset_descriptors,
    build_monthly_issue_snapshots,
    write_monthly_issue_snapshots,
)
from .scan_snapshot_builder import (
    build_monthly_scan_snapshots,
    write_monthly_scan_snapshots,
)


_FIELD_SOURCE_TYPE = "saltminer.asset.source_type"
_FIELD_ASSET_TYPE  = "saltminer.asset.asset_type"
_FIELD_SOURCE_ID   = "saltminer.asset.source_id"


def _get(doc, *path: str, default=None):
    cur = doc
    for key in path:
        if not hasattr(cur, 'get'):
            return default
        cur = cur.get(key, default)
        if cur is None:
            return default
    return cur


def _normalize_source_type(raw: str, available: list[str]) -> str | None:
    """
    Match a user-supplied source type (e.g. 'FOD', 'Saltworks.FOD') against the
    actual values found in ES.  Comparison is case-insensitive on the trailing
    component after the last '.'.
    """
    tail = raw.split(".")[-1].lower()
    for avail in available:
        if avail.split(".")[-1].lower() == tail:
            return avail
    return None


def discover_source_type_asset_type_pairs(es) -> list[tuple[str, str]]:
    """Return [(source_type, asset_type), ...] found across all issue indices."""
    query: dict[str, Any] = {
        "aggs": {
            "pairs": {
                "composite": {
                    "size": 200,
                    "sources": [
                        {"source_type": {"terms": {"field": _FIELD_SOURCE_TYPE}}},
                        {"asset_type":  {"terms": {"field": _FIELD_ASSET_TYPE}}},
                    ],
                }
            }
        },
        "size": 0,
    }
    pairs: list[tuple[str, str]] = []
    after_key = None
    while True:
        if after_key:
            query["aggs"]["pairs"]["composite"]["after"] = after_key
        result = es.Search("issues_*", queryBody=query, size=0, navToData=False)
        buckets = _get(result, "aggregations", "pairs", "buckets", default=[])
        for b in buckets:
            pairs.append((b["key"]["source_type"], b["key"]["asset_type"]))
        after_key = _get(result, "aggregations", "pairs", "after_key")
        if not after_key or len(buckets) == 0:
            break
    return pairs


def _pair_needs_backfill(es, source_type: str, asset_type: str) -> bool:
    """True when the historical issue-snapshot index is missing or empty."""
    idx = historical_snapshot_index(asset_type, source_type)
    return es.Count(idx, suppressErrorOnMissingIndex=True) == 0


def _earliest_found_dates(
    es, pairs: list[tuple[str, str]]
) -> dict[tuple[str, str], datetime.datetime]:
    """
    Single aggregate query: for each (source_type, asset_type) in `pairs`,
    return the earliest vulnerability.found_date truncated to month start.
    Pairs with no non-null found_date are omitted from the result.
    """
    if not pairs:
        return {}
    should_clauses = [
        {"bool": {"must": [
            {"term": {_FIELD_SOURCE_TYPE: st}},
            {"term": {_FIELD_ASSET_TYPE:  at}},
        ]}}
        for st, at in pairs
    ]
    query: dict[str, Any] = {
        "query": {"bool": {"should": should_clauses, "minimum_should_match": 1}},
        "aggs": {
            "pairs": {
                "composite": {
                    "size": 200,
                    "sources": [
                        {"source_type": {"terms": {"field": _FIELD_SOURCE_TYPE}}},
                        {"asset_type":  {"terms": {"field": _FIELD_ASSET_TYPE}}},
                    ],
                },
                "aggs": {
                    "min_found": {"min": {"field": "vulnerability.found_date"}},
                },
            }
        },
        "size": 0,
    }
    out: dict[tuple[str, str], datetime.datetime] = {}
    after_key = None
    while True:
        if after_key:
            query["aggs"]["pairs"]["composite"]["after"] = after_key
        res = es.Search("issues_*", queryBody=query, size=0, navToData=False)
        buckets = _get(res, "aggregations", "pairs", "buckets", default=[])
        for b in buckets:
            raw = b.get("min_found", {}).get("value_as_string")
            if not raw:
                continue
            try:
                dt = datetime.datetime.fromisoformat(raw.replace("Z", "+00:00"))
            except ValueError:
                logging.warning("Could not parse min_found '%s' for bucket %s", raw, b.get("key"))
                continue
            key = (b["key"]["source_type"], b["key"]["asset_type"])
            out[key] = month_start(dt.year, dt.month)
        after_key = _get(res, "aggregations", "pairs", "after_key")
        if not after_key or len(buckets) == 0:
            break
    return out


def _collect_all_source_ids(es, asset_type: str, source_type: str, page_size: int = 1000) -> list[str]:
    index = issue_index_pattern(asset_type)
    ids: list[str] = []
    after_key = None
    while True:
        composite: dict[str, Any] = {
            "size": page_size,
            "sources": [{"source_id": {"terms": {"field": _FIELD_SOURCE_ID}}}],
        }
        if after_key:
            composite["after"] = after_key
        query: dict[str, Any] = {
            "query": {"term": {_FIELD_SOURCE_TYPE: source_type}},
            "aggs": {"ids": {"composite": composite}},
            "size": 0,
        }
        result = es.Search(index, queryBody=query, size=0, navToData=False)
        buckets = _get(result, "aggregations", "ids", "buckets", default=[])
        ids.extend(b["key"]["source_id"] for b in buckets)
        after_key = _get(result, "aggregations", "ids", "after_key")
        if not after_key or len(buckets) == 0:
            break
    return ids


def _chunk(lst: list, n: int) -> list[list]:
    size = max(1, math.ceil(len(lst) / n))
    return [lst[i:i + size] for i in range(0, len(lst), size)]


def _process_partition(
    es,
    asset_type: str,
    source_type: str,
    partition_ids: list[str],
    months: list[tuple[int, int]],
    composite_page_size: int,
    source_id_chunk_size: int,
    current_month: tuple[int, int] | None = None,
) -> None:
    """Process all months for one partition of source IDs.

    months: historical months to write into per-month indices.
    current_month: if provided, also processed and written to the _current index.
    """
    logging.info("Partition of %d source IDs starting (%s / %s)", len(partition_ids), source_type, asset_type)

    all_months = list(months)
    if current_month and current_month not in all_months:
        all_months.append(current_month)

    descriptors = fetch_asset_descriptors(es, asset_type, source_type, partition_ids)
    logging.debug("Fetched %d asset descriptors for partition", len(descriptors))

    prev_avg_loc: dict[str, float] = {}

    id_chunks = _chunk(partition_ids, max(1, math.ceil(len(partition_ids) / source_id_chunk_size)))

    for year, month in all_months:
        is_current = current_month == (year, month)
        issue_write_target = current_snapshot_index(asset_type, source_type) if is_current \
                             else historical_snapshot_index(asset_type, source_type)
        scan_write_target  = current_scan_snapshot_index(asset_type, source_type) if is_current \
                             else historical_scan_snapshot_index(asset_type, source_type)
        snap_source_index  = issue_write_target  # scan builder reads from the same index issue snapshots were just written to

        issue_docs: list[dict] = []
        for chunk in id_chunks:
            issue_docs.extend(build_monthly_issue_snapshots(
                es, asset_type, source_type, chunk, year, month, descriptors, composite_page_size
            ))
        write_monthly_issue_snapshots(
            es, asset_type, source_type, partition_ids, year, month, issue_docs,
            target_index=issue_write_target,
        )

        scan_docs: list[dict] = []
        for chunk in id_chunks:
            month_scan_docs, prev_avg_loc = build_monthly_scan_snapshots(
                es, asset_type, source_type, chunk, year, month, prev_avg_loc,
                snapshot_source_index=snap_source_index,
                page_size=composite_page_size,
            )
            scan_docs.extend(month_scan_docs)
        write_monthly_scan_snapshots(
            es, asset_type, source_type, partition_ids, year, month, scan_docs,
            target_index=scan_write_target,
        )

    logging.info("Partition complete (%d source IDs, %d months, current=%s)",
                 len(partition_ids), len(all_months), current_month is not None)


def run_snapshot_history(
    app_settings,
    source_type_arg: str | None = None,
    worker_count: int = 4,
    composite_page_size: int = 1000,
    source_id_chunk_size: int = 1000,
    rebuild: bool = False,
) -> None:
    """
    Smart snapshot entry point.

    On each run:
      - Discover all (source_type, asset_type) pairs from issues_*.
      - For pairs whose _historical index is missing or empty, build historical
        months starting from min(vulnerability.found_date) for that pair.
      - For pairs whose _historical already has data, skip historical and only
        refresh _current.
      - Always refresh _current for every selected pair.

    source_type_arg: optional source-type filter (e.g. 'FOD' or 'Saltworks.FOD').
    rebuild: requires source_type_arg. Deletes the _historical issue and scan
             indices for the matched pairs before running, so the normal flow
             rebuilds from earliest data.
    """
    if rebuild and not source_type_arg:
        raise ValueError("rebuild=True requires source_type_arg")

    es = app_settings.Application.GetElasticClient()

    all_pairs = discover_source_type_asset_type_pairs(es)
    if not all_pairs:
        logging.warning("No source type / asset type pairs found in issues_* indices. Nothing to do.")
        return

    if source_type_arg:
        available = list({st for st, _ in all_pairs})
        resolved = _normalize_source_type(source_type_arg, available)
        if not resolved:
            logging.error("Source type '%s' not found in data. Available: %s",
                          source_type_arg, available)
            return
        selected_pairs = [(st, at) for st, at in all_pairs if st == resolved]
    else:
        selected_pairs = all_pairs

    if rebuild:
        for source_type, asset_type in selected_pairs:
            for idx in (
                historical_snapshot_index(asset_type, source_type),
                historical_scan_snapshot_index(asset_type, source_type),
            ):
                if es.IndexExists(idx):
                    logging.info("Rebuild: deleting %s", idx)
                    es.DeleteIndex(idx)

    needs_backfill: list[tuple[str, str]] = []
    current_only:   list[tuple[str, str]] = []
    for source_type, asset_type in selected_pairs:
        if _pair_needs_backfill(es, source_type, asset_type):
            needs_backfill.append((source_type, asset_type))
        else:
            current_only.append((source_type, asset_type))

    logging.info("Selected %d pair(s): %d need backfill, %d current-only",
                 len(selected_pairs), len(needs_backfill), len(current_only))

    earliest: dict[tuple[str, str], datetime.datetime] = (
        _earliest_found_dates(es, needs_backfill) if needs_backfill else {}
    )

    now = datetime.datetime.now(tz=datetime.timezone.utc)
    this_month = (now.year, now.month)

    for source_type, asset_type in selected_pairs:
        logging.info("Starting snapshot: source_type=%s, asset_type=%s", source_type, asset_type)

        if (source_type, asset_type) in needs_backfill:
            start = earliest.get((source_type, asset_type))
            if not start:
                logging.warning(
                    "No valid vulnerability.found_date for %s / %s — skipping backfill, refreshing _current only.",
                    source_type, asset_type,
                )
                historical_months: list[tuple[int, int]] = []
            else:
                all_months = list(iter_months(start, now))
                historical_months = [m for m in all_months if m != this_month]
                logging.info(
                    "%s / %s: building %d historical month(s) from %s",
                    source_type, asset_type, len(historical_months), start.date(),
                )
        else:
            historical_months = []

        all_source_ids = _collect_all_source_ids(es, asset_type, source_type)
        if not all_source_ids:
            logging.warning("No source IDs found for %s / %s — skipping.", source_type, asset_type)
            continue
        logging.info("Found %d distinct source IDs to process", len(all_source_ids))

        partitions = _chunk(all_source_ids, worker_count)
        logging.info("Divided into %d partition(s) for %d worker(s)", len(partitions), worker_count)

        if worker_count == 1:
            _process_partition(
                es, asset_type, source_type, partitions[0], historical_months,
                composite_page_size, source_id_chunk_size,
                current_month=this_month,
            )
        else:
            with ThreadPoolExecutor(max_workers=worker_count) as pool:
                futures = {
                    pool.submit(
                        _process_partition,
                        es, asset_type, source_type, part, historical_months,
                        composite_page_size, source_id_chunk_size,
                        this_month,
                    ): i
                    for i, part in enumerate(partitions)
                }
                for future in as_completed(futures):
                    idx = futures[future]
                    try:
                        future.result()
                    except Exception:
                        logging.exception("Partition %d failed", idx)

        logging.info("Completed snapshot for %s / %s", source_type, asset_type)
