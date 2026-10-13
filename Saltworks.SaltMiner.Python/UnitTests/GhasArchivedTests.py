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


import asyncio
import copy
import json
import os
import unittest
from unittest.mock import MagicMock

from Sources.GHAS import GHASAdapter as adapter_module
from Sources.GHAS.GHASAdapter import GHASAdapter, ArchivedWriteError, _SmIssueWriter
from Sources.GHAS import ghas_orphan_cleanup as oc

module = os.path.splitext(os.path.basename(__file__))[0]

REPO = {"id": 4242, "full_name": "acme/old-repo", "archived": True}


def make_issue(n, status=None, suppressed=False, alert_state="open", removed=None):
    attrs = {"ghas_alert_state": alert_state}
    if status:
        attrs["issue_status"] = status
    return {
        "id": f"issue-{n}",
        "lastUpdated": "2026-01-01T00:00:00Z",
        "vulnerability": {"isSuppressed": suppressed, "removedDate": removed, "foundDate": "2025-12-01T00:00:00Z"},
        "saltminer": {"attributes": attrs, "scan": {"id": "scan-1"}},
    }


class FakeState:
    def __init__(self, watermarks=None):
        self.watermarks = dict(watermarks or {})
        self.saved = 0

    def get_watermark(self, full_name, engine):
        return self.watermarks.get(f"{full_name}/{engine}")

    def clear_watermark(self, full_name, engine):
        self.watermarks.pop(f"{full_name}/{engine}", None)

    async def save_async(self):
        self.saved += 1


class FakeWriter:
    """Stands in for _SmIssueWriter: no network."""
    batch_size = 100

    def __init__(self, issues, fail_bulk=None):
        self.issues = issues
        self.fail_bulk = fail_bulk
        self.searches = []
        self.sent = []

    def search_scope_issues(self, search):
        self.searches.append(search)
        return iter(copy.deepcopy(self.issues))

    def bulk_update(self, docs):
        if self.fail_bulk:
            raise self.fail_bulk
        self.sent.append(copy.deepcopy(docs))


def make_adapter(issues, rebaseline=False, watermark=True, fail_bulk=None):
    a = GHASAdapter.__new__(GHASAdapter)
    a.instance = "ghas1"
    a._is_rebaseline = rebaseline
    a._state = FakeState({"acme/old-repo/code_scanning": "2026-01-01T00:00:00Z"} if watermark else {})
    a._state_lock = asyncio.Lock()
    a._queue_lock = asyncio.Lock()
    a._archived_suppressed_scopes = []
    a._write_failed_scopes = []
    a._issue_writer = FakeWriter(issues, fail_bulk)
    a._data_client = MagicMock()
    a.map_scan = MagicMock()
    return a


def run(coro):
    return asyncio.run(coro)


