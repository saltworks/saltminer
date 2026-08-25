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

'''
Tanium debug harness.  Hit F5 on this file - no arguments, no argparse.

Every check is a boolean in the SWITCHES block below.  Flip what you want,
set a breakpoint anywhere, run.  Read-only throughout: nothing is written to
SaltMiner, the queue, or Elasticsearch.

OFFLINE checks stub the transport and need neither a token nor a network path.
Run them first - they cover every defect fixed in commit 431db48.

LIVE checks make real requests and need API_Key populated in
Config/Sources/TaniumClient.json.  They are skipped with a clear message if the
token is empty, rather than failing confusingly.

Working directory is handled for you: this file chdir's to the Python root so
Application can find Config/ no matter where the debugger launched from.
'''

import json
import os
import re
import sys
import time
import traceback

# Python root, three levels up from Sources/Tanium/.
_PY_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, _PY_ROOT)
os.chdir(_PY_ROOT)

from Core.Application import Application
from Core.ApplicationExceptions import ApplicationConfigurationException
import Sources.Tanium.TaniumClient as TC
import Sources.Tanium.tanium_runner as R
from Sources.Tanium.TaniumClient import TaniumClient, TaniumException, TaniumGraphQLException


# ===========================================================================
# SWITCHES - flip these by hand
# ===========================================================================

# -- offline: no token, no network required ---------------------------------
CHECK_CONFIG_LOADS            = False  # B2  client constructs off Base_Url / API_Key
CHECK_BASE_URL_PATH           = False  # B3  Base_Url is the endpoint, not the host
CHECK_AUTH_HEADER             = False  # D1  session header only, no bearer machinery
CHECK_SORT_ENUM               = False  # B1  sort order is lowercase `asc`
CHECK_QUERY_COMPOSITION       = False  # D2  core fields present, extension guarded
CHECK_NONLEAF_FIELD_REFUSED   = False  # D2  OBJECT-typed fields dropped, not requested
CHECK_RESTRICT_OWNER_COMMENT  = False  # D4  filter hazard is documented in the query
CHECK_TOTALRECORDS_QUERY      = False  # D5  baseline query selects no nodes
CHECK_CURSOR_GUARD            = False  # C1  hasNextPage + null endCursor raises
CHECK_NULL_VS_EMPTY           = False  # C2  IterFindings keeps None and [] distinct
CHECK_CENSUS_EXAMPLE_SLOT     = False  # C3  null_cve_findings example is captured
CHECK_IDLE_CLOCK_SCOPE        = False  # C4  only the paged query stamps the cursor clock
CHECK_THRESHOLD_ARGS          = False  # C5  --min-endpoints / --min-findings exist

# -- live: needs API_Key set ------------------------------------------------
LIVE_PREFLIGHT                = True  # totalRecords + sample shape + token metadata
LIVE_FIELD_RESOLUTION         = True  # real introspection: what the schema exposes
LIVE_SINGLE_PAGE              = True  # one page, dump it to the console
LIVE_PAGING                   = True  # 3 pages, cursors advance, no duplicate ids
LIVE_CENSUS                   = True  # null vs empty vs populated across the fleet
LIVE_PROBE_EXTENDED           = True  # which experimental fields the tenant populates
LIVE_SIZING                   = True  # bytes and latency at first = 10/50/100/250
LIVE_INTROSPECT_FINDING_TYPE  = True  # field list on EndpointComplianceCveFinding
LIVE_TOKEN_METADATA           = True  # expiration + trustedIPAddresses (best effort)

# -- knobs ------------------------------------------------------------------
PAGE_SIZE                = 5        # `first` for live page fetches
CENSUS_LIMIT             = 100      # endpoints to walk in LIVE_CENSUS
PAGING_PAGES             = 3
OUT_DIR                  = "./tanium_out/debug/"
WRITE_ARTIFACTS          = True     # write JSON for live checks
PRINT_FULL_PAYLOADS      = False    # dump whole nodes to console (noisy)
EXPECTED_FINDING_FIELDS  = 44       # schema says 44 on EndpointComplianceCveFinding
BREAK_BEFORE_EACH_CHECK  = False    # call breakpoint() before every enabled check
STOP_ON_FIRST_FAILURE    = False


