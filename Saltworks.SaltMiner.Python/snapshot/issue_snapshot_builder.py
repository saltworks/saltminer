import logging
import uuid
import datetime
from typing import Any

from elasticsearch import BadRequestError

from .index_names import issue_index_pattern, historical_snapshot_index
from .month_range import (
    month_start, next_month_start, month_end_inclusive, snapshot_date_for_month
)

_SEVERITY_FIELDS = ("Critical", "High", "Medium", "Low", "Info", "Zero")
_FIELD_SOURCE_ID   = "saltminer.asset.source_id"
_FIELD_SOURCE_TYPE = "saltminer.asset.source_type"


def _get(doc, *path: str, default=None):
    cur = doc
    for key in path:
        if not hasattr(cur, 'get'):
            return default
        cur = cur.get(key, default)
        if cur is None:
            return default
    return cur


def fetch_asset_descriptors(es, asset_type: str, source_type: str, source_ids: list[str]) -> dict[str, dict]:
    """Return {source_id: descriptor_source} for each source_id, fetched via one composite agg + top_hits."""
    index = issue_index_pattern(asset_type)
    descriptors: dict[str, dict] = {}
    after_key = None

    while True:
        composite_sources = [
            {"source_id": {"terms": {"field": _FIELD_SOURCE_ID}}},
            {"instance":  {"terms": {"field": "saltminer.asset.instance"}}},
        ]
        composite: dict[str, Any] = {"size": 200, "sources": composite_sources}
        if after_key:
            composite["after"] = after_key

        query: dict[str, Any] = {
            "query": {
                "bool": {
                    "must": [
                        {"term":  {_FIELD_SOURCE_TYPE: source_type}},
                        {"terms": {_FIELD_SOURCE_ID: source_ids}},
                    ]
                }
            },
            "aggs": {
                "assets": {
                    "composite": composite,
                    "aggs": {
                        "descriptor": {
                            "top_hits": {
                                "size": 1,
                                "sort": [{"last_updated": {"order": "desc"}}],
                                "_source": {
                                    "includes": [
                                        "saltminer.asset",
                                        "saltminer.scan",
                                        "saltminer.source",
                                        "saltminer.engagement",
                                        "saltminer.inventory_asset",
                                        "vulnerability.category",
                                        "vulnerability.classification",
                                        "vulnerability.score",
                                        "vulnerability.scanner",
                                    ]
                                },
                            }
                        }
                    },
                }
            },
            "size": 0,
        }

        result = es.Search(index, queryBody=query, size=0, navToData=False)
        buckets = _get(result, "aggregations", "assets", "buckets", default=[])
        for bucket in buckets:
            sid = bucket["key"]["source_id"]
            hit = _get(bucket, "descriptor", "hits", "hits", default=[])
            if hit:
                descriptors[sid] = hit[0]["_source"]

        after_key = _get(result, "aggregations", "assets", "after_key")
        if not after_key or len(buckets) == 0:
            break

    return descriptors


def _build_snapshot_doc(
    bucket_key: dict,
    open_count: int,
    opened_count: int,
    removed_count: int,
    descriptor: dict,
    snap_date: datetime.datetime,
) -> dict:
    severity = bucket_key["severity"]
    asset = _get(descriptor, "saltminer", "asset", default={})
    scan = _get(descriptor, "saltminer", "scan", default={})
    source = _get(descriptor, "saltminer", "source", default={})
    engagement = _get(descriptor, "saltminer", "engagement")
    inv = _get(descriptor, "saltminer", "inventory_asset", default={})
    vul_scanner = _get(descriptor, "vulnerability", "scanner", default={})
    vul_score = _get(descriptor, "vulnerability", "score", default={})

    return {
        "id": str(uuid.uuid4()),
        "timestamp": datetime.datetime.now(tz=datetime.timezone.utc).isoformat(),
        "last_updated": datetime.datetime.now(tz=datetime.timezone.utc).isoformat(),
        "saltminer": {
            "snapshot_date": snap_date.isoformat(),
            "is_historical": True,
            "critical": open_count if severity == "Critical" else 0,
            "high":     open_count if severity == "High"     else 0,
            "medium":   open_count if severity == "Medium"   else 0,
            "low":      open_count if severity == "Low"      else 0,
            "info":     open_count if severity == "Info"     else 0,
            "noscan":   0,
            "opened":   opened_count,
            "removed":  removed_count,
            "total":    open_count,
            "asset": {
                "id":                  asset.get("id"),
                "name":                asset.get("name"),
                "description":         asset.get("description"),
                "version_id":          asset.get("version_id"),
                "version":             asset.get("version"),
                "source_id":           bucket_key["source_id"],
                "instance":            bucket_key["instance"],
                "source_type":         asset.get("source_type"),
                "asset_type":          asset.get("asset_type"),
                "is_saltminer_source": asset.get("is_saltminer_source"),
                "is_retired":          asset.get("is_retired"),
                "is_production":       asset.get("is_production"),
                "last_scan_days_policy": asset.get("last_scan_days_policy"),
                "host":                asset.get("host"),
                "ip":                  asset.get("ip"),
                "scheme":              asset.get("scheme"),
                "port":                asset.get("port"),
                "attributes":          asset.get("attributes", {}),
            },
            "scan": {
                "assessment_type": bucket_key["assess_type"],
                "product":         scan.get("product") or vul_scanner.get("product"),
                "product_type":    scan.get("product_type") or vul_scanner.get("product_type"),
                "vendor":          scan.get("vendor") or vul_scanner.get("vendor"),
            },
            "source": {
                "analyzer":     source.get("analyzer"),
                "confidence":   source.get("confidence"),
                "impact":       source.get("impact"),
                "issue_status": source.get("issue_status"),
                "kingdom":      source.get("kingdom"),
                "likelihood":   source.get("likelihood"),
            },
            "engagement": engagement,
            "inventory_asset": {
                "key": inv.get("key"),
            },
            "vulnerability": {
                "name":           bucket_key["vuln_name"],
                "severity":       severity,
                "source_severity": _get(descriptor, "vulnerability", "source_severity"),
                "category":       _get(descriptor, "vulnerability", "category"),
                "classification": _get(descriptor, "vulnerability", "classification"),
                "scanner": {
                    "assessment_type": bucket_key["assess_type"],
                    "product":         vul_scanner.get("product"),
                    "vendor":          vul_scanner.get("vendor"),
                },
                "score": {
                    "base":          vul_score.get("base"),
                    "environmental": vul_score.get("environmental"),
                    "temporal":      vul_score.get("temporal"),
                    "version":       vul_score.get("version"),
                },
            },
        },
    }


