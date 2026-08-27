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
from unittest.mock import MagicMock

from Core.DataClient import QueueStatus
from Sources.Template.TemplateAdapter import TemplateAdapter
from Sources.Template.TemplateClient import MockTemplateClient


SCAN_ID = "qscan-001"
ASSET_ID = "qasset-001"


class _StubSettings:
    def GetSource(self, source_name, key, default=None):
        return default


class _StubApp:
    Settings = _StubSettings()


class _Boom(Exception):
    ''' A distinct vendor/mapping failure type so propagation is unambiguous. '''


def make_data_client() -> MagicMock:
    ''' A mock DataClient whose scan/asset creates return DataApi-shaped docs. '''
    dc = MagicMock(name="DataClient")
    dc.queue_scan_add_update.return_value = {
        "id": SCAN_ID, "saltminer": {"scan": {"reportId": "report-001"}}}
    dc.queue_asset_add_update.return_value = {"id": ASSET_ID}
    return dc


def make_adapter(data_client=None, dry_run=False) -> TemplateAdapter:
    return TemplateAdapter(_StubApp(), "TEMPLATE1", data_client=data_client, dry_run=dry_run)


def status_calls(dc: MagicMock):
    return [c.args for c in dc.queue_scan_update_status.call_args_list]


def _interrupting_issues(exc):
    ''' Yields one real issue, then raises `exc` from inside the issues loop. '''
    yield MockTemplateClient.MOCK_ISSUES["mock-asset-001"][0]
    raise exc


class SyncAssetCancelOnFailureTests(unittest.TestCase):
    """sync_asset() hands an abandoned Loading scan to cleanup as Cancel (mock DataClient)."""

    def setUp(self):
        self.asset = MockTemplateClient.MOCK_ASSETS[0]
        self.issues = MockTemplateClient.MOCK_ISSUES["mock-asset-001"]

    # 1. mid-chain failure -> Cancel, original exception propagates
    def test_mapping_failure_cancels_scan_and_reraises(self):
        dc = make_data_client()
        adapter = make_adapter(dc)
        bad_issue = {k: v for k, v in self.issues[0].items() if k != "id"}
        with self.assertRaises(KeyError):  # map_issue builds Scanner.Id from issue["id"]
            adapter.sync_asset(self.asset, [bad_issue])
        self.assertEqual(status_calls(dc), [(SCAN_ID, QueueStatus.CANCEL)])
        self.assertEqual(QueueStatus.CANCEL, "Cancel")

    def test_vendor_failure_in_issue_stream_cancels_scan(self):
        dc = make_data_client()
        adapter = make_adapter(dc)
        with self.assertRaises(_Boom):
            adapter.sync_asset(self.asset, _interrupting_issues(_Boom("vendor 500")))
        self.assertEqual(status_calls(dc), [(SCAN_ID, QueueStatus.CANCEL)])

    def test_failure_setting_pending_still_cancels(self):
        dc = make_data_client()
        dc.queue_scan_update_status.side_effect = [_Boom("pending failed"), None]
        adapter = make_adapter(dc)
        with self.assertRaises(_Boom):
            adapter.sync_asset(self.asset, self.issues)
        self.assertEqual(status_calls(dc),
                         [(SCAN_ID, QueueStatus.PENDING), (SCAN_ID, QueueStatus.CANCEL)])

    # 2. cancel itself fails -> logged, original exception still propagates
    def test_cancel_failure_is_logged_not_raised(self):
        dc = make_data_client()
        dc.queue_scan_update_status.side_effect = RuntimeError("DataApi down")
        adapter = make_adapter(dc)
        with self.assertLogs("Sources.Template.TemplateAdapter", level="ERROR") as logs:
            with self.assertRaises(_Boom):
                adapter.sync_asset(self.asset, _interrupting_issues(_Boom("vendor 500")))
        self.assertEqual(status_calls(dc), [(SCAN_ID, QueueStatus.CANCEL)])
        self.assertTrue(any("Could not cancel abandoned scan qscan-001" in m for m in logs.output))

    # 3. KeyboardInterrupt -> Cancel fired, the interrupt itself propagates unwrapped
    def test_keyboard_interrupt_cancels_scan_and_propagates(self):
        dc = make_data_client()
        adapter = make_adapter(dc)
        with self.assertRaises(KeyboardInterrupt):
            adapter.sync_asset(self.asset, _interrupting_issues(KeyboardInterrupt()))
        self.assertEqual(status_calls(dc), [(SCAN_ID, QueueStatus.CANCEL)])

    def test_system_exit_cancels_scan_and_propagates(self):
        dc = make_data_client()
        adapter = make_adapter(dc)
        with self.assertRaises(SystemExit):
            adapter.sync_asset(self.asset, _interrupting_issues(SystemExit(1)))
        self.assertEqual(status_calls(dc), [(SCAN_ID, QueueStatus.CANCEL)])

    # 4. success -> Pending, never Cancel
    def test_success_sets_pending_and_never_cancels(self):
        dc = make_data_client()
        adapter = make_adapter(dc)
        count = adapter.sync_asset(self.asset, self.issues)
        self.assertEqual(count, 1)
        self.assertEqual(status_calls(dc), [(SCAN_ID, QueueStatus.PENDING)])
        # flush (None) after the one issue
        self.assertIsNone(dc.queue_issue_add_update_batch.call_args_list[-1].args[0])

    # 5. dry run -> no scan, no status calls
    def test_dry_run_makes_no_status_calls(self):
        dc = make_data_client()
        adapter = make_adapter(dc, dry_run=True)
        count = adapter.sync_asset(self.asset, self.issues)
        self.assertEqual(count, 1)
        dc.queue_scan_add_update.assert_not_called()
        dc.queue_scan_update_status.assert_not_called()

    def test_dry_run_failure_has_nothing_to_cancel(self):
        adapter = make_adapter(None, dry_run=True)
        with self.assertRaises(_Boom):
            adapter.sync_asset(self.asset, _interrupting_issues(_Boom("vendor 500")))
        self.assertIsNone(adapter.data_client)

    def test_cancel_helper_without_client_is_a_noop(self):
        adapter = make_adapter(None, dry_run=True)
        adapter._cancel_abandoned_scan(SCAN_ID)  # must not raise or build a DataClient
        self.assertIsNone(adapter._data_client)


if __name__ == "__main__":
    unittest.main()
