# Source-Agnostic Snapshot Generation Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace SSC-specific monthly snapshot generation with a source-agnostic pipeline that uses Elasticsearch aggregations, supports parallel workers, and writes per-`(asset_type, source_type)` `_current` / `_historical` indices.

**Architecture:** New `Snapshots/` package + `RunUtilSnapshotHistory.py` runner. Pre-implementation probe script verifies every ES query against opov before pipeline code is written. TDD on pure transforms/query builders; integration test against opov gated by env flag. Legacy SSC helpers retired in final cleanup commit.

**Tech Stack:** Python 3, `unittest`, `concurrent.futures.ThreadPoolExecutor`, existing `Core.ElasticClient` (methods: `Search`, `SearchScroll`, `Count`, `BulkInsert`, `DeleteByQuery`, `GetMapping`, `MapIndexWithMapping`, `PutSettings`, `DeleteIndex`, `CloneIndex`).

**Reference design doc:** [docs/plans/2026-05-13-source-agnostic-snapshots-design.md](2026-05-13-source-agnostic-snapshots-design.md)

**Run all Python tests/scripts with:**
```
cd C:/Source/saltminer/Saltworks.SaltMiner.Python
SALTMINER_2_CONFIG_PATH="C:/Source/saltminer-internal/config/python" python -m unittest <module> -v
```
(See `ai/python-debug.md`.)

**Probe / integration ES connection:** opov, see `ai/scratch/opov.saltminer.io.md` (host `https://opov.saltminer.io:9200`, user `elastic`, password in that file). Do NOT commit credentials.

---

## Phase 0 — Pre-implementation Probe (verify ES queries on opov)

The probe script and its results doc are TEMPORARY. They are deleted in the final cleanup task. Every aggregation and filter used in the pipeline must first be exercised here.

### Task 0.1: Create probe script skeleton

**Files:**
- Create: `Saltworks.SaltMiner.Python/Scratch/__init__.py` (empty)
- Create: `Saltworks.SaltMiner.Python/Scratch/snapshot_query_probe.py`
- Create: `Saltworks.SaltMiner.Python/Scratch/snapshot_query_probe_results.md`

**Step 1: Write the probe skeleton**

```python
# TODO: TEMPORARY DEBUGGING - Remove after testing
'''
Probe script — verifies every ES query the snapshot pipeline will use, against opov.
Connection: ai/scratch/opov.saltminer.io.md (do NOT commit credentials).
Run: SALTMINER_2_CONFIG_PATH=... python -m Scratch.snapshot_query_probe
'''
import json
import logging
from Core.Application import Application

logging.basicConfig(level=logging.INFO)

def main():
    app = Application()
    es = app.GetElasticClient()
    print("[probe] connected; cluster info:")
    print(json.dumps(es.GetClusterHealth() if hasattr(es, "GetClusterHealth") else {"note": "no health method"}, indent=2, default=str))

if __name__ == "__main__":
    main()
```

**Step 2: Run the skeleton to confirm connection**

```
cd C:/Source/saltminer/Saltworks.SaltMiner.Python
SALTMINER_2_CONFIG_PATH="C:/Source/saltminer-internal/config/python" python -m Scratch.snapshot_query_probe
```

Expected: prints "connected" with no exceptions. If the config points elsewhere, ask the user to point `SALTMINER_2_CONFIG_PATH` at an opov-pointing config.

**Step 3: Commit**

```
git add Saltworks.SaltMiner.Python/Scratch/__init__.py Saltworks.SaltMiner.Python/Scratch/snapshot_query_probe.py Saltworks.SaltMiner.Python/Scratch/snapshot_query_probe_results.md
git commit -m "chore(snapshots): probe script skeleton"
```

---

### Task 0.2: Probe — combo discovery

**Files:**
- Modify: `Saltworks.SaltMiner.Python/Scratch/snapshot_query_probe.py`

**Step 1: Add the combo-discovery function**

```python
def probe_combos(es):
    print("\n[probe 2] combo discovery (asset_type x source_type) on issues*")
    query = {
      "size": 0,
      "aggs": {
        "combos": {
          "composite": {
            "size": 100,
            "sources": [
              {"asset_type":   {"terms": {"field": "saltminer.asset.asset_type"}}},
              {"source_type":  {"terms": {"field": "saltminer.asset.source_type"}}}
            ]
          }
        }
      }
    }
    r = es.Search("issues*", queryBody=query, size=0, navToData=False)
    buckets = r.get("aggregations", {}).get("combos", {}).get("buckets", [])
    for b in buckets:
        print(f"  {b['key']['asset_type']:<10} {b['key']['source_type']:<30} docs={b['doc_count']}")
    print(f"  total combos: {len(buckets)}")
    return [b['key'] for b in buckets]
```

