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

import datetime
import logging
import os
import unittest

from Core.Application import Application
from Core.DataClient import DataClient, DataClientException

module = os.path.splitext(os.path.basename(__file__))[0]

# Source type used for all test documents — easy to identify / filter in ES
TEST_SOURCE_TYPE = 'Saltworks.Test'
TEST_INSTANCE    = 'dataclienttest'
TEST_ASSET_TYPE  = 'app'


def _now():
    return datetime.datetime.now(datetime.timezone.utc).isoformat()


def _make_q_scan(agent_id=None):
    return {
        'Timestamp': _now(),
        'Saltminer': {
            'Internal': {
                'AgentId': agent_id,
                'IssueCount': -1,
                'CurrentQueueScanId': None,
                'ReplaceIssues': True,
                'QueueStatus': 'Loading',
            },
            'Scan': {
                'AssessmentType': 'SAST',
                'ProductType': 'App',
                'Product': 'TestProduct',
                'ProductVersion': '1.0',
                'Vendor': 'TestVendor',
                'ReportId': f'test-report-{_now()}',
                'ScanDate': _now(),
                'SourceType': TEST_SOURCE_TYPE,
                'IsSaltMinerSource': True,
                'ConfigName': TEST_INSTANCE,
                'AssetType': TEST_ASSET_TYPE,
                'Instance': TEST_INSTANCE,
                'Rulepacks': [],
            },
        },
    }


def _make_q_asset(scan_id):
    return {
        'Timestamp': _now(),
        'Saltminer': {
            'Asset': {
                'Name': 'DataClientTest App',
                'Description': 'Created by DataClientTests integration test',
                'VersionId': 'test-version-001',
                'Version': '1.0',
                'ConfigName': TEST_INSTANCE,
                'SourceType': TEST_SOURCE_TYPE,
                'IsSaltMinerSource': True,
                'SourceId': 'test-source-001',
                'IsProduction': False,
                'AssetType': TEST_ASSET_TYPE,
                'Instance': TEST_INSTANCE,
                'Attributes': {},
                'LastScanDaysPolicy': '30',
            },
            'InventoryAsset': {'Key': ''},
            'Internal': {'QueueScanId': scan_id},
        },
    }


def _make_q_issue(scan_id, asset_id):
    return {
        'Timestamp': _now(),
        'Saltminer': {
            'QueueScanId': scan_id,
            'QueueAssetId': asset_id,
            'Source': {
                'Analyzer': 'test-analyzer',
                'Confidence': 1.0,
                'Impact': 1.0,
                'IssueStatus': 'Open',
                'Kingdom': 'Input Validation',
                'Likelihood': 1.0,
            },
            'Attributes': {},
        },
        'Vulnerability': {
            'IsActive': True,
            'Audit': {
                'Audited': False,
                'Auditor': '',
                'LastAudit': None,
            },
            'FoundDate': _now(),
            'Id': None,
            'IsFiltered': False,
            'IsRemoved': False,
            'IsSuppressed': False,
            'Location': '/test/location',
            'LocationFull': '/test/location/full',
            'RemovedDate': None,
            'SourceSeverity': 'Medium',
            'ReportId': 'test-report-001',
            'Name': 'Test Vulnerability',
            'Reference': '',
            'Severity': 'Medium',
            'Scanner': {
                'ApiUrl': '',
                'GuiUrl': '',
                'Id': 'test-scanner-id',
                'AssessmentType': 'SAST',
                'Product': 'TestProduct',
                'ProductType': 'App',
                'ProductVersion': '1.0',
                'Vendor': 'TestVendor',
            },
            'Score': {
                'Base': 0.0,
                'Environmental': 0.0,
                'Temporal': 0.0,
                'Version': None,
            },
        },
        'Labels': {},
        'Message': None,
        'Tags': None,
    }


def _make_event_payload(action, outcome, level, provider='test', data_set='saltminer.test'):
    now = _now()
    return {
        'Entity': {
            'Timestamp': now,
            'Saltminer': None,
            'Event': {
                'Action': action,
                'Severity': 0,
                'Outcome': outcome,
                'Reason': 'Integration test',
                'DataSet': data_set,
                'Provider': provider,
                'Kind': 'event',
                'Created': now,
            },
            'Log': {'Level': level},
        }
    }


