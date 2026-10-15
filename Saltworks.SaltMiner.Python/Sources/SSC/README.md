# Fortify SSC Source

> Context ID: `python/ssc`

Integration with **Fortify Software Security Center (SSC)**. Pulls project versions,
scans, issues and attributes out of the SSC REST API, stages them in Elasticsearch, then
turns that staged data into SaltMiner queue scans / assets / issues for ingest.

Unlike the newer single-class adapters (`Sources/Snyk`, `Sources/GHAS`, …), SSC runs as a
**two-stage pipeline** over intermediate `ssc*` Elasticsearch indices:

1. **Sync** — `SyncExtractor.py` reads SSC and writes `sscprojects`, `sscprojscans`,
   `sscprojissues`, `sscprojcounts`, `sscprojattrs`, and records what changed in
   `sscupdatequeue`.
2. **Refresh** — `AppVulsProcessor.py` walks that update queue and produces the reporting
   data: the `app_*_ssc` indices and (when SM API integration is enabled) SaltMiner queue
   documents submitted through `Utility/SmApiClient.py`.

## Files

| File | Purpose |
|---|---|
| `SyncExtractor.py` | Stage 1. Syncs SSC → the `ssc*` staging indices; batch (`Process`) and single project version (`ProcessOne`). |
| `AppVulsProcessor.py` | Stage 2. Builds reporting/queue data from staging; batch (`PopulateVuls`) and single project version (`PopulateVulsOne`). |
| `SscUtilities.py` | SSC REST API wrapper (issue counts, filter sets, scans, attributes). Older methods are deprecated in favour of the newer SSC client. |
| `SscEsUtils.py` | Small helpers over the `ssc*` staging indices. |
| `RefreshSSC.py` | Writes `sscupdatequeue` records to force a refresh of one or all project versions. |
| `AuthHelper.py` | Syncs SSC user/role/project-version assignments into SaltMiner authorization data. |
| `SscSnapshotHelper.py`, `SscScanSnapshotHelper.py` | Legacy monthly snapshot builders (superseded by `snapshot/` — see Context ID `python/snapshots`). |
| `VulComparer.py` | Diagnostic: compares SSC issue counts against what landed in SaltMiner. |

## Running

Run from the `Saltworks.SaltMiner.Python/` directory with a resolvable config path
(`SALTMINER_CONFIG_PATH`). Entry points:

- `RunSync.py` — stage 1 batch sync.
- `RunPopulateAppVuls.py` — stage 2 batch refresh.
- `RunAgentService.py` — the agent/worker service; `Sources/SyncWorker.py` runs both
  stages for a single project version per queue item (`ProcessOne` then `PopulateVulsOne`).
- `RunUtilSscQuickRefresh.py` — targeted refresh utility.

Per-source configuration (SSC URL, credentials, `AssessmentTypeMap`,
`V3ExpectedAssessmentTypes`, …) lives in the deployed `Config/Sources/*.json`.
