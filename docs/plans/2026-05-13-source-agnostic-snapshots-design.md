# Source-Agnostic Snapshot Generation — Design

**Date**: 2026-05-13
**Status**: Approved (pending implementation plan)
**Replaces**: `RunUtilSscSnapshotHistory.py`, `SscSnapshotHelper`, `SscScanSnapshotHelper`

## Goals

1. Generate point-in-time monthly snapshots from `issues*` indices for **all source types**, not just SSC.
2. Drop scan-level snapshots (`scan_snapshots_app_historical`); keep only the more detailed asset-source × vuln-name grain.
3. Use Elasticsearch aggregations instead of scrolling where possible.
4. Support parallel processing for large daily updates (target: 30 MM+ issues).
5. Produce per-asset-type, per-source-type indices:
   - `snapshots_{asset_type}_{source}_historical` — append-only across completed months
   - `snapshots_{asset_type}_{source}_current` — rewritten on every run
6. `source` is `saltminer.asset.source_type` with the `saltworks.` prefix stripped.

## Non-Goals

- No scan-level snapshots.
- No retroactive re-computation of historical months on the daily run — only on explicit `rebuild` command.
- No handling of moving `found_date` (assumed stable for historical correctness).

## Output Index Shape

One document per `(asset.source_id, vulnerability.name, snapshot_date)`.

### Fields

| Field | Source | Notes |
|---|---|---|
| `saltminer.asset.source_id` | passthrough | grain key |
| `saltminer.asset.asset_type` | passthrough | redundant with index suffix, kept for cross-index queries |
| `saltminer.asset.source_type` | passthrough | redundant with index suffix, kept for cross-index queries |
| `saltminer.asset.*` (composite_key, name, version, version_id, instance, source_id, is_saltminer_source, is_retired, is_production, **attributes**, host, ip, scheme, port) | passthrough from one issue per source_id | per-asset, stable within a source_id |
| `saltminer.engagement.*` | passthrough from one issue per source_id | per-asset |
| `saltminer.inventory_asset.*` | passthrough from one issue per source_id | per-asset |
| `vulnerability.name` | grain key | |
| `vulnerability.classification` | `min` aggregation across the bucket | collapses to one value if values disagree within the bucket |
| `vulnerability.category` | `min` aggregation across the bucket | collapses to one value if values disagree within the bucket |
| `snapshot_date` | runner | last-day-of-month EOD for historical; "now" for current |
| `critical`, `high`, `medium`, `low`, `info` | computed | count of issues per severity bucket |
| `total` | computed | sum of the five buckets |

### Explicitly EXCLUDED from passthrough

These can vary within a single `vuln.name` bucket and are therefore unsafe to passthrough at this grain:

- `saltminer.tags`, `saltminer.labels`, `saltminer.score`
- `vulnerability.source_severity`
- `vulnerability.scanner.product`, `vulnerability.scanner.vendor`, `vulnerability.scanner.assessment_type`

## Filter Semantics

### Current-month document
Each daily run rewrites `_current`. Filter:

```
saltminer.asset.asset_type = <asset_type>
AND saltminer.asset.source_type = <source_type>
AND vulnerability.is_active = true
```

`is_active` is the source-of-truth flag for "issue is currently open and not suppressed/removed/filtered".

### Historical-month document (month-end T)
Filter:

```
saltminer.asset.asset_type = <asset_type>
AND saltminer.asset.source_type = <source_type>
AND vulnerability.found_date <= T
AND ( vulnerability.removed_date does-not-exist
      OR vulnerability.removed_date > T )
AND vulnerability.is_filtered = false
AND vulnerability.is_suppressed = false
```

`T` is the last millisecond of the month. No `is_active` — point-in-time correctness for past months derives from `found_date` / `removed_date`.

*Open question for implementation: confirm the field name is `vulnerability.removed_date` (matches `issue.json` template). User initially wrote `closed_date`; we use `removed_date` unless the probe phase shows otherwise.*

## Pipeline

### New layout

```
Saltworks.SaltMiner.Python/
  Snapshots/
    __init__.py
    SnapshotHelper.py          # core class, source-agnostic
    SnapshotQueries.py         # ES query builders (current, historical, discovery)
    SnapshotTransforms.py      # pure bucket->doc transform functions
  RunUtilSnapshotHistory.py    # new runner
  Scratch/
    snapshot_query_probe.py    # TEMPORARY pre-implementation verification
```

Legacy files removed in final cleanup commit:
- `Sources/SSC/SscSnapshotHelper.py`
- `Sources/SSC/SscScanSnapshotHelper.py`
- `RunUtilSscSnapshotHistory.py`

### Runner CLI

```
RunUtilSnapshotHistory.py
  --asset-type APP                  # optional
  --source-type saltworks.ssc       # optional
  --mode {daily,rebuild}            # default: daily
  --start YYYY-MM                   # rebuild only
  --end YYYY-MM                     # rebuild only
  --workers N                       # default 1; set 4 once stable
```

Discovery rules:
- If `--source-type` is provided: asset_type is implied (a given source produces one asset_type). No discovery aggregation needed.
- If both omitted: run a single `terms` aggregation against `issues*` grouped by `(asset_type, source_type)` to enumerate combos, then process each combo sequentially.

### Run modes

