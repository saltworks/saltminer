import logging
import uuid
import datetime
from typing import Any

from elasticsearch import BadRequestError, NotFoundError

from .index_names import scan_index_pattern, historical_snapshot_index, historical_scan_snapshot_index
from .month_range import month_start, next_month_start, snapshot_date_for_month

_FIELD_SOURCE_ID  = "saltminer.asset.source_id"
_FIELD_SCAN_DATE  = "saltminer.scan.scan_date"


def _get(doc, *path: str, default=None):
    cur = doc
    for key in path:
        if not hasattr(cur, 'get'):
            return default
        cur = cur.get(key, default)
        if cur is None:
            return default
    return cur


def _fetch_vuln_counts_for_month(
    es,
    snapshot_source_index: str,
    source_id: str,
    year: int,
    month: int,
) -> dict[str, int]:
    """Return {'Critical': n, 'High': n, ...} from the given snapshot index for this asset/month."""
    m_start = month_start(year, month)
    m_next  = next_month_start(year, month)
    query = {
        "query": {
            "bool": {
                "must": [
                    {"term":  {_FIELD_SOURCE_ID: source_id}},
                    {"range": {"saltminer.snapshot_date": {
                        "gte": m_start.isoformat(),
                        "lt":  m_next.isoformat(),
                    }}},
                ]
            }
        },
        "aggs": {
            "by_severity": {
                "terms": {"field": "saltminer.vulnerability.severity", "size": 20}
            }
        },
        "size": 0,
    }
    try:
        result = es.Search(snapshot_source_index, queryBody=query, size=0, navToData=False)
    except NotFoundError:
        return {}
    if not result:
        return {}
    buckets = _get(result, "aggregations", "by_severity", "buckets", default=[])
    return {b["key"]: b["doc_count"] for b in buckets}


