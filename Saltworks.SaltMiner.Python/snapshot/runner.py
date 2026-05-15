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
from .month_range import earliest_data_date, iter_months, month_start, next_month_start
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
    source_type_arg: str,
    start_date: datetime.datetime | None = None,
    worker_count: int = 4,
    composite_page_size: int = 1000,
    source_id_chunk_size: int = 1000,
    mode: str = "all",
) -> None:
    """
    Entry point for snapshot generation.

    mode values:
      "all"        — rebuild historical monthly indices AND refresh the _current index (default)
      "current"    — refresh only the _current index for the live month (fast, runs daily)
      "historical" — rebuild historical monthly indices only, skip _current

    app_settings: ApplicationSettings instance from Application.Settings
    source_type_arg: 'all' or a source type name like 'FOD' / 'Saltworks.FOD'
    start_date: override earliest start for historical months; ignored in "current" mode
    """
    if mode not in ("all", "current", "historical", "daily"):
        raise ValueError(f"Invalid mode '{mode}'. Expected one of: all, current, historical, daily")

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

    now = datetime.datetime.now(tz=datetime.timezone.utc)
    this_month = (now.year, now.month)

    for source_type, asset_type in selected_pairs:
        logging.info("Starting snapshot [mode=%s]: source_type=%s, asset_type=%s",
                     mode, source_type, asset_type)

        # Determine which months go to historical per-month indices
        if mode == "current":
            historical_months: list[tuple[int, int]] = []

        elif mode == "daily":
            # Refresh the _current index every run, and rebuild the last closed month
            # only if the _historical index has no docs for it (handles month roll-over).
            last_closed = (now.year, now.month - 1) if now.month > 1 else (now.year - 1, 12)
            hist_idx = historical_snapshot_index(asset_type, source_type)
            lc_start = month_start(*last_closed)
            lc_next  = next_month_start(*last_closed)
            has_last_month = False
            if es.IndexExists(hist_idx):
                count_result = es.Search(hist_idx, queryBody={
                    "query": {"range": {"saltminer.snapshot_date": {
                        "gte": lc_start.isoformat(),
                        "lt":  lc_next.isoformat(),
                    }}},
                    "size": 0,
                }, size=0, navToData=False)
                has_last_month = bool(
                    count_result and
                    count_result.get("hits", {}).get("total", {}).get("value", 0) > 0
                )
            if has_last_month:
                logging.info(
                    "Daily: %d-%02d data present in %s — skipping historical rebuild.",
                    *last_closed, hist_idx,
                )
                historical_months = []
            else:
                logging.info(
                    "Daily: %d-%02d data missing from %s — will build.",
                    *last_closed, hist_idx,
                )
                historical_months = [last_closed]

        else:
            if start_date:
                effective_start = start_date.replace(
                    day=1, hour=0, minute=0, second=0, microsecond=0,
                    tzinfo=datetime.timezone.utc,
                )
            else:
                detected = earliest_data_date(es, issue_index_pattern(asset_type), source_type)
                if not detected:
                    logging.warning("No issue data found for %s / %s — skipping.", source_type, asset_type)
                    continue
                effective_start = detected

            # Historical = all closed months (exclude the current live month)
            all_months = list(iter_months(effective_start, now))
            historical_months = [m for m in all_months if m != this_month]
            logging.info(
                "Historical: %d months from %s to last closed month",
                len(historical_months), effective_start.date(),
            )

        # Determine whether to refresh the _current index
        current_month = this_month if mode in ("all", "current", "daily") else None

        if not historical_months and current_month is None:
            logging.warning("Nothing to process for %s / %s in mode '%s'.", source_type, asset_type, mode)
            continue

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
                current_month=current_month,
            )
        else:
            with ThreadPoolExecutor(max_workers=worker_count) as pool:
                futures = {
                    pool.submit(
                        _process_partition,
                        es, asset_type, source_type, part, historical_months,
                        composite_page_size, source_id_chunk_size,
                        current_month,
                    ): i
                    for i, part in enumerate(partitions)
                }
                for future in as_completed(futures):
                    idx = futures[future]
                    try:
                        future.result()
                    except Exception:
                        logging.exception("Partition %d failed", idx)

        logging.info("Completed snapshot [mode=%s] for %s / %s", mode, source_type, asset_type)
