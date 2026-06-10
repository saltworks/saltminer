# Tenable Adapter Inheritance Refactor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Refactor `TenableAdapter`, `TenableVulnManagementAdapter`, and `TenableWasAdapter` so the sub-adapters inherit from `TenableAdapter` via `super().__init__(app)` and shared mapping logic lives in the base class.

**Architecture:** `TenableAdapter` becomes the inheritance base and config-driven dispatcher. Both sub-adapters call `super().__init__(app)` to get shared state, then call `super().map_scan/asset/issue()` to get a partially-filled doc before adding only their specific fields. `finalize_all_scans` moves to the base since both implementations are identical.

**Tech Stack:** Python 3, `unittest`, `unittest.mock`

**Design doc:** `docs/plans/2026-05-04-tenable-inheritance-refactor-design.md`

---

## Shared Test Fixtures

These fixtures are used across all tasks. Define them once at the top of `UnitTests/TenableAdapterTests.py`.

```python
VM_FINDING = {
    'asset': {
        'uuid': 'asset-uuid-123',
        'netbios_name': None,
        'hostname': 'test-host.example.com',
        'ipv4': '192.168.1.100',
        'ipv6': None,
        'mac_address': None,
        'agent_uuid': None,
        'bios_uuid': None,
        'fqdn': 'test-host.example.com',
        'last_scan_target': None,
        'operating_system': ['Windows 10'],
    },
    'port': {'port': 443, 'protocol': 'TCP'},
    'severity': 'high',
    'first_found': '2024-01-01T00:00:00Z',
    'last_found': '2024-02-01T00:00:00Z',
    'state': 'OPEN',
    'last_fixed': None,
    'finding_id': 'finding-123',
    'plugin': {
        'name': 'Test Vulnerability',
        'description': 'A test vuln',
        'solution': 'Apply patch',
        'cve': ['CVE-2024-1234'],
        'risk_factor': 'High',
    },
}

WAS_FINDING = {
    'asset': {
        'uuid': 'was-asset-uuid-456',
        'fqdn': 'app.example.com',
        'ipv4': '10.0.0.1',
    },
    'port': {'port': 443},
    'url': 'https://app.example.com/login',
    'severity': 'medium',
    'first_found': '2024-01-15T00:00:00Z',
    'last_found': '2024-03-01T00:00:00Z',
    'state': 'OPEN',
    'last_fixed': None,
    'finding_id': 'was-finding-789',
    'scan': {'uuid': 'scan-uuid-abc', 'completed_at': '2024-03-01T12:00:00Z'},
    'plugin': {
        'id': 98765,
        'name': 'XSS Vulnerability',
        'description': 'Cross-site scripting',
        'solution': 'Sanitize inputs',
        'cwe': [79],
        'risk_factor': 'Medium',
    },
}

VM_SCAN_DICT = {
    'queue_scan_id': 'qs-001',
    'queue_asset_id': 'qa-001',
    'report_id': 'report-001',
    'schedule_uuid': 'schedule-uuid-xyz',
}

WAS_SCAN_DICT = {
    'queue_scan_id': 'qs-002',
    'queue_asset_id': 'qa-002',
    'report_id': 'report-002',
}

def make_mock_app():
    app = MagicMock()
    app.Settings.GetSource.side_effect = lambda source, key: {
        ("Tenable", "VulnManagement"): True,
        ("Tenable", "WAS"): True,
    }.get((source, key), None)
    return app
```

---

### Task 1: Create test file with failing tests for base class mapping

**Files:**
- Create: `UnitTests/TenableAdapterTests.py`

**Step 1: Create the test file**

