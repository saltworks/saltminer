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
import datetime
import inspect
import os
import re
import unittest

from Core.DataClient import DataClientException, DataClientNotFoundException
from Reports import EngagementData
from Reports.EngagementData import (
    DataApiSource,
    ReportSettings,
    assemble_engagement,
    format_dotnet_date,
    load_report_settings,
)
from Reports.Tests import data_api_fixture as fx
from Reports.WordMerge import bind_roots

_HERE = os.path.dirname(os.path.abspath(__file__))
_PACKAGE = os.path.normpath(os.path.join(_HERE, ".."))

# The merge-field names of the shipped template: `inventory_template.py` prints 35 distinct data names.
TEMPLATE_NAMES = {
    "Name", "State", "Customer", "Timestamp", "Summary", "Id", "Description", "Severity", "Total",
    "Product", "Vendor", "ReportId", "AssetId", "FoundDate", "TestStatus", "TestingInstructions",
    "IsSuppressed", "IsActive", "IsRemoved", "RemovedDate", "Location", "LocationFull", "Classification",
    "Enumeration", "Reference", "Proof", "Details", "Implication", "Recommendation", "References",
    "EngagementAttributes|tester", "IssueAttributes|tested_by", "IssueAttributes|influencers",
    "IssueAttributes|developer_note", "IssueAttributes|comments",
}

# The fields CreateReportEngagementDto and CreateIssueDetail build, per level of the record
# (ReportProcessor.cs:510-540 for the root, the TOC constructors at 544-640, CreateIssueDetail for issues).
_ISSUE_DETAIL_FIELDS = {
    "Name", "Description", "Product", "Vendor", "Severity", "SeverityLevel", "ReportId", "AssetId",
    "FoundDate", "TestStatus", "TestingInstructions", "IsSuppressed", "IsActive", "IsRemoved",
    "RemovedDate", "Location", "LocationFull", "Classification", "Enumeration", "Reference",
    "Proof", "ProofText", "ProofImgs", "Details", "DetailsText", "DetailsImgs",
    "Implication", "ImplicationText", "ImplicationImgs", "Recommendation", "RecommendationText",
    "RecommendationImgs", "References", "ReferencesText", "ReferencesImgs",
    "Comments", "AppVersion", "EngagementId", "Id",
}
DOTNET_FIELDS = {
    "root": {
        "Id", "Name", "State", "Customer", "Timestamp", "Summary", "PublishDate",
        "Critical", "High", "Medium", "Low", "Information", "Total",
        "Closed_Critical", "Closed_High", "Closed_Medium", "Closed_Low", "Closed_Information", "Closed_Total",
        "AssetTocs", "IssueTocs", "IssueDetails", "IssueDetailsRemoved", "IssueDetailsAll",
        "IssueSummary", "IssueSummaryRemoved", "IssueSummaryAll",
    },
    "AssetTocs": {"Id", "Name", "Description", "SeverityGroups"},
    "AssetTocs.SeverityGroups": {"Severity", "SeverityLevel", "Issues"},
    "AssetTocs.SeverityGroups.Issues": {"Name", "Total"},
    "IssueTocs": {"Severity", "SeverityLevel", "SeverityGroups"},
    "IssueTocs.SeverityGroups": {"Name", "Total"},
    **{name: _ISSUE_DETAIL_FIELDS for name in (
        "IssueDetails", "IssueDetailsRemoved", "IssueDetailsAll",
        "IssueSummary", "IssueSummaryRemoved", "IssueSummaryAll")},
}


def build(transport=None, **settings_kwargs):
    transport = transport or fx.FakeTransport()
    source = DataApiSource(None, transport)
    record, markdown = assemble_engagement(fx.ENGAGEMENT_ID, source, ReportSettings(**settings_kwargs))
    return record, markdown, transport