class ArchivedPathTests(unittest.TestCase):

    def test_archived_never_requests_replace(self):
        a = make_adapter([make_issue(1)])
        run(a._suppress_archived_async(REPO, "code_scanning"))
        a.map_scan.assert_not_called()
        a._data_client.AddQueueScan.assert_not_called()
        a._data_client.AddQueueAsset.assert_not_called()
        for call in a.map_scan.call_args_list:
            self.assertFalse(call.kwargs.get("replace", False))
        print(f"[TEST SUCCESS] {module}:test_archived_never_requests_replace")

    def test_marks_suppressed_and_issue_status(self):
        original = make_issue(1, alert_state="fixed", removed="2026-02-02T00:00:00Z")
        a = make_adapter([original])
        run(a._suppress_archived_async(REPO, "code_scanning"))
        self.assertEqual(len(a._issue_writer.sent), 1)
        doc = a._issue_writer.sent[0][0]
        self.assertIs(doc["vulnerability"]["isSuppressed"], True)
        self.assertEqual(doc["saltminer"]["attributes"]["issue_status"], "archived")
        self.assertEqual(doc["vulnerability"]["removedDate"], original["vulnerability"]["removedDate"])
        self.assertEqual(doc["vulnerability"]["foundDate"], original["vulnerability"]["foundDate"])
        self.assertEqual(doc["saltminer"]["attributes"]["ghas_alert_state"], "fixed")
        self.assertEqual(a._archived_suppressed_scopes, [("acme/old-repo/code_scanning", 1, 0)])
        self.assertEqual(a._state.watermarks, {})
        self.assertEqual(a._write_failed_scopes, [])
        print(f"[TEST SUCCESS] {module}:test_marks_suppressed_and_issue_status")

    def test_issue_with_no_removed_date_gains_none(self):
        a = make_adapter([make_issue(1)])
        run(a._suppress_archived_async(REPO, "code_scanning"))
        self.assertIsNone(a._issue_writer.sent[0][0]["vulnerability"]["removedDate"])
        print(f"[TEST SUCCESS] {module}:test_issue_with_no_removed_date_gains_none")

    def test_skips_already_archived(self):
        a = make_adapter([make_issue(1, status="archived", suppressed=True),
                          make_issue(2, status="archived", suppressed=True),
                          make_issue(3)])
        run(a._suppress_archived_async(REPO, "code_scanning"))
        self.assertEqual([d["id"] for d in a._issue_writer.sent[0]], ["issue-3"])
        self.assertEqual(a._archived_suppressed_scopes[0][1:], (1, 2))

        b = make_adapter([make_issue(1, status="archived", suppressed=True)])
        run(b._suppress_archived_async(REPO, "code_scanning"))
        self.assertEqual(b._issue_writer.sent, [])
        self.assertEqual(b._state.watermarks, {})
        print(f"[TEST SUCCESS] {module}:test_skips_already_archived")

    def test_sarif_suppressed_still_marked(self):
        a = make_adapter([make_issue(1, suppressed=True, alert_state="suppressed")])
        run(a._suppress_archived_async(REPO, "code_scanning"))
        doc = a._issue_writer.sent[0][0]
        self.assertEqual(doc["saltminer"]["attributes"]["issue_status"], "archived")
        self.assertEqual(doc["saltminer"]["attributes"]["ghas_alert_state"], "suppressed")
        print(f"[TEST SUCCESS] {module}:test_sarif_suppressed_still_marked")

    def test_search_filter_is_the_scope(self):
        a = make_adapter([], rebaseline=True)
        run(a._suppress_archived_async(REPO, "dependabot"))
        search = a._issue_writer.searches[0]
        self.assertEqual(search["instance"], "ghas1")
        fm = search["filter"]["filterMatches"]
        self.assertEqual(fm["Saltminer.Asset.SourceId"], "4242")
        self.assertEqual(fm["Saltminer.Scan.AssessmentType"], "Open")
        print(f"[TEST SUCCESS] {module}:test_search_filter_is_the_scope")

    def test_incremental_no_watermark_skips(self):
        a = make_adapter([make_issue(1)], rebaseline=False, watermark=False)
        run(a._suppress_archived_async(REPO, "code_scanning"))
        self.assertEqual(a._issue_writer.searches, [])
        self.assertEqual(a._issue_writer.sent, [])
        print(f"[TEST SUCCESS] {module}:test_incremental_no_watermark_skips")

    def test_rebaseline_fires(self):
        a = make_adapter([make_issue(1)], rebaseline=True, watermark=False)
        run(a._suppress_archived_async(REPO, "code_scanning"))
        self.assertEqual(len(a._issue_writer.sent), 1)
        print(f"[TEST SUCCESS] {module}:test_rebaseline_fires")

    def test_failure_keeps_watermark(self):
        a = make_adapter([make_issue(1)], fail_bulk=ArchivedWriteError("issue/bulk returned HTTP 500"))
        run(a._suppress_archived_async(REPO, "code_scanning"))
        self.assertIn("acme/old-repo/code_scanning", a._state.watermarks)
        self.assertEqual(len(a._write_failed_scopes), 1)
        self.assertEqual(a._archived_suppressed_scopes, [])
        print(f"[TEST SUCCESS] {module}:test_failure_keeps_watermark")

    def test_batches_follow_batch_size(self):
        a = make_adapter([make_issue(i) for i in range(5)])
        a._issue_writer.batch_size = 2
        run(a._suppress_archived_async(REPO, "code_scanning"))
        self.assertEqual([len(b) for b in a._issue_writer.sent], [2, 2, 1])
        print(f"[TEST SUCCESS] {module}:test_batches_follow_batch_size")