def build_monthly_scan_snapshots(
    es,
    asset_type: str,
    source_type: str,
    source_ids: list[str],
    year: int,
    month: int,
    previous_avg_loc: dict[str, float],
    snapshot_source_index: str,
    page_size: int = 1000,
) -> tuple[list[dict], dict[str, float]]:
    """
    Run the scan composite agg for one month.

    snapshot_source_index: the issue snapshot index to read vuln counts from
      (historical_snapshot_index for closed months, current_snapshot_index for live month).
    Returns (docs, updated_previous_avg_loc).
    previous_avg_loc is carried forward for assets with no scans in a given month.
    """
    index = scan_index_pattern(asset_type)
    m_start = month_start(year, month)
    m_next = next_month_start(year, month)
    snap_date = snapshot_date_for_month(year, month)

    docs: list[dict] = []
    after_key = None
    page = 0

    while True:
        page += 1
        composite_sources = [
            {"source_id":   {"terms": {"field": _FIELD_SOURCE_ID}}},
            {"instance":    {"terms": {"field": "saltminer.asset.instance"}}},
            {"assess_type": {"terms": {"field": "saltminer.scan.assessment_type"}}},
        ]
        composite: dict[str, Any] = {"size": page_size, "sources": composite_sources}
        if after_key:
            composite["after"] = after_key

        query: dict[str, Any] = {
            "query": {
                "bool": {
                    "must": [
                        {"range": {_FIELD_SCAN_DATE: {
                            "gte": m_start.isoformat(),
                            "lt":  m_next.isoformat(),
                        }}},
                        {"terms": {_FIELD_SOURCE_ID: source_ids}},
                    ]
                }
            },
            "aggs": {
                "buckets": {
                    "composite": composite,
                    "aggs": {
                        "total_loc":  {"sum":         {"field": "saltminer.scan.lines_of_code"}},
                        "avg_loc":    {"avg":         {"field": "saltminer.scan.lines_of_code"}},
                        "scan_count": {"value_count": {"field": _FIELD_SCAN_DATE}},
                        "descriptor": {
                            "top_hits": {
                                "size": 1,
                                "sort": [{_FIELD_SCAN_DATE: {"order": "desc"}}],
                                "_source": {
                                    "includes": [
                                        "saltminer.asset",
                                        "saltminer.scan.product",
                                        "saltminer.scan.vendor",
                                        "saltminer.scan.product_type",
                                    ]
                                },
                            }
                        },
                    },
                }
            },
            "size": 0,
        }

        result = es.Search(index, queryBody=query, size=0, navToData=False)
        buckets = _get(result, "aggregations", "buckets", "buckets", default=[])

        for bucket in buckets:
            key = bucket["key"]
            sid = key["source_id"]
            scan_count = _get(bucket, "scan_count", "value", default=0)
            total_loc = float(_get(bucket, "total_loc", "value") or 0.0)
            avg_loc_val = float(_get(bucket, "avg_loc", "value") or 0.0)

            hit = _get(bucket, "descriptor", "hits", "hits", default=[])
            asset = _get(hit[0]["_source"] if hit else {}, "saltminer", "asset", default={})
            scan_meta = _get(hit[0]["_source"] if hit else {}, "saltminer", "scan", default={})

            if scan_count > 0:
                previous_avg_loc[sid] = avg_loc_val

            effective_avg_loc = previous_avg_loc.get(sid, 0.0)
            vuln_counts = _fetch_vuln_counts_for_month(es, snapshot_source_index, sid, year, month)
            total_vulns = sum(vuln_counts.values())

            density1k  = (total_vulns / effective_avg_loc) * 1_000    if effective_avg_loc else 0.0
            density10k = (total_vulns / effective_avg_loc) * 10_000   if effective_avg_loc else 0.0
            density100k= (total_vulns / effective_avg_loc) * 100_000  if effective_avg_loc else 0.0

            docs.append({
                "id":           str(uuid.uuid4()),
                "timestamp":    datetime.datetime.now(tz=datetime.timezone.utc).isoformat(),
                "last_updated": datetime.datetime.now(tz=datetime.timezone.utc).isoformat(),
                "saltminer": {
                    "snapshot_date": snap_date.isoformat(),
                    "is_historical": True,
                    "asset": {
                        "id":                  asset.get("id"),
                        "name":                asset.get("name"),
                        "source_id":           sid,
                        "instance":            key["instance"],
                        "source_type":         asset.get("source_type"),
                        "asset_type":          asset.get("asset_type"),
                        "version_id":          asset.get("version_id"),
                        "version":             asset.get("version"),
                        "is_production":       asset.get("is_production"),
                        "is_retired":          asset.get("is_retired"),
                        "is_saltminer_source": asset.get("is_saltminer_source"),
                        "attributes":          asset.get("attributes", {}),
                    },
                    "scan": {
                        "assessment_type": key["assess_type"],
                        "product":         scan_meta.get("product"),
                        "product_type":    scan_meta.get("product_type"),
                        "vendor":          scan_meta.get("vendor"),
                    },
                    "scan_count":   scan_count,
                    "total_loc":    total_loc,
                    "average_loc":  effective_avg_loc,
                    "density1k":    density1k,
                    "density10k":   density10k,
                    "density100k":  density100k,
                    "critical":     vuln_counts.get("Critical", 0),
                    "high":         vuln_counts.get("High", 0),
                    "medium":       vuln_counts.get("Medium", 0),
                    "low":          vuln_counts.get("Low", 0),
                    "info":         vuln_counts.get("Info", 0),
                    "total_vulns":  total_vulns,
                },
            })

        logging.debug("%d-%02d scan page %d: %d buckets", year, month, page, len(buckets))
        after_key = _get(result, "aggregations", "buckets", "after_key")
        if not after_key or len(buckets) == 0:
            break

    return docs, previous_avg_loc


def write_monthly_scan_snapshots(
    es,
    asset_type: str,
    source_type: str,
    source_ids: list[str],
    year: int,
    month: int,
    docs: list[dict],
    target_index: str | None = None,
) -> None:
    if not docs:
        return

    target = target_index or historical_scan_snapshot_index(asset_type, source_type)

    if not es.IndexExists(target):
        try:
            es.MapIndexWithMapping(target, es.GetMapping("AppScanHistory"), False)
        except BadRequestError as e:
            if "resource_already_exists_exception" not in str(e):
                raise

    m_start = month_start(year, month)
    m_next  = next_month_start(year, month)
    delete_query = {
        "query": {
            "bool": {
                "must": [
                    {"terms": {_FIELD_SOURCE_ID: source_ids}},
                    {"range": {"saltminer.snapshot_date": {
                        "gte": m_start.isoformat(),
                        "lt":  m_next.isoformat(),
                    }}},
                ]
            }
        }
    }
    es.DeleteByQuery(target, delete_query, flushAfter=True, wait=True, ignoreMissingIndex=True)

    bulk_actions = [
        {"_index": target, "_id": doc["id"], "_source": doc}
        for doc in docs
    ]
    es.BulkInsert(bulk_actions)
    logging.info("%d-%02d: wrote %d scan snapshot docs to %s", year, month, len(docs), target)