Call it from `main()`:
```python
combos = probe_combos(es)
```

**Step 2: Run**

Same command as Task 0.1. Expected: a small list of combos with non-zero doc counts.

**Step 3: Record results**

Append to `snapshot_query_probe_results.md` under a "Probe 2: combo discovery" section: paste output verbatim, note total combos and any surprises.

**Step 4: Commit**

```
git add Saltworks.SaltMiner.Python/Scratch/snapshot_query_probe.py Saltworks.SaltMiner.Python/Scratch/snapshot_query_probe_results.md
git commit -m "chore(snapshots): probe combo discovery"
```

---

### Task 0.3: Probe — source_id discovery for one combo (current-month filter)

**Files:**
- Modify: `Saltworks.SaltMiner.Python/Scratch/snapshot_query_probe.py`

**Step 1: Add function**

```python
def probe_source_ids_current(es, asset_type, source_type):
    print(f"\n[probe 3] source_id discovery — current filter — {asset_type} / {source_type}")
    query = {
      "size": 0,
      "query": {
        "bool": {
          "must": [
            {"term": {"saltminer.asset.asset_type":  asset_type}},
            {"term": {"saltminer.asset.source_type": source_type}},
            {"term": {"vulnerability.is_active":     True}}
          ]
        }
      },
      "aggs": {
        "sids": {
          "composite": {
            "size": 1000,
            "sources": [{"sid": {"terms": {"field": "saltminer.asset.source_id"}}}]
          }
        }
      }
    }
    r = es.Search("issues*", queryBody=query, size=0, navToData=False)
    buckets = r.get("aggregations", {}).get("sids", {}).get("buckets", [])
    print(f"  source_id buckets (first page): {len(buckets)}")
    for b in buckets[:5]:
        print(f"    {b['key']['sid']} docs={b['doc_count']}")
    return [b['key']['sid'] for b in buckets]
```

Call it from `main()` using the first combo returned by `probe_combos`.

**Step 2: Run & record**

Note: does the response have `after_key`? If yes, log it — pagination will be required in production code.

**Step 3: Commit**

```
git commit -am "chore(snapshots): probe source_id discovery (current)"
```

---

### Task 0.4: Probe — per-source_id composite aggregation (vuln.name × severity)

**Files:**
- Modify: `Saltworks.SaltMiner.Python/Scratch/snapshot_query_probe.py`

**Step 1: Add function**

```python
def probe_vuln_name_agg(es, asset_type, source_type, source_id):
    print(f"\n[probe 4] per-source_id agg — {source_id}")
    query = {
      "size": 0,
      "query": {
        "bool": {
          "must": [
            {"term": {"saltminer.asset.asset_type":  asset_type}},
            {"term": {"saltminer.asset.source_type": source_type}},
            {"term": {"saltminer.asset.source_id":   source_id}},
            {"term": {"vulnerability.is_active":     True}}
          ]
        }
      },
      "aggs": {
        "names": {
          "composite": {
            "size": 500,
            "sources": [{"name": {"terms": {"field": "vulnerability.name"}}}]
          },
          "aggs": {
            "sev":            {"terms": {"field": "vulnerability.severity", "size": 10}},
            "classification": {"min":   {"field": "vulnerability.classification"}},
            "category":       {"min":   {"field": "vulnerability.category"}}
          }
        }
      }
    }
    r = es.Search("issues*", queryBody=query, size=0, navToData=False)
    buckets = r.get("aggregations", {}).get("names", {}).get("buckets", [])
    print(f"  vuln.name buckets: {len(buckets)}")
    for b in buckets[:3]:
        sev_buckets = b['sev']['buckets']
        print(f"    name={b['key']['name']} total={b['doc_count']} sev={[(x['key'], x['doc_count']) for x in sev_buckets]}")
    return buckets
```

**Important:** `min` on a `keyword` field is not allowed in ES; classification/category are `keyword` types per the issue template. If the query fails, fall back to `terms { size: 1, order: { _key: "asc" } }` and document the change in the results doc. The pipeline will use whichever the probe confirms works.

**Step 2: Run & record**

Document in results: actual aggregation type used for classification/category. Capture cardinality (max buckets seen).

**Step 3: Commit**

```
git commit -am "chore(snapshots): probe per-source_id vuln.name agg"
```

---

### Task 0.5: Probe — passthrough query (one issue per source_id)

**Files:**
- Modify: `Saltworks.SaltMiner.Python/Scratch/snapshot_query_probe.py`

**Step 1: Add function**

