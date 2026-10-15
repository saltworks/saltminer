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

# Data API responses for one engagement, keyed by route, and a fake transport that serves them.
#
# The engagement is published and has one engagement attribute (tester, text), two assets, three active
# issues and one removed issue. The issue attributes are tested_by (single select), influencers (multi
# select), developer_note (text) and comments (markdown). The first issue has four user comments at
# distinct times and one system comment. Every value is invented.
#
# The documents are shaped as the Data API returns them (camelCase, response envelope with data and
# pagingInfo). The shape was written from the Core entity classes, not recorded from a live response;
# the live test compares the two.

import copy

ENGAGEMENT_ID = "eng-0001"
ASSET_ZULU = "asset-zulu"
ASSET_ALPHA = "asset-alpha"

ISSUE_HIGH = "issue-high"          # active, High, on Zulu, four user comments
ISSUE_CRITICAL = "issue-critical"  # active, Critical, on Alpha
ISSUE_INFO = "issue-info"          # active, Info, on Alpha
ISSUE_REMOVED = "issue-removed"    # removed, Medium, on Zulu

COMMENT_TEXTS = ["first note", "second note", "third note", "fourth note"]

ATTRIBUTE_DEFINITIONS_DOCS = [
    {
        "id": "def-engagement",
        "type": "Engagement",
        "values": [
            {"section": "", "name": "tester", "display": "Tester", "type": "text", "hidden": False,
             "default": None},
        ],
    },
    {
        "id": "def-issue",
        "type": "Issue",
        "values": [
            {"section": "", "name": "tested_by", "display": "Tested by", "type": "single select",
             "hidden": False, "default": None},
            {"section": "", "name": "influencers", "display": "Influencers", "type": "multi select",
             "hidden": False, "default": None},
            {"section": "", "name": "developer_note", "display": "Developer note", "type": "text",
             "hidden": False, "default": "none recorded"},
            {"section": "", "name": "comments", "display": "Comments", "type": "markdown", "hidden": False,
             "default": None},
        ],
    },
]

ENGAGEMENT_DOC = {
    "id": ENGAGEMENT_ID,
    "timestamp": "2026-03-01T09:30:00.1234567Z",
    "lastUpdated": "2026-03-20T10:00:00Z",
    "saltminer": {
        "engagement": {
            "id": ENGAGEMENT_ID,
            "name": "Quarterly web test",
            "customer": "Example Customer",
            "summary": "A summary of the quarterly test.",
            "subtype": "Web",
            "status": "Published",
            "groupId": "group-1",
            "publishDate": "2026-03-20T00:00:00Z",
            "attributes": {"tester": "A. Ringrose"},
        }
    },
}


def _asset(asset_id, name, description):
    return {
        "id": asset_id,
        "timestamp": "2026-03-02T00:00:00Z",
        "saltminer": {
            "engagement": {"id": ENGAGEMENT_ID},
            "asset": {"name": name, "description": description, "sourceType": "Pentest",
                      "assetType": "Pen", "instance": "PenTest"},
        },
    }


ASSET_DOCS = [
    # Stored in the reverse of name order, so the sort is exercised.
    _asset(ASSET_ZULU, "Zulu API", "The Zulu API."),
    _asset(ASSET_ALPHA, "Alpha Portal", "The Alpha portal."),
]


def _issue(issue_id, asset_id, name, severity, attributes, removed_date=None, **vulnerability):
    return {
        "id": issue_id,
        "timestamp": "2026-03-02T00:00:00Z",
        "saltminer": {
            "engagement": {"id": ENGAGEMENT_ID},
            "asset": {"id": asset_id},
            "attributes": attributes,
        },
        "vulnerability": {
            "name": name,
            "description": f"Description of {name}.",
            "severity": severity,
            "reportId": f"R-{issue_id}",
            "foundDate": "2026-03-03T08:15:00Z",
            "removedDate": removed_date,
            "isRemoved": removed_date is not None,
            "isActive": removed_date is None,
            "isSuppressed": False,
            "testStatus": "Found",
            "location": "  /login  ",
            "locationFull": " https://example.invalid/login ",
            "classification": "CWE-79",
            "enumeration": "CWE-79",
            "reference": "REF-1",
            "scanner": {"vendor": " Example Vendor ", "product": " Example Scanner "},
            "proof": "Proof text ![shot](https://example.invalid/a.png) more proof",
            "details": "Details text",
            "implication": "",
            "recommendation": "Fix it ![a](x.png)![b](y.png)",
            "references": "https://example.invalid/ref",
            "testingInstructions": "Try it.",
            **vulnerability,
        },
    }