#### `daily` (default)

For each (asset_type, source_type):
1. **Month-rollover check**: query `_historical` for any document with `snapshot_date` equal to the last day of the previous month (EOD UTC). If absent, build that month into `_historical` using the historical-month filter.
2. **Current rewrite**: build `_current` using the current-month filter (`is_active=true`). Written via temp-index + clone swap (same pattern as legacy) so dashboards never see partial state.

#### `rebuild`

For each (asset_type, source_type) × each month in `[start, end]`:
1. Delete-by-query in `_historical` for `snapshot_date == T` (idempotency).
2. Build the month using historical filter.
3. Bulk-write to `_historical`.

Months can be processed in parallel (outer dimension) along with source_ids (inner). Practically: a single flat task queue of `(asset_type, source_type, month, source_id)` tuples consumed by N workers.

### Per-month data flow

1. **Discovery aggregation** (one ES request per month):
   ```
   terms agg on saltminer.asset.source_id
     filter: <month filter>
     size: dynamically sized (composite agg if cardinality exceeds terms cap)
   ```
   Result: list of source_ids that have qualifying issues for this month.

2. **Worker fan-out** (`ThreadPoolExecutor`, configurable workers). Each worker consumes one source_id and:
   a. Runs a `size:1` passthrough query to harvest per-asset fields (`saltminer.asset.*`, `engagement`, `inventory_asset`).
   b. Runs a composite aggregation on `vulnerability.name` for that source_id with:
      - `terms` sub-agg on `vulnerability.severity` (severity bucket counts)
      - `min` sub-agg on `vulnerability.classification` and `vulnerability.category` (collapse to one value)
   c. Transforms each bucket → snapshot document.
   d. Bulk-writes batches of 5,000 docs to the target index.

3. **Finalize**:
   - For `_current`: settings write-block on temp index, delete real index, clone temp → real, unblock writes.
   - For `_historical`: no clone; documents have already been written incrementally.

### Threading model

- `ThreadPoolExecutor` (not `multiprocessing`) — workload is ES-bound, not CPU-bound.
- Default 1 worker for initial validation. Bump to 4 once stable.
- Each worker holds its own `ElasticClient` instance (no shared state).
- A bounded queue of source_ids prevents memory blowup on big customers.

## Pre-Testing Phase (Verification Before Pipeline Code)

Before any production code is written, a temporary probe script verifies that every planned ES query returns the expected data shape and counts on real opov data.

### Script: `Saltworks.SaltMiner.Python/Scratch/snapshot_query_probe.py`

Marked `# TODO: TEMPORARY DEBUGGING - Remove after testing`.

Probes:
1. **Connectivity** — confirm opov ES cluster health and index existence.
2. **Combo discovery** — terms agg on `(asset_type, source_type)` against `issues*`; print combos.
3. **Source-ID discovery** — for one chosen combo (e.g. APP × `saltworks.ssc`), run the discovery agg for current month; print bucket count and a sample of source_ids.
4. **Per source_id aggregation** — for one chosen source_id, run the composite agg with severity sub-agg and `min` classification/category; print buckets.
5. **Passthrough query** — for the same source_id, run the `size:1` passthrough query; print the extracted fields.
6. **Filter equivalence checks**:
   - Current filter (`is_active=true`) vs. equivalent expanded filter using lifecycle dates — count comparison on one source_id, ensure they agree (within expected drift).
   - Historical filter for a recent closed month — sanity-check counts against a known dashboard if possible.
7. **Ground-truth cross-check** — scroll over one source_id's issues for the current filter, build counts in Python, compare to the aggregation result. Must match.

### Companion doc

`Saltworks.SaltMiner.Python/Scratch/snapshot_query_probe_results.md` (also TEMPORARY) records:
- Expected vs actual outputs for each probe.
- Cardinality notes (e.g. "5 combos, ~30k source_ids in worst combo").
- Any anomalies that require design changes.

Only after probe results are reviewed does the production code get written.

## Testing

- **Unit tests** (`UnitTests/SnapshotTransformsTests.py`): bucket → document transforms are pure functions; tested with synthetic agg responses including missing severity buckets, empty classification, etc.
- **Integration test** (`UnitTests/SnapshotIntegrationTests.py`, gated by `SALTMINER_INTEGRATION_OPOV` env flag): runs the full pipeline against opov for one source_id, verifies one written doc.
- **Manual validation**: side-by-side comparison of new `_current` index vs legacy `snapshots_app_monthly_historical` for an SSC source on opov before retiring legacy helpers.

## Cleanup

After parity validation:
1. Delete `Sources/SSC/SscSnapshotHelper.py`, `Sources/SSC/SscScanSnapshotHelper.py`, `RunUtilSscSnapshotHistory.py`.
2. Delete `Scratch/snapshot_query_probe.py` and `snapshot_query_probe_results.md`.
3. Drop legacy indices `snapshots_app_monthly_historical` and `scan_snapshots_app_historical` after a grace period (operator decision, not in this plan).

## Open Items for Implementation

- Confirm `vulnerability.removed_date` is the correct field (user used "closed_date" colloquially).
- Decide composite aggregation page size based on probe results (vuln.name cardinality per source_id).
- Index template for the new snapshot indices — derive from the doc shape above; not yet drafted.