```python
def probe_passthrough(es, asset_type, source_type, source_id):
    print(f"\n[probe 5] passthrough query — {source_id}")
    query = {
      "size": 1,
      "_source": [
        "saltminer.asset",
        "saltminer.engagement",
        "saltminer.inventory_asset"
      ],
      "query": {
        "bool": {
          "must": [
            {"term": {"saltminer.asset.asset_type":  asset_type}},
            {"term": {"saltminer.asset.source_type": source_type}},
            {"term": {"saltminer.asset.source_id":   source_id}}
          ]
        }
      }
    }
    r = es.Search("issues*", queryBody=query, size=1, navToData=False)
    hits = r.get("hits", {}).get("hits", [])
    if not hits:
        print("  NO HITS — investigate")
        return None
    print(json.dumps(hits[0]["_source"], indent=2, default=str)[:1500])
    return hits[0]["_source"]
```

**Step 2: Run & record**

Document fields actually present vs the design doc's expected passthrough fields. Note any missing.

**Step 3: Commit**

```
git commit -am "chore(snapshots): probe passthrough query"
```

---

### Task 0.6: Probe — historical-month filter

**Files:**
- Modify: `Saltworks.SaltMiner.Python/Scratch/snapshot_query_probe.py`

**Step 1: Add function**

```python
def probe_historical_filter(es, asset_type, source_type, source_id, month_end_iso):
    print(f"\n[probe 6] historical filter — {source_id} @ {month_end_iso}")
    query = {
      "size": 0,
      "query": {
        "bool": {
          "must": [
            {"term":  {"saltminer.asset.asset_type":  asset_type}},
            {"term":  {"saltminer.asset.source_type": source_type}},
            {"term":  {"saltminer.asset.source_id":   source_id}},
            {"range": {"vulnerability.found_date":    {"lte": month_end_iso}}},
            {"term":  {"vulnerability.is_filtered":   False}},
            {"term":  {"vulnerability.is_suppressed": False}}
          ],
          "must_not": [
            {"bool": {
              "must": [
                {"exists": {"field": "vulnerability.removed_date"}},
                {"range":  {"vulnerability.removed_date": {"lte": month_end_iso}}}
              ]
            }}
          ]
        }
      }
    }
    count = es.Count("issues*", queryBody=query)
    print(f"  historical-filter count: {count}")
    return count
```

Call with a recent closed month-end, e.g. last month's last instant.

**Step 2: Run & record**

Capture count. Compare to a separate `vulnerability.is_active=true` count for the same source_id — note the delta as the "drifted into inactive but should still count historically" set.

**Step 3: Commit**

```
git commit -am "chore(snapshots): probe historical filter"
```

---

### Task 0.7: Probe — ground-truth scroll cross-check

**Files:**
- Modify: `Saltworks.SaltMiner.Python/Scratch/snapshot_query_probe.py`

**Step 1: Add function**

For one chosen source_id, scroll `issues*` for the current-month filter, build counts grouped by `(vuln.name, severity)` in Python, and compare to the aggregation result from probe 4. They must agree exactly.

```python
def probe_ground_truth(es, asset_type, source_type, source_id):
    print(f"\n[probe 7] ground-truth scroll vs agg — {source_id}")
    query = {
      "_source": ["vulnerability.name", "vulnerability.severity"],
      "query": {
        "bool": {
          "must": [
            {"term": {"saltminer.asset.asset_type":  asset_type}},
            {"term": {"saltminer.asset.source_type": source_type}},
            {"term": {"saltminer.asset.source_id":   source_id}},
            {"term": {"vulnerability.is_active":     True}}
          ]
        }
      }
    }
    counts = {}
    scroller = es.SearchScroll("issues*", query, 1000, None)
    while len(scroller.Results):
        for dto in scroller.Results:
            v = dto['_source']['vulnerability']
            key = (v.get('name'), v.get('severity'))
            counts[key] = counts.get(key, 0) + 1
        scroller.GetNext()
    print(f"  scroll distinct (name,sev) pairs: {len(counts)}")
    return counts
```

In `main()`, run both probe 4 and probe 7 on the same source_id; print a diff of bucket counts. If any mismatch, halt and investigate before proceeding.

**Step 2: Record results**

Final section of the results doc: confirm parity. List any data-shape surprises (missing severity, null vuln.name, etc.) that the transform code must handle.

**Step 3: Commit**

```
git commit -am "chore(snapshots): probe ground-truth cross-check"
```

---

## Phase 1 — Pure Functions (TDD)

These have no ES dependency.

### Task 1.1: SnapshotTransforms — bucket → document