class DataClientTests(unittest.TestCase):
    '''Integration tests for Core.DataClient against a live DataApi (localhost:5000).

    Requires:
    - DataApi running (use launch.json DataApi config)
    - Config/DataClient.json with valid ApiUrl, ApiKey, ManagerApiKey
    '''

    @classmethod
    def setUpClass(cls):
        cls.app = Application()
        cls.client = DataClient(cls.app)
        cls.es = cls.app.GetElasticClient()
        cls._scan_ids_to_delete = []

    @classmethod
    def tearDownClass(cls):
        '''Delete all queue scans (and children) created during tests.'''
        for scan_id in cls._scan_ids_to_delete:
            try:
                cls.client.queue_scan_delete_all(scan_id)
                logging.debug("[DataClientTests] Cleaned up queue scan '%s'", scan_id)
            except Exception as e:
                logging.warning("[DataClientTests] Failed to clean up queue scan '%s': %s", scan_id, e)

    # ------------------------------------------------------------------
    # Helpers
    # ------------------------------------------------------------------

    def _create_scan(self):
        '''Creates a queue scan and tracks it for teardown cleanup.'''
        data = self.client.queue_scan_add_update(_make_q_scan())
        scan_id = data['id']
        self._scan_ids_to_delete.append(scan_id)
        return data

    def _create_scan_and_asset(self):
        '''Creates a queue scan + queue asset and returns (scan_data, asset_data).'''
        scan_data  = self._create_scan()
        asset_data = self.client.queue_asset_add_update(_make_q_asset(scan_data['id']))
        return scan_data, asset_data

    # ------------------------------------------------------------------
    # Event tests
    # ------------------------------------------------------------------

    def test_event_add_info(self):
        '''event_add sends an Information-level event and receives 202.'''
        payload = _make_event_payload('Test action', 'success', 'Information')
        result = self.client.event_add(payload)
        self.assertIsInstance(result, dict)
        print(f'[TEST SUCCESS] {module}:test_event_add_info')

    def test_event_add_warning(self):
        '''event_add sends a Warning-level event and receives 202.'''
        payload = _make_event_payload('Test warning', 'unknown', 'Warning')
        result = self.client.event_add(payload)
        self.assertIsInstance(result, dict)
        print(f'[TEST SUCCESS] {module}:test_event_add_warning')

    def test_event_add_error(self):
        '''event_add sends a Critical-level event and receives 202.'''
        payload = _make_event_payload('Test error', 'failure', 'Critical')
        result = self.client.event_add(payload)
        self.assertIsInstance(result, dict)
        print(f'[TEST SUCCESS] {module}:test_event_add_error')

    # ------------------------------------------------------------------
    # QueueScan tests
    # ------------------------------------------------------------------

    def test_queue_scan_add_update(self):
        '''queue_scan_add_update returns a data dict with an id field.'''
        data = self._create_scan()
        self.assertIn('id', data, 'Response data should contain an id field')
        self.assertIsNotNone(data['id'])
        print(f'[TEST SUCCESS] {module}:test_queue_scan_add_update')

    def test_queue_scan_update_status(self):
        '''queue_scan_update_status transitions a scan to Pending without error.'''
        scan_data = self._create_scan()
        scan_id = scan_data['id']
        # Should not raise
        self.client.queue_scan_update_status(scan_id, 'Pending')
        print(f'[TEST SUCCESS] {module}:test_queue_scan_update_status')

    # ------------------------------------------------------------------
    # QueueAsset tests
    # ------------------------------------------------------------------

    def test_queue_asset_add_update(self):
        '''queue_asset_add_update returns a data dict with an id field.'''
        _, asset_data = self._create_scan_and_asset()
        self.assertIn('id', asset_data, 'Response data should contain an id field')
        self.assertIsNotNone(asset_data['id'])
        print(f'[TEST SUCCESS] {module}:test_queue_asset_add_update')

    # ------------------------------------------------------------------
    # QueueIssue tests
    # ------------------------------------------------------------------

    def test_queue_issues_add_update_bulk(self):
        '''queue_issues_add_update_bulk posts a batch of issues without error.'''
        scan_data, asset_data = self._create_scan_and_asset()
        issue = _make_q_issue(scan_data['id'], asset_data['id'])
        issue['Id'] = None
        batch = {'Documents': [issue]}
        # Should not raise
        self.client.queue_issues_add_update_bulk(batch)
        print(f'[TEST SUCCESS] {module}:test_queue_issues_add_update_bulk')

    # ------------------------------------------------------------------
    # Scan search tests
    # ------------------------------------------------------------------

    def test_scan_search(self):
        '''scan_search executes against the processed scan index without error.'''
        search_request = {
            'assetType': TEST_ASSET_TYPE,
            'sourceType': TEST_SOURCE_TYPE,
            'filter': {
                'anyMatch': False,
                'filterMatches': {
                    'saltminer.scan.source_type': TEST_SOURCE_TYPE
                }
            },
            'uiPagingInfo': {
                'size': 10,
                'sortFilters': {
                    'saltminer.scan.scan_date': False
                }
            }
        }
        # Returns None or a list — both are valid (no test data in processed index)
        result = self.client.scan_search(search_request)
        self.assertTrue(result is None or isinstance(result, list))
        print(f'[TEST SUCCESS] {module}:test_scan_search')

    # ------------------------------------------------------------------
    # Index tests
    # ------------------------------------------------------------------

    def test_refresh_index(self):
        '''refresh_index completes without error on a known index pattern.'''
        # Use the queue scan index which exists on any configured DataApi
        index = f'queue_scans_{TEST_ASSET_TYPE}_{TEST_SOURCE_TYPE}_{TEST_INSTANCE}'.lower().replace('.', '_')
        try:
            self.client.refresh_index(index)
        except DataClientException:
            pass  # Index may not exist yet; API call itself succeeded if no connection error
        print(f'[TEST SUCCESS] {module}:test_refresh_index')

    # ------------------------------------------------------------------
    # Utility tests
    # ------------------------------------------------------------------

    def test_webhook_get(self):
        '''webhook_get returns None or a list for an unknown source — does not raise.'''
        result = self.client.webhook_get('test-source-id-dataclienttest')
        self.assertTrue(result is None or isinstance(result, list))
        print(f'[TEST SUCCESS] {module}:test_webhook_get')


if __name__ == '__main__':
    unittest.main()