ISSUE_DOCS = [
    _issue(ISSUE_HIGH, ASSET_ZULU, "SQL Injection", "High",
           {"tested_by": "aringrose", "influencers": "[alice],[bob]", "developer_note": "fix in sprint",
            "comments": "**bold** note", "stray": "no definition for this one"}),
    _issue(ISSUE_CRITICAL, ASSET_ALPHA, "Broken Access Control", "Critical",
           {"tested_by": "tokafor", "influencers": "[carol]", "comments": "plain"}),
    _issue(ISSUE_INFO, ASSET_ALPHA, "Verbose Server Banner", "Info",
           {"tested_by": "tokafor", "influencers": "", "developer_note": "n/a", "comments": ""}),
    _issue(ISSUE_REMOVED, ASSET_ZULU, "Missing HSTS", "Medium",
           {"tested_by": "aringrose", "influencers": "[dave]", "developer_note": "closed", "comments": "x"},
           removed_date="2026-03-10T00:00:00Z"),
]


def _comment(issue_id, added, user, message, kind="User"):
    return {
        "id": f"comment-{added}",
        "saltminer": {
            "engagement": {"id": ENGAGEMENT_ID},
            "issue": {"id": issue_id},
            "comment": {"added": added, "user": user, "userFullName": f"{user} (full)", "message": message,
                        "type": kind},
        },
    }


COMMENT_DOCS = [
    # Stored out of time order, so the sort is exercised.
    _comment(ISSUE_HIGH, "2026-03-05T10:00:00Z", "tokafor", COMMENT_TEXTS[2]),
    _comment(ISSUE_HIGH, "2026-03-03T10:00:00Z", "aringrose", COMMENT_TEXTS[0]),
    _comment(ISSUE_HIGH, "2026-03-06T10:00:00Z", "svc-scanner", COMMENT_TEXTS[3]),
    _comment(ISSUE_HIGH, "2026-03-04T10:00:00Z", "tokafor", COMMENT_TEXTS[1]),
    _comment(ISSUE_HIGH, "2026-03-07T10:00:00Z", "system", "status changed", kind="System"),
    _comment(ISSUE_CRITICAL, "2026-03-05T12:30:00.5000000Z", "aringrose", "critical note"),
]


class FakeTransport:
    """Serves the fixture in the shape of the Data API routes and records every call.

    A search honours the request's FilterMatches (equal values, or several values joined with '||+') and
    its SubFilter, and pages by PagingInfo, so a wrong filter in the request fails a test.
    """

    def __init__(self, engagement=None, assets=None, issues=None, comments=None, definitions=None,
                 draft=False):
        self.engagement = copy.deepcopy(ENGAGEMENT_DOC if engagement is None else engagement)
        self.assets = copy.deepcopy(ASSET_DOCS if assets is None else assets)
        self.issues = copy.deepcopy(ISSUE_DOCS if issues is None else issues)
        self.comments = copy.deepcopy(COMMENT_DOCS if comments is None else comments)
        self.definitions = copy.deepcopy(ATTRIBUTE_DEFINITIONS_DOCS if definitions is None else definitions)
        self.calls = []
        if draft:
            self.engagement["saltminer"]["engagement"]["status"] = "Draft"
            self.issues = [self._as_queue_issue(i) for i in self.issues]

    @staticmethod
    def _as_queue_issue(issue):
        issue = copy.deepcopy(issue)
        asset = issue["saltminer"].pop("asset")
        issue["saltminer"]["queueAssetId"] = asset["id"]
        return issue

    def __call__(self, method, route, body):
        self.calls.append((method, route, copy.deepcopy(body)))
        if method == "GET" and route == f"engagement/{ENGAGEMENT_ID}":
            return {"success": True, "data": self.engagement}
        if method == "POST" and route in _SEARCH_ROUTES:
            return self._serve(_SEARCH_ROUTES[route], body)
        return {"success": False, "statusCode": 404, "errorMessages": [f"no route {method} {route}"]}

    def routes_called(self):
        return [route for _method, route, _body in self.calls]

    def _serve(self, kind, body):
        documents = getattr(self, kind)
        filtered = [d for d in documents if _matches(d, body.get("Filter") or {})]
        paging = body.get("PagingInfo") or {"Page": 1, "Size": 1000}
        size, page = paging["Size"], paging["Page"]
        chunk = filtered[(page - 1) * size: page * size]
        return {"success": True, "data": chunk,
                "pagingInfo": {"page": page, "size": size, "currentHits": len(chunk),
                               "totalHits": len(filtered)}}


_SEARCH_ROUTES = {
    "asset/search": "assets",
    "queueasset/search": "assets",
    "issue/search": "issues",
    "queueissue/search": "issues",
    "comment/search": "comments",
    "attributedefinition/search": "definitions",
}


def _lookup(document, dotted):
    current = document
    for part in dotted.split("."):
        if not isinstance(current, dict):
            return None
        for key, value in current.items():
            if key.lower() == part.lower():
                current = value
                break
        else:
            return None
    return current


def _matches(document, flt):
    for field, wanted in (flt.get("FilterMatches") or {}).items():
        actual = _lookup(document, field)
        options = [w for w in wanted.split("||+")]
        if str(actual).lower() not in [o.lower() for o in options]:
            return False
    sub = flt.get("SubFilter")
    return _matches(document, sub) if sub else True
