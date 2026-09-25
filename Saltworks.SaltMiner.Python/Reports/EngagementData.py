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

"""Engagement report data assembly, read through the Data API.

A Python port of the assembly half of the JobManager report processor: CreateReportEngagementDto,
CreateIssueDetail, GetCommentText and CreateAttributeProperties (ReportProcessor.cs, read on
origin/3.6 at 331aeb4). It builds the record the merge engine binds to the root group and returns
the set of fields whose values are markdown. It fills no template and renders no markdown.

    assemble_engagement(engagement_id, source, settings) -> (record, markdown_fields)

The record is plain dicts and lists. Every group is a list of dicts keyed by the group name and every
leaf is a str (None becomes "", ints become str(n), booleans become "True" or "False"). The record
does not depend on any template: it carries every field the .NET path builds.

    root            Id Name State Customer Timestamp Summary PublishDate
                    Critical High Medium Low Information Total
                    Closed_Critical Closed_High Closed_Medium Closed_Low Closed_Information Closed_Total
                    EngagementAttribute_<k> and EngagementAttributes|<k> for each engagement attribute
                    AssetTocs IssueTocs IssueDetails IssueDetailsRemoved IssueDetailsAll
                    IssueSummary IssueSummaryRemoved IssueSummaryAll
    AssetTocs[]     Id Name Description
                    SeverityGroups[]: Severity SeverityLevel, Issues[]: Name Total
    IssueTocs[]     Severity SeverityLevel, SeverityGroups[]: Name Total
    issue detail    Name Description Product Vendor Severity SeverityLevel ReportId AssetId FoundDate
    (six lists)     TestStatus TestingInstructions IsSuppressed IsActive IsRemoved RemovedDate Location
                    LocationFull Classification Enumeration Reference
                    Proof ProofText ProofImgs Details DetailsText DetailsImgs Implication
                    ImplicationText ImplicationImgs Recommendation RecommendationText RecommendationImgs
                    References ReferencesText ReferencesImgs
                    Comments AppVersion EngagementId Id
                    IssueAttribute_<k> and IssueAttributes|<k> for each issue attribute

The IssueSummary lists hold the same dict objects as the IssueDetails lists, as in .NET. Treat the
record as read-only.

Every read goes through the Data API client (Core.DataClient). The mapping the UI API applies between
a Data API document and the report value (the AssetFull, IssueFull and EngagementSummary constructors)
is ported here, because the UI API is not called.
"""

import json
import logging
import re
from dataclasses import dataclass
from datetime import datetime
from typing import Any, Callable

from Core.Application import Application
from Core.DataClient import DataClient
from Core.DataClient import DataClientException, DataClientNotFoundException

# ReportProcessor.cs:58-77. A value merged into one of these fields is markdown.
MARKDOWN_FIELDS: frozenset[str] = frozenset({
    "Proof", "ProofText", "ProofImgs",
    "References", "ReferencesText", "ReferencesImgs",
    "Recommendation", "RecommendationText", "RecommendationImgs",
    "Implication", "ImplicationText", "ImplicationImgs",
    "Details", "DetailsText", "DetailsImgs",
    "TestingInstructions", "TestingInstructionsText", "TestingInstructionsImgs",
})

# Report settings, read from the Reports config section. A missing key falls back to the JobManager
# default (JobManagerConfig.cs:76,84-86).
SETTINGS_SECTION = "Reports"
DEFAULT_COMMENT_TEMPLATE = "[{Date:d}] {User}: {Message}"
DEFAULT_MAX_ISSUE_COMMENTS = 3
DEFAULT_COMMENT_SORT_LATEST_FIRST = False
DEFAULT_INCLUDE_SYSTEM_COMMENTS = False

# Page sizes. Issues use 300 per page as ReportProcessor.GetAllEngagementAssetIssues does; comments use 1000
# as GetEngagementComments does.
ISSUE_PAGE_SIZE = 300
COMMENT_PAGE_SIZE = 1000
DEFAULT_PAGE_SIZE = 1000
_MAX_PAGES = 100000

# UiApiConfig.cs:105-107: every engagement asset and issue is a pen test one.
ASSET_TYPE = "Pen"
SOURCE_TYPE = "Pentest"
INSTANCE = "PenTest"

_DRAFT_STATUS = "Draft"
_SEVERITY_LEVELS = {"critical": 1, "high": 2, "medium": 3, "low": 4, "zero": 5, "info": 6, "noscan": 7}
_INFO_LEVEL = 6
_COUNTED_SEVERITIES = ("Critical", "High", "Medium", "Low")