# ===========================================================================
# harness
# ===========================================================================

_RESULTS = []

def _record(name, passed, detail=""):
    _RESULTS.append((name, passed, detail))
    flag = "PASS" if passed else "FAIL"
    line = f"  [{flag}] {name}"
    if detail:
        line += f"\n         {detail}"
    print(line)
    return passed


def _skip(name, why):
    _RESULTS.append((name, None, why))
    print(f"  [SKIP] {name}\n         {why}")


def _banner(text):
    print(f"\n{'=' * 74}\n{text}\n{'=' * 74}")


def _preview(value, width=60):
    if isinstance(value, (list, dict)):
        value = json.dumps(value, default=str)
    value = "" if value is None else str(value)
    return value if len(value) <= width else value[:width - 3] + "..."


def _write(name, obj):
    if not WRITE_ARTIFACTS:
        return
    os.makedirs(OUT_DIR, exist_ok=True)
    path = os.path.join(OUT_DIR, name)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(obj, f, indent=2, default=str)
    print(f"         wrote {path}")


# ===========================================================================
# raw query/response capture
# ===========================================================================
#
# Every check's derived artifacts (census.json, sizing.json, ...) are computed
# summaries. This captures the actual GraphQL request/response pairs behind
# them, one file per Post() call, numbered in call order and labeled with
# whichever check was running - so they can be read directly rather than
# re-derived.

_CALL_COUNT = 0
_CURRENT_CHECK = "unlabeled"


def _SafeName(text):
    return re.sub(r"[^A-Za-z0-9_.-]+", "_", text).strip("_") or "call"


def _InstrumentPost(client):
    '''
    Wraps client.Post so every live request/response pair this run makes is
    written under OUT_DIR/queries and OUT_DIR/responses. Errors (including
    GraphQL error arrays) are written to the response side rather than lost.
    '''
    original_post = client.Post
    queries_dir = os.path.join(OUT_DIR, "queries")
    responses_dir = os.path.join(OUT_DIR, "responses")

    def wrapped(query, variables=None):
        global _CALL_COUNT
        _CALL_COUNT += 1
        name = f"{_CALL_COUNT:03d}_{_SafeName(_CURRENT_CHECK)}.json"

        if WRITE_ARTIFACTS:
            os.makedirs(queries_dir, exist_ok=True)
            os.makedirs(responses_dir, exist_ok=True)
            with open(os.path.join(queries_dir, name), "w", encoding="utf-8") as f:
                json.dump({ "query": query, "variables": variables or {} }, f, indent=2, default=str)

        try:
            data = original_post(query, variables)
        except TaniumException as e:
            if WRITE_ARTIFACTS:
                with open(os.path.join(responses_dir, name), "w", encoding="utf-8") as f:
                    json.dump({ "error": type(e).__name__, "message": str(e),
                               "graphql_errors": getattr(e, "Errors", None) }, f, indent=2, default=str)
            raise

        if WRITE_ARTIFACTS:
            with open(os.path.join(responses_dir, name), "w", encoding="utf-8") as f:
                json.dump(data, f, indent=2, default=str)
        return data

    client.Post = wrapped
    return client


def _stub_introspection(client, endpoint_fields, finding_fields, nonleaf=()):
    '''
    Replaces IntrospectType on one client instance so query composition can be
    exercised with no network.  `nonleaf` names are reported as OBJECT-typed and
    must therefore be refused by the resolver.
    '''
    by_type = {
        TC.ENDPOINT_TYPE: list(endpoint_fields),
        TC.FINDING_TYPE:  list(finding_fields)
    }

    def fake(type_name):
        names = by_type.get(type_name)
        if names is None:
            return None
        fields = []
        for n in names:
            kind = "OBJECT" if n in nonleaf else "SCALAR"
            fields.append({ "name": n, "type": { "kind": "LIST", "ofType": { "kind": kind, "name": n } } })
        return { "name": type_name, "kind": "OBJECT", "fields": fields }

    client.IntrospectType = fake
    return client


# ===========================================================================
# shared setup
# ===========================================================================