class FakeResponse:
    def __init__(self, status, body):
        self.status_code = status
        self.text = json.dumps(body)


class FakeRest:
    def __init__(self, responses):
        self.responses = list(responses)
        self.requests = []

    def Post(self, url, body=None, **kwargs):
        self.requests.append((url, copy.deepcopy(body)))
        return self.responses.pop(0)


def make_writer(responses):
    w = _SmIssueWriter.__new__(_SmIssueWriter)
    w.batch_size = 100
    w._client = FakeRest(responses)
    return w


class WriterTests(unittest.TestCase):

    def test_bulk_202_with_rejected_docs_keeps_watermark(self):
        bad = FakeResponse(202, {"success": False, "errorType": "Error", "affected": 0,
                                 "errorMessages": ["Please see Bulk Errors"],
                                 "bulkErrors": {"issue-1": "mapper_parsing_exception"}})
        a = make_adapter([make_issue(1)])
        a._issue_writer = make_writer([FakeResponse(200, {"data": [make_issue(1)], "pagingInfo": {}}), bad])
        run(a._suppress_archived_async(REPO, "code_scanning"))
        self.assertIn("acme/old-repo/code_scanning", a._state.watermarks)
        self.assertEqual(len(a._write_failed_scopes), 1)
        self.assertIn("mapper_parsing_exception", a._write_failed_scopes[0][1])
        self.assertEqual(a._archived_suppressed_scopes, [])
        print(f"[TEST SUCCESS] {module}:test_bulk_202_with_rejected_docs_keeps_watermark")

    def test_bulk_affected_short_raises(self):
        w = make_writer([FakeResponse(202, {"affected": 1})])
        with self.assertRaises(ArchivedWriteError):
            w.bulk_update([{"id": "a"}, {"id": "b"}])
        print(f"[TEST SUCCESS] {module}:test_bulk_affected_short_raises")

    def test_bulk_clean_202_passes(self):
        w = make_writer([FakeResponse(202, {"affected": 2, "success": True})])
        w.bulk_update([{"id": "a"}, {"id": "b"}])
        self.assertEqual(w._client.requests[0][0], "issue/bulk")
        self.assertEqual(len(w._client.requests[0][1]["Documents"]), 2)
        print(f"[TEST SUCCESS] {module}:test_bulk_clean_202_passes")

    def test_bulk_non_202_raises(self):
        w = make_writer([FakeResponse(500, {"errorType": "EsClientException"})])
        with self.assertRaises(ArchivedWriteError):
            w.bulk_update([{"id": "a"}])
        print(f"[TEST SUCCESS] {module}:test_bulk_non_202_raises")

    def test_search_pages_past_first_page(self):
        page1 = [make_issue(i) for i in range(500)]
        page2 = [make_issue(i) for i in range(500, 1000)]
        page3 = [make_issue(i) for i in range(1000, 1003)]
        w = make_writer([
            FakeResponse(200, {"data": page1, "pagingInfo": {"page": 1, "enablePit": True, "pitPagingToken": "pit-1", "nextAfterKeys": ["k500"]}}),
            FakeResponse(200, {"data": page2, "pagingInfo": {"page": 2, "enablePit": True, "pitPagingToken": "pit-2", "nextAfterKeys": ["k1000"]}}),
            FakeResponse(200, {"data": page3, "pagingInfo": {"page": 3, "enablePit": True, "pitPagingToken": "pit-3", "nextAfterKeys": None}}),
        ])
        got = list(w.search_scope_issues({"assetType": "app"}))
        self.assertEqual(len(got), 1003)
        reqs = [r[1]["pagingInfo"] for r in w._client.requests]
        self.assertEqual(len(reqs), 3)
        self.assertEqual(reqs[0], {"page": 1, "size": 500, "enablePit": True})
        self.assertEqual(reqs[1]["page"], 2)
        self.assertEqual(reqs[1]["currentAfterKeys"], ["k500"])
        self.assertEqual(reqs[1]["pitPagingToken"], "pit-1")
        self.assertIsNone(reqs[1]["nextAfterKeys"])
        self.assertEqual(reqs[2]["page"], 3)
        self.assertEqual(reqs[2]["currentAfterKeys"], ["k1000"])
        self.assertEqual(reqs[2]["pitPagingToken"], "pit-2")

        a = make_adapter([])
        a._issue_writer = make_writer([
            FakeResponse(200, {"data": page1, "pagingInfo": {"page": 1, "pitPagingToken": "p", "nextAfterKeys": ["a"]}}),
            FakeResponse(200, {"data": page3, "pagingInfo": {"page": 2, "nextAfterKeys": None}}),
        ] + [FakeResponse(202, {"affected": 100})] * 6)
        run(a._suppress_archived_async(REPO, "code_scanning"))
        marked = sum(len(r[1]["Documents"]) for r in a._issue_writer._client.requests if r[0] == "issue/bulk")
        self.assertEqual(marked, 503)
        print(f"[TEST SUCCESS] {module}:test_search_pages_past_first_page")

    def test_search_no_progress_raises(self):
        page = [make_issue(i) for i in range(500)]
        body = {"data": page, "pagingInfo": {"page": 1, "pitPagingToken": "p", "nextAfterKeys": ["a"]}}
        w = make_writer([FakeResponse(200, body), FakeResponse(200, body)])
        with self.assertRaises(ArchivedWriteError):
            list(w.search_scope_issues({}))

        a = make_adapter([])
        a._issue_writer = make_writer([FakeResponse(200, body), FakeResponse(200, body)])
        run(a._suppress_archived_async(REPO, "code_scanning"))
        self.assertIn("acme/old-repo/code_scanning", a._state.watermarks)
        self.assertEqual(len(a._write_failed_scopes), 1)
        print(f"[TEST SUCCESS] {module}:test_search_no_progress_raises")

    def test_search_non_200_raises(self):
        w = make_writer([FakeResponse(403, {})])
        with self.assertRaises(ArchivedWriteError):
            list(w.search_scope_issues({}))
        print(f"[TEST SUCCESS] {module}:test_search_non_200_raises")

    def test_missing_manager_key_is_write_failed_not_deleted(self):
        settings = MagicMock()
        settings.Get.side_effect = lambda cfg, key, default=None: {"ApiUrl": "https://sm"}.get(key, default)
        a = make_adapter([])
        a._issue_writer = None
        a._settings = settings
        run(a._suppress_archived_async(REPO, "code_scanning"))
        self.assertIn("acme/old-repo/code_scanning", a._state.watermarks)
        self.assertEqual(len(a._write_failed_scopes), 1)
        self.assertIn("ManagerApiKey", a._write_failed_scopes[0][1])
        a._data_client.AddQueueScan.assert_not_called()
        print(f"[TEST SUCCESS] {module}:test_missing_manager_key_is_write_failed_not_deleted")