_MD_IMAGE = re.compile(r"(!\[[^\]]*\]\([^\)]*\))")
_DATE_TOKEN = re.compile(r"\{Date:([^\}]+)\}")

Transport = Callable[[str, str, "dict | None"], dict]


# --------------------------------------------------------------------------------------------------
# Settings
# --------------------------------------------------------------------------------------------------

@dataclass(frozen=True)
class ReportSettings:
    """The four report settings the assembly honours, under the names the JobManager config uses.

    comment_template            ReportCommentTemplate
    max_issue_comments          ReportMaxIssueComments
    comment_sort_latest_first   ReportIssueCommentSortLatestFirst
    include_system_comments     ReportIncludeSystemComments
    """

    comment_template: str = DEFAULT_COMMENT_TEMPLATE
    max_issue_comments: int = DEFAULT_MAX_ISSUE_COMMENTS
    comment_sort_latest_first: bool = DEFAULT_COMMENT_SORT_LATEST_FIRST
    include_system_comments: bool = DEFAULT_INCLUDE_SYSTEM_COMMENTS


def _as_bool(value: Any, default: bool) -> bool:
    if isinstance(value, bool):
        return value
    if isinstance(value, str) and value.strip().lower() in ("true", "false"):
        return value.strip().lower() == "true"
    return default


def _as_int(value: Any, default: int) -> int:
    try:
        return int(value)
    except (TypeError, ValueError):
        return default


def load_report_settings(app: Application) -> ReportSettings:
    """Reads the report settings from the Reports config section (Config/Reports.json)."""
    get = app.Settings.Get
    template = get(SETTINGS_SECTION, "ReportCommentTemplate", DEFAULT_COMMENT_TEMPLATE)
    return ReportSettings(
        comment_template=template if isinstance(template, str) else DEFAULT_COMMENT_TEMPLATE,
        max_issue_comments=_as_int(get(SETTINGS_SECTION, "ReportMaxIssueComments", DEFAULT_MAX_ISSUE_COMMENTS),
                                   DEFAULT_MAX_ISSUE_COMMENTS),
        comment_sort_latest_first=_as_bool(
            get(SETTINGS_SECTION, "ReportIssueCommentSortLatestFirst", DEFAULT_COMMENT_SORT_LATEST_FIRST),
            DEFAULT_COMMENT_SORT_LATEST_FIRST),
        include_system_comments=_as_bool(
            get(SETTINGS_SECTION, "ReportIncludeSystemComments", DEFAULT_INCLUDE_SYSTEM_COMMENTS),
            DEFAULT_INCLUDE_SYSTEM_COMMENTS),
    )


# --------------------------------------------------------------------------------------------------
# Small helpers
# --------------------------------------------------------------------------------------------------

def _dig(doc: Any, *path: str) -> Any:
    """Reads a nested value, matching key names without regard to case. A missing path gives None."""
    current = doc
    for name in path:
        if not isinstance(current, dict):
            return None
        if name in current:
            current = current[name]
            continue
        wanted = name.lower()
        for key, value in current.items():
            if isinstance(key, str) and key.lower() == wanted:
                current = value
                break
        else:
            return None
    return current


def _text(value: Any) -> str:
    if value is None:
        return ""
    if isinstance(value, bool):
        return "True" if value else "False"
    return str(value)


def _trim(value: Any) -> str:
    return _text(value).strip()


_ISO = re.compile(r"^(\d{4})-(\d{2})-(\d{2})(?:[T ](\d{2}):(\d{2})(?::(\d{2})(?:\.(\d+))?)?)?")


def _parse_datetime(value: Any) -> datetime | None:
    """Parses an ISO 8601 value as the wall clock it carries. The offset, if any, is ignored, which is how
    .NET formats a value it read as UTC."""
    if value is None or value == "":
        return None
    if isinstance(value, datetime):
        return value.replace(tzinfo=None)
    match = _ISO.match(str(value).strip())
    if not match:
        logging.warning("[Reports][_parse_datetime] Unreadable date value '%s'", value)
        return None
    year, month, day, hour, minute, second, fraction = match.groups()
    micro = int((fraction or "0")[:6].ljust(6, "0"))
    return datetime(int(year), int(month), int(day), int(hour or 0), int(minute or 0), int(second or 0), micro)