def BuildClient():
    '''
    Constructs a client off the real config.  Returns (client, error_or_None).

    Kept separate from the checks so you can drop a breakpoint here and inspect
    app.Settings before anything touches it.
    '''
    try:
        app = Application(skipCleanFiles=True)
        client = TaniumClient(app.Settings)
        return client, None
    except ApplicationConfigurationException as e:
        return None, (f"[{type(e).__name__}] {e}\n         A key name is likely wrong. The "
                      f"client reads Base_Url and API_Key from source 'Tanium'.")
    except Exception as e:
        return None, (f"[{type(e).__name__}] {e}\n         Not a Tanium problem - this is the "
                      f"shared config/logging path failing, and it would break any Run script "
                      f"in this tree.")


def TokenPresent(client):
    return bool(client and client.token and str(client.token).strip())


# ===========================================================================
# offline checks
# ===========================================================================

def Check_ConfigLoads(client, err):
    if client is None:
        return _record("B2 config loads and client constructs", False, err)
    return _record("B2 config loads and client constructs", True,
                   f"page_size={client.page_size} allNamespaces={client.all_namespaces} "
                   f"timeout={client.request_timeout}")


def Check_BaseUrlPath(client):
    url = client.base_url or ""
    looks_like_endpoint = "/" in url.split("://", 1)[-1]
    return _record("B3 Base_Url is an endpoint path, not a bare host", looks_like_endpoint,
                   url if looks_like_endpoint else
                   f"{url!r} has no path. Posting GraphQL to a host root returns HTML, "
                   f"not a GraphQL error array.")


def Check_AuthHeader(client):
    headers = client.BuildHeaders()
    expected = { "Content-Type": "application/json", "session": client.token }
    clean = (headers == expected
             and not hasattr(TC, "AUTH_BEARER")
             and not hasattr(R, "ModeAuth")
             and "auth" not in R.MODES
             and "preflight" in R.MODES)
    return _record("D1 session header only, dual-auth machinery gone", clean,
                   f"headers={ {k: _preview(v, 24) for k, v in headers.items()} }  modes={sorted(R.MODES)}")


def Check_SortEnum(client):
    _stub_introspection(client, [], [])
    query = client.BuildEndpointsQuery()
    good = "order: asc" in query and "order: ASC" not in query
    return _record("B1 sort order is lowercase asc", good,
                   "SortOrder is asc/desc. `ASC` fails validation and every query dies.")


def Check_QueryComposition(client):
    _stub_introspection(client,
                        endpoint_fields=["entityProviderName"],          # entityProviderType absent
                        finding_fields=["detectedProducts"])
    keep_ep, keep_fi = client.ResolveFields(force=True)
    query = client.BuildEndpointsQuery()

    core_present    = all(f in query for f in TC.CORE_ENDPOINT_FIELDS + TC.CORE_FINDING_FIELDS)
    kept_present    = "entityProviderName" in query and "detectedProducts" in query
    dropped_absent  = "entityProviderType" not in query
    dropped_logged  = "entityProviderType" in client.DroppedFields.get(TC.ENDPOINT_TYPE, [])

    if PRINT_FULL_PAYLOADS:
        print(query)

    return _record("D2 core always requested, extension guarded by introspection",
                   core_present and kept_present and dropped_absent and dropped_logged,
                   f"kept: {TC.ENDPOINT_TYPE}={keep_ep} {TC.FINDING_TYPE}={keep_fi}  "
                   f"dropped={client.DroppedFields}")


def Check_NonLeafRefused(client):
    _stub_introspection(client,
                        endpoint_fields=[],
                        finding_fields=["detectedProducts", "cwes"],
                        nonleaf=("cwes",))
    _, keep_fi = client.ResolveFields(force=True)
    query = client.BuildEndpointsQuery()
    refused = "cwes" not in keep_fi and "cwes" not in query
    return _record("D2 OBJECT-typed extension field refused, not requested bare", refused,
                   f"kept={keep_fi}. An object field named without a sub-selection fails "
                   f"validation for the whole document.")


