# Design: Tenable Adapter Inheritance Refactor
**Date:** 2026-05-04
**Status:** Approved

---

## Problem

`TenableAdapter`, `TenableVulnManagementAdapter`, and `TenableWasAdapter` currently use composition — sub-adapters hold a `self.base` reference and call `self.base.data_client`, `self.base.sm_docs`, etc. This means shared state is accessed indirectly and mapping methods duplicate significant boilerplate (identical `Product`, `Vendor`, `SourceType`, `Instance`, `Severity`, `FoundDate`, etc. field assignments in both sub-adapters).

---

## Goal

Restructure so `TenableAdapter` is a proper inheritance base. Sub-adapters call `super().__init__(app)` to inherit shared state directly and call `super().map_*()` to get common fields pre-filled, then only set the fields that differ between VM and WAS.

---

## Class Structure

```
TenableAdapter (base, dispatcher)
├── TenableVulnManagementAdapter(TenableAdapter)
└── TenableWasAdapter(TenableAdapter)
```

`RunPythonAdapter.py` instantiates `TenableAdapter(app)` and calls `run_sync()` — no change to the external interface.

---

## Section 1 — Initialization & Dispatch

**`TenableAdapter.__init__(self, app)`** initializes all shared state:
- `self.app = app` (stored for sub-adapter construction in `run_sync`)
- `self.tenable_client = TenableClient(settings)`
- `self.data_client = DataClient(app)`
- `self.sm_docs = SnykDocs()`
- `self._es = app.GetElasticClient()`
- `self.vuln_management` and `self.was` config flags

**`TenableAdapter.run_sync(self, first_load=False)`** remains the dispatcher:
```python
def run_sync(self, first_load=False):
    if self.was:
        TenableWasAdapter(self.app).run_process(first_load)
    if self.vuln_management:
        TenableVulnManagementAdapter(self.app).run_process(first_load)
```
Each sub-adapter is instantiated with `app` and gets a fully initialized copy of shared state via `super().__init__(app)`.

`sm_scans_generator` and `get_sm_scans` remain on the base class.

**`TenableVulnManagementAdapter.__init__(self, app)`** calls `super().__init__(app)` then adds its own private state: `sm_scan_data_dict`, `current_scan_asset_dict`, `tenable_att_tags`, `first_load`.

**`TenableWasAdapter.__init__(self, app)`** calls `super().__init__(app)` then adds `current_scan_asset_dict`.

---

## Section 2 — Mapping Methods

All `self.base.*` references become `self.*` throughout both sub-adapters.

### `map_scan`

**Base fills (identical across both adapters):**
- Doc init via `self.sm_docs.map_scan_doc()`
- `Timestamp`, `IssueCount = -1`, `Attributes = {}`
- `Product = "Tenable"`, `Vendor = "Tenable"`
- `SourceType = "Saltworks.Tenable"`, `Instance = "Tenable1"`, `AssetType = "app"`

**Sub-class fills after `q_scan_doc = super().map_scan()`:**
- VM: `AssessmentType = "SAST"`, `ProductType = 'app'`, `ReportId` (from scan_record + asset uuid + timestamp), `ScanDate` (from epoch timestamp)
- WAS: `AssessmentType = "DAST"`, `ProductType = 'App'`, `ReportId` (from asset uuid + fqdn + datetime), `ScanDate = finding['scan']['completed_at']`

### `map_asset`

**Base fills (signature: `map_asset(self, finding, queue_scan_id)`):**
- Doc init via `self.sm_docs.map_asset_doc()`
- `Timestamp`, `QueueScanId = queue_scan_id`
- `VersionId = finding['asset']['uuid']`, `SourceId = finding['asset']['uuid']`
- `Instance = 'Tenable1'`, `AssetType = 'app'`, `SourceType = 'Saltworks.Tenable'`
- `Ip = finding['asset'].get('ipv4')`

**Sub-class fills after `q_asset_doc = super().map_asset(finding, queue_scan_id)`:**
- VM: `Name` (netbios_name/hostname), `Version`, `Host` (hostname), `Port` (default `'None'`), `Scheme` (port protocol), then calls `self.map_asset_attributes()`
- WAS: `Name` (fqdn), `Version`, `Host` (fqdn), `Port` (default `0`), `Scheme` (from URL), inline `Attributes` dict

### `map_issue`

**Base fills (signature: `map_issue(self, finding, current_scan_dict)`):**
- Doc init via `self.sm_docs.map_issue_doc()`
- `Timestamp`, `QueueScanId`, `QueueAssetId`
- `Severity = finding['severity'].title()`
- `FoundDate = finding['first_found']`
- `ReportId = current_scan_dict['report_id']`
- `Recommendation = finding['plugin'].get('solution')`
- RemovedDate: `if finding['state'] == 'FIXED': vulnerability['RemovedDate'] = finding['last_fixed']`
- `scanner['Product'] = 'Tenable'`, `scanner['Vendor'] = 'Tenable'`

**Sub-class fills after `q_issue_doc = super().map_issue(finding, current_scan_dict)`:**
- VM: `Description`, `Name`, `Id` (CVE list), `Location` (asset_name), `LocationFull` (name + port + protocol), `scanner['Id']`, `scanner['AssessmentType'] = "SAST"`, `scanner['GuiUrl']` — then calls `self.map_issue_attributes()`
- WAS: `Description` (with output fallback), `Name` (with id fallback), `Id` (CWE/OWASP), `Location` (url), `LocationFull` (url), `scanner['Id']`, `scanner['AssessmentType'] = "DAST"`, `scanner['GuiUrl']` — then calls `self.map_issue_attributes()`

### `map_asset_attributes` / `map_issue_attributes`

Remain as private methods on each sub-class — their field sources are entirely different between VM and WAS.

---

## Section 3 — Additional Shared Logic

**`finalize_all_scans` moves to the base class.** Both sub-classes have functionally identical implementations. Since both initialize `self.current_scan_asset_dict = {}` in their `__init__` and `self.data_client` is inherited, the base owns this method and both sub-classes drop their copies.

---

## What Does Not Change

- `RunPythonAdapter.py` — no changes
- `TenableClient.py` — no changes
- `run_process` logic in each sub-adapter — no changes
- `compare_tenable_scans`, `sync_scan`, `get_asset_attributes`, `schedule_uuid_agg_query` — no changes
- `map_asset_attributes`, `map_issue_attributes` — stay on sub-classes, no changes to their logic