# .NET date formatting, for the tokens a report date or a comment template uses. The invariant culture is
# assumed [Likely]: the JobManager container sets no culture, so "d" is MM/dd/yyyy.
SHORT_DATE_FORMAT = "MM/dd/yyyy"
_STANDARD_FORMATS = {
    "d": SHORT_DATE_FORMAT,
    "D": "dddd, dd MMMM yyyy",
    "t": "HH:mm",
    "T": "HH:mm:ss",
    "f": "dddd, dd MMMM yyyy HH:mm",
    "F": "dddd, dd MMMM yyyy HH:mm:ss",
    "g": "MM/dd/yyyy HH:mm",
    "G": "MM/dd/yyyy HH:mm:ss",
    "s": "yyyy'-'MM'-'dd'T'HH':'mm':'ss",
}
_MONTHS = ["January", "February", "March", "April", "May", "June", "July", "August", "September",
           "October", "November", "December"]
_DAYS = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]


def format_dotnet_date(value: datetime, fmt: str) -> str:
    """Formats a date the way DateTime.ToString(fmt) does for the tokens this port supports.

    Raises ValueError for a token it does not support, so the caller can fall back to the default
    comment template rather than print a wrong date.
    """
    if fmt == "":
        fmt = "G"
    if len(fmt) == 1:
        if fmt not in _STANDARD_FORMATS:
            raise ValueError(f"unsupported standard date format '{fmt}'")
        fmt = _STANDARD_FORMATS[fmt]
    out: list[str] = []
    i = 0
    while i < len(fmt):
        ch = fmt[i]
        if ch in ("'", '"'):
            end = fmt.find(ch, i + 1)
            if end < 0:
                raise ValueError("unterminated quoted literal in date format")
            out.append(fmt[i + 1:end])
            i = end + 1
        elif ch == "\\":
            if i + 1 >= len(fmt):
                raise ValueError("trailing escape in date format")
            out.append(fmt[i + 1])
            i += 2
        elif ch == "%":
            if i + 1 >= len(fmt):
                raise ValueError("trailing percent in date format")
            out.append(_date_token(value, fmt[i + 1], 1))
            i += 2
        else:
            run = 1
            while i + run < len(fmt) and fmt[i + run] == ch:
                run += 1
            out.append(_date_token(value, ch, run))
            i += run
    return "".join(out)


def _date_token(value: datetime, ch: str, run: int) -> str:
    """One custom date token, `ch` repeated `run` times."""
    if ch == "y":
        return str(value.year % 100) if run == 1 else f"{value.year % 100:02d}" if run == 2 else f"{value.year:0{run}d}"
    if ch == "M":
        name = _MONTHS[value.month - 1]
        return str(value.month) if run == 1 else f"{value.month:02d}" if run == 2 else name[:3] if run == 3 else name
    if ch == "d":
        name = _DAYS[value.weekday()]
        return str(value.day) if run == 1 else f"{value.day:02d}" if run == 2 else name[:3] if run == 3 else name
    if ch == "H":
        return f"{value.hour:d}" if run == 1 else f"{value.hour:02d}"
    if ch == "h":
        hour = value.hour % 12 or 12
        return f"{hour:d}" if run == 1 else f"{hour:02d}"
    if ch == "m":
        return f"{value.minute:d}" if run == 1 else f"{value.minute:02d}"
    if ch == "s":
        return f"{value.second:d}" if run == 1 else f"{value.second:02d}"
    if ch == "f":
        if run > 7:
            raise ValueError("too many fraction digits in date format")
        return f"{value.microsecond:06d}0"[:run]
    if ch == "t":
        mark = "AM" if value.hour < 12 else "PM"
        return mark[0] if run == 1 else mark
    if ch in ("F", "g", "K", "z"):
        raise ValueError(f"unsupported date format token '{ch * run}'")
    return ch * run


def _format_report_date(value: Any, fmt: str) -> str:
    parsed = _parse_datetime(value)
    return "" if parsed is None else format_dotnet_date(parsed, fmt)


def get_markdown_text(markdown: str) -> str:
    """The value with markdown image tags removed (ReportProcessor.GetMarkdownText)."""
    if not markdown or not markdown.strip():
        return ""
    return _MD_IMAGE.sub("", markdown).strip()


def get_markdown_images(markdown: str) -> str:
    """The image tags in the value, joined (ReportProcessor.GetMarkdownImages)."""
    if not markdown or not markdown.strip():
        return ""
    return "".join(match.group(1) for match in _MD_IMAGE.finditer(markdown)).strip()


def _severity_level(severity: str) -> int:
    return _SEVERITY_LEVELS.get((severity or "").strip().lower(), _INFO_LEVEL)


def _name_key(name: str) -> tuple[str, str]:
    """Approximates the culture-aware string ordering .NET's OrderBy applies to names."""
    return (name.casefold(), name)


# --------------------------------------------------------------------------------------------------
# Data API reads
# --------------------------------------------------------------------------------------------------