class FakeES(oc.ESRest):
    """ESRest without a network: scan() serves canned hits, _req records deletes."""

    def __init__(self, archived_issue_hits, issue_candidates, asset_candidates, sibling_present=True):
        self.archived_issue_hits = archived_issue_hits
        self.issue_candidates = issue_candidates
        self.asset_candidates_hits = asset_candidates
        self.deletes = []

    def scan(self, index, query, source_fields, page_size=1000):
        if index.startswith("issues") and "is_suppressed" in json.dumps(query):
            return iter(self.archived_issue_hits)
        if index.startswith("issues"):
            return iter(self.issue_candidates)
        return iter(self.asset_candidates_hits)

    def _req(self, method, path, body=None, params=None):
        if path.endswith("_delete_by_query"):
            self.deletes.append((path, body["query"]["ids"]["values"]))
            return {"deleted": len(body["query"]["ids"]["values"])}
        raise AssertionError(f"unexpected request {method} {path}")

    def latest_run_ts(self, index):
        return "2026-10-12T12:00:00Z"

    def count_retired_skipped(self, index, cutoff_iso):
        return 0


def ihit(_id, status=None, repo="acme/r", source_id="1"):
    attrs = {"ghas_repo_full_name": repo}
    if status:
        attrs["issue_status"] = status
    return {"_id": _id, "_source": {"saltminer": {"attributes": attrs, "asset": {"source_id": source_id}}}}