def Check_RestrictOwnerComment(client):
    _stub_introspection(client, [], [])
    query = client.BuildEndpointsQuery()
    return _record("D4 restrictOwner hazard documented at the cveFindings selection",
                   "restrictOwner: false" in query,
                   "A filter defaulting restrictOwner:true drops clean endpoints, which "
                   "then read as absent and mass-close.")


def Check_TotalRecordsQuery():
    q = TC.TOTAL_RECORDS_QUERY
    minimal = "totalRecords" in q and "edges" not in q and "compliance" not in q
    return _record("D5 baseline query selects no nodes", minimal,
                   "Reads one integer without pulling an endpoint and its findings.")


def Check_CursorGuard():
    raised = False
    try:
        R.NextCursor({ "hasNextPage": True, "endCursor": None }, page=1)
    except TaniumGraphQLException:
        raised = True

    tail = R.NextCursor({ "hasNextPage": False, "endCursor": "z" }, page=1)
    mid  = R.NextCursor({ "hasNextPage": True,  "endCursor": "abc" }, page=1)

    return _record("C1 hasNextPage with null endCursor raises instead of restarting",
                   raised and tail is None and mid == "abc",
                   "Passing that null as `after` silently re-requests page one, "
                   "duplicating every row with no error.")


def Check_NullVsEmpty():
    null_findings  = list(R.IterFindings({ "compliance": { "cveFindings": None } }))
    empty_findings = list(R.IterFindings({ "compliance": { "cveFindings": [] } }))
    populated      = list(R.IterFindings({ "compliance": { "cveFindings": [{ "cveId": "CVE-0000-1" }] } }))
    null_compl     = list(R.IterFindings({ "compliance": None }))

    counts = (R.CountFindings({ "compliance": None }),
              R.CountFindings({ "compliance": { "cveFindings": None } }),
              R.CountFindings({ "compliance": { "cveFindings": [] } }),
              R.CountFindings({ "compliance": { "cveFindings": [1, 2] } }))

    good = (null_findings == [] and empty_findings == [] and null_compl == []
            and len(populated) == 1 and counts == (0, 0, 0, 2))
    return _record("C2 null and empty stay distinct at the node", good,
                   f"CountFindings across (null compliance, null findings, [], 2 items) = {counts}")


def Check_CensusExampleSlot():
    with open(os.path.join("Sources", "Tanium", "tanium_runner.py"), encoding="utf-8") as f:
        src = f.read()
    wired = '"null_cve_findings": None' in src and 'examples["null_cve_findings"] = node' in src
    return _record("C3 census captures an example for the cveFindings-is-null shape", wired,
                   "Three absence shapes exist; all three now write a file to examples/.")


def Check_IdleClockScope():
    # Private attributes are name-mangled, so co_names carries the mangled form.
    # Matching on the bare "__last_request_at" passes vacuously - it is absent
    # from every method, including the ones that do set it.
    attr = f"_{TC.TaniumClient.__name__}__last_request_at"
    in_post = attr in TC.TaniumClient.Post.__code__.co_names
    in_page = attr in TC.TaniumClient.GetEndpointsPage.__code__.co_names
    # A by-id refetch is not part of any walk.  If it stamped the clock it would
    # extend the apparent life of a cursor it has nothing to do with - worse than
    # introspection doing it, because the fan-out design calls it constantly.
    in_byid = attr in TC.TaniumClient.GetEndpointById.__code__.co_names
    return _record("C4 only the paged query stamps the cursor idle clock",
                   (not in_post) and in_page and (not in_byid),
                   f"{attr}: Post={in_post} GetEndpointsPage={in_page} "
                   f"GetEndpointById={in_byid}. Idle window is per-cursor; anything "
                   f"outside the walk resetting it would mask an expired cursor.")


def Check_ThresholdArgs():
    defaults = R.ParseArgs([])
    tuned    = R.ParseArgs(["--min-endpoints", "10", "--min-findings", "5", "--probe-extended"])
    good = (defaults.min_endpoints == 0 and defaults.min_findings == 0
            and defaults.probe_extended is False
            and tuned.min_endpoints == 10 and tuned.min_findings == 5
            and tuned.probe_extended is True)
    return _record("C5 thresholds and --probe-extended parse, default off", good,
                   "Exit 2 when a run succeeds but collects nothing.")