class DataApiSource:
    """Every Data API read the assembly makes, and nothing else.

    It makes the same calls the UI API makes on the report's behalf (Ui.Api ContextBase.cs:490-770), with
    the Data API manager key. `transport` is a seam for tests: a callable taking (method, route, body) and
    returning the parsed response body.
    """

    def __init__(self, data_client: DataClient | None, transport: Transport | None = None) -> None:
        if data_client is None and transport is None:
            raise ValueError("DataApiSource needs a DataClient or a transport")
        self._data_client = data_client
        self._transport = transport or self._send

    def request(self, method: str, route: str, body: dict | None = None) -> dict:
        """One Data API call. Returns the parsed response body."""
        return self._transport(method, route, body)

    def _send(self, method: str, route: str, body: dict | None) -> dict:
        client = self._data_client.manager_client

        async def call() -> dict:
            response = await (client.Get(route) if method == "GET" else client.Post(route, body))
            status = response.status_code
            if not 200 <= status < 300:
                message = f"Data API {method} '{route}' failed: [{status}]"
                if status == 404:
                    raise DataClientNotFoundException(message)
                raise DataClientException(message)
            return json.loads(response.text)

        # The client's own loop; the one private call in this module. A public DataClient method would
        # replace it.
        return self._data_client._run_async(call())

    def _checked(self, method: str, route: str, body: dict | None) -> dict:
        response = self.request(method, route, body)
        if _dig(response, "success") is False:
            messages = _dig(response, "errorMessages") or [_dig(response, "message") or "no message"]
            raise DataClientException(f"Data API {method} '{route}' failed: {'; '.join(map(str, messages))}")
        return response

    def _pages(self, route: str, request: dict, size: int) -> list[dict]:
        """Every document a search returns, one page at a time until a page comes back empty."""
        documents: list[dict] = []
        for page in range(1, _MAX_PAGES + 1):
            body = {**request, "PagingInfo": {"Page": page, "Size": size}}
            data = _dig(self._checked("POST", route, body), "data") or []
            if not data:
                break
            documents.extend(data)
        return documents

    def get_engagement(self, engagement_id: str) -> dict:
        response = self._checked("GET", f"engagement/{engagement_id}", None)
        document = _dig(response, "data")
        if not document:
            raise DataClientNotFoundException(f"Engagement '{engagement_id}' not found")
        return document

    def get_assets(self, engagement_id: str, draft: bool) -> list[dict]:
        request = {
            "Filter": {"AnyMatch": False, "FilterMatches": {"Saltminer.Engagement.Id": engagement_id}},
            "AssetType": ASSET_TYPE, "SourceType": SOURCE_TYPE, "Instance": INSTANCE,
        }
        return self._pages("queueasset/search" if draft else "asset/search", request, DEFAULT_PAGE_SIZE)

    def get_issues(self, engagement_id: str, asset_id: str, state: str, draft: bool) -> list[dict]:
        """One asset's issues in one state, 'IsActive' or 'IsRemoved' (ContextBase.EngagementIssueSearch)."""
        state_field = {"IsActive": "Vulnerability.IsActive", "IsRemoved": "Vulnerability.IsRemoved"}[state]
        asset_field = "Saltminer.QueueAssetId" if draft else "Saltminer.Asset.Id"
        request: dict = {
            "Filter": {
                "AnyMatch": False,
                "FilterMatches": {"Saltminer.Engagement.Id": engagement_id, asset_field: asset_id},
                "SubFilter": {"AnyMatch": True, "FilterMatches": {state_field: "true"}},
            },
        }
        if not draft:
            request.update({"AssetType": ASSET_TYPE, "SourceType": SOURCE_TYPE, "Instance": INSTANCE})
        return self._pages("queueissue/search" if draft else "issue/search", request, ISSUE_PAGE_SIZE)

    def get_comments(self, engagement_id: str, include_system: bool) -> list[dict]:
        matches = {"Saltminer.Engagement.Id": engagement_id}
        if not include_system:
            matches["Saltminer.Comment.Type"] = "User"
        request = {"Filter": {"AnyMatch": False, "FilterMatches": matches}}
        return self._pages("comment/search", request, COMMENT_PAGE_SIZE)

    def get_attribute_definitions(self) -> list[dict]:
        return self._pages("attributedefinition/search", {"Filter": {"AnyMatch": False, "FilterMatches": {}}},
                           DEFAULT_PAGE_SIZE)


# --------------------------------------------------------------------------------------------------
# Attribute definitions and attributes
# --------------------------------------------------------------------------------------------------

@dataclass(frozen=True)
class _AttributeDefinition:
    name: str
    type: str
    hidden: bool
    default: str | None