```python
''' --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
*
* ----
'''

import unittest
from unittest.mock import MagicMock, patch

from Sources.Tenable.TenableAdapter import TenableAdapter

# --- fixtures ---

VM_FINDING = { ... }   # paste full VM_FINDING from plan header
WAS_FINDING = { ... }  # paste full WAS_FINDING from plan header
VM_SCAN_DICT = { ... } # paste full VM_SCAN_DICT from plan header
WAS_SCAN_DICT = { ... } # paste full WAS_SCAN_DICT from plan header

def make_mock_app():
    app = MagicMock()
    app.Settings.GetSource.side_effect = lambda source, key: {
        ("Tenable", "VulnManagement"): True,
        ("Tenable", "WAS"): True,
    }.get((source, key), None)
    return app


@patch('Sources.Tenable.TenableAdapter.DataClient')
@patch('Sources.Tenable.TenableAdapter.TenableClient')
class TenableAdapterBaseTests(unittest.TestCase):

    def test_base_map_scan_common_fields(self, MockTenableClient, MockDataClient):
        adapter = TenableAdapter(make_mock_app())
        doc = adapter.map_scan()
        scan = doc['Saltminer']['Scan']
        self.assertEqual(doc['Saltminer']['Internal']['IssueCount'], -1)
        self.assertEqual(scan['Product'], 'Tenable')
        self.assertEqual(scan['Vendor'], 'Tenable')
        self.assertEqual(scan['SourceType'], 'Saltworks.Tenable')
        self.assertEqual(scan['Instance'], 'Tenable1')
        self.assertEqual(scan['AssetType'], 'app')
        self.assertEqual(scan['Attributes'], {})
        self.assertIsNotNone(doc['Timestamp'])

    def test_base_map_asset_common_fields(self, MockTenableClient, MockDataClient):
        adapter = TenableAdapter(make_mock_app())
        doc = adapter.map_asset(VM_FINDING, 'qs-001')
        asset = doc['Saltminer']['Asset']
        self.assertEqual(doc['Saltminer']['Internal']['QueueScanId'], 'qs-001')
        self.assertEqual(asset['VersionId'], 'asset-uuid-123')
        self.assertEqual(asset['SourceId'], 'asset-uuid-123')
        self.assertEqual(asset['Instance'], 'Tenable1')
        self.assertEqual(asset['AssetType'], 'app')
        self.assertEqual(asset['SourceType'], 'Saltworks.Tenable')
        self.assertEqual(asset['Ip'], '192.168.1.100')
        self.assertIsNotNone(doc['Timestamp'])

    def test_base_map_issue_common_fields(self, MockTenableClient, MockDataClient):
        adapter = TenableAdapter(make_mock_app())
        doc = adapter.map_issue(VM_FINDING, VM_SCAN_DICT)
        saltminer = doc['Saltminer']
        vuln = doc['Vulnerability']
        scanner = vuln['Scanner']
        self.assertEqual(saltminer['QueueScanId'], 'qs-001')
        self.assertEqual(saltminer['QueueAssetId'], 'qa-001')
        self.assertEqual(vuln['Severity'], 'High')
        self.assertEqual(vuln['FoundDate'], '2024-01-01T00:00:00Z')
        self.assertEqual(vuln['ReportId'], 'report-001')
        self.assertEqual(vuln['Recommendation'], 'Apply patch')
        self.assertIsNone(vuln['RemovedDate'])
        self.assertEqual(scanner['Product'], 'Tenable')
        self.assertEqual(scanner['Vendor'], 'Tenable')
        self.assertIsNotNone(doc['Timestamp'])

    def test_base_map_issue_sets_removed_date_when_fixed(self, MockTenableClient, MockDataClient):
        adapter = TenableAdapter(make_mock_app())
        fixed_finding = {**VM_FINDING, 'state': 'FIXED', 'last_fixed': '2024-06-01T00:00:00Z'}
        doc = adapter.map_issue(fixed_finding, VM_SCAN_DICT)
        self.assertEqual(doc['Vulnerability']['RemovedDate'], '2024-06-01T00:00:00Z')

    def test_base_finalize_all_scans_flushes_and_resets(self, MockTenableClient, MockDataClient):
        adapter = TenableAdapter(make_mock_app())
        adapter.current_scan_asset_dict = {
            'asset-1': {'queue_scan_id': 'qs-001'},
            'asset-2': {'queue_scan_id': 'qs-002'},
        }
        adapter.finalize_all_scans()
        adapter.data_client.queue_issue_add_update_batch.assert_called_once_with(None)
        self.assertEqual(adapter.data_client.queue_scan_update_status.call_count, 2)
        self.assertEqual(adapter.current_scan_asset_dict, {})


if __name__ == '__main__':
    unittest.main()
```

**Step 2: Run tests to confirm they all fail**

```bash
cd "Saltworks.SaltMiner.Python"
python -m unittest UnitTests.TenableAdapterTests -v
```