**Files:**
- Create: `Saltworks.SaltMiner.Python/Snapshots/__init__.py` (empty)
- Create: `Saltworks.SaltMiner.Python/Snapshots/SnapshotTransforms.py`
- Create: `Saltworks.SaltMiner.Python/UnitTests/SnapshotTransformsTests.py`

**Step 1: Write the failing test**

```python
# UnitTests/SnapshotTransformsTests.py
import unittest
from Snapshots.SnapshotTransforms import bucket_to_doc

class BucketToDocTests(unittest.TestCase):
    def test_severity_buckets_populated(self):
        bucket = {
            "key": {"name": "SQL Injection"},
            "doc_count": 7,
            "sev": {"buckets": [
                {"key": "Critical", "doc_count": 3},
                {"key": "High",     "doc_count": 4}
            ]},
            "classification": {"value_as_string": "A03"},
            "category":       {"value_as_string": "Injection"}
        }
        passthrough = {"saltminer": {"asset": {"source_id": "abc"}}}
        doc = bucket_to_doc(bucket, passthrough, snapshot_date="2026-04-30T23:59:59Z")
        self.assertEqual(doc["critical"], 3)
        self.assertEqual(doc["high"], 4)
        self.assertEqual(doc["medium"], 0)
        self.assertEqual(doc["low"], 0)
        self.assertEqual(doc["info"], 0)
        self.assertEqual(doc["total"], 7)
        self.assertEqual(doc["vulnerability"]["name"], "SQL Injection")
        self.assertEqual(doc["vulnerability"]["classification"], "A03")
        self.assertEqual(doc["snapshot_date"], "2026-04-30T23:59:59Z")
        self.assertEqual(doc["saltminer"]["asset"]["source_id"], "abc")

if __name__ == "__main__":
    unittest.main()
```

**Step 2: Run test (expect failure — module missing)**

```
cd C:/Source/saltminer/Saltworks.SaltMiner.Python
SALTMINER_2_CONFIG_PATH="C:/Source/saltminer-internal/config/python" python -m unittest UnitTests.SnapshotTransformsTests -v
```
Expected: ImportError.

**Step 3: Implement minimal `bucket_to_doc`**

```python
# Snapshots/SnapshotTransforms.py
SEVERITY_BUCKETS = ("Critical", "High", "Medium", "Low", "Info")

def bucket_to_doc(name_bucket, passthrough, snapshot_date):
    sev_counts = {b["key"]: b["doc_count"] for b in name_bucket.get("sev", {}).get("buckets", [])}
    doc = {
        "snapshot_date": snapshot_date,
        "vulnerability": {
            "name":           name_bucket["key"]["name"],
            "classification": _extract_value(name_bucket.get("classification")),
            "category":       _extract_value(name_bucket.get("category")),
        },
        "critical": sev_counts.get("Critical", 0),
        "high":     sev_counts.get("High",     0),
        "medium":   sev_counts.get("Medium",   0),
        "low":      sev_counts.get("Low",      0),
        "info":     sev_counts.get("Info",     0),
    }
    doc["total"] = doc["critical"] + doc["high"] + doc["medium"] + doc["low"] + doc["info"]
    # Merge passthrough (deep-copy semantics not required — caller owns the source)
    if passthrough:
        for k, v in passthrough.items():
            doc[k] = v
    return doc

def _extract_value(agg):
    if not agg:
        return None
    if "value_as_string" in agg:
        return agg["value_as_string"]
    if "buckets" in agg and agg["buckets"]:
        return agg["buckets"][0]["key"]
    return agg.get("value")
```

**Step 4: Run tests (expect pass)**

Same command as Step 2. Expected: 1 test passed.

**Step 5: Commit**

```
git add Saltworks.SaltMiner.Python/Snapshots/__init__.py Saltworks.SaltMiner.Python/Snapshots/SnapshotTransforms.py Saltworks.SaltMiner.Python/UnitTests/SnapshotTransformsTests.py
git commit -m "feat(snapshots): bucket_to_doc transform with severity buckets"
```

---

### Task 1.2: SnapshotTransforms — edge cases (missing severity, null name)

**Files:**
- Modify: `Saltworks.SaltMiner.Python/UnitTests/SnapshotTransformsTests.py`
- Modify: `Saltworks.SaltMiner.Python/Snapshots/SnapshotTransforms.py` (only if tests fail)

**Step 1: Add tests**

- `test_missing_severity_buckets_are_zero` — empty `sev.buckets` → all zero counts, total=0.
- `test_unknown_severity_value_is_ignored` — bucket with `"key": "Unknown"` → not summed.
- `test_null_classification_passes_through_as_none` — no `classification` key → doc has `classification: None`.

**Step 2: Run; if fail, adjust `bucket_to_doc`. Step 3: Commit.**