def _definitions_for(documents: list[dict], kind: str) -> list[_AttributeDefinition]:
    """The value list of the first definition document of this type, as `FirstOrDefault(x => x.Type == kind)`."""
    for document in documents:
        if _text(_dig(document, "type")).lower() == kind.lower():
            values = _dig(document, "values") or []
            return [
                _AttributeDefinition(
                    name=_text(_dig(v, "name")),
                    type=_text(_dig(v, "type")),
                    hidden=bool(_dig(v, "hidden")),
                    default=_dig(v, "default"),
                )
                for v in values
            ]
    return []


def _attribute_fields(attributes: Any, definitions: list[_AttributeDefinition]) -> list[tuple[str, str | None]]:
    """The (name, value) pairs UiApiClient FieldExtensions.ToAttributeFields builds.

    A stored attribute with no definition is dropped. A hidden one reads as empty. A definition the
    document lacks is added with its default value.
    """
    stored = attributes if isinstance(attributes, dict) else {}
    fields: list[tuple[str, str | None]] = []
    for key, value in stored.items():
        definition = next((d for d in definitions if d.name.lower() == key.lower()), None)
        if definition is None:
            continue
        fields.append((definition.name, "" if definition.hidden else value))
    for definition in definitions:
        if definition.name not in stored:
            fields.append((definition.name, definition.default))
    return fields


def _add_attribute_properties(target: dict, prefix: str, fields: list[tuple[str, str | None]],
                              definitions: list[_AttributeDefinition]) -> None:
    """ReportProcessor.CreateAttributeProperties, emitting both spellings of each name."""
    for key, value in fields:
        text = value
        definition = next((d for d in definitions if d.name == key), None)
        if definition is not None and "multi select" in definition.type.lower():
            text = (value or "").replace("[", "").replace("]", "")
        text = _text(text)
        target[f"{prefix}Attribute_{key}"] = text
        target[f"{prefix}Attributes|{key}"] = text


def _markdown_fields(engagement_defs: list[_AttributeDefinition],
                     issue_defs: list[_AttributeDefinition]) -> frozenset[str]:
    names = set(MARKDOWN_FIELDS)
    for prefix, definitions in (("Engagement", engagement_defs), ("Issue", issue_defs)):
        for definition in definitions:
            if "markdown" in definition.type.lower():
                names.add(f"{prefix}Attribute_{definition.name}")
                names.add(f"{prefix}Attributes|{definition.name}")
    return frozenset(names)


# --------------------------------------------------------------------------------------------------
# Comments (ReportProcessor.GetCommentText)
# --------------------------------------------------------------------------------------------------

@dataclass(frozen=True)
class _Comment:
    issue_id: str
    added: datetime
    user: str
    user_full_name: str
    message: str


def _comment_from(document: dict) -> _Comment:
    return _Comment(
        issue_id=_text(_dig(document, "saltminer", "issue", "id")),
        added=_parse_datetime(_dig(document, "saltminer", "comment", "added")) or datetime.min,
        user=_text(_dig(document, "saltminer", "comment", "user")),
        user_full_name=_text(_dig(document, "saltminer", "comment", "userFullName")),
        message=_text(_dig(document, "saltminer", "comment", "message")),
    )


def _comment_text(issue_id: str, comments: list[_Comment], settings: ReportSettings) -> str:
    template = settings.comment_template
    maximum = settings.max_issue_comments
    if maximum < 1:
        logging.warning("[Reports][_comment_text] Report setting ReportMaxIssueComments is invalid. Setting to 1.")
        maximum = 1
    if not template:
        logging.warning("[Reports][_comment_text] Report setting ReportCommentTemplate is invalid. "
                        "Setting to default.")
        template = DEFAULT_COMMENT_TEMPLATE
    date_format = "d"
    date_token = "{Date}"
    if "{Date:" in template:
        match = _DATE_TOKEN.search(template)
        date_format = match.group(1) if match else ""
        date_token = "{Date:" + date_format + "}"

    chosen = sorted((c for c in comments if c.issue_id == issue_id), key=lambda c: c.added, reverse=True)[:maximum]
    if not settings.comment_sort_latest_first:
        chosen = sorted(chosen, key=lambda c: c.added)

    out: list[str] = []
    busted = False
    for comment in chosen:
        if busted:
            out.append(_default_comment(comment))
            continue
        try:
            rendered = (template
                        .replace(date_token, format_dotnet_date(comment.added, date_format))
                        .replace("{User}", comment.user)
                        .replace("{UserName}", comment.user_full_name)
                        .replace("{Message}", comment.message))
            out.append(rendered + "\n")
        except ValueError as exc:
            logging.error("[Reports][_comment_text] Report comment render failed for template: %s. Using "
                          "default comment template instead. Error: %s", template, exc)
            busted = True
            out.append(_default_comment(comment))
    return "".join(out)