Expected: 5 failures — `map_scan`, `map_asset`, `map_issue`, `finalize_all_scans` not defined on base class, `current_scan_asset_dict` doesn't exist on base.

---

### Task 2: Implement base class changes in `TenableAdapter`

**Files:**
- Modify: `Sources/Tenable/TenableAdapter.py`

**Step 1: Add `self.app = app` to `TenableAdapter.__init__`**

In `__init__`, add `self.app = app` as the first line after `settings = app.Settings`.

**Step 2: Add `map_scan(self)` to `TenableAdapter`**

Add after `get_sm_scans`:

```python
def map_scan(self):
    q_scan_doc = self.sm_docs.map_scan_doc()
    q_scan_doc['Timestamp'] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
    q_scan_doc['Saltminer']['Internal']['IssueCount'] = -1
    scan = q_scan_doc['Saltminer']['Scan']
    scan['Attributes'] = {}
    scan['Product'] = "Tenable"
    scan['Vendor'] = "Tenable"
    scan['SourceType'] = "Saltworks.Tenable"
    scan['Instance'] = "Tenable1"
    scan['AssetType'] = "app"
    return q_scan_doc
```

**Step 3: Add `map_asset(self, finding, queue_scan_id)` to `TenableAdapter`**

```python
def map_asset(self, finding, queue_scan_id):
    q_asset_doc = self.sm_docs.map_asset_doc()
    q_asset_doc['Timestamp'] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
    q_asset_doc['Saltminer']['Internal']['QueueScanId'] = queue_scan_id
    asset = q_asset_doc['Saltminer']['Asset']
    asset['VersionId'] = finding['asset']['uuid']
    asset['SourceId'] = finding['asset']['uuid']
    asset['Instance'] = 'Tenable1'
    asset['AssetType'] = 'app'
    asset['SourceType'] = 'Saltworks.Tenable'
    asset['Ip'] = finding['asset'].get('ipv4')
    return q_asset_doc
```

**Step 4: Add `map_issue(self, finding, current_scan_dict)` to `TenableAdapter`**

```python
def map_issue(self, finding, current_scan_dict):
    q_issue_doc = self.sm_docs.map_issue_doc()
    q_issue_doc['Timestamp'] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
    saltminer = q_issue_doc['Saltminer']
    saltminer['QueueScanId'] = current_scan_dict['queue_scan_id']
    saltminer['QueueAssetId'] = current_scan_dict['queue_asset_id']
    vulnerability = q_issue_doc['Vulnerability']
    vulnerability['Severity'] = finding['severity'].title()
    vulnerability['FoundDate'] = finding['first_found']
    vulnerability['ReportId'] = current_scan_dict['report_id']
    vulnerability['Recommendation'] = finding['plugin'].get('solution')
    if finding['state'] == 'FIXED':
        vulnerability['RemovedDate'] = finding['last_fixed']
    scanner = vulnerability['Scanner']
    scanner['Product'] = 'Tenable'
    scanner['Vendor'] = 'Tenable'
    return q_issue_doc
```

**Step 5: Add `finalize_all_scans(self)` to `TenableAdapter`**

```python
def finalize_all_scans(self):
    self.data_client.queue_issue_add_update_batch(None)
    for _, scan_data in self.current_scan_asset_dict.items():
        self.data_client.queue_scan_update_status(scan_data['queue_scan_id'], QueueStatus.PENDING)
    self.current_scan_asset_dict = {}
```

**Step 6: Update `run_sync` to pass `self.app`**

Change:
```python
was = TenableWasAdapter(self)
...
vm = TenableVulnManagementAdapter(self)
```
To:
```python
was = TenableWasAdapter(self.app)
...
vm = TenableVulnManagementAdapter(self.app)
```

**Step 7: Run the base class tests**

```bash
python -m unittest UnitTests.TenableAdapterTests.TenableAdapterBaseTests -v
```

Expected: all 5 tests PASS.

**Step 8: Commit**

```bash
git add Sources/Tenable/TenableAdapter.py UnitTests/TenableAdapterTests.py
git commit -m "Add base class mapping methods and tests to TenableAdapter"
```

---

### Task 3: Write failing tests for VM sub-adapter, then refactor it