```
git commit -am "test(snapshots): bucket_to_doc edge cases"
```

---

### Task 1.3: SnapshotQueries — query builders (TDD)

**Files:**
- Create: `Saltworks.SaltMiner.Python/Snapshots/SnapshotQueries.py`
- Create: `Saltworks.SaltMiner.Python/UnitTests/SnapshotQueriesTests.py`

**Step 1: Tests**

```python
import unittest
from Snapshots.SnapshotQueries import (
    build_current_filter, build_historical_filter,
    build_source_id_discovery_query, build_vuln_name_agg_query,
    build_passthrough_query, build_combo_discovery_query
)

class CurrentFilterTests(unittest.TestCase):
    def test_only_is_active(self):
        f = build_current_filter("APP", "saltworks.ssc")
        self.assertIn({"term": {"vulnerability.is_active": True}}, f["bool"]["must"])
        self.assertIn({"term": {"saltminer.asset.asset_type":  "APP"}}, f["bool"]["must"])

class HistoricalFilterTests(unittest.TestCase):
    def test_excludes_removed_before_T(self):
        f = build_historical_filter("APP", "saltworks.ssc", "2026-04-30T23:59:59Z")
        self.assertIn({"term":  {"vulnerability.is_filtered":   False}}, f["bool"]["must"])
        self.assertIn({"term":  {"vulnerability.is_suppressed": False}}, f["bool"]["must"])
        # must_not block for removed_date <= T
        mn = f["bool"]["must_not"]
        self.assertTrue(any("must" in x.get("bool", {}) for x in mn))
```

(Add similar focused tests for the other builders. Aggregations should be dict-shape-asserted only — no ES round-trip.)

**Step 2: Run (expect failures)**

**Step 3: Implement builders in `SnapshotQueries.py`** — copy the query shapes verified by the probes in Phase 0, with the `must_not` block from probe 0.6 for the historical filter.

**Step 4: Run tests (expect pass)**

**Step 5: Commit**

```
git commit -m "feat(snapshots): query builders"
```

---

## Phase 2 — SnapshotHelper Core

### Task 2.1: SnapshotHelper — class skeleton + index name builder

**Files:**
- Create: `Saltworks.SaltMiner.Python/Snapshots/SnapshotHelper.py`
- Create: `Saltworks.SaltMiner.Python/UnitTests/SnapshotHelperTests.py`

**Step 1: Test the index-name function**

```python
from Snapshots.SnapshotHelper import build_index_name

class IndexNameTests(unittest.TestCase):
    def test_strips_saltworks_prefix(self):
        self.assertEqual(build_index_name("APP", "saltworks.ssc", "historical"),
                         "snapshots_app_ssc_historical")
        self.assertEqual(build_index_name("APP", "saltworks.ssc", "current"),
                         "snapshots_app_ssc_current")
    def test_no_prefix_unchanged(self):
        self.assertEqual(build_index_name("HOST", "tenable", "current"),
                         "snapshots_host_tenable_current")
```

**Step 2: Implement**

```python
SOURCE_PREFIX = "saltworks."

def build_index_name(asset_type, source_type, kind):
    assert kind in ("current", "historical")
    src = source_type[len(SOURCE_PREFIX):] if source_type.startswith(SOURCE_PREFIX) else source_type
    return f"snapshots_{asset_type.lower()}_{src.lower()}_{kind}"
```

**Step 3: Run, pass, commit**

```
git commit -m "feat(snapshots): index name builder"
```

---

### Task 2.2: SnapshotHelper — composite aggregation pagination helper

**Files:**
- Modify: `Saltworks.SaltMiner.Python/Snapshots/SnapshotHelper.py`
- Modify: `Saltworks.SaltMiner.Python/UnitTests/SnapshotHelperTests.py`

**Step 1: Test using a fake ES client**

```python
class CompositePaginationTests(unittest.TestCase):
    def test_walks_pages_until_no_after_key(self):
        # fake es returning two pages then empty
        pages = [
            {"aggregations": {"a": {"buckets": [{"key": {"x": 1}, "doc_count": 5}], "after_key": {"x": 1}}}},
            {"aggregations": {"a": {"buckets": [{"key": {"x": 2}, "doc_count": 3}]}}}  # no after_key
        ]
        class FakeEs:
            def __init__(self): self.calls = []
            def Search(self, index, queryBody, size, navToData=False, **kw):
                self.calls.append(queryBody)
                return pages.pop(0)
        from Snapshots.SnapshotHelper import iter_composite_buckets
        es = FakeEs()
        base_query = {"size":0,"aggs":{"a":{"composite":{"size":10,"sources":[{"x":{"terms":{"field":"x"}}}]}}}}
        out = list(iter_composite_buckets(es, "issues*", base_query, "a"))
        self.assertEqual([b["key"]["x"] for b in out], [1, 2])
        self.assertEqual(len(es.calls), 2)
        self.assertEqual(es.calls[1]["aggs"]["a"]["composite"]["after"], {"x": 1})
```