def _default_comment(comment: _Comment) -> str:
    return f"[{format_dotnet_date(comment.added, SHORT_DATE_FORMAT)}] {comment.user}: {comment.message}\n"


# --------------------------------------------------------------------------------------------------
# Document mapping (the AssetFull, IssueFull and EngagementSummary constructors)
# --------------------------------------------------------------------------------------------------

@dataclass(frozen=True)
class _Asset:
    id: str
    name: str
    description: str


@dataclass(frozen=True)
class _Issue:
    id: str
    asset_id: str
    engagement_id: str
    name: str
    description: str
    product: str
    vendor: str
    severity: str
    severity_level: int
    report_id: str
    found_date: Any
    test_status: str
    testing_instructions: str
    is_suppressed: bool
    is_active: bool
    is_removed: bool
    removed_date: Any
    location: str
    location_full: str
    classification: str
    enumeration: str
    reference: str
    proof: str
    details: str
    implication: str
    recommendation: str
    references: str
    attributes: Any


def _asset_from(document: dict) -> _Asset:
    return _Asset(
        id=_text(_dig(document, "id")),
        name=_text(_dig(document, "saltminer", "asset", "name")),
        description=_text(_dig(document, "saltminer", "asset", "description")),
    )


def _flag(document: dict, name: str, fallback: bool) -> bool:
    value = _dig(document, "vulnerability", name)
    return fallback if value is None else bool(value)


def _issue_from(document: dict, draft: bool) -> _Issue:
    severity = _text(_dig(document, "vulnerability", "severity"))
    removed_date = _dig(document, "vulnerability", "removedDate")
    is_suppressed = bool(_dig(document, "vulnerability", "isSuppressed"))
    is_removed = _flag(document, "isRemoved", removed_date is not None)
    test_status = _text(_dig(document, "vulnerability", "testStatus"))
    is_filtered = bool(_dig(document, "vulnerability", "isFiltered"))
    is_active = _flag(document, "isActive",
                      not is_suppressed and not is_removed and not is_filtered
                      and (test_status == "" or test_status == "Found"))

    def vuln(name: str) -> str:
        return _text(_dig(document, "vulnerability", name))

    return _Issue(
        id=_text(_dig(document, "id")),
        asset_id=_text(_dig(document, "saltminer", "queueAssetId") if draft
                       else _dig(document, "saltminer", "asset", "id")),
        engagement_id=_text(_dig(document, "saltminer", "engagement", "id")),
        name=vuln("name"),
        description=vuln("description"),
        product=_trim(_dig(document, "vulnerability", "scanner", "product")),
        vendor=_trim(_dig(document, "vulnerability", "scanner", "vendor")),
        severity=severity,
        severity_level=_severity_level(severity),
        report_id=vuln("reportId"),
        found_date=_dig(document, "vulnerability", "foundDate"),
        test_status=test_status,
        testing_instructions=vuln("testingInstructions"),
        is_suppressed=is_suppressed,
        is_active=is_active,
        is_removed=is_removed,
        removed_date=removed_date,
        location=_trim(_dig(document, "vulnerability", "location")),
        location_full=_trim(_dig(document, "vulnerability", "locationFull")),
        classification=vuln("classification"),
        enumeration=vuln("enumeration"),
        reference=vuln("reference"),
        proof=vuln("proof"),
        details=vuln("details"),
        implication=vuln("implication"),
        recommendation=vuln("recommendation"),
        references=vuln("references"),
        attributes=_dig(document, "saltminer", "attributes"),
    )


# --------------------------------------------------------------------------------------------------
# Assembly
# --------------------------------------------------------------------------------------------------

