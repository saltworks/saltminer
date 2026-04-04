# SaltMiner Source Adapter Reference

This document is the authoritative guide for building a new source adapter. Read it fully before writing any code. When in doubt, use `Sources/SNYK/SnykAdapter.py` and `Sources/SNYK/SnykClient.py` as the primary reference implementation.

---

## 1. What a Source Adapter Does

A source adapter is responsible for:
1. Pulling vulnerability/scan data from a third-party security tool's API
2. Normalizing it into SaltMiner's three-tier data model (Scan, Asset, Issue)
3. Queuing it to SaltMiner via the `DataClient` API
4. Tracking sync state in Elasticsearch to avoid reprocessing unchanged data

Adapters do **not** write directly to Elasticsearch. All data goes through the SaltMiner queue API.

---

## 2. Files to Create

For a source named `Acme` with instance `Acme1`:

```
Sources/Acme/
├── AcmeClient.py        # All API communication with the vendor
└── AcmeAdapter.py       # Orchestration, mapping, and queueing logic

Config/Sources/Acme.json # Connection config loaded by ApplicationSettings
RunAcmeAdapter.py        # Entry point script (thin wrapper)
```

**Naming conventions:**
- Class names: `AcmeClient`, `AcmeAdapter`
- SourceType (used in DTO fields): `Saltworks.Acme` — always `Saltworks.<ProductName>`
- Instance (from config `SourceName`): e.g., `Acme1`
- Config key (used in `settings.GetSource()`): matches `Source` field in JSON

---

## 3. Core Dependencies

Every adapter uses the same set of core imports:

```python
import asyncio
import json
import logging
from datetime import datetime, timezone

from Sources.Acme.AcmeClient import AcmeClient
from Core.SmDocsAndDTOs import SnykDocs, MapAssetDocDTO, MapIssueDocDTO, MapScanDocDTO
from Core.DataClient import DataClient, QueueStatus
from Core.ElasticClient import ElasticClient
```

- **`SnykDocs`** — despite the name, this is the shared document template factory used by all adapters. Do not create a new template class.
- **`MapAssetDocDTO`, `MapScanDocDTO`, `MapIssueDocDTO`** — Pydantic DTOs that validate documents before they are queued. Always validate with these before calling queue methods.
- **`DataClient`** — manages all queueing API calls to SaltMiner. Prefer the `_async` methods for new adapters (see Section 4).
- **`QueueStatus`** — constants for scan queue state (`QueueStatus.PENDING`, `QueueStatus.LOADING`).
- **`ElasticClient`** — used for state tracking (querying what has already been synced).

`Application` is passed in as `app` to `__init__`. Use `app.Settings.GetSource("Acme1", "FieldName")` to read values from `Config/Sources/Acme.json`.

---

## 4. The Three-Tier Queueing Pattern

This is the single most important pattern. **Always queue in this exact order:**

```
Scan → Asset → Issues (batched) → flush batch → set status Pending
```

Never skip a step. Never queue issues before the scan and asset are created. Never set the scan status before flushing the issue batch.

### Recommended: async adapter

See section 14 for an example of an async `run_sync` block.  New adapters should use the `_async` methods so the event loop is not blocked during I/O. Run the top-level entry point with `asyncio.run()`:

```python
async def sync_issues_async(self, source_object, ...):
    # 1. Map and queue scan
    mapped_scan = self.map_scan(source_object)
    queue_scan = await self._data_client.queue_scan_add_update_async(
        json.loads(mapped_scan.model_dump_json())
    )

    # 2. Map and queue asset (requires queue_scan['id'])
    mapped_asset = self.map_asset(source_object, queue_scan['id'])
    queue_asset = await self._data_client.queue_asset_add_update_async(
        json.loads(mapped_asset.model_dump_json())
    )

    # 3. Map and queue all issues (requires both IDs)
    for issue in issues_generator:
        mapped_issue = self.map_issue(
            issue, queue_scan['id'], queue_asset['id'], ...
        )
        await self._data_client.queue_issue_add_update_batch_async(
            json.loads(mapped_issue.model_dump_json())
        )

    # 4. Flush batch and mark scan pending
    await self._data_client.queue_issue_add_update_batch_async(None)
    await self._data_client.queue_scan_update_status_async(
        queue_scan['id'], QueueStatus.PENDING
    )
```