# ===========================================================================
# live checks
# ===========================================================================

def Live_Preflight(client):
    total = client.GetTotalRecords()
    _record("LIVE preflight: totalRecords returned", isinstance(total, int), f"totalRecords={total}")

    page = client.GetEndpointsPage(first=1)
    edges = (page or {}).get("edges") or []
    if not edges:
        return _record("LIVE preflight: sample endpoint", False, "no endpoints at first=1")

    node = edges[0].get("node") or {}
    compliance = node.get("compliance")
    findings = None if compliance is None else compliance.get("cveFindings")
    shape = ("compliance is null" if compliance is None
             else "cveFindings is null" if findings is None
             else "cveFindings is empty" if len(findings) == 0
             else f"cveFindings populated ({len(findings)})")
    if PRINT_FULL_PAYLOADS:
        print(json.dumps(node, indent=2, default=str))
    _write("preflight_node.json", node)
    return _record("LIVE preflight: sample endpoint", True,
                   f"id={node.get('id')}  shape={shape}")


def Live_FieldResolution(client):
    keep_ep, keep_fi = client.ResolveFields(force=True)
    dropped = client.DroppedFields
    _write("field_resolution.json", { "kept_endpoint": keep_ep, "kept_finding": keep_fi,
                                      "dropped": dropped })
    any_dropped = any(v for v in dropped.values())
    detail = f"kept: {TC.ENDPOINT_TYPE}={keep_ep} {TC.FINDING_TYPE}={keep_fi}"
    if any_dropped:
        detail += f"\n         DROPPED (schema no longer exposes): {dropped}"
    return _record("LIVE extension fields resolved against the running schema", True, detail)


def Live_SinglePage(client):
    page = client.GetEndpointsPage(first=PAGE_SIZE)
    edges = (page or {}).get("edges") or []
    info = (page or {}).get("pageInfo") or {}
    findings = sum(R.CountFindings(e.get("node") or {}) for e in edges)
    if PRINT_FULL_PAYLOADS:
        print(json.dumps(page, indent=2, default=str))
    _write("single_page.json", { "data": { "endpoints": page } })
    return _record("LIVE single page fetch", bool(edges),
                   f"{len(edges)} endpoints, {findings} findings, "
                   f"hasNextPage={info.get('hasNextPage')}, {client.LastResponseBytes:,} bytes")


def Live_Paging(client):
    client.BeginWalk()
    after = None
    cursors, seen, dupes, rows = [], {}, [], []

    for page in range(1, PAGING_PAGES + 1):
        client.CheckCursorLifetime()
        endpoints = client.GetEndpointsPage(after=after, first=PAGE_SIZE)
        edges = (endpoints or {}).get("edges") or []
        info = (endpoints or {}).get("pageInfo") or {}

        ids = []
        for edge in edges:
            node_id = (edge.get("node") or {}).get("id")
            ids.append(node_id)
            if node_id in seen:
                dupes.append({ "id": node_id, "first_page": seen[node_id], "repeat_page": page })
            else:
                seen[node_id] = page

        rows.append({ "page": page, "count": len(edges), "ids": ids,
                      "endCursor": info.get("endCursor"), "hasNextPage": info.get("hasNextPage") })
        cursors.append(info.get("endCursor"))
        print(f"         page {page}: {len(edges)} endpoints  hasNextPage={info.get('hasNextPage')}")

        after = R.NextCursor(info, page)
        if after is None:
            break

    non_null = [c for c in cursors if c is not None]
    advanced = len(set(non_null)) == len(non_null)
    _write("paging.json", { "pages": rows, "cursors_advanced": advanced, "duplicates": dupes })
    return _record("LIVE paging: cursors advance, no duplicate node.id",
                   advanced and not dupes,
                   f"{len(seen)} unique ids across {len(rows)} page(s); "
                   f"{len(dupes)} duplicate(s)")