def _issue_detail(issue: _Issue, definitions: list[_AttributeDefinition], comments: list[_Comment],
                  settings: ReportSettings) -> dict:
    """ReportProcessor.CreateIssueDetail."""
    detail = {
        "Name": issue.name,
        "Description": issue.description,
        "Product": issue.product,
        "Vendor": issue.vendor,
        "Severity": issue.severity,
        "SeverityLevel": str(issue.severity_level),
        "ReportId": issue.report_id,
        "AssetId": issue.asset_id,
        "FoundDate": _format_report_date(issue.found_date, "yyyy/MM/dd"),
        "TestStatus": issue.test_status,
        "TestingInstructions": issue.testing_instructions,
        # .NET writes issue.IsSuppressed.ToString() on a field object, which prints a type name and not the
        # value; this port writes the value, as the field evidently intends.
        "IsSuppressed": _text(issue.is_suppressed),
        "IsActive": _text(issue.is_active),
        "IsRemoved": _text(issue.is_removed),
        "RemovedDate": _format_report_date(issue.removed_date, "yyyy/MM/dd"),
        "Location": issue.location,
        "LocationFull": issue.location_full,
        "Classification": issue.classification,
        "Enumeration": issue.enumeration,
        "Reference": issue.reference,
    }
    for name in ("Proof", "Details", "Implication", "Recommendation", "References"):
        value = getattr(issue, name.lower())
        detail[name] = value
        detail[f"{name}Text"] = get_markdown_text(value)
        detail[f"{name}Imgs"] = get_markdown_images(value)
    detail["Comments"] = _comment_text(issue.id, comments, settings)
    # The UI API's own application version; the Data API has no such value.
    detail["AppVersion"] = ""
    detail["EngagementId"] = issue.engagement_id
    detail["Id"] = issue.id
    _add_attribute_properties(detail, "Issue", _attribute_fields(issue.attributes, definitions), definitions)
    return detail


def _fetch_issues(source: DataApiSource, engagement_id: str, assets: list[_Asset], state: str,
                  draft: bool) -> list[_Issue]:
    """One asset at a time, assets in name order, as ReportProcessor.CreateWordReport does.

    Design observation 2 settled: issues are fetched per asset, not by engagement id alone. The UI API's
    issue search adds the asset filter when the report asks for it (ContextBase.cs IssuesSearch and
    QueueIssuesSearch: Saltminer.Asset.Id, or Saltminer.QueueAssetId on a draft) and the .NET report always
    asks, so an issue on an asset the engagement no longer lists is never fetched. Fetching by engagement
    alone would add such issues to the totals. Each asset's issues are sorted here by severity level, name
    and id, the UI API's sort keys, and the caller then stable-sorts the whole list by severity level."""
    issues: list[_Issue] = []
    for asset in assets:
        found = [_issue_from(d, draft) for d in source.get_issues(engagement_id, asset.id, state, draft)]
        found.sort(key=lambda i: (i.severity_level, _name_key(i.name), i.id))
        issues.extend(found)
    return issues


def assemble_engagement(engagement_id: str, source: DataApiSource,
                        settings: ReportSettings) -> tuple[dict, frozenset[str]]:
    """Builds the engagement record and the markdown field set for one engagement.

    Returns (record, markdown_fields). The record is the root group's record; see the module docstring
    for its shape. Raises DataClientNotFoundException when the engagement does not exist.
    """
    engagement = source.get_engagement(engagement_id)
    status = _text(_dig(engagement, "saltminer", "engagement", "status"))
    draft = status.lower() == _DRAFT_STATUS.lower()

    definition_documents = source.get_attribute_definitions()
    engagement_defs = _definitions_for(definition_documents, "Engagement")
    issue_defs = _definitions_for(definition_documents, "Issue")

    assets = sorted((_asset_from(d) for d in source.get_assets(engagement_id, draft)),
                    key=lambda a: _name_key(a.name))
    comments = [_comment_from(d) for d in source.get_comments(engagement_id, settings.include_system_comments)]
    # ReportProcessor.CreateWordReport: active and removed issues, fetched per asset. Both lists are then
    # stable-sorted by severity level (CreateReportEngagementDto).
    active = sorted(_fetch_issues(source, engagement_id, assets, "IsActive", draft), key=lambda i: i.severity_level)
    removed = sorted(_fetch_issues(source, engagement_id, assets, "IsRemoved", draft),
                     key=lambda i: i.severity_level)
    logging.debug("[Reports][assemble_engagement] %s issues, %s removed issues, %s comments, %s assets",
                  len(active), len(removed), len(comments), len(assets))

    record: dict = {
        "Id": _text(_dig(engagement, "id")),
        "Name": _text(_dig(engagement, "saltminer", "engagement", "name")),
        "State": status,
        "Customer": _text(_dig(engagement, "saltminer", "engagement", "customer")),
        "Timestamp": _format_report_date(_dig(engagement, "timestamp"), "MM/dd/yyyy"),
        "Summary": _text(_dig(engagement, "saltminer", "engagement", "summary")),
        "PublishDate": _format_report_date(_dig(engagement, "saltminer", "engagement", "publishDate"),
                                           "MMMM dd, yyyy"),
    }
    _add_attribute_properties(
        record, "Engagement",
        _attribute_fields(_dig(engagement, "saltminer", "engagement", "attributes"), engagement_defs),
        engagement_defs)

    counts = {name: 0 for name in ("Critical", "High", "Medium", "Low", "Information", "Total")}
    closed = {name: 0 for name in ("Critical", "High", "Medium", "Low", "Information", "Total")}
    details: list[dict] = []
    details_removed: list[dict] = []
    asset_tocs: list[dict] = [
        {"Id": a.id, "Name": a.name, "Description": a.description, "SeverityGroups": []} for a in assets
    ]
    issue_tocs: list[dict] = []
    # The .NET code assigns the two TOC lists to the record from inside the issue loop, after the asset
    # check, so an engagement with no reportable issue reports empty TOCs. This port keeps that.
    published_tocs = False

    for issue in active:
        _count(counts, issue.severity)
        toc = next((t for t in asset_tocs if t["Id"] == issue.asset_id), None)
        if toc is None:
            logging.error("[Reports][assemble_engagement] Issue asset '%s' not found in list of assets for "
                          "engagement '%s'", issue.asset_id, engagement_id)
            continue
        _add_to_asset_toc(toc, issue)
        _add_to_issue_toc(issue_tocs, issue)
        published_tocs = True
        details.append(_issue_detail(issue, issue_defs, comments, settings))

    for issue in removed:
        _count(closed, issue.severity)
        details_removed.append(_issue_detail(issue, issue_defs, comments, settings))

    record.update({name: str(value) for name, value in counts.items() if name != "Total"})
    record.update({f"Closed_{name}": str(value) for name, value in closed.items() if name != "Total"})
    record["Total"] = str(counts["Total"])
    record["Closed_Total"] = str(closed["Total"])
    record["AssetTocs"] = _finish_asset_tocs(asset_tocs) if published_tocs else []
    record["IssueTocs"] = _finish_issue_tocs(issue_tocs) if published_tocs else []
    record["IssueDetails"] = details
    record["IssueDetailsRemoved"] = details_removed
    record["IssueDetailsAll"] = details + details_removed
    record["IssueSummary"] = list(details)
    record["IssueSummaryRemoved"] = list(details_removed)
    record["IssueSummaryAll"] = details + details_removed
    return record, _markdown_fields(engagement_defs, issue_defs)


