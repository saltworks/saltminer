import datetime
import logging
import math
from concurrent.futures import ThreadPoolExecutor, as_completed
from typing import Any

from .index_names import issue_index_pattern
from .month_range import earliest_data_date, iter_months
from .issue_snapshot_builder import (
    fetch_asset_descriptors,
    build_monthly_issue_snapshots,
    write_monthly_issue_snapshots,
)
from .scan_snapshot_builder import (
    build_monthly_scan_snapshots,
    write_monthly_scan_snapshots,
)


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
                        {"source_type": {"terms": {"field": "saltminer.asset.source_type"}}},
                        {"asset_type":  {"terms": {"field": "saltminer.asset.asset_type"}}},
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


def _collect_all_source_ids(es, asset_type: str, source_type: str, page_size: int = 1000) -> list[str]:
    index = issue_index_pattern(asset_type)
    ids: list[str] = []
    after_key = None
    while True:
        composite: dict[str, Any] = {
            "size": page_size,
            "sources": [{"source_id": {"terms": {"field": "saltminer.asset.source_id"}}}],
        }
        if after_key:
            composite["after"] = after_key
        query: dict[str, Any] = {
            "query": {"term": {"saltminer.asset.source_type": source_type}},
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
) -> None:
    """Process all months for one partition of source IDs."""
    logging.info("Partition of %d source IDs starting (%s / %s)", len(partition_ids), source_type, asset_type)

    # Build per-asset descriptors once for the whole partition
    descriptors = fetch_asset_descriptors(es, asset_type, source_type, partition_ids)
    logging.debug("Fetched %d asset descriptors for partition", len(descriptors))

    # Carry forward average LOC per source_id across months for the scan snapshot
    prev_avg_loc: dict[str, float] = {}

    # For large partitions, chunk the terms filter to avoid too_many_clauses
    id_chunks = _chunk(partition_ids, max(1, math.ceil(len(partition_ids) / source_id_chunk_size)))

    for year, month in months:
        # Build and write issue snapshots first — scan builder reads from them
        issue_docs: list[dict] = []
        for chunk in id_chunks:
            issue_docs.extend(build_monthly_issue_snapshots(
                es, asset_type, source_type, chunk, year, month, descriptors, composite_page_size
            ))
        write_monthly_issue_snapshots(es, asset_type, source_type, partition_ids, year, month, issue_docs)

        # Now build and write scan snapshots (queries the just-written issue snapshot index)
        scan_docs: list[dict] = []
        for chunk in id_chunks:
            month_scan_docs, prev_avg_loc = build_monthly_scan_snapshots(
                es, asset_type, source_type, chunk, year, month, prev_avg_loc, composite_page_size
            )
            scan_docs.extend(month_scan_docs)
        write_monthly_scan_snapshots(es, asset_type, source_type, partition_ids, year, month, scan_docs)

    logging.info("Partition complete (%d source IDs, %d months)", len(partition_ids), len(months))


def run_snapshot_history(
    app_settings,
    source_type_arg: str,
    start_date: datetime.datetime | None = None,
    worker_count: int = 4,
    composite_page_size: int = 1000,
    source_id_chunk_size: int = 1000,
) -> None:
    """
    Entry point for full historical snapshot generation.

    app_settings: ApplicationSettings instance from Application.Settings
    source_type_arg: 'all' or a source type name like 'FOD' / 'Saltworks.FOD'
    start_date: override earliest start; if None the earliest data date is used
    """
    es = app_settings.Application.GetElasticClient()

    all_pairs = discover_source_type_asset_type_pairs(es)
    if not all_pairs:
        logging.warning("No source type / asset type pairs found in issues_* indices. Nothing to do.")
        return

    available_source_types = list({st for st, _ in all_pairs})

    if source_type_arg.lower() == "all":
        selected_pairs = all_pairs
    else:
        resolved = _normalize_source_type(source_type_arg, available_source_types)
        if not resolved:
            logging.error(
                "Source type '%s' not found in data. Available: %s",
                source_type_arg, available_source_types,
            )
            return
        selected_pairs = [(st, at) for st, at in all_pairs if st == resolved]

    for source_type, asset_type in selected_pairs:
        logging.info("Starting snapshot history: source_type=%s, asset_type=%s", source_type, asset_type)

        # Determine start month
        if start_date:
            effective_start = start_date.replace(day=1, hour=0, minute=0, second=0, microsecond=0,
                                                  tzinfo=datetime.timezone.utc)
        else:
            detected = earliest_data_date(es, issue_index_pattern(asset_type), source_type)
            if not detected:
                logging.warning("No issue data found for %s / %s — skipping.", source_type, asset_type)
                continue
            effective_start = detected

        now = datetime.datetime.now(tz=datetime.timezone.utc)
        months = list(iter_months(effective_start, now))
        logging.info("Processing %d months from %s to %s", len(months), effective_start.date(), now.date())

        all_source_ids = _collect_all_source_ids(es, asset_type, source_type)
        if not all_source_ids:
            logging.warning("No source IDs found for %s / %s — skipping.", source_type, asset_type)
            continue
        logging.info("Found %d distinct source IDs to process", len(all_source_ids))

        partitions = _chunk(all_source_ids, worker_count)
        logging.info("Divided into %d partition(s) for %d worker(s)", len(partitions), worker_count)

        if worker_count == 1:
            _process_partition(
                es, asset_type, source_type, partitions[0], months,
                composite_page_size, source_id_chunk_size,
            )
        else:
            with ThreadPoolExecutor(max_workers=worker_count) as pool:
                futures = {
                    pool.submit(
                        _process_partition,
                        es, asset_type, source_type, part, months,
                        composite_page_size, source_id_chunk_size,
                    ): i
                    for i, part in enumerate(partitions)
                }
                for future in as_completed(futures):
                    idx = futures[future]
                    try:
                        future.result()
                    except Exception:
                        logging.exception("Partition %d failed", idx)

        logging.info("Completed snapshot history for %s / %s", source_type, asset_type)