**Files:**
- Modify: `UnitTests/TenableAdapterTests.py`
- Modify: `Sources/Tenable/TenableAdapter.py`

**Step 1: Add VM sub-adapter tests to `TenableAdapterTests.py`**

Add a second test class after `TenableAdapterBaseTests`:

```python
@patch('Sources.Tenable.TenableAdapter.DataClient')
@patch('Sources.Tenable.TenableAdapter.TenableClient')
class TenableVulnManagementAdapterTests(unittest.TestCase):

    def test_vm_map_scan_includes_common_and_vm_fields(self, MockTenableClient, MockDataClient):
        from Sources.Tenable.TenableAdapter import TenableVulnManagementAdapter
        adapter = TenableVulnManagementAdapter(make_mock_app())
        scan_record = {
            'uuid': 'scan-uuid-001',
            'last_modification_date': 1704067200,  # 2024-01-01T00:00:00Z
            'schedule_uuid': 'sched-uuid-xyz',
        }
        doc = adapter.map_scan(scan_record, VM_FINDING)
        scan = doc['Saltminer']['Scan']
        # common fields (from base)
        self.assertEqual(scan['Product'], 'Tenable')
        self.assertEqual(scan['Vendor'], 'Tenable')
        self.assertEqual(scan['SourceType'], 'Saltworks.Tenable')
        self.assertEqual(scan['Instance'], 'Tenable1')
        self.assertEqual(scan['AssetType'], 'app')
        # VM-specific fields
        self.assertEqual(scan['AssessmentType'], 'SAST')
        self.assertEqual(scan['ProductType'], 'app')
        self.assertIn('scan-uuid-001', scan['ReportId'])
        self.assertIn('asset-uuid-123', scan['ReportId'])
        self.assertIsNotNone(scan['ScanDate'])

    def test_vm_map_asset_includes_common_and_vm_fields(self, MockTenableClient, MockDataClient):
        from Sources.Tenable.TenableAdapter import TenableVulnManagementAdapter
        adapter = TenableVulnManagementAdapter(make_mock_app())
        doc = adapter.map_asset(VM_FINDING, 'qs-001')
        asset = doc['Saltminer']['Asset']
        # common fields
        self.assertEqual(asset['VersionId'], 'asset-uuid-123')
        self.assertEqual(asset['SourceId'], 'asset-uuid-123')
        self.assertEqual(asset['Instance'], 'Tenable1')
        self.assertEqual(asset['AssetType'], 'app')
        self.assertEqual(asset['SourceType'], 'Saltworks.Tenable')
        self.assertEqual(asset['Ip'], '192.168.1.100')
        # VM-specific fields
        self.assertEqual(asset['Name'], 'test-host.example.com')
        self.assertEqual(asset['Host'], 'test-host.example.com')
        self.assertEqual(asset['Port'], 443)
        self.assertEqual(asset['Scheme'], 'TCP')

    def test_vm_map_issue_includes_common_and_vm_fields(self, MockTenableClient, MockDataClient):
        from Sources.Tenable.TenableAdapter import TenableVulnManagementAdapter
        adapter = TenableVulnManagementAdapter(make_mock_app())
        doc = adapter.map_issue(VM_FINDING, VM_SCAN_DICT)
        vuln = doc['Vulnerability']
        scanner = vuln['Scanner']
        # common fields
        self.assertEqual(vuln['Severity'], 'High')
        self.assertEqual(vuln['FoundDate'], '2024-01-01T00:00:00Z')
        self.assertEqual(vuln['ReportId'], 'report-001')
        self.assertEqual(vuln['Recommendation'], 'Apply patch')
        self.assertEqual(scanner['Product'], 'Tenable')
        self.assertEqual(scanner['Vendor'], 'Tenable')
        # VM-specific fields
        self.assertEqual(vuln['Id'], ['CVE-2024-1234'])
        self.assertEqual(vuln['Name'], 'Test Vulnerability')
        self.assertEqual(vuln['Description'], 'A test vuln')
        self.assertEqual(scanner['AssessmentType'], 'SAST')
        self.assertIn('finding-123', scanner['Id'])
        self.assertIn('finding-123', scanner['GuiUrl'])
```

**Step 2: Run VM tests to confirm they fail**