**Step 2: Implement `iter_composite_buckets`** — generator that copies the query, sets `aggs.<name>.composite.after = after_key` between pages, stops when `after_key` is absent.

**Step 3: Pass + commit**

```
git commit -m "feat(snapshots): composite agg pagination helper"
```

---

### Task 2.3: SnapshotHelper — `process_source_id`

Processes one source_id for one month: passthrough query → vuln.name agg → bucket-to-doc → bulk batch.

**Files:**
- Modify: `Saltworks.SaltMiner.Python/Snapshots/SnapshotHelper.py`

**Step 1: Test with a FakeEs that records bulk-insert calls**

Synthesize: 1 passthrough hit, 2 vuln.name buckets. Assert 2 docs handed to the bulk-writer with correct shapes and the target index name.

**Step 2: Implement**

```python
class SnapshotHelper:
    def __init__(self, es, batch_size=5000):
        self._es = es
        self._batch_size = batch_size

    def process_source_id(self, asset_type, source_type, source_id,
                          target_index, snapshot_date, agg_query, passthrough_query):
        # 1) passthrough
        pt = self._es.Search("issues*", queryBody=passthrough_query, size=1, navToData=False)
        hits = pt.get("hits", {}).get("hits", [])
        passthrough = hits[0]["_source"] if hits else {}
        # 2) walk vuln.name buckets
        batch = []
        for bkt in iter_composite_buckets(self._es, "issues*", agg_query, "names"):
            doc = bucket_to_doc(bkt, passthrough, snapshot_date)
            batch.append({"_index": target_index, "_id": str(uuid.uuid4()), "_source": doc})
            if len(batch) >= self._batch_size:
                self._es.BulkInsert(batch)
                batch = []
        if batch:
            self._es.BulkInsert(batch)
```

**Step 3: Pass + commit**

```
git commit -m "feat(snapshots): process_source_id"
```

---

### Task 2.4: SnapshotHelper — month driver (current vs historical)

**Files:**
- Modify: `Saltworks.SaltMiner.Python/Snapshots/SnapshotHelper.py`

**Step 1: Tests**

- `test_run_month_current_uses_temp_index_and_clones` — fake ES records `MapIndexWithMapping`, `BulkInsert`, `PutSettings(write=True)`, `DeleteIndex`, `CloneIndex`, `PutSettings(write=False)` in that order.
- `test_run_month_historical_uses_delete_by_query_first` — fake ES records `DeleteByQuery` on the target index with `snapshot_date == T` filter, then `BulkInsert`s.

**Step 2: Implement**

```python
def run_month(self, asset_type, source_type, kind, snapshot_date_iso, snapshot_mapping_name, workers=1):
    target = build_index_name(asset_type, source_type, kind)
    if kind == "current":
        temp = f"{target}_tmp"
        self._es.MapIndexWithMapping(temp, self._es.GetMapping(snapshot_mapping_name), True)
        work_target = temp
    else:
        # idempotent re-run of an existing historical month
        self._es.DeleteByQuery(target, {"query": {"term": {"snapshot_date": snapshot_date_iso}}}, ignoreMissingIndex=True)
        work_target = target

    source_ids = self._discover_source_ids(asset_type, source_type, kind, snapshot_date_iso)
    filter_fn  = build_current_filter if kind == "current" else lambda at, st: build_historical_filter(at, st, snapshot_date_iso)

    def task(sid):
        agg_q = build_vuln_name_agg_query(asset_type, source_type, sid, filter_fn(asset_type, source_type))
        pt_q  = build_passthrough_query(asset_type, source_type, sid)
        self.process_source_id(asset_type, source_type, sid, work_target, snapshot_date_iso, agg_q, pt_q)

    if workers <= 1:
        for sid in source_ids: task(sid)
    else:
        with ThreadPoolExecutor(max_workers=workers) as pool:
            for _ in pool.map(task, source_ids): pass

    if kind == "current":
        self._es.PutSettings(temp, {"index": {"blocks": {"write": True}}})
        self._es.DeleteIndex(target)
        self._es.CloneIndex(temp, target)
        self._es.PutSettings(temp, {"index": {"blocks": {"write": False}}})
```

**Step 3: Pass + commit**

```
git commit -m "feat(snapshots): run_month for current and historical"
```

---

### Task 2.5: Snapshot index template

