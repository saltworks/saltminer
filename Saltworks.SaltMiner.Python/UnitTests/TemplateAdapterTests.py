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

import threading
import unittest
from unittest.mock import MagicMock, patch

import Sources.Template.TemplateAdapter as adapter_module

from Core.DataClient import QueueStatus
from Sources.Template.TemplateAdapter import SourceLoader, TemplateAdapter
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


class BuildGuiUrlTests(unittest.TestCase):
    """build_gui_url is the overridable hook behind Scanner.GuiUrl."""

    def test_default_returns_asset_gui_url(self):
        adapter = make_adapter(None, dry_run=True)
        asset = MockTemplateClient.MOCK_ASSETS[0]
        issue = MockTemplateClient.MOCK_ISSUES["mock-asset-001"][0]
        self.assertEqual(adapter.build_gui_url(asset, issue), asset["gui_url"])
        doc = adapter.map_issue(issue, "s", "a", "r", asset)
        self.assertEqual(doc["Vulnerability"]["Scanner"]["GuiUrl"], asset["gui_url"])

    def test_override_feeds_map_issue(self):
        class Composed(TemplateAdapter):
            def build_gui_url(self, asset, issue=None):
                return f"https://ui.example.com/{asset['id']}#{issue['id']}"
        adapter = Composed(_StubApp(), "TEMPLATE1", dry_run=True)
        asset = MockTemplateClient.MOCK_ASSETS[0]
        issue = MockTemplateClient.MOCK_ISSUES["mock-asset-001"][0]
        doc = adapter.map_issue(issue, "s", "a", "r", asset)
        self.assertEqual(doc["Vulnerability"]["Scanner"]["GuiUrl"],
                         "https://ui.example.com/mock-asset-001#mock-issue-001")

    def test_status_attribute_is_no_longer_written_by_default(self):
        adapter = make_adapter(None, dry_run=True)
        asset = MockTemplateClient.MOCK_ASSETS[0]
        issue = MockTemplateClient.MOCK_ISSUES["mock-asset-001"][0]
        doc = adapter.map_issue(issue, "s", "a", "r", asset)
        names = [a["Name"] if isinstance(a, dict) else a for a in doc["Saltminer"]["Attributes"]] \
            if isinstance(doc["Saltminer"]["Attributes"], list) else list(doc["Saltminer"]["Attributes"])
        self.assertNotIn("status", names)


# -- entry role: construction + config-driven run_sync --------------------------

class _ConfigSettings:
    def __init__(self, **cfg):
        self.cfg = cfg

    def GetSource(self, source_name, key, default=None):
        return self.cfg.get(key, default)


class _ConfigApp:
    def __init__(self, **cfg):
        self.Settings = _ConfigSettings(**cfg)


def _many_assets(count):
    return [dict(MockTemplateClient.MOCK_ASSETS[0], id=f"asset-{i:03d}") for i in range(count)]


class _ManyAssetsClient(MockTemplateClient):
    ''' Mock client with a configurable asset listing; issues come from a shared table. '''
    ASSETS = []
    ISSUES = {}
    FEED_FAILURE = None      # raised by get_assets_generator after the first asset

    def get_assets_generator(self):
        for i, asset in enumerate(self.ASSETS):
            if i == 1 and self.FEED_FAILURE is not None:
                raise self.FEED_FAILURE
            yield asset

    def get_issues_generator(self, asset_id):
        yield from self.ISSUES.get(asset_id, [MockTemplateClient.MOCK_ISSUES["mock-asset-001"][0]])


class EntryConstructionTests(unittest.TestCase):
    """TemplateAdapter's entry role: defaulted source_name, lazy client and loader."""

    def test_source_name_defaults_to_first_instance(self):
        adapter = TemplateAdapter(_StubApp())
        self.assertEqual(adapter._source_name, "TEMPLATE1")
        self.assertEqual(adapter.instance, "template1")

    def test_explicit_source_name_wins(self):
        adapter = TemplateAdapter(_StubApp(), "TEMPLATE2")
        self.assertEqual(adapter._source_name, "TEMPLATE2")

    def test_client_built_from_config_on_first_use(self):
        app = _StubApp()
        with patch.object(adapter_module, "TemplateClient") as client_cls:
            adapter = TemplateAdapter(app)
            self.assertIsNone(adapter._client)
            self.assertIs(adapter.client, client_cls.return_value)
            client_cls.assert_called_once_with(app.Settings, "TEMPLATE1")

    def test_injected_client_and_lazy_loader(self):
        client = MockTemplateClient(source_name="TEMPLATE1")
        adapter = TemplateAdapter(_StubApp(), client=client, dry_run=True)
        self.assertIs(adapter.client, client)
        self.assertIsNone(adapter._loader)
        loader = adapter.loader
        self.assertIsInstance(loader, SourceLoader)
        self.assertIs(adapter.loader, loader)