```bash
python -m unittest UnitTests.TenableAdapterTests.TenableVulnManagementAdapterTests -v
```

Expected: 3 failures — `TenableVulnManagementAdapter.__init__` still takes `base`, not `app`.

**Step 3: Refactor `TenableVulnManagementAdapter`**

In `Sources/Tenable/TenableAdapter.py`:

1. Change class signature: `class TenableVulnManagementAdapter(TenableAdapter):`

2. Change `__init__`:
```python
def __init__(self, app):
    super().__init__(app)
    self.sm_scan_data_dict = {}
    self.current_scan_asset_dict = {}
    self.tenable_att_tags = {}
    self.first_load = False
```

3. Replace every `self.base.` with `self.` throughout the class.

4. Update `map_scan` to call super and set only VM-specific fields:
```python
def map_scan(self, scan_record, finding):
    q_scan_doc = super().map_scan()
    scan = q_scan_doc['Saltminer']['Scan']
    scan['ReportId'] = (
        scan_record['uuid'] + " | " +
        finding['asset']['uuid'] + " | " +
        datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
    )
    timestamp = scan_record['last_modification_date']
    dt = datetime.fromtimestamp(timestamp, tz=timezone.utc)
    scan['ScanDate'] = dt.strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
    scan['AssessmentType'] = "SAST"
    scan['ProductType'] = 'app'
    return q_scan_doc
```

5. Update `map_asset` to call super and set only VM-specific fields:
```python
def map_asset(self, finding, queue_scan_id):
    asset_name = finding['asset']['netbios_name'] if finding['asset'].get('name') else finding['asset']['hostname']
    q_asset_doc = super().map_asset(finding, queue_scan_id)
    asset = q_asset_doc['Saltminer']['Asset']
    asset['Name'] = asset_name
    asset['Version'] = asset_name
    asset['Host'] = finding['asset'].get('hostname')
    asset['Port'] = finding['port']['port'] if finding.get('port') else 'None'
    asset['Scheme'] = finding['port']['protocol'] if finding.get('port') else 'None'
    q_asset_doc = self.map_asset_attributes(finding, q_asset_doc)
    return q_asset_doc
```

6. Update `map_issue` to call super and set only VM-specific fields:
```python
def map_issue(self, finding, current_scan_dict):
    asset_name = finding['asset']['netbios_name'] if finding['asset'].get('netbios_name') else finding['asset']['hostname']
    q_issue_doc = super().map_issue(finding, current_scan_dict)
    vulnerability = q_issue_doc['Vulnerability']
    vulnerability['Description'] = finding['plugin'].get('description')
    vulnerability['Id'] = (
        [item for item in finding['plugin'].get('cve', [])]
        if finding['plugin'].get('cve')
        else ["None"]
    )
    vulnerability['Name'] = finding['plugin']['name']
    vulnerability['Location'] = asset_name
    vulnerability['LocationFull'] = (
        asset_name + "|" +
        str(finding['port']['port']) + "|" +
        finding['port']['protocol']
    )
    scanner = vulnerability['Scanner']
    scanner['Id'] = finding['finding_id'] + " | " + asset_name
    scanner['AssessmentType'] = "SAST"
    scanner['GuiUrl'] = (
        f"https://cloud.tenable.com/vm/#/explore/findings/host-vulnerabilities"
        f"/finding-details/{finding['finding_id']}"
    )
    q_issue_doc = self.map_issue_attributes(q_issue_doc, finding, current_scan_dict)
    return q_issue_doc
```

7. Delete `finalize_all_scans` from `TenableVulnManagementAdapter` entirely (now inherited).

**Step 4: Run VM tests**

```bash
python -m unittest UnitTests.TenableAdapterTests.TenableVulnManagementAdapterTests -v
```

Expected: all 3 PASS.

**Step 5: Run all tests so far**

```bash
python -m unittest UnitTests.TenableAdapterTests -v
```

Expected: all 8 tests PASS.

**Step 6: Commit**

```bash
git add Sources/Tenable/TenableAdapter.py UnitTests/TenableAdapterTests.py
git commit -m "Refactor TenableVulnManagementAdapter to inherit from TenableAdapter"
```

---

### Task 4: Write failing tests for WAS sub-adapter, then refactor it