### Sync adapter (existing/simple)

For adapters that do not need async, the sync methods are identical in name without the `_async` suffix:

```python
def sync_issues(self, source_object, ...):
    mapped_scan = self.map_scan(source_object)
    queue_scan = self._data_client.queue_scan_add_update(
        json.loads(mapped_scan.model_dump_json())
    )

    mapped_asset = self.map_asset(source_object, queue_scan['id'])
    queue_asset = self._data_client.queue_asset_add_update(
        json.loads(mapped_asset.model_dump_json())
    )

    for issue in issues_generator:
        mapped_issue = self.map_issue(
            issue, queue_scan['id'], queue_asset['id'], ...
        )
        self._data_client.queue_issue_add_update_batch(
            json.loads(mapped_issue.model_dump_json())
        )

    # Flush batch and mark scan pending
    self._data_client.queue_issue_add_update_batch(None)
    self._data_client.queue_scan_update_status(
        queue_scan['id'], QueueStatus.PENDING
    )
```

**Important details:**
- `IssueCount` should be set to `-1` in the scan doc to disable SaltMiner's issue count validation.
- `ReplaceIssues` on the scan doc controls whether SaltMiner replaces all existing issues on sync (`True`) or accumulates/updates incrementally (`False`). This is set from the kickoff prompt input.
- `QueueStatus.LOADING` is set on the scan doc at mapping time. `QueueStatus.PENDING` is set explicitly after flushing issues — this signals SaltMiner to process the queue.
- Wrap the entire `sync_issues` body in a `try/except` and log errors; do not crash the full run on a single project failure.

---

## 5. DTO Field Reference

All three DTOs share `Timestamp`. Always set it:
```python
doc['Timestamp'] = datetime.now().strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
```

### 5.1 `map_scan_doc()` — Required Fields