def Live_Census(client, probe_extended=False):
    client.EnableExtendedFields(probe_extended)
    # Field resolution is otherwise lazy (deferred to the first page fetch),
    # so RequestedFindingFields below would still reflect the *previous*
    # extension set. Force it now so the population dict matches what this
    # run actually requests.
    client.ResolveFields()
    label = "census + probe-extended" if probe_extended else "census"

    counts = { "seen": 0, "null_compliance": 0, "null_findings": 0, "empty_findings": 0,
               "populated": 0, "dup_cve": 0, "total_findings": 0, "max_findings": 0 }
    fields = client.RequestedFindingFields
    population = { f: { "non_null": 0, "null": 0, "example": None } for f in fields }

    stop = False
    for page in client.GetEndpointsGenerator(first=PAGE_SIZE):
        for edge in (page.get("edges") or []):
            node = edge.get("node")
            if node is None:
                continue
            counts["seen"] += 1
            compliance = node.get("compliance")
            if compliance is None:
                counts["null_compliance"] += 1
            else:
                findings = compliance.get("cveFindings")
                if findings is None:
                    counts["null_findings"] += 1
                elif len(findings) == 0:
                    counts["empty_findings"] += 1
                else:
                    counts["populated"] += 1
                    counts["total_findings"] += len(findings)
                    counts["max_findings"] = max(counts["max_findings"], len(findings))
                    cve_ids = []
                    for finding in findings:
                        cve_ids.append(finding.get("cveId"))
                        for f in fields:
                            value = finding.get(f)
                            if value is None:
                                population[f]["null"] += 1
                            else:
                                population[f]["non_null"] += 1
                                if population[f]["example"] is None:
                                    population[f]["example"] = value
                    if len(cve_ids) != len(set(cve_ids)):
                        counts["dup_cve"] += 1
            if counts["seen"] >= CENSUS_LIMIT:
                stop = True
                break
        if stop:
            break

    for key, value in counts.items():
        print(f"         {key:<18}: {value}")

    if probe_extended:
        print(f"\n         {'field':<26} {'non_null':>9} {'null':>9}  example")
        for f in fields:
            s = population[f]
            print(f"         {f:<26} {s['non_null']:>9} {s['null']:>9}  {_preview(s['example'], 40)}")
        dropped = { k: v for k, v in client.DroppedFields.items() if v }
        if dropped:
            print(f"\n         not in schema: {dropped}")

    _write("census_extended.json" if probe_extended else "census.json",
           { "counts": counts, "requested_fields": fields, "population": population,
             "dropped": client.DroppedFields })

    client.EnableExtendedFields(False)
    return _record(f"LIVE {label}", counts["seen"] > 0,
                   f"{counts['seen']} endpoints, {counts['total_findings']} findings. "
                   f"All-empty is ambiguous, not a pass.")


def Live_Sizing(client):
    rows = []
    for first in (10, 50, 100, 250):
        started = time.monotonic()
        page = client.GetEndpointsPage(first=first)
        elapsed = time.monotonic() - started
        edges = (page or {}).get("edges") or []
        findings = sum(R.CountFindings(e.get("node") or {}) for e in edges)
        per = round(client.LastResponseBytes / len(edges)) if edges else 0
        rows.append({ "first": first, "elapsed_s": round(elapsed, 3),
                      "bytes": client.LastResponseBytes, "endpoints": len(edges),
                      "findings": findings, "bytes_per_endpoint": per })
        print(f"         first={first:>4}  {elapsed:6.2f}s  {client.LastResponseBytes:>10,} bytes  "
              f"{len(edges):>4} endpoints  {findings:>5} findings  {per:>8,} b/endpoint")
    _write("sizing.json", rows)
    return _record("LIVE sizing", True, "Use bytes/endpoint and latency to pick Page_Size.")