**Files:**
- Modify: `UnitTests/TenableAdapterTests.py`
- Modify: `Sources/Tenable/TenableAdapter.py`

**Step 1: Add WAS sub-adapter tests to `TenableAdapterTests.py`**

Add a third test class:

```python
@patch('Sources.Tenable.TenableAdapter.DataClient')
@patch('Sources.Tenable.TenableAdapter.TenableClient')
class TenableWasAdapterTests(unittest.TestCase):

    def test_was_map_scan_includes_common_and_was_fields(self, MockTenableClient, MockDataClient):
        from Sources.Tenable.TenableAdapter import TenableWasAdapter
        adapter = TenableWasAdapter(make_mock_app())
        doc = adapter.map_scan(WAS_FINDING)
        scan = doc['Saltminer']['Scan']
        # common fields
        self.assertEqual(scan['Product'], 'Tenable')
        self.assertEqual(scan['Vendor'], 'Tenable')
        self.assertEqual(scan['SourceType'], 'Saltworks.Tenable')
        self.assertEqual(scan['Instance'], 'Tenable1')
        self.assertEqual(scan['AssetType'], 'app')
        # WAS-specific fields
        self.assertEqual(scan['AssessmentType'], 'DAST')
        self.assertEqual(scan['ProductType'], 'App')
        self.assertIn('was-asset-uuid-456', scan['ReportId'])
        self.assertEqual(scan['ScanDate'], '2024-03-01T12:00:00Z')

    def test_was_map_asset_includes_common_and_was_fields(self, MockTenableClient, MockDataClient):
        from Sources.Tenable.TenableAdapter import TenableWasAdapter
        adapter = TenableWasAdapter(make_mock_app())
        doc = adapter.map_asset(WAS_FINDING, 'qs-002')
        asset = doc['Saltminer']['Asset']
        # common fields
        self.assertEqual(asset['VersionId'], 'was-asset-uuid-456')
        self.assertEqual(asset['SourceId'], 'was-asset-uuid-456')
        self.assertEqual(asset['Instance'], 'Tenable1')
        self.assertEqual(asset['AssetType'], 'app')
        self.assertEqual(asset['SourceType'], 'Saltworks.Tenable')
        self.assertEqual(asset['Ip'], '10.0.0.1')
        # WAS-specific fields
        self.assertEqual(asset['Name'], 'app.example.com')
        self.assertEqual(asset['Host'], 'app.example.com')
        self.assertEqual(asset['Port'], 443)
        self.assertEqual(asset['Scheme'], 'https')

    def test_was_map_issue_includes_common_and_was_fields(self, MockTenableClient, MockDataClient):
        from Sources.Tenable.TenableAdapter import TenableWasAdapter
        adapter = TenableWasAdapter(make_mock_app())
        doc = adapter.map_issue(WAS_FINDING, WAS_SCAN_DICT)
        vuln = doc['Vulnerability']
        scanner = vuln['Scanner']
        # common fields
        self.assertEqual(vuln['Severity'], 'Medium')
        self.assertEqual(vuln['FoundDate'], '2024-01-15T00:00:00Z')
        self.assertEqual(vuln['ReportId'], 'report-002')
        self.assertEqual(vuln['Recommendation'], 'Sanitize inputs')
        self.assertEqual(scanner['Product'], 'Tenable')
        self.assertEqual(scanner['Vendor'], 'Tenable')
        # WAS-specific fields
        self.assertEqual(vuln['Id'], ['CWE-79'])
        self.assertEqual(vuln['Name'], 'XSS Vulnerability')
        self.assertEqual(vuln['Location'], 'https://app.example.com/login')
        self.assertEqual(scanner['AssessmentType'], 'DAST')
        self.assertEqual(scanner['Id'], 'was-finding-789')
        self.assertIn('was-finding-789', scanner['GuiUrl'])
```

**Step 2: Run WAS tests to confirm they fail**

```bash
python -m unittest UnitTests.TenableAdapterTests.TenableWasAdapterTests -v
```

Expected: 3 failures — `TenableWasAdapter.__init__` still takes `base`, not `app`.

**Step 3: Refactor `TenableWasAdapter`**

In `Sources/Tenable/TenableAdapter.py`:

1. Change class signature: `class TenableWasAdapter(TenableAdapter):`