class RunSyncDispatchTests(unittest.TestCase):
    """run_sync reads Threaded / WorkerCount from config and dispatches, closing in a finally."""

    def setUp(self):
        _ManyAssetsClient.ASSETS = _many_assets(4)
        _ManyAssetsClient.FEED_FAILURE = None

    def _entry(self, **cfg):
        dc = make_data_client()
        adapter = TemplateAdapter(_ConfigApp(**cfg), client=_ManyAssetsClient(source_name="TEMPLATE1"),
                                  data_client=dc)
        return adapter, dc

    def test_threaded_false_runs_single_threaded_loader(self):
        adapter, dc = self._entry(Threaded=False)
        with patch.object(SourceLoader, "run", return_value={"marker": "run"}) as run, \
                patch.object(SourceLoader, "run_threaded") as run_threaded:
            self.assertEqual(adapter.run_sync(first_load=True), {"marker": "run"})
        run.assert_called_once_with(first_load=True)
        run_threaded.assert_not_called()
        dc.close.assert_called_once()

    def test_threaded_default_runs_thread_pool_with_worker_count(self):
        adapter, dc = self._entry(WorkerCount=7)   # Threaded omitted -> default true
        with patch.object(SourceLoader, "run") as run, \
                patch.object(SourceLoader, "run_threaded", return_value={"marker": "threaded"}) as run_threaded:
            self.assertEqual(adapter.run_sync(), {"marker": "threaded"})
        run.assert_not_called()
        run_threaded.assert_called_once_with(7, first_load=False, client_factory=_ManyAssetsClient)
        dc.close.assert_called_once()

    def test_threaded_string_false_from_config_is_honoured(self):
        adapter, _ = self._entry(Threaded="false")
        with patch.object(SourceLoader, "run", return_value={}) as run, \
                patch.object(SourceLoader, "run_threaded") as run_threaded:
            adapter.run_sync()
        run.assert_called_once()
        run_threaded.assert_not_called()

    def test_close_runs_even_when_the_loader_raises(self):
        adapter, dc = self._entry(Threaded=False)
        with patch.object(SourceLoader, "run", side_effect=_Boom("paging died")):
            with self.assertRaises(_Boom):
                adapter.run_sync()
        dc.close.assert_called_once()

    def test_end_to_end_both_paths_dry_run(self):
        ''' The folded main() shape: mock client + dry run through run_sync, no sends. '''
        for threaded in (False, True):
            adapter = TemplateAdapter(_ConfigApp(Threaded=threaded, WorkerCount=2),
                                      client=_ManyAssetsClient(source_name="TEMPLATE1"), dry_run=True)
            self.assertEqual(adapter.run_sync(first_load=True),
                             {"skipped": 0, "completed": 4, "errored": 0, "issues": 4},
                             msg=f"Threaded={threaded}")
            self.assertIsNone(adapter._data_client)


# -- in-memory threading (run_threaded) ----------------------------------------

class _RecordingRun:
    '''
    While active, every TemplateAdapter the loader builds per thread gets its
    own mock DataClient, and the run records which thread drove each adapter.
    '''

    def __init__(self, failing_asset_ids=()):
        self.adapters = []
        self.threads_by_adapter = {}
        self.failing = set(failing_asset_ids)
        self.lock = threading.Lock()

    def __enter__(self):
        run = self

        class Recording(TemplateAdapter):
            def __init__(self, app, source_name=None, data_client=None, client=None, dry_run=False):
                super().__init__(app, source_name, data_client=make_data_client(),
                                 client=client, dry_run=dry_run)
                with run.lock:
                    run.adapters.append(self)

            def sync_asset(self, asset, issues_iterable):
                with run.lock:
                    run.threads_by_adapter.setdefault(id(self), set()).add(threading.get_ident())
                if asset["id"] in run.failing:
                    raise _Boom(f"vendor failure on {asset['id']}")
                return super().sync_asset(asset, issues_iterable)

        self._patch = patch.object(adapter_module, "TemplateAdapter", Recording)
        self._patch.start()
        return self

    def __exit__(self, *exc):
        self._patch.stop()