| Field | Path | Notes |
|-------|------|-------|
| Timestamp | `doc['Timestamp']` | UTC ISO8601 |
| IssueCount | `doc['Saltminer']['Internal']['IssueCount']` | Set to the total issue count (or `-1` if it's not feasible to count) |
| ReplaceIssues | `doc['Saltminer']['Internal']['ReplaceIssues']` | `True` or `False` — if incoming issues include closed then set to `True`, otherwise `False` |
| QueueStatus | `doc['Saltminer']['Internal']['QueueStatus']` | Set to `QueueStatus.LOADING` at map time |
| AssessmentType | `doc['Saltminer']['Scan']['AssessmentType']` | See Section 8 - set to one of the AssessmentType constants |
| ProductType | `doc['Saltminer']['Scan']['ProductType']` | Always `"Application"` for app security tools |
| Product | `doc['Saltminer']['Scan']['Product']` | Vendor product name, e.g. `"Snyk"` |
| Vendor | `doc['Saltminer']['Scan']['Vendor']` | Vendor company name, e.g. `"Snyk"` |
| ReportId | `doc['Saltminer']['Scan']['ReportId']` | Unique scan identifier — may need to be improvised if there's not an ID, like combine project ID + name + timestamp |
| ScanDate | `doc['Saltminer']['Scan']['ScanDate']` | UTC ISO8601 — use scan's completion date if available, or today's date if not |
| SourceType | `doc['Saltminer']['Scan']['SourceType']` | `"Saltworks.<ProductName>"` |
| AssetType | `doc['Saltminer']['Scan']['AssetType']` | `"app"` for application security tools |
| Instance | `doc['Saltminer']['Scan']['Instance']` | From config `SourceName`, e.g. `"Acme1"` |

### 5.2 `map_asset_doc()` — Required Fields

| Field | Path | Notes |
|-------|------|-------|
| Timestamp | `doc['Timestamp']` | UTC ISO8601 |
| QueueScanId | `doc['Saltminer']['Internal']['QueueScanId']` | `queue_scan['id']` returned from `queue_scan_add_update` |
| Name | `doc['Saltminer']['Asset']['Name']` | Human-readable asset name (e.g. project/app name) |
| VersionId | `doc['Saltminer']['Asset']['VersionId']` | Unique, stable ID for this asset version — typically the source's project ID |
| Version | `doc['Saltminer']['Asset']['Version']` | Human-readable version label |
| SourceId | `doc['Saltminer']['Asset']['SourceId']` | Source's native project/asset ID |
| SourceType | `doc['Saltminer']['Asset']['SourceType']` | `"Saltworks.<ProductName>"` |
| AssetType | `doc['Saltminer']['Asset']['AssetType']` | `"app"` |
| Instance | `doc['Saltminer']['Asset']['Instance']` | From config `SourceName` |
| Attributes | `doc['Saltminer']['Asset']['Attributes']` | Dict of source-specific metadata (see Section 9) |

### 5.3 `map_issue_doc()` — Required Fields

| Field | Path | Notes |
|-------|------|-------|
| Timestamp | `doc['Timestamp']` | UTC ISO8601 |
| QueueScanId | `doc['Saltminer']['QueueScanId']` | From `queue_scan['id']` |
| QueueAssetId | `doc['Saltminer']['QueueAssetId']` | From `queue_asset['id']` |
| IssueType | `doc['Saltminer']['IssueType']` | Per-issue assessment type (may differ from scan-level if source mixes types) |
| FoundDate | `doc['Vulnerability']['FoundDate']` | ISO8601 — when the vuln was first detected |
| Name | `doc['Vulnerability']['Name']` | Vulnerability title |
| Severity | `doc['Vulnerability']['Severity']` | `"Critical"`, `"High"`, `"Medium"`, `"Low"`, `"Info"` — title-cased |
| IsRemoved | `doc['Vulnerability']['IsRemoved']` | Set to `False`; becomes `True` automatically when `RemovedDate` is set |
| Id | `doc['Vulnerability']['Id']` | List of CVE/CWE/vendor IDs — can be empty list |
| Scanner.Id | `doc['Vulnerability']['Scanner']['Id']` | Unique issue identifier from the source |
| Scanner.AssessmentType | `doc['Vulnerability']['Scanner']['AssessmentType']` | Matches `IssueType` |
| Scanner.Product | `doc['Vulnerability']['Scanner']['Product']` | Vendor product name |
| Scanner.Vendor | `doc['Vulnerability']['Scanner']['Vendor']` | Vendor company name |
| ReportId | `doc['Vulnerability']['ReportId']` | Same `ReportId` used in the scan doc |
| Attributes | `doc['Saltminer']['Attributes']` | Dict of source-specific metadata (see Section 9) |

**Optional but populate when available:**

| Field | Path | Notes |
|-------|------|-------|
| RemovedDate | `doc['Vulnerability']['RemovedDate']` | ISO8601 — set when issue is resolved/closed; triggers `IsRemoved = True` in SM |
| IsSuppressed | `doc['Vulnerability']['IsSuppressed']` | `True` if the issue has been waived/ignored in the source |
| Location | `doc['Vulnerability']['Location']` | File path or asset location |
| LocationFull | `doc['Vulnerability']['LocationFull']` | File path with line/column detail |
| Recommendation | `doc['Vulnerability']['Recommendation']` | Remediation guidance |
| Details | `doc['Vulnerability']['Details']` | Human-readable description |
| Scanner.GuiUrl | `doc['Vulnerability']['Scanner']['GuiUrl']` | Deep-link to issue in vendor UI (see Section 10) |
| Score.Base | `doc['Vulnerability']['Score']['Base']` | CVSS base score (float) |

---

## 6. State Tracking Pattern

Adapters track what has been synced to avoid reprocessing unchanged data. The pattern uses an Elasticsearch aggregation against the final issues index. Dev TODO: rework this pattern to use local file db source metrics to reduce need for extra elastic queries.

```python
def get_last_updated(self):
    """Load the most recent sync timestamp per project/version from Saltminer."""
    index = 'issues_app_saltworks.acme_acme1'   # See naming formula below
    if self._es.IndexExists(index):
        result = self._es.Search(
            index=index,
            queryBody={
                "aggs": {
                    "version_id": {
                        "terms": {
                            "field": "saltminer.asset.version_id",
                            "size": 10000,
                            "order": {"_key": "desc"}
                        },
                        "aggs": {
                            "last_synced": {
                                "max": {
                                    "field": "saltminer.attributes.<source>_last_updated"
                                }
                            }
                        }
                    }
                },
                "size": 0
            },
            size=10000,
            navToData=False
        )
        for bucket in result['aggregations']['version_id']['buckets']:
            self.version_last_updated[bucket['key']] = \
                bucket['last_synced'].get('value_as_string')
```

**Index naming formula:**
```
issues_app_{source_type_lower}_{instance_lower}

e.g. SourceType = "Saltworks.Acme", Instance = "Acme1"
  →  issues_app_saltworks.acme_acme1
```

**Using the state dict during sync:**
```python
last_updated = self.version_last_updated.get(project_id)
if last_updated:
    # Only fetch issues updated after this date
    issues_gen = self.client.get_issues_generator(project_id, updated_after=last_updated)
else:
    # First load — fetch all issues
    issues_gen = self.client.get_issues_generator(project_id)
```

Store the tracked attribute in `Saltminer.Attributes` on the issue doc (e.g., `acme_last_updated`) so the aggregation can find it.

---

## 7. Config JSON Format

Create `Config/Sources/Acme.json`:

```json
{
    "Source": "Acme",
    "SourceName": "Acme1",
    "Enabled": false,
    "BaseUrl": "https://api.vendor.com/",
    "ApiKey": ""
}
```

- `Source` — key used when calling `settings.GetSource("Acme", ...)`. Match the class/directory name.
- `SourceName` — the `Instance` value used in all DTO fields. e.g., `"Acme1"`.
- `Enabled` — set to `false` by default; operators enable it at deployment.
- `BaseUrl` — trailing slash.
- Auth fields — name these to match the vendor's terminology. Common patterns: `ApiKey`, `AccessKey`+`SecretKey`, `ClientId`+`ClientSecret`.

---

## 8. Pagination / Generator Pattern

All client methods that return collections must use the generator pattern. This keeps memory usage constant regardless of result set size.

```python
def get_items_generator(self, ...):
    """Fetches all items, handling pagination transparently."""
    url = self.base_url + "endpoint"
    params = {"limit": 100, ...}

    try:
        response = requests.get(url, params=params, headers=self.headers, timeout=30)
        response.raise_for_status()
        data = response.json()
    except requests.exceptions.RequestException as e:
        logging.error("Request failed: %s", e)
        return

    yield from data.get('data', [])

    while data.get('links', {}).get('next'):
        try:
            response = requests.get(
                url=self.base_url + data['links']['next'],
                headers=self.headers,
                timeout=30
            )
            response.raise_for_status()
            data = response.json()
            yield from data.get('data', [])
        except requests.exceptions.RequestException as e:
            logging.error("Pagination request failed: %s", e)
            break
```

**Notes:**
- Always use `response.raise_for_status()` before parsing JSON.
- Always include a `timeout` on every request (30 seconds for normal calls).
- Log errors and `return`/`break` — do not let a failed request propagate as an unhandled exception.
- If the API uses cursor, offset, or page-number pagination instead of `links.next`, adapt the `while` condition accordingly.
- Use the "peek at first item" pattern in the adapter to check whether there are any results before queuing a scan:

```python
issues_gen = self.client.get_issues_generator(project_id)
first_issue = next(issues_gen, None)
if first_issue:
    self.sync_issues(project, first_issue, issues_gen, ...)
else:
    logging.info("No issues for project %s, skipping.", project_id)
```

---

## 9. Assessment Type Catalog

Use one of these values for `AssessmentType` (scan and issue level):

| Value | Use When |
|-------|----------|
| `"SAST"` | Static analysis (source code scanning) |
| `"DAST"` | Dynamic analysis (runtime/web app scanning) |
| `"Open"` | Open source / SCA (software composition analysis) |
| `"IAC"` | Infrastructure as code scanning |
| `"Cloud"` | Cloud security posture scanning |
| `"License"` | Open source license compliance |
| `"Network"` | Network/host vulnerability scanning |
| `"Custom"` | Vendor-specific type that doesn't fit above |

If the source mixes types per issue (like Snyk), implement a `get_assessment_type()` method that maps the source's type string to one of the values above. The scan-level `AssessmentType` should reflect the dominant or primary type.

---

## 10. GUI URL

`Vulnerability.Scanner.GuiUrl` should be a direct deep-link to the issue in the vendor's web interface.

Three common scenarios:
1. **API returns the URL directly** — use it as-is.
2. **URL must be constructed** — use the `GUI URL Pattern` from the kickoff prompt, substituting issue/project IDs at mapping time. E.g., pattern `https://vendor.com/org/{org_id}/issue/{issue_id}` → `f"https://vendor.com/org/{org_id}/issue/{issue['id']}"`.
3. **No GUI URL available** — leave `Scanner.GuiUrl` as `None`.

Store the base URL or pattern as an instance variable in `__init__` so it is not repeated in every `map_issue` call.

---

## 11. Naming Conventions Summary

| Thing | Convention | Example |
|-------|-----------|---------|
| Directory | `Sources/<ProductName>/` | `Sources/Acme/` |
| Client class | `<ProductName>Client` | `AcmeClient` |
| Adapter class | `<ProductName>Adapter` | `AcmeAdapter` |
| Run script | `Run<ProductName>Adapter.py` | `RunAcmeAdapter.py` |
| Config file | `Config/Sources/<ProductName>.json` | `Config/Sources/Acme.json` |
| SourceType field | `"Saltworks.<ProductName>"` | `"Saltworks.Acme"` |
| Instance field | `"<ProductName><N>"` | `"Acme1"` |
| Attributes keys | `snake_case`, source-prefixed where sensible | `acme_last_updated`, `status` |
| Issues index | `issues_app_{sourcetype_lower}_{instance_lower}` | `issues_app_saltworks.acme_acme1` |

---

## 12. Attributes — Source-Specific Metadata

Both assets and issues have an `Attributes` dict for storing source-specific fields that don't map to a standard SaltMiner field. Guidelines:

- **Issue attributes** — always include a `<source>_last_updated` field (ISO8601) so the state tracking aggregation works. Include any fields useful for filtering/reporting in Kibana: status, workflow state, fix availability, relevant IDs, etc.
- **Asset attributes** — include organizational metadata from the source: team, owner, project group, business unit, tags, etc.
- Keys must be Elasticsearch-compatible: lowercase, snake_case, no special characters except `_`.
- Keep values as primitive types (string, number, bool). For list values, join as a delimited string if needed.

---

## 13. `__init__` Checklist

Every adapter's `__init__` should accept an `Application` instance and construct `DataClient` from it:

```python
def __init__(self, app):
    self.client = AcmeClient(app.Settings)
    self.sm_docs = SnykDocs()
    self._es = ElasticClient(app.Settings)
    self._data_client = DataClient(app)
    self.version_last_updated = {}          # State tracking dict
    self.base_gui_url = "https://..."       # Or None if not applicable
```

`DataClient` reads its own connection config (`Config/DataClient.json`) via `app.Settings` — no source name argument is needed.

---

## 14. run_sync Entry Point

**Async adapter (recommended):**

```python
def run_sync(self, first_load=False):
    if not first_load:
        self.get_last_updated()
    asyncio.run(self._run_async(first_load=first_load))
    self._data_client.close()

async def _run_async(self, first_load=False):
    await self.get_sync_async(first_load=first_load)
```

**Sync adapter:**

```python
def run_sync(self, first_load=False):
    if not first_load:
        self.get_last_updated()
    self.get_sync(first_load=first_load)
```

`first_load=True` means pull everything from the source, ignoring any prior state. `first_load=False` (default) is the incremental sync path.

---

## 15. Reference Implementations

| Adapter | Best for understanding |
|---------|----------------------|
| `Sources/SNYK/SnykAdapter.py` + `SnykClient.py` | Primary reference — most complete and documented. Read first. |
| `Sources/Tenable/TenableAdapter.py` | Multi-asset-per-scan pattern (one scan → multiple assets/issues) |
| `Sources/Coverity_on_Polaris/COPAdapter.py` | JSON:API format with `relationships` and `included` arrays |
| `Sources/Seeker/SeekerAdapter.py` | Simple, linear structure — good minimal example |