def ahit(_id, source_id, name="acme/r"):
    return {"_id": _id, "_source": {"saltminer": {"asset": {"source_id": source_id, "name": name}}}}


class CleanupTests(unittest.TestCase):

    def test_cleanup_drops_archived_candidates(self):
        es = FakeES([], [ihit("a", "archived"), ihit("b"), ihit("c", "archived"), ihit("d")], [])
        ids, kept, repos = es.candidate_ids("issues_app_saltworks.ghas_ghas1", "2026-10-12T10:00:00Z", True)
        self.assertEqual(ids, ["b", "d"])
        self.assertEqual(kept, 2)
        self.assertEqual(repos, {"acme/r": 2})
        print(f"[TEST SUCCESS] {module}:test_cleanup_drops_archived_candidates")

    def test_cleanup_apply_deletes_only_unarchived_issues(self):
        es = FakeES([], [ihit("a", "archived"), ihit("b")], [])
        r = oc.process_index(es, "issues_app_saltworks.ghas_ghas1", None, 120, True)
        self.assertEqual(es.deletes, [("/issues_app_saltworks.ghas_ghas1/_delete_by_query", ["b"])])
        self.assertEqual(r["archived_kept"], 1)
        dry = FakeES([], [ihit("a", "archived"), ihit("b")], [])
        oc.process_index(dry, "issues_app_saltworks.ghas_ghas1", None, 120, False)
        self.assertEqual(dry.deletes, [])
        print(f"[TEST SUCCESS] {module}:test_cleanup_apply_deletes_only_unarchived_issues")

    def test_cleanup_keeps_archived_asset(self):
        issues = "issues_app_saltworks.ghas_ghas1"
        assets = "assets_app_saltworks.ghas_ghas1"
        for apply in (False, True):
            es = FakeES([ihit("i1", "archived", source_id="X")], [], [ahit("aX", "X"), ahit("aY", "Y")])
            results = oc.run_all(es, [assets, issues], None, 120, apply)
            asset_result = [r for r in results if r["index"] == assets][0]
            self.assertEqual(asset_result["archived_kept"], 1)
            self.assertEqual(asset_result["orphans"], 1)
            if apply:
                self.assertEqual(es.deletes, [(f"/{assets}/_delete_by_query", ["aY"])])
            else:
                self.assertEqual(es.deletes, [])
        print(f"[TEST SUCCESS] {module}:test_cleanup_keeps_archived_asset")

    def test_archived_sources_ignore_sarif(self):
        es = FakeES([ihit("s1", None, source_id="S"),
                     ihit("c1", "archived", source_id="C")], [], [])
        self.assertEqual(es.archived_source_ids("issues_app_saltworks.ghas_ghas1"), {"C"})
        print(f"[TEST SUCCESS] {module}:test_archived_sources_ignore_sarif")

    def test_archived_source_query_uses_mapped_fields_only(self):
        seen = {}

        class Spy(FakeES):
            def scan(self, index, query, source_fields, page_size=1000):
                seen["query"] = query
                return iter([])
        Spy([], [], []).archived_source_ids("issues_app_saltworks.ghas_ghas1")
        text = json.dumps(seen["query"])
        self.assertIn("saltminer.asset.source_type", text)
        self.assertIn("vulnerability.is_suppressed", text)
        self.assertNotIn("issue_status", text)
        print(f"[TEST SUCCESS] {module}:test_archived_source_query_uses_mapped_fields_only")

    def test_issue_index_runs_before_asset_index(self):
        issues = "issues_app_saltworks.ghas_ghas1"
        assets = "assets_app_saltworks.ghas_ghas1"
        self.assertEqual(sorted([issues, assets]), [assets, issues])
        calls = []
        original = oc.process_index
        oc.process_index = lambda es, idx, *a, **k: calls.append(idx)
        try:
            es = FakeES([], [], [])
            oc.run_all(es, sorted([issues, assets]), None, 120, False)
        finally:
            oc.process_index = original
        self.assertEqual(calls, [issues, assets])
        print(f"[TEST SUCCESS] {module}:test_issue_index_runs_before_asset_index")

    def test_asset_protection_failure_deletes_nothing(self):
        issues = "issues_app_saltworks.ghas_ghas1"
        assets = "assets_app_saltworks.ghas_ghas1"

        class Broken(FakeES):
            def archived_source_ids(self, issue_index):
                raise RuntimeError("ES down")
        es = Broken([], [], [ahit("aY", "Y")])
        oc.run_all(es, [assets, issues], None, 120, True)
        self.assertEqual([d for d in es.deletes if "assets" in d[0]], [])
        print(f"[TEST SUCCESS] {module}:test_asset_protection_failure_deletes_nothing")