def Live_IntrospectFindingType(client):
    result = client.IntrospectType(TC.FINDING_TYPE)
    if result is None:
        return _record("LIVE introspect EndpointComplianceCveFinding", False,
                       f"schema has no type named {TC.FINDING_TYPE!r}")
    fields = result.get("fields") or []
    names = [f.get("name") for f in fields]
    _write("introspect_finding_type.json", result)

    mapped = set(TC.CORE_FINDING_FIELDS) | set(TC.EXTENSION_FINDING_FIELDS)
    unrequested = [n for n in names if n not in mapped]
    print(f"         {len(fields)} fields; {len(unrequested)} not currently requested")
    if PRINT_FULL_PAYLOADS:
        for f in fields:
            ftype = f.get("type") or {}
            print(f"           {f.get('name')}: {ftype.get('name') or (ftype.get('ofType') or {}).get('name')}")

    return _record("LIVE introspect EndpointComplianceCveFinding",
                   len(fields) == EXPECTED_FINDING_FIELDS,
                   f"got {len(fields)}, expected {EXPECTED_FINDING_FIELDS}. A mismatch means "
                   f"the schema moved - re-check the stability tiers before trusting the mapping.")


def Live_TokenMetadata(client):
    tokens = client.GetMyApiTokens()
    if not tokens:
        # GetMyApiTokens() swallows the real error (see TaniumClient.py), so
        # re-introspect here to tell a validation failure (query shape is wrong -
        # fixable) apart from a permission failure (needs Token - View - not
        # fixable from here). APITokenQueryPayload is the type name the live
        # gateway reported in the GRAPHQL_VALIDATION_FAILED error; if that name
        # ever changes this just reports "no such type" instead of guessing.
        shape = client.IntrospectType("APITokenQueryPayload")
        if shape:
            names = [f.get("name") for f in (shape.get("fields") or [])]
            _write("token_metadata_shape.json", shape)
            detail = (f"Query shape is wrong, not a permissions problem: "
                      f"APITokenQueryPayload actually has fields {names}.")

            # Walk NON_NULL/LIST wrappers to find the named OBJECT type of the
            # `tokens` field (if present) and introspect one level further, so
            # the fix doesn't require a second manual round trip.
            tokens_field = next((f for f in (shape.get("fields") or []) if f.get("name") == "tokens"), None)
            if tokens_field:
                node = tokens_field.get("type") or {}
                for _ in range(4):
                    if node.get("name"):
                        break
                    node = node.get("ofType") or {}
                token_type_name = node.get("name")
                if token_type_name:
                    token_shape = client.IntrospectType(token_type_name)
                    if token_shape:
                        token_names = [f.get("name") for f in (token_shape.get("fields") or [])]
                        _write("token_shape.json", token_shape)
                        detail += (f" tokens[] is {token_type_name!r} with fields {token_names}. "
                                  f"Update MY_API_TOKENS_QUERY in TaniumClient.py to match.")

            return _record("LIVE token metadata", False, detail)
        return _record("LIVE token metadata", False,
                       "Unavailable. Needs 'Token - View' on the token, and the myAPITokens "
                       "shape in this client is inferred - verify against your schema file.")
    _write("token_metadata.json", tokens)
    for t in (tokens if isinstance(tokens, list) else [tokens]):
        if isinstance(t, dict):
            print(f"         id={t.get('id')} expires={t.get('expiration')} "
                  f"trustedIPs={t.get('trustedIPAddresses')}")
    return _record("LIVE token metadata", True,
                   "Expiration and trustedIPAddresses both silently break a scheduled run.")


# ===========================================================================
# entry point
# ===========================================================================

_ABORT = False

def _run(label, fn, *args):
    global _ABORT, _CURRENT_CHECK
    if _ABORT:
        _skip(label, "skipped - STOP_ON_FIRST_FAILURE is set and an earlier check failed")
        return False

    _CURRENT_CHECK = label

    if BREAK_BEFORE_EACH_CHECK:
        print(f"\n>>> breakpoint before: {label}")
        breakpoint()

    try:
        result = fn(*args)
    except TaniumException as e:
        result = _record(label, False, f"[{type(e).__name__}] {e}")
    except Exception as e:
        _record(label, False, f"[{type(e).__name__}] {e}")
        traceback.print_exc()
        result = False

    if result is False and STOP_ON_FIRST_FAILURE:
        _ABORT = True
    return result