**Files:**
- Create: `Saltworks.SaltMiner.IndexTemplates/index-templates/snapshots.json`

**Step 1:** Author a template with `index_patterns: ["snapshots_*"]` and mappings covering: `snapshot_date` (date), `saltminer.asset.*` (mirror from `issue.json`), `saltminer.engagement`, `saltminer.inventory_asset`, `vulnerability.name`, `vulnerability.classification`, `vulnerability.category`, severity bucket integers, `total` integer.

**Step 2:** Register in `GetMapping` (check how SSC mapping `AppVersionSnapshots` is registered — `Saltworks.SaltMiner.Python/Sources/SSC/SscEsUtils.py` or similar) and add a new name `Snapshots`.

**Step 3: Commit**

```
git commit -m "feat(snapshots): index template"
```

---

## Phase 3 — Runner

### Task 3.1: `RunUtilSnapshotHistory.py` — CLI + daily mode

**Files:**
- Create: `Saltworks.SaltMiner.Python/RunUtilSnapshotHistory.py`

**Step 1: Implement**

```python
'''Source-agnostic snapshot runner. See docs/plans/2026-05-13-source-agnostic-snapshots-design.md.'''
import argparse, logging, datetime, calendar
from Core.Application import Application
from Snapshots.SnapshotHelper import SnapshotHelper, build_index_name
from Snapshots.SnapshotQueries import build_combo_discovery_query

def parse_args():
    p = argparse.ArgumentParser()
    p.add_argument("--asset-type")
    p.add_argument("--source-type")
    p.add_argument("--mode", choices=["daily","rebuild"], default="daily")
    p.add_argument("--start")          # YYYY-MM (rebuild)
    p.add_argument("--end")            # YYYY-MM (rebuild)
    p.add_argument("--workers", type=int, default=1)
    return p.parse_args()

def discover_combos(es):
    r = es.Search("issues*", queryBody=build_combo_discovery_query(), size=0, navToData=False)
    return [(b["key"]["asset_type"], b["key"]["source_type"])
            for b in r.get("aggregations", {}).get("combos", {}).get("buckets", [])]

def last_instant_of_month(year, month):
    days = calendar.monthrange(year, month)[1]
    return datetime.datetime(year, month, days, 23, 59, 59, 999000, tzinfo=datetime.timezone.utc).isoformat()

def previous_month(now):
    y, m = (now.year, now.month - 1) if now.month > 1 else (now.year - 1, 12)
    return y, m

def historical_exists(es, asset_type, source_type, t_iso):
    idx = build_index_name(asset_type, source_type, "historical")
    q = {"query": {"term": {"snapshot_date": t_iso}}}
    return es.Count(idx, queryBody=q, suppressErrorOnMissingIndex=True) > 0

def main():
    args = parse_args()
    logging.basicConfig(level=logging.INFO)
    app = Application()
    es = app.GetElasticClient()
    helper = SnapshotHelper(es)

    combos = ([(args.asset_type, args.source_type)] if args.source_type
              else discover_combos(es))

    if args.mode == "daily":
        now = datetime.datetime.now(datetime.timezone.utc)
        py, pm = previous_month(now)
        prev_t = last_instant_of_month(py, pm)
        for at, st in combos:
            if not historical_exists(es, at, st, prev_t):
                logging.info("Building historical for %s/%s @ %s", at, st, prev_t)
                helper.run_month(at, st, "historical", prev_t, "Snapshots", workers=args.workers)
            logging.info("Building current for %s/%s", at, st)
            helper.run_month(at, st, "current", now.isoformat(), "Snapshots", workers=args.workers)
    else:
        # rebuild range
        start = datetime.datetime.strptime(args.start, "%Y-%m")
        end   = datetime.datetime.strptime(args.end,   "%Y-%m")
        cur = start
        while cur <= end:
            t = last_instant_of_month(cur.year, cur.month)
            for at, st in combos:
                helper.run_month(at, st, "historical", t, "Snapshots", workers=args.workers)
            cur = (cur.replace(day=28) + datetime.timedelta(days=4)).replace(day=1)

if __name__ == "__main__":
    main()
```

**Step 2: Smoke-run daily mode against opov for one explicit combo, workers=1**