class _State:
    def __init__(self):
        self.watermarks = {"acme/old-repo/code_scanning": "2026-01-01T00:00:00Z"}

    def get_watermark(self, full_name, engine):
        return self.watermarks.get(f"{full_name}/{engine}")

    def clear_watermark(self, full_name, engine):
        self.watermarks.pop(f"{full_name}/{engine}", None)

    async def save_async(self):
        return None


class _Writer:
    batch_size = 100

    def search_scope_issues(self, search):
        return iter([])

    def bulk_update(self, docs):
        return None


class ArchivedReproTests(unittest.TestCase):
    """PBI-102 reproduction: an archived repository's findings must not be deleted.

    The delete is triggered by a scan queued with ReplaceIssues=True, which the
    adapter requests through map_scan(..., replace=True). On the 3.4.0 base this
    test fails because the archived path makes that call.
    """

    def test_archived_never_requests_replace(self):
        a = GHASAdapter.__new__(GHASAdapter)
        a.instance = "ghas1"
        a._is_rebaseline = False
        a._state = _State()
        a._state_lock = asyncio.Lock()
        a._queue_lock = asyncio.Lock()
        a._archived_suppressed_scopes = []
        a._archived_purged_scopes = []
        a._write_failed_scopes = []
        a._issue_writer = _Writer()
        a._data_client = MagicMock()
        scan = MagicMock()
        scan.model_dump_json.return_value = "{}"
        a.map_scan = MagicMock(return_value=scan)
        a.map_asset = MagicMock(return_value=scan)

        method = getattr(a, "_suppress_archived_async", None) or a._tombstone_archived_async
        try:
            asyncio.run(method({"id": 4242, "full_name": "acme/old-repo", "archived": True}, "code_scanning"))
        except Exception:
            pass

        for call in a.map_scan.call_args_list:
            self.assertFalse(call.kwargs.get("replace", False),
                             "archived path requested replace=True, which hard-deletes the scope's findings")
        print(f"[TEST SUCCESS] {module}:test_archived_never_requests_replace")


if __name__ == '__main__':
    unittest.main()
