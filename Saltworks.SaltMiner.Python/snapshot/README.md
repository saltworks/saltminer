# Issue Snapshots

> Context ID: `python/snapshots`

Generates point-in-time summaries ("snapshots") of the issues present on each asset,
indexed into `snapshots_<asset_type>_<source>_{historical,current}`. Snapshots let
analysts track vulnerability posture over time and break it down by asset, vulnerability,
severity, and scanner product.

> **Scan snapshots retired.** An earlier scan-based snapshot path (`scan_snapshot_builder`,
> `scan_snapshots_*` indices) was removed — it had no consumers. This package now generates
> issue snapshots only.

## Methodology

A snapshot **row summarizes one group of issues on one asset instance at one snapshot
point in time**. Rows are produced monthly by a single Elasticsearch composite
aggregation over the issue indices (`build_monthly_issue_snapshots`).

A row is uniquely identified by its **grouping key**: `source_id` + `instance` +
`vulnerability.name` + `scanner.assessment_type` + `severity` + `scanner.product`.
Scanner product is a grouping dimension, so an asset scanned by more than one product
(for example a Fortify SCA scan and a Fortify WebInspect scan) splits into a separate
row per product instead of collapsing onto one mislabeled row.

Each row draws from three sources:

- **Grouping keys** come directly from the composite bucket key — always the group's own
  values.
- **Computed counts** come from `filter` sub-aggregations inside each bucket:
  - `open_prior` — issues found before the month and still open at month start.
  - `opened_in_month` / `removed_in_month` — issues opened / closed during the month.
  - `total = open_prior + opened_in_month - removed_in_month` (the row balance). The
    balance is written to the matching per-severity count field (critical/high/…); the
    other severity fields are `0`.
  - A `must_not` painless guard drops bad-data issues whose `removed_date` precedes
    `found_date` (legacy-import sentinels) so they cannot leak into the counts.
- **Per-group representative** descriptive fields (category, classification, score.\*,
  scanner vendor, scanner product_type, source_severity, severity_level) come from a
  `top_hits size:1` sub-agg inside the same bucket, so they always correspond to the
  row's own group rather than an unrelated issue on the asset.

Separately, **asset-static** fields (asset identity/name/description/version, host and
network locators, flags, attributes, inventory-asset key, engagement) are fetched once per
asset from a single linked-asset descriptor (`fetch_asset_descriptors`) and applied to all
of that asset's rows. Engagement is empty for scanner sources and preserves context for
pentest-sourced data.

The stored shape is governed by the `snapshot` composable index template
(`Saltworks.SaltMiner.IndexTemplates/index-templates/snapshot.json`, `dynamic:false`).
On a shape change all snapshots are rebuilt: deploy the corrected template, drop the
snapshot indices (issue `_historical` and `_current`), then rebuild from earliest data via
`run_snapshot_history(..., rebuild=True)`. Dropped indices auto-recreate from the template
on first write.

### Known limitation

Some scanners (for example Tenable) do not populate `vulnerability.scanner.product_type`
at the issue level, so those snapshot rows carry an empty `product_type`. This is a
documented follow-up (an upstream ingest fix), not a defect in snapshot generation; such
rows are still produced (Tenable issues are never dropped) and are grouped by
`scanner.product`.

## Field table

Source legend: **key** = composite grouping key · **computed** = from a filter sub-agg
count · **top_hits** = per-group representative from the bucket's `descriptor` sub-agg ·
**descriptor** = asset-static, from the single linked-asset top_hits · **run** = set by the
run/rebuild mode · **constant** = fixed value.

| Snapshot field | Source | Description |
|---|---|---|
| `saltminer.snapshot_date` | run | Date the snapshot was captured (15th for completed months, now() for current). |
| `saltminer.critical/high/medium/low/info` | computed | Issue count at that severity for the group (`total` if the row's severity matches, else 0). |
| `saltminer.opened` | computed | Issues newly opened in the group this period. |
| `saltminer.removed` | computed | Issues newly removed (closed) in the group this period. |
| `saltminer.total` | computed | Total issue count for the group (`open_prior + opened - removed`). |
| `saltminer.noscan` | constant `0` | Kept for backward compatibility; vestigial. |
| `saltminer.asset.id` | descriptor | SaltMiner unique id of the related asset. |
| `saltminer.asset.source_id` | **key** | Source-system unique id of the asset. |
| `saltminer.asset.instance` | **key** | Asset instance. |
| `saltminer.asset.version_id` / `version` | descriptor | Source version id / version name. |
| `saltminer.asset.name` / `description` | descriptor | Asset name / description from source. |
| `saltminer.asset.host` / `ip` / `scheme` / `port` | descriptor | Network locators of the asset. |
| `saltminer.asset.source_type` / `asset_type` | descriptor | Source system / asset type (App/Net/Ctr). |
| `saltminer.asset.is_saltminer_source` / `is_retired` / `is_production` | descriptor | Asset flags. |
| `saltminer.asset.attributes` | descriptor | Custom source-level reporting attributes. |
| `saltminer.asset.last_scan_days_policy` | descriptor | Days between scans allowed by policy. |
| `saltminer.inventory_asset.key` | descriptor | Inventory-asset reference key. |
| `saltminer.engagement` | descriptor | Engagement (id/name/subtype/publish_date/customer); empty for scanner sources, populated for pentest. |
| `vulnerability.name` | **key** | Name / short description of the issue (e.g. SQL Injection). |
| `vulnerability.severity` | **key** | Critical/High/Medium/Low/Info/Zero. |
| `vulnerability.severity_level` | top_hits | Numeric rank of severity (Critical 1 … Zero 6). |
| `vulnerability.source_severity` | top_hits | Original source severity string. |
| `vulnerability.category` | top_hits | System/architecture the vuln affects (default "Application"). |
| `vulnerability.classification` | top_hits | Vulnerability scoring-system classification (e.g. CVSS). |
| `vulnerability.score.base/environmental/temporal` | top_hits | CVSS-style score components (0–10). |
| `vulnerability.score.version` | top_hits | NVD qualitative ranking / CVSS version. |
| `vulnerability.scanner.assessment_type` | **key** | SAST/DAST/OSS/PENTEST. |
| `vulnerability.scanner.product` | **key** | Product used to run the scan (the grouping dimension). |
| `vulnerability.scanner.vendor` | top_hits | Vendor of the scanner (co-varies with product). |
| `vulnerability.scanner.product_type` | top_hits | Type of scan run (e.g. "Fortify SCA"). Empty for scanners that don't populate it (Tenable) — documented follow-up. |