def build_monthly_issue_snapshots(
    es,
    asset_type: str,
    source_type: str,
    source_ids: list[str],
    year: int,
    month: int,
    descriptors: dict[str, dict],
    page_size: int = 1000,
) -> list[dict]:
    """Run the composite aggregation for one month and return snapshot docs."""
    index = issue_index_pattern(asset_type)
    m_start = month_start(year, month)
    m_end = month_end_inclusive(year, month)
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
            {"vuln_name":   {"terms": {"field": "vulnerability.name"}}},
            {"assess_type": {"terms": {"field": "vulnerability.scanner.assessment_type"}}},
            {"severity":    {"terms": {"field": "vulnerability.severity"}}},
        ]
        composite: dict[str, Any] = {"size": page_size, "sources": composite_sources}
        if after_key:
            composite["after"] = after_key

        query: dict[str, Any] = {
            "query": {
                "bool": {
                    "must": [
                        {"term":  {"vulnerability.is_filtered": False}},
                        {"term":  {_FIELD_SOURCE_TYPE: source_type}},
                        {"terms": {_FIELD_SOURCE_ID: source_ids}},
                    ]
                }
            },
            "aggs": {
                "buckets": {
                    "composite": composite,
                    "aggs": {
                        "open_now": {
                            "filter": {
                                "bool": {
                                    "must": [
                                        {"range": {"vulnerability.found_date": {"lte": m_end.isoformat()}}},
                                        {"bool": {"should": [
                                            {"term":  {"vulnerability.is_removed": False}},
                                            {"range": {"vulnerability.removed_date": {"gt": m_end.isoformat()}}},
                                        ]}},
                                    ]
                                }
                            }
                        },
                        "opened_in_month": {
                            "filter": {
                                "range": {"vulnerability.found_date": {
                                    "gte": m_start.isoformat(),
                                    "lt":  m_next.isoformat(),
                                }}
                            }
                        },
                        "removed_in_month": {
                            "filter": {
                                "bool": {
                                    "must": [
                                        {"term":  {"vulnerability.is_removed": True}},
                                        {"range": {"vulnerability.removed_date": {
                                            "gte": m_start.isoformat(),
                                            "lt":  m_next.isoformat(),
                                        }}},
                                    ]
                                }
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
            open_count    = _get(bucket, "open_now",         "doc_count", default=0)
            opened_count  = _get(bucket, "opened_in_month",  "doc_count", default=0)
            removed_count = _get(bucket, "removed_in_month", "doc_count", default=0)

            if open_count == 0 and opened_count == 0 and removed_count == 0:
                continue

            descriptor = descriptors.get(key["source_id"], {})
            docs.append(_build_snapshot_doc(key, open_count, opened_count, removed_count, descriptor, snap_date))

        logging.debug("%d-%02d page %d: %d buckets, %d docs so far", year, month, page, len(buckets), len(docs))
        after_key = _get(result, "aggregations", "buckets", "after_key")
        if not after_key or len(buckets) == 0:
            break

    return docs


def write_monthly_issue_snapshots(
    es,
    asset_type: str,
    source_type: str,
    source_ids: list[str],
    year: int,
    month: int,
    docs: list[dict],
    target_index: str | None = None,
) -> None:
    """Delete this month's docs for the partition from the target index, then bulk-insert new ones.

    target_index: explicit destination; defaults to the _historical index.
    The delete is always scoped to the given month so other months' data is not disturbed.
    """
    if not docs:
        return

    target = target_index or historical_snapshot_index(asset_type, source_type)

    if not es.IndexExists(target):
        try:
            es.MapIndexWithMapping(target, es.GetMapping("AppVersionSnapshots"), False)
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
    logging.info("%d-%02d: wrote %d issue snapshot docs to %s", year, month, len(docs), target)