2. Change `__init__`:
```python
def __init__(self, app):
    super().__init__(app)
    self.current_scan_asset_dict = {}
```

3. Replace every `self.base.` with `self.` throughout the class.

4. Update `map_scan` to call super and set only WAS-specific fields:
```python
def map_scan(self, finding):
    q_scan_doc = super().map_scan()
    scan = q_scan_doc['Saltminer']['Scan']
    scan['ReportId'] = (
        finding['asset']['uuid'] + " | " +
        finding['asset']['fqdn'] + " | " +
        str(datetime.now(timezone.utc))
    )
    scan['ScanDate'] = finding['scan']['completed_at']
    scan['AssessmentType'] = "DAST"
    scan['ProductType'] = 'App'
    return q_scan_doc
```

5. Update `map_asset` to call super and set only WAS-specific fields:
```python
def map_asset(self, finding, queue_scan_id):
    asset = finding['asset']
    url = finding.get('url', '')
    scheme = url.split("://")[0] if "://" in url else "https"
    q_asset_doc = super().map_asset(finding, queue_scan_id)
    sm_asset = q_asset_doc['Saltminer']['Asset']
    sm_asset['Name'] = asset['fqdn']
    sm_asset['Version'] = asset['fqdn']
    sm_asset['Host'] = asset['fqdn']
    sm_asset['Port'] = finding['port']['port'] if finding.get('port') else 0
    sm_asset['Scheme'] = scheme
    sm_asset['Attributes'] = {
        "was_asset_id": asset['uuid'],
        "was_asset_fqdn": asset['fqdn'],
    }
    return q_asset_doc
```

6. Update `map_issue` to call super and set only WAS-specific fields:
```python
def map_issue(self, finding, current_scan_dict):
    plugin = finding['plugin']
    q_issue_doc = super().map_issue(finding, current_scan_dict)
    vulnerability = q_issue_doc['Vulnerability']
    vulnerability['Description'] = plugin.get('description') or finding.get('output')
    vulnerability['Name'] = plugin.get('name') or str(plugin['id'])
    vulnerability['Location'] = finding.get('url', '')
    vulnerability['LocationFull'] = finding.get('url', '')

    ids = [f"CWE-{c}" for c in plugin.get('cwe', [])]
    if not ids:
        owasp = (plugin.get('owasp_2021') or plugin.get('owasp_2017') or
                 plugin.get('owasp_api_2019') or [])
        ids = list(owasp)
    if not ids:
        ids = [str(plugin['id'])]
    vulnerability['Id'] = ids

    scanner = vulnerability['Scanner']
    scanner['Id'] = finding['finding_id']
    scanner['AssessmentType'] = "DAST"
    scanner['GuiUrl'] = (
        f"https://cloud.tenable.com/was/scans/{finding['scan']['uuid']}"
        f"/vulnerabilities/{finding['finding_id']}"
    )
    q_issue_doc = self.map_issue_attributes(q_issue_doc, finding)
    return q_issue_doc
```

7. Delete `finalize_all_scans` from `TenableWasAdapter` entirely (now inherited).

**Step 4: Run all tests**

```bash
python -m unittest UnitTests.TenableAdapterTests -v
```

Expected: all 11 tests PASS.

**Step 5: Commit**

```bash
git add Sources/Tenable/TenableAdapter.py UnitTests/TenableAdapterTests.py
git commit -m "Refactor TenableWasAdapter to inherit from TenableAdapter"
```

---

### Task 5: Final smoke check

**Step 1: Run full unit test suite**

```bash
python -m unittest discover -s UnitTests -v
```

Expected: all existing tests still PASS, no regressions.

**Step 2: Verify class hierarchy**

```bash
python -c "
from unittest.mock import MagicMock, patch
with patch('Sources.Tenable.TenableAdapter.TenableClient'), \
     patch('Sources.Tenable.TenableAdapter.DataClient'):
    from Sources.Tenable.TenableAdapter import TenableAdapter, TenableVulnManagementAdapter, TenableWasAdapter
    print('VM is subclass:', issubclass(TenableVulnManagementAdapter, TenableAdapter))
    print('WAS is subclass:', issubclass(TenableWasAdapter, TenableAdapter))
"
```

Expected output:
```
VM is subclass: True
WAS is subclass: True
```