def field_names_by_level(record):
    """The field names present at each level of the record, keyed by dotted group path."""
    levels = {}

    def walk(node, path):
        names = levels.setdefault(path, set())
        for key, value in node.items():
            names.add(key)
            if isinstance(value, list):
                for child in value:
                    walk(child, key if path == "root" else f"{path}.{key}")

    walk(record, "root")
    return levels


def leaves(node):
    if isinstance(node, dict):
        for value in node.values():
            yield from leaves(value)
    elif isinstance(node, list):
        for value in node:
            yield from leaves(value)
    else:
        yield node


def issue_by_name(record, name, group="IssueDetails"):
    return next(i for i in record[group] if i["Name"] == name)


class GroupsAndShape(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.record, cls.markdown, cls.transport = build()

    def test_group_counts(self):
        record = self.record
        self.assertEqual(len(bind_roots(record)["Section1"]), 1)
        self.assertEqual(len(record["AssetTocs"]), 2)
        self.assertEqual(len(record["IssueDetails"]), 3)
        self.assertTrue(all(t["SeverityGroups"] for t in record["AssetTocs"]))
        self.assertTrue(all(t["SeverityGroups"] for t in record["IssueTocs"]))
        self.assertEqual(len(record["IssueDetailsRemoved"]), 1)
        self.assertEqual(len(record["IssueDetailsAll"]), 4)

    def test_contains_dotnet_fields(self):
        levels = field_names_by_level(self.record)
        for level, expected in DOTNET_FIELDS.items():
            self.assertIn(level, levels)
            self.assertLessEqual(expected, levels[level], f"{level} lacks {sorted(expected - levels[level])}")
        self.assertEqual(len(TEMPLATE_NAMES), 35)
        every_name = set().union(*levels.values())
        self.assertLessEqual(TEMPLATE_NAMES, every_name, sorted(TEMPLATE_NAMES - every_name))
        self.assertLessEqual({"EngagementAttributes|tester"}, levels["root"])
        self.assertLessEqual({f"IssueAttributes|{k}" for k in ("tested_by", "influencers", "developer_note",
                                                                "comments")}, levels["IssueDetails"])

    def test_no_template_input(self):
        parameters = inspect.signature(assemble_engagement).parameters
        self.assertEqual(list(parameters), ["engagement_id", "source", "settings"])
        with open(os.path.join(_PACKAGE, "EngagementData.py"), encoding="utf-8") as f:
            text = f.read().lower()
        self.assertNotIn("docx", text)
        self.assertNotIn("template_path", text)

    def test_every_leaf_is_a_string(self):
        for leaf in leaves(self.record):
            self.assertIsInstance(leaf, str)

    def test_summary_lists_share_the_detail_records(self):
        self.assertIs(self.record["IssueSummary"][0], self.record["IssueDetails"][0])
        self.assertEqual(self.record["IssueSummaryAll"], self.record["IssueDetailsAll"])

    def test_engagement_and_issue_values(self):
        record = self.record
        self.assertEqual(record["Id"], fx.ENGAGEMENT_ID)
        self.assertEqual(record["Name"], "Quarterly web test")
        self.assertEqual(record["State"], "Published")
        self.assertEqual(record["Customer"], "Example Customer")
        self.assertEqual(record["Timestamp"], "03/01/2026")
        self.assertEqual(record["PublishDate"], "March 20, 2026")
        high = issue_by_name(record, "SQL Injection")
        self.assertEqual(high["FoundDate"], "2026/03/03")
        self.assertEqual(high["RemovedDate"], "")
        self.assertEqual(high["Location"], "/login")
        self.assertEqual(high["LocationFull"], "https://example.invalid/login")
        self.assertEqual(high["Vendor"], "Example Vendor")
        self.assertEqual(high["Product"], "Example Scanner")
        self.assertEqual(high["SeverityLevel"], "2")
        self.assertEqual((high["IsActive"], high["IsRemoved"], high["IsSuppressed"]), ("True", "False", "False"))
        self.assertEqual(high["AssetId"], fx.ASSET_ZULU)
        self.assertEqual(high["EngagementId"], fx.ENGAGEMENT_ID)
        self.assertEqual(high["Id"], fx.ISSUE_HIGH)
        removed = issue_by_name(record, "Missing HSTS", "IssueDetailsRemoved")
        self.assertEqual((removed["RemovedDate"], removed["IsRemoved"], removed["IsActive"]),
                         ("2026/03/10", "True", "False"))

    def test_markdown_text_and_images(self):
        high = issue_by_name(self.record, "SQL Injection")
        self.assertEqual(high["Proof"], "Proof text ![shot](https://example.invalid/a.png) more proof")
        self.assertEqual(high["ProofText"], "Proof text  more proof")
        self.assertEqual(high["ProofImgs"], "![shot](https://example.invalid/a.png)")
        self.assertEqual(high["RecommendationText"], "Fix it")
        self.assertEqual(high["RecommendationImgs"], "![a](x.png)![b](y.png)")
        self.assertEqual((high["ImplicationText"], high["ImplicationImgs"]), ("", ""))


class Counting(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.record, _markdown, cls.transport = build()

    def test_counts_and_order(self):
        record = self.record
        counts = {k: record[k] for k in ("Critical", "High", "Medium", "Low", "Information", "Total")}
        self.assertEqual(counts, {"Critical": "1", "High": "1", "Medium": "0", "Low": "0",
                                  "Information": "1", "Total": "3"})
        closed = {k: record[k] for k in ("Closed_Critical", "Closed_High", "Closed_Medium", "Closed_Low",
                                          "Closed_Information", "Closed_Total")}
        self.assertEqual(closed, {"Closed_Critical": "0", "Closed_High": "0", "Closed_Medium": "1",
                                  "Closed_Low": "0", "Closed_Information": "0", "Closed_Total": "1"})
        self.assertEqual([i["Name"] for i in record["IssueDetails"]],
                         ["Broken Access Control", "SQL Injection", "Verbose Server Banner"])
        self.assertEqual([t["Name"] for t in record["AssetTocs"]], ["Alpha Portal", "Zulu API"])
        alpha = record["AssetTocs"][0]
        self.assertEqual([(g["Severity"], g["SeverityLevel"]) for g in alpha["SeverityGroups"]],
                         [("Critical", "1"), ("Info", "6")])
        self.assertEqual(alpha["SeverityGroups"][0]["Issues"], [{"Name": "Broken Access Control", "Total": "1"}])
        self.assertEqual([t["Severity"] for t in record["IssueTocs"]], ["Critical", "High", "Info"])
        self.assertEqual(record["IssueTocs"][1]["SeverityGroups"], [{"Name": "SQL Injection", "Total": "1"}])

    def test_same_name_issues_total_together(self):
        issues = copy.deepcopy(fx.ISSUE_DOCS)
        twin = copy.deepcopy(issues[1])
        twin["id"] = "issue-critical-twin"
        issues.append(twin)
        record, _m, _t = build(fx.FakeTransport(issues=issues))
        alpha = record["AssetTocs"][0]
        self.assertEqual(alpha["SeverityGroups"][0]["Issues"], [{"Name": "Broken Access Control", "Total": "2"}])
        self.assertEqual(record["Critical"], "2")

    def test_issues_are_fetched_per_asset_by_state(self):
        issue_calls = [(r, b) for _m, r, b in self.transport.calls if r == "issue/search"
                       and b["PagingInfo"]["Page"] == 1]
        self.assertEqual(len(issue_calls), 4)
        asset_filters = sorted(b["Filter"]["FilterMatches"]["Saltminer.Asset.Id"] for _r, b in issue_calls)
        self.assertEqual(asset_filters, [fx.ASSET_ALPHA, fx.ASSET_ALPHA, fx.ASSET_ZULU, fx.ASSET_ZULU])
        for _route, body in issue_calls:
            self.assertEqual(body["Filter"]["FilterMatches"]["Saltminer.Engagement.Id"], fx.ENGAGEMENT_ID)
            self.assertEqual(body["PagingInfo"]["Size"], 300)
            self.assertEqual(body["AssetType"], "Pen")
            self.assertEqual(body["SourceType"], "Pentest")
            self.assertEqual(body["Instance"], "PenTest")
        states = sorted(list(b["Filter"]["SubFilter"]["FilterMatches"])[0] for _r, b in issue_calls)
        self.assertEqual(states, ["Vulnerability.IsActive"] * 2 + ["Vulnerability.IsRemoved"] * 2)

    def test_draft_engagement_reads_the_queue_routes(self):
        record, _m, transport = build(fx.FakeTransport(draft=True))
        routes = set(transport.routes_called())
        self.assertLessEqual({"queueasset/search", "queueissue/search"}, routes)
        self.assertNotIn("asset/search", routes)
        self.assertNotIn("issue/search", routes)
        self.assertEqual(record["State"], "Draft")
        self.assertEqual(len(record["IssueDetails"]), 3)
        self.assertEqual(issue_by_name(record, "SQL Injection")["AssetId"], fx.ASSET_ZULU)

    def test_pages_are_read_until_an_empty_page(self):
        transport = fx.FakeTransport()
        real_serve = transport._serve

        def small_pages(kind, body):
            body = copy.deepcopy(body)
            body["PagingInfo"]["Size"] = 1
            return real_serve(kind, body)

        transport._serve = small_pages
        record, _m, _t = build(transport)
        self.assertEqual(len(record["IssueDetails"]), 3)
        self.assertEqual(len(record["AssetTocs"]), 2)

    def test_missing_asset_counts_toward_totals_but_is_not_listed(self):
        inner = fx.FakeTransport()

        def ghost(method, route, body):
            response = inner(method, route, body)
            if route == "issue/search":
                for document in response["data"]:
                    if document["id"] == fx.ISSUE_HIGH:
                        document["saltminer"]["asset"]["id"] = "asset-gone"
            return response

        record, _m, _t = build(ghost)
        self.assertEqual(record["Total"], "3")
        self.assertEqual(record["High"], "1")
        self.assertEqual(len(record["IssueDetails"]), 2)
        self.assertNotIn("High", [t["Severity"] for t in record["IssueTocs"]])

    def test_tocs_are_empty_when_there_is_no_reportable_issue(self):
        # The .NET code assigns the TOC lists from inside the issue loop, so none means empty TOCs.
        record, _m, _t = build(fx.FakeTransport(issues=[]))
        self.assertEqual((record["AssetTocs"], record["IssueTocs"], record["IssueDetails"]), ([], [], []))
        self.assertEqual(record["Total"], "0")

    def test_missing_engagement_raises_not_found(self):
        transport = fx.FakeTransport()

        def none_found(method, route, body):
            return {"success": True, "data": None} if method == "GET" else transport(method, route, body)

        with self.assertRaises(DataClientNotFoundException):
            build(none_found)


class MarkdownSet(unittest.TestCase):
    def _definitions(self, comments_type):
        definitions = copy.deepcopy(fx.ATTRIBUTE_DEFINITIONS_DOCS)
        for value in definitions[1]["values"]:
            if value["name"] == "comments":
                value["type"] = comments_type
        return definitions

    def test_markdown_set_markdown(self):
        _record, markdown, _t = build(fx.FakeTransport(definitions=self._definitions("markdown")))
        self.assertEqual(len(markdown), 20)
        self.assertLessEqual(EngagementData.MARKDOWN_FIELDS, markdown)
        self.assertLessEqual({"IssueAttribute_comments", "IssueAttributes|comments"}, markdown)

    def test_markdown_set_text(self):
        _record, markdown, _t = build(fx.FakeTransport(definitions=self._definitions("text")))
        self.assertEqual(len(markdown), 18)
        self.assertEqual(markdown, EngagementData.MARKDOWN_FIELDS)

    def test_markdown_type_match_ignores_case_and_covers_engagement_attributes(self):
        definitions = copy.deepcopy(fx.ATTRIBUTE_DEFINITIONS_DOCS)
        definitions[0]["values"][0]["type"] = "Markdown"
        _record, markdown, _t = build(fx.FakeTransport(definitions=definitions))
        self.assertLessEqual({"EngagementAttribute_tester", "EngagementAttributes|tester"}, markdown)
        self.assertEqual(len(markdown), 22)

    def test_the_set_does_not_grow_across_calls(self):
        build(fx.FakeTransport(definitions=self._definitions("markdown")))
        _record, markdown, _t = build(fx.FakeTransport(definitions=self._definitions("text")))
        self.assertEqual(len(markdown), 18)


class AttributeSpellings(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.record, _markdown, _t = build()

    def test_attribute_spellings(self):
        record = self.record
        self.assertEqual(record["EngagementAttribute_tester"], "A. Ringrose")
        self.assertEqual(record["EngagementAttributes|tester"], "A. Ringrose")
        for issue in record["IssueDetailsAll"]:
            for key in ("tested_by", "influencers", "developer_note", "comments"):
                self.assertIn(f"IssueAttribute_{key}", issue)
                self.assertEqual(issue[f"IssueAttribute_{key}"], issue[f"IssueAttributes|{key}"])

    def test_multi_select_brackets_are_stripped(self):
        high = issue_by_name(self.record, "SQL Injection")
        self.assertEqual(high["IssueAttribute_influencers"], "alice,bob")
        self.assertEqual(high["IssueAttributes|influencers"], "alice,bob")
        self.assertEqual(high["IssueAttribute_tested_by"], "aringrose")

    def test_an_attribute_without_a_definition_is_dropped_and_a_missing_one_takes_its_default(self):
        high = issue_by_name(self.record, "SQL Injection")
        self.assertNotIn("IssueAttribute_stray", high)
        critical = issue_by_name(self.record, "Broken Access Control")
        self.assertEqual(critical["IssueAttribute_developer_note"], "none recorded")

    def test_a_hidden_attribute_reads_empty(self):
        definitions = copy.deepcopy(fx.ATTRIBUTE_DEFINITIONS_DOCS)
        definitions[1]["values"][0]["hidden"] = True
        record, _m, _t = build(fx.FakeTransport(definitions=definitions))
        self.assertEqual(issue_by_name(record, "SQL Injection")["IssueAttribute_tested_by"], "")


class Comments(unittest.TestCase):
    def _comments(self, **settings):
        record, _m, _t = build(**settings)
        return issue_by_name(record, "SQL Injection")["Comments"]

    def test_comment_cap_latest_first(self):
        text = self._comments(max_issue_comments=2, comment_sort_latest_first=True)
        self.assertEqual(text, "[03/06/2026] svc-scanner: fourth note\n[03/05/2026] tokafor: third note\n")
        self.assertLess(text.index("fourth note"), text.index("third note"))
        self.assertNotIn("first note", text)
        self.assertNotIn("second note", text)

    def test_comment_oldest_first(self):
        text = self._comments(max_issue_comments=2, comment_sort_latest_first=False)
        self.assertEqual(text, "[03/05/2026] tokafor: third note\n[03/06/2026] svc-scanner: fourth note\n")

    def test_default_settings_take_three_comments_oldest_first(self):
        text = self._comments()
        self.assertEqual([line.split(": ", 1)[1] for line in text.splitlines()],
                         ["second note", "third note", "fourth note"])

    def test_system_comments_are_excluded_unless_asked_for(self):
        self.assertNotIn("status changed", self._comments(max_issue_comments=10))
        with_system = self._comments(max_issue_comments=10, include_system_comments=True)
        self.assertIn("status changed", with_system)

    def test_the_comment_query_filters_by_type(self):
        _record, _m, transport = build()
        body = next(b for _mth, r, b in transport.calls if r == "comment/search")
        self.assertEqual(body["Filter"]["FilterMatches"]["Saltminer.Comment.Type"], "User")
        self.assertEqual(body["PagingInfo"]["Size"], 1000)
        _record, _m, transport = build(include_system_comments=True)
        body = next(b for _mth, r, b in transport.calls if r == "comment/search")
        self.assertNotIn("Saltminer.Comment.Type", body["Filter"]["FilterMatches"])

    def test_template_tokens_and_date_format(self):
        text = self._comments(max_issue_comments=1, comment_sort_latest_first=True,
                              comment_template="{Date:yyyy-MM-dd HH:mm} <{UserName}> {Message}")
        self.assertEqual(text, "2026-03-06 10:00 <svc-scanner (full)> fourth note\n")

    def test_comment_bad_format_fallback(self):
        text = self._comments(max_issue_comments=3, comment_template="[{Date:zzz}] {Message}")
        self.assertEqual(text, "[03/04/2026] tokafor: second note\n[03/05/2026] tokafor: third note\n"
                               "[03/06/2026] svc-scanner: fourth note\n")

    def test_empty_template_and_low_cap_use_defaults(self):
        text = self._comments(comment_template="", max_issue_comments=0, comment_sort_latest_first=True)
        self.assertEqual(text, "[03/06/2026] svc-scanner: fourth note\n")

    def test_an_issue_without_comments_gets_an_empty_string(self):
        record, _m, _t = build()
        self.assertEqual(issue_by_name(record, "Verbose Server Banner")["Comments"], "")
        self.assertEqual(issue_by_name(record, "Broken Access Control")["Comments"],
                         "[03/05/2026] aringrose: critical note\n")


class DateFormats(unittest.TestCase):
    WHEN = datetime.datetime(2026, 3, 7, 14, 5, 9, 250000)

    def test_supported_formats(self):
        cases = {
            "d": "03/07/2026",
            "MM/dd/yyyy": "03/07/2026",
            "MMMM dd, yyyy": "March 07, 2026",
            "yyyy/MM/dd": "2026/03/07",
            "M/d/yy": "3/7/26",
            "ddd, MMM d": "Sat, Mar 7",
            "dddd": "Saturday",
            "HH:mm:ss": "14:05:09",
            "h:mm tt": "2:05 PM",
            "yyyy-MM-dd'T'HH:mm:ss.fff": "2026-03-07T14:05:09.250",
            "s": "2026-03-07T14:05:09",
            "%d": "7",
            "dd\\.MM": "07.03",
        }
        for fmt, expected in cases.items():
            self.assertEqual(format_dotnet_date(self.WHEN, fmt), expected, fmt)

    def test_unsupported_formats_raise(self):
        for fmt in ("zzz", "K", "o", "yyyy 'open", "s.F", "%"):
            with self.assertRaises(ValueError, msg=fmt):
                format_dotnet_date(self.WHEN, fmt)


class Settings(unittest.TestCase):
    class _Settings:
        def __init__(self, values):
            self.values = values

        def Get(self, section, key, default=None):
            return self.values.get((section, key), default)

    class _App:
        def __init__(self, values):
            self.Settings = Settings._Settings(values)

    def test_defaults_match_the_job_manager(self):
        settings = load_report_settings(self._App({}))
        self.assertEqual(settings, ReportSettings("[{Date:d}] {User}: {Message}", 3, False, False))

    def test_values_are_read_under_their_existing_names(self):
        settings = load_report_settings(self._App({
            ("Reports", "ReportCommentTemplate"): "{Message}",
            ("Reports", "ReportMaxIssueComments"): "5",
            ("Reports", "ReportIssueCommentSortLatestFirst"): "true",
            ("Reports", "ReportIncludeSystemComments"): True,
        }))
        self.assertEqual(settings, ReportSettings("{Message}", 5, True, True))

    def test_the_settings_are_frozen(self):
        with self.assertRaises(Exception):
            ReportSettings().max_issue_comments = 9


class PackageRules(unittest.TestCase):
    def test_no_direct_index_access_and_the_data_api_client_is_imported(self):
        # AC-6. The pattern is built from parts so this file does not match itself.
        pattern = re.compile("|".join([
            "elastic" + "search", "Elastic" + r"search\(", r"es\." + "search", "_" + "search"]))
        offenders = []
        for folder, _dirs, files in os.walk(_PACKAGE):
            for name in files:
                if name.endswith(".py"):
                    path = os.path.join(folder, name)
                    with open(path, encoding="utf-8") as f:
                        for number, line in enumerate(f, 1):
                            if pattern.search(line):
                                offenders.append(f"{path}:{number}")
        self.assertEqual(offenders, [])
        with open(os.path.join(_PACKAGE, "EngagementData.py"), encoding="utf-8") as f:
            self.assertIn("from Core.DataClient import DataClient\n", f.read())

    def test_no_em_dashes_or_debug_markers(self):
        for name in ("EngagementData.py", os.path.join("Tests", "data_api_fixture.py"),
                     os.path.join("Tests", "test_engagement_data.py"),
                     os.path.join("Tests", "test_engagement_data_live.py")):
            with open(os.path.join(_PACKAGE, name), encoding="utf-8") as f:
                text = f.read()
            self.assertNotIn(chr(0x2014), text, name)
            self.assertNotIn("TEMPORARY " + "DEBUGGING", text, name)


class _Response:
    def __init__(self, status_code, text):
        self.status_code = status_code
        self.text = text


class _FakeManagerClient:
    def __init__(self, status_code=200, text='{"success": true, "data": []}'):
        self.calls = []
        self.status_code = status_code
        self.text = text

    async def Get(self, route):
        self.calls.append(("GET", route, None))
        return _Response(self.status_code, self.text)

    async def Post(self, route, body):
        self.calls.append(("POST", route, body))
        return _Response(self.status_code, self.text)


class _FakeDataClient:
    def __init__(self, manager):
        self.manager_client = manager
        self.loop = asyncio.new_event_loop()

    def _run_async(self, coro):
        return self.loop.run_until_complete(coro)


class SourceTransport(unittest.TestCase):
    def test_the_default_transport_sends_through_the_manager_client(self):
        manager = _FakeManagerClient()
        source = DataApiSource(_FakeDataClient(manager))
        self.assertEqual(source.get_comments("eng-1", False), [])
        method, route, body = manager.calls[0]
        self.assertEqual((method, route), ("POST", "comment/search"))
        self.assertEqual(body["Filter"]["FilterMatches"]["Saltminer.Engagement.Id"], "eng-1")

    def test_the_default_transport_maps_status_codes_to_client_exceptions(self):
        with self.assertRaises(DataClientNotFoundException):
            DataApiSource(_FakeDataClient(_FakeManagerClient(404, "{}"))).get_engagement("eng-1")
        with self.assertRaises(DataClientException):
            DataApiSource(_FakeDataClient(_FakeManagerClient(500, "{}"))).get_engagement("eng-1")

    def test_a_source_needs_a_client_or_a_transport(self):
        with self.assertRaises(ValueError):
            DataApiSource(None)

    def test_a_failed_response_raises(self):
        source = DataApiSource(None, lambda method, route, body: {"success": False, "errorMessages": ["boom"]})
        with self.assertRaises(Exception) as caught:
            source.get_attribute_definitions()
        self.assertIn("boom", str(caught.exception))


if __name__ == "__main__":
    unittest.main()