def _count(tally: dict, severity: str) -> None:
    if severity in _COUNTED_SEVERITIES:
        tally[severity] += 1
    elif severity == "Info":
        tally["Information"] += 1
    tally["Total"] += 1


def _add_to_asset_toc(toc: dict, issue: _Issue) -> None:
    group = next((g for g in toc["SeverityGroups"] if g["Severity"] == issue.severity), None)
    if group is None:
        toc["SeverityGroups"].append({
            "Severity": issue.severity,
            "SeverityLevel": issue.severity_level,
            "Issues": [{"Name": issue.name, "Total": 1}],
        })
        return
    entry = next((e for e in group["Issues"] if e["Name"] == issue.name), None)
    if entry is None:
        group["Issues"].append({"Name": issue.name, "Total": 1})
    else:
        entry["Total"] += 1


def _add_to_issue_toc(tocs: list[dict], issue: _Issue) -> None:
    toc = next((t for t in tocs if t["Severity"] == issue.severity), None)
    if toc is None:
        tocs.append({
            "Severity": issue.severity,
            "SeverityLevel": issue.severity_level,
            "SeverityGroups": [{"Name": issue.name, "Total": 1}],
        })
        return
    entry = next((e for e in toc["SeverityGroups"] if e["Name"] == issue.name), None)
    if entry is None:
        toc["SeverityGroups"].append({"Name": issue.name, "Total": 1})
    else:
        entry["Total"] += 1


def _finish_asset_tocs(tocs: list[dict]) -> list[dict]:
    """Turns the working counts into the str leaves the record promises."""
    return [
        {
            "Id": t["Id"], "Name": t["Name"], "Description": t["Description"],
            "SeverityGroups": [
                {
                    "Severity": g["Severity"],
                    "SeverityLevel": str(g["SeverityLevel"]),
                    "Issues": [{"Name": e["Name"], "Total": str(e["Total"])} for e in g["Issues"]],
                }
                for g in t["SeverityGroups"]
            ],
        }
        for t in tocs
    ]


def _finish_issue_tocs(tocs: list[dict]) -> list[dict]:
    return [
        {
            "Severity": t["Severity"],
            "SeverityLevel": str(t["SeverityLevel"]),
            "SeverityGroups": [{"Name": e["Name"], "Total": str(e["Total"])} for e in t["SeverityGroups"]],
        }
        for t in tocs
    ]