def Main():
    print(f"cwd -> {os.getcwd()}")

    client, err = BuildClient()
    if client is not None:
        _InstrumentPost(client)

    _banner("OFFLINE  (no token or network required)")

    if CHECK_CONFIG_LOADS:
        _run("B2 config loads", Check_ConfigLoads, client, err)
    if client is None:
        print("\nClient could not be constructed; skipping everything else.")
        return _summary()

    # Offline checks stub introspection on a throwaway client so the live checks
    # below still talk to the real schema.
    offline_client, _ = BuildClient()

    if CHECK_BASE_URL_PATH:
        _run("B3 Base_Url path", Check_BaseUrlPath, offline_client)
    if CHECK_AUTH_HEADER:
        _run("D1 auth header", Check_AuthHeader, offline_client)
    if CHECK_SORT_ENUM:
        _run("B1 sort enum", Check_SortEnum, offline_client)
    if CHECK_QUERY_COMPOSITION:
        _run("D2 query composition", Check_QueryComposition, offline_client)
    if CHECK_NONLEAF_FIELD_REFUSED:
        _run("D2 non-leaf refused", Check_NonLeafRefused, offline_client)
    if CHECK_RESTRICT_OWNER_COMMENT:
        _run("D4 restrictOwner comment", Check_RestrictOwnerComment, offline_client)
    if CHECK_TOTALRECORDS_QUERY:
        _run("D5 totalRecords query", Check_TotalRecordsQuery)
    if CHECK_CURSOR_GUARD:
        _run("C1 cursor guard", Check_CursorGuard)
    if CHECK_NULL_VS_EMPTY:
        _run("C2 null vs empty", Check_NullVsEmpty)
    if CHECK_CENSUS_EXAMPLE_SLOT:
        _run("C3 census example slot", Check_CensusExampleSlot)
    if CHECK_IDLE_CLOCK_SCOPE:
        _run("C4 idle clock scope", Check_IdleClockScope)
    if CHECK_THRESHOLD_ARGS:
        _run("C5 threshold args", Check_ThresholdArgs)

    live_wanted = any([LIVE_PREFLIGHT, LIVE_FIELD_RESOLUTION, LIVE_SINGLE_PAGE, LIVE_PAGING,
                       LIVE_CENSUS, LIVE_PROBE_EXTENDED, LIVE_SIZING,
                       LIVE_INTROSPECT_FINDING_TYPE, LIVE_TOKEN_METADATA])
    if not live_wanted:
        print("\nNo live checks enabled.")
        return _summary()

    _banner(f"LIVE  ->  {client.base_url}")

    if not TokenPresent(client):
        _skip("all live checks",
              "API_Key is empty in Config/Sources/TaniumClient.json. Set it and re-run.")
        return _summary()

    if LIVE_PREFLIGHT:
        _run("LIVE preflight", Live_Preflight, client)
    if LIVE_FIELD_RESOLUTION:
        _run("LIVE field resolution", Live_FieldResolution, client)
    if LIVE_SINGLE_PAGE:
        _run("LIVE single page", Live_SinglePage, client)
    if LIVE_PAGING:
        _run("LIVE paging", Live_Paging, client)
    if LIVE_CENSUS:
        _run("LIVE census", Live_Census, client, False)
    if LIVE_PROBE_EXTENDED:
        _run("LIVE probe extended", Live_Census, client, True)
    if LIVE_SIZING:
        _run("LIVE sizing", Live_Sizing, client)
    if LIVE_INTROSPECT_FINDING_TYPE:
        _run("LIVE introspect finding type", Live_IntrospectFindingType, client)
    if LIVE_TOKEN_METADATA:
        _run("LIVE token metadata", Live_TokenMetadata, client)

    stats = client.run_stats
    print(f"\n  requests made: {stats['requests']}   elapsed: {stats['elapsed_s']}s")
    return _summary()


def _summary():
    _banner("SUMMARY")
    passed  = [r for r in _RESULTS if r[1] is True]
    failed  = [r for r in _RESULTS if r[1] is False]
    skipped = [r for r in _RESULTS if r[1] is None]
    for name, _, detail in failed:
        print(f"  FAIL  {name}")
    print(f"\n  {len(passed)} passed, {len(failed)} failed, {len(skipped)} skipped")
    return 0 if not failed else 1


if __name__ == "__main__":
    sys.exit(Main())