def make_loader(client):
    return SourceLoader(_StubApp(), client, make_adapter(make_data_client()), "TEMPLATE1")


def _worker_threads_alive():
    return [t for t in threading.enumerate() if t.name.startswith("TEMPLATE1-worker")]


class RunThreadedTests(unittest.TestCase):
    """In-memory fan-out: per-thread adapters, per-asset failure boundary, graceful stop."""

    def setUp(self):
        _ManyAssetsClient.ASSETS = _many_assets(12)
        _ManyAssetsClient.ISSUES = {}
        _ManyAssetsClient.FEED_FAILURE = None

    def test_every_asset_synced_once_with_distinct_adapters_per_thread(self):
        loader = make_loader(_ManyAssetsClient(source_name="TEMPLATE1"))
        with _RecordingRun() as run:
            summary = loader.run_threaded(3, first_load=True, client_factory=_ManyAssetsClient)

        self.assertEqual(summary, {"skipped": 0, "completed": 12, "errored": 0, "issues": 12})
        # one adapter per worker thread, all distinct, each with its own DataClient
        self.assertEqual(len(run.adapters), 3)
        self.assertEqual(len({id(a) for a in run.adapters}), 3)
        self.assertEqual(len({id(a._data_client) for a in run.adapters}), 3)
        # an adapter is only ever driven from a single thread
        for threads in run.threads_by_adapter.values():
            self.assertEqual(len(threads), 1)
        # every asset went through exactly one adapter's DataClient, then Pending
        scans = sum(a._data_client.queue_scan_add_update.call_count for a in run.adapters)
        self.assertEqual(scans, 12)
        for adapter in run.adapters:
            for call in adapter._data_client.queue_scan_update_status.call_args_list:
                self.assertEqual(call.args[1], QueueStatus.PENDING)
            adapter._data_client.close.assert_called_once()   # closed on thread exit
        self.assertFalse(_worker_threads_alive())

    def test_single_asset_failure_is_counted_and_does_not_stop_the_run(self):
        loader = make_loader(_ManyAssetsClient(source_name="TEMPLATE1"))
        with _RecordingRun(failing_asset_ids={"asset-004", "asset-007"}):
            with self.assertLogs("Sources.Template.TemplateAdapter", level="ERROR") as logs:
                summary = loader.run_threaded(2, first_load=True, client_factory=_ManyAssetsClient)
        self.assertEqual(summary, {"skipped": 0, "completed": 10, "errored": 2, "issues": 10})
        self.assertEqual(sum("failed" in m for m in logs.output), 2)

    def test_feed_interrupt_stops_feeding_joins_workers_and_reraises(self):
        _ManyAssetsClient.FEED_FAILURE = KeyboardInterrupt()
        loader = make_loader(_ManyAssetsClient(source_name="TEMPLATE1"))
        with _RecordingRun() as run:
            with self.assertRaises(KeyboardInterrupt):
                loader.run_threaded(2, first_load=True, client_factory=_ManyAssetsClient)
        # workers were joined and closed; the one fed asset finished or never started
        self.assertFalse(_worker_threads_alive())
        for adapter in run.adapters:
            adapter._data_client.close.assert_called_once()
        synced = sum(a._data_client.queue_scan_add_update.call_count for a in run.adapters)
        self.assertLessEqual(synced, 1)

    def test_worker_adapters_inherit_dry_run_and_client_factory_defaults_to_TemplateClient(self):
        loader = SourceLoader(_StubApp(), _ManyAssetsClient(source_name="TEMPLATE1"),
                              make_adapter(None, dry_run=True), "TEMPLATE1")
        with patch.object(adapter_module, "TemplateClient", _ManyAssetsClient):
            summary = loader.run_threaded(2, first_load=True)
        self.assertEqual(summary, {"skipped": 0, "completed": 12, "errored": 0, "issues": 12})

    def test_run_and_run_threaded_return_the_same_summary_shape(self):
        client = _ManyAssetsClient(source_name="TEMPLATE1")
        loader = SourceLoader(_StubApp(), client, make_adapter(None, dry_run=True), "TEMPLATE1")
        self.assertEqual(loader.run(first_load=True),
                         loader.run_threaded(2, first_load=True, client_factory=_ManyAssetsClient))


if __name__ == "__main__":
    unittest.main()