```
cd C:/Source/saltminer/Saltworks.SaltMiner.Python
SALTMINER_2_CONFIG_PATH="C:/Source/saltminer-internal/config/python" \
  python RunUtilSnapshotHistory.py --asset-type APP --source-type saltworks.ssc --workers 1
```
Expected: completes without errors; check ES for `snapshots_app_ssc_current` document count > 0 and shape correct (use the probe script's passthrough function or Kibana).

**Step 3: Commit**

```
git commit -m "feat(snapshots): runner with daily + rebuild modes"
```

---

### Task 3.2: Integration test (opov-gated)

**Files:**
- Create: `Saltworks.SaltMiner.Python/UnitTests/SnapshotIntegrationTests.py`

**Step 1: Implement**

```python
import os, unittest
@unittest.skipUnless(os.environ.get("SALTMINER_INTEGRATION_OPOV"), "opov integration disabled")
class SnapshotIntegrationTests(unittest.TestCase):
    def test_current_run_writes_documents_for_one_combo(self):
        from Core.Application import Application
        from Snapshots.SnapshotHelper import SnapshotHelper, build_index_name
        import datetime
        app = Application(); es = app.GetElasticClient()
        helper = SnapshotHelper(es)
        now_iso = datetime.datetime.now(datetime.timezone.utc).isoformat()
        helper.run_month("APP", "saltworks.ssc", "current", now_iso, "Snapshots", workers=1)
        idx = build_index_name("APP", "saltworks.ssc", "current")
        count = es.Count(idx, queryBody={"query": {"match_all": {}}})
        self.assertGreater(count, 0)
```

**Step 2: Run with env flag**

```
SALTMINER_INTEGRATION_OPOV=1 SALTMINER_2_CONFIG_PATH=... python -m unittest UnitTests.SnapshotIntegrationTests -v
```
Expected: pass with document count > 0.

**Step 3: Commit**

```
git commit -m "test(snapshots): integration test against opov"
```

---

## Phase 4 — Parity Validation + Concurrency

### Task 4.1: Manual parity check against legacy

Compare `snapshots_app_ssc_current` (new) vs `snapshots_app_monthly_historical` (legacy, most recent snapshot) for the same SSC asset(s) on opov:
- Total doc counts grouped by `vulnerability.name` should match within an expected tolerance (legacy is monthly, new is current-state — small drift is normal).
- Severity sums should match within tolerance.

Record findings in `Scratch/snapshot_query_probe_results.md` under "Parity check". If any large discrepancies, do not proceed to cleanup until investigated.

No code change; commit only the results doc updates.

```
git commit -am "docs(snapshots): parity-check results"
```

---

### Task 4.2: Enable 4 workers

**Files:**
- Modify: `Saltworks.SaltMiner.Python/RunUtilSnapshotHistory.py` (no code change; document recommended default in `--help` text)

**Step 1: Smoke-run with workers=4**

```
python RunUtilSnapshotHistory.py --asset-type APP --source-type saltworks.ssc --workers 4
```

Verify: same final doc count as workers=1 run, walltime materially shorter. Record both in the results doc.

**Step 2: Commit (results doc only)**

```
git commit -am "docs(snapshots): worker=4 timing"
```

---

## Phase 5 — Cleanup

### Task 5.1: Remove legacy SSC snapshot helpers + scan-snapshot pipeline

**Files:**
- Delete: `Saltworks.SaltMiner.Python/Sources/SSC/SscSnapshotHelper.py`
- Delete: `Saltworks.SaltMiner.Python/Sources/SSC/SscScanSnapshotHelper.py`
- Delete: `Saltworks.SaltMiner.Python/RunUtilSscSnapshotHistory.py`
- Grep for any remaining imports/refs to these and remove.

**Step 1: Verify nothing else imports them**

```
grep -rn "SscSnapshotHelper\|SscScanSnapshotHelper\|RunUtilSscSnapshotHistory" Saltworks.SaltMiner.Python --include='*.py'
```
Expected: no hits (or only the files themselves, before deletion).

**Step 2: Delete + commit**

```
git commit -m "chore(snapshots): retire legacy SSC snapshot pipeline"
```

---

### Task 5.2: Remove probe script + results doc

**Files:**
- Delete: `Saltworks.SaltMiner.Python/Scratch/snapshot_query_probe.py`
- Delete: `Saltworks.SaltMiner.Python/Scratch/snapshot_query_probe_results.md`
- Delete: `Saltworks.SaltMiner.Python/Scratch/__init__.py` (if no other contents)

**Step 1: Verify no production code references the probe**

```
grep -rn "snapshot_query_probe" Saltworks.SaltMiner.Python --include='*.py'
```
Expected: no hits.

**Step 2: Delete + commit**

```
git commit -m "chore(snapshots): remove temporary probe script"
```

---

### Task 5.3: Final run-through

- Run full unit test suite: `python -m unittest discover -v` — all green.
- Run runner once in daily mode against opov — completes, writes both indices.
- Tag/note in PR description: legacy `snapshots_app_monthly_historical` and `scan_snapshots_app_historical` indices can be dropped by ops after grace period (NOT in this PR).
