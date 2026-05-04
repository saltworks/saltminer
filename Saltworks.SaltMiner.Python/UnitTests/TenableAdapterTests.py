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

import sys
import unittest
from unittest.mock import MagicMock, patch

# Break circular import chain before importing TenableAdapter
sys.modules.setdefault('Core.DataClient', MagicMock())
sys.modules.setdefault('Sources.Tenable.TenableClient', MagicMock())

from Sources.Tenable.TenableAdapter import TenableAdapter

# --- fixtures ---

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


if __name__ == '__main__':
    unittest.main()
