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
Tanium Gateway test runner.

Read-only.  Executes the client, writes raw payloads to disk, prints a summary.
Writes nothing to SaltMiner, the queue, or Elasticsearch.

The point of this file is to put real payloads on disk so the mapping can be
written against captured data instead of documentation.

    python Sources/Tanium/tanium_runner.py --mode preflight
    python Sources/Tanium/tanium_runner.py --mode dump --pages 2 --first 5 --out ./tanium_out/
    python Sources/Tanium/tanium_runner.py --mode census --limit 500
    python Sources/Tanium/tanium_runner.py --mode census --limit 500 --probe-extended
    python Sources/Tanium/tanium_runner.py --mode introspect --type EndpointComplianceCveFinding

Run it from the Saltworks.SaltMiner.Python directory.  Imports are resolved off
this file's own location, but Application locates Config/ relative to the
working directory.

Exit codes: 0 ok, 1 TaniumException, 2 threshold not met, 130 interrupt.
'''

import argparse
import json
import logging
import os
import sys
import time
from datetime import datetime, timedelta, timezone

# Repo root, three levels up from Sources/Tanium/, so the script runs standalone.
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

from Core.Application import Application
from Sources.Tanium.TaniumClient import (
    TaniumClient,
    TaniumException,
    TaniumGraphQLException,
    ENDPOINT_TYPE,
    FINDING_TYPE
)


# Filter-cost probe.  Selects one scalar and no compliance block, so the only
# thing that varies between the three runs is the filter - transfer time would
# otherwise swamp the lookup cost this is trying to measure.
#
# totalRecords is deliberately NOT selected: on a filtered query it may force a
# count the unfiltered one gets from a cached stat, which would confound exactly
# the comparison being made.
FILTER_COST_PROBE_QUERY = '''
query TaniumFilterCost($allNamespaces: Boolean = true%(filter_var)s) {
  endpoints(
    first: 1
    source: { tds: { allNamespaces: $allNamespaces } }
%(filter_arg)s  ) {
    edges { node { id } }
  }
}
'''

# P2 probe only.  Kept here rather than in the client because nothing in
# collection uses it: it exists to answer whether UPDATED_AFTER is usable at
# all, and a negative answer is a perfectly good outcome.
UPDATED_AFTER_PROBE_QUERY = '''
query TaniumUpdatedAfterProbe($allNamespaces: Boolean = true, $since: String!) {
  endpoints(
    first: 1
    filter: { path: "%(path)s", op: UPDATED_AFTER, value: $since }
    source: { tds: { allNamespaces: $allNamespaces } }
  ) {
    totalRecords
  }
}
'''


# ---------------------------------------------------------------------------
# artifact helpers
# ---------------------------------------------------------------------------

def WriteJson(out_dir, name, obj):
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, name)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(obj, f, indent=2, default=str)
    print(f"  wrote {path}")
    return path


def Envelope(endpoints):
    '''
    Rebuilds the response envelope around a raw `endpoints` block.

    The client strips only the outer `data` key, and an `errors` payload raises
    rather than returning, so this is the full response as received.
    '''
    return { "data": { "endpoints": endpoints } }


def NextCursor(page_info, page):
    '''
    Returns the cursor for the next page, or None when the walk is complete.

    Raises when Tanium reports hasNextPage with no endCursor.  Passing that null
    straight through as `after` restarts the walk from record one, and the caller
    cannot tell the difference - the second page simply repeats the first, with
    duplicate rows and no error.  GetEndpointsGenerator guards this already; the
    hand-rolled loops in this file are what dropped it.
    '''
    if not page_info.get("hasNextPage"):
        return None
    cursor = page_info.get("endCursor")
    if not cursor:
        raise TaniumGraphQLException(
            f"Tanium reported hasNextPage=true with no endCursor at page {page}. "
            "Refusing to loop - the next request would silently restart the walk.")
    return cursor


def IterFindings(node):
    '''
    Yields findings for one endpoint node without collapsing null into empty.
    Returns nothing for both cases; callers that care must inspect the node.
    '''
    compliance = node.get("compliance")
    if compliance is None:
        return
    findings = compliance.get("cveFindings")
    if findings is None:
        return
    for finding in findings:
        yield finding


def CountFindings(node):
    compliance = node.get("compliance")
    if compliance is None:
        return 0
    findings = compliance.get("cveFindings")
    if findings is None:
        return 0
    return len(findings)


# ---------------------------------------------------------------------------
# modes
# ---------------------------------------------------------------------------

def ModePreflight(client, args):
    '''
    One first: 1 request.  Confirms the endpoint path, the token, and that the
    compliance block comes back at all.  Prints raw error text on failure.

    Replaces the old dual-scheme auth probe.  Tanium authenticates with the API
    token in a `session` header; Authorization: Bearer returns 401.  There is no
    longer a scheme to discover.
    '''
    print("\n== preflight ==")
    result = { "ok": False, "total_records": None, "error": None }

    try:
        total = client.GetTotalRecords()
        result["total_records"] = total
        print(f"  OK      totalRecords={total}")
    except TaniumException as e:
        result["error"] = f"[{type(e).__name__}] {e}"
        print(f"  FAILED  [{type(e).__name__}] {e}")
        print("\n  A non-JSON body here usually means Base_Url points at the host")
        print("  rather than the GraphQL endpoint path.")
        WriteJson(args.out, "preflight.json", result)
        return { "endpoints": 0, "findings": 0 }

    # Field resolution is one introspection call per type, and it is the thing
    # most likely to be wrong after a vendor release.  Surface it here.
    keep_ep, keep_fi = client.ResolveFields()
    result["extension_fields_kept"] = { ENDPOINT_TYPE: keep_ep, FINDING_TYPE: keep_fi }
    result["extension_fields_dropped"] = client.DroppedFields
    print(f"\n  extension fields kept   : {ENDPOINT_TYPE}={keep_ep} {FINDING_TYPE}={keep_fi}")
    for type_name, dropped in client.DroppedFields.items():
        if dropped:
            print(f"  DROPPED on {type_name}: {dropped}")

    endpoints = client.GetEndpointsPage(first=1)
    edges = (endpoints or {}).get("edges") or []
    if not edges:
        print("\n  no endpoints returned at first=1.")
        result["ok"] = True
        WriteJson(args.out, "preflight.json", result)
        return { "endpoints": 0, "findings": 0 }

    node = edges[0].get("node") or {}
    compliance = node.get("compliance")
    findings = None if compliance is None else compliance.get("cveFindings")
    shape = ("compliance is null" if compliance is None
             else "cveFindings is null" if findings is None
             else "cveFindings is empty" if len(findings) == 0
             else f"cveFindings populated ({len(findings)})")
    result["sample_endpoint_id"] = node.get("id")
    result["sample_shape"] = shape
    result["ok"] = True
    print(f"  sample endpoint         : {node.get('id')}")
    print(f"  compliance shape        : {shape}")

    tokens = client.GetMyApiTokens()
    if tokens:
        print("\n  token metadata:")
        token_summaries = []
        for i, t in enumerate(tokens if isinstance(tokens, list) else [tokens], start=1):
            if isinstance(t, dict):
                trusted_ips = t.get("trustedIPAddresses")
                if isinstance(trusted_ips, list):
                    trusted_ips_summary = "configured" if len(trusted_ips) > 0 else "none"
                elif trusted_ips:
                    trusted_ips_summary = "configured"
                else:
                    trusted_ips_summary = "none"
                expiration_state = "set" if t.get("expiration") else "none"
                token_summaries.append({
                    "token_index": i,
                    "expiration": expiration_state,
                    "trusted_ips": trusted_ips_summary
                })
        print(f"    tokens discovered     : {len(token_summaries)}")
        result["token_summaries"] = token_summaries
    else:
        print("\n  token metadata          : unavailable (needs 'Token - View')")

    # Filter cost.  Needs a known-good id and serial, which the sample above just
    # supplied - hence preflight rather than census.
    if node.get("serialNumber"):
        cost = RunProbe("filter_cost",
                        lambda: ProbeFilterCost(client, node.get("id"), node.get("serialNumber")))
        result["filter_cost"] = cost
        print("\n  == filter cost (is the id filter index-backed?) ==")
        for name in ("baseline", "id", "serial"):
            t = (cost.get("timings") or {}).get(name)
            if t:
                print(f"    {name:<10} min {t['min_ms']:>8.1f} ms   "
                      f"median {t['median_ms']:>8.1f} ms   ({t['runs']} runs)")
            else:
                print(f"    {name:<10} did not complete")
        if cost.get("serial_vs_id") is not None:
            print(f"    ratios     id/baseline={cost.get('id_vs_baseline')}  "
                  f"serial/id={cost.get('serial_vs_id')}")
        for name, err in (cost.get("errors") or {}).items():
            print(f"    error [{name}]: {err}")
        print(f"    verdict    {cost.get('verdict')}")
        total = result.get("total_records")
        if total is not None and isinstance(total, int) and total < 1000:
            print(f"    NOTE       tenant has {total} endpoint(s); a scan and an index lookup "
                  f"are indistinguishable at this size.\n"
                  f"               serial/id is the only signal that survives a small fleet.")
    else:
        print("\n  filter cost probe       : skipped (sample endpoint has no serialNumber)")

    WriteJson(args.out, "preflight.json", result)
    return { "endpoints": len(edges), "findings": CountFindings(node) }


def ModeDump(client, args):
    '''
    Pulls --pages pages at --first.  Writes each full response envelope
    unmodified, plus a flattened findings.jsonl.

    Keeps its own pager rather than using GetEndpointsGenerator because the
    generator yields nodes and discards the page envelope, which is the artifact
    this mode exists to produce.
    '''
    print(f"\n== dump ==  pages={args.pages} first={args.first or client.page_size}")
    os.makedirs(args.out, exist_ok=True)
    jsonl_path = os.path.join(args.out, "findings.jsonl")

    after = None
    total_endpoints = 0
    total_findings = 0
    client.BeginWalk()

    with open(jsonl_path, "w", encoding="utf-8") as jsonl:
        for page in range(1, args.pages + 1):
            client.CheckCursorLifetime()
            endpoints = client.GetEndpointsPage(after=after, first=args.first)
            WriteJson(args.out, f"page_{page:03d}.json", Envelope(endpoints))

            edges = (endpoints or {}).get("edges") or []
            for edge in edges:
                node = edge.get("node") or {}
                total_endpoints += 1
                for finding in IterFindings(node):
                    row = dict(finding)
                    row["_endpoint_id"] = node.get("id")
                    row["_endpoint_name"] = node.get("name")
                    jsonl.write(json.dumps(row, default=str) + "\n")
                    total_findings += 1

            page_info = (endpoints or {}).get("pageInfo") or {}
            print(f"  page {page}: {len(edges)} endpoints, hasNextPage={page_info.get('hasNextPage')}")
            after = NextCursor(page_info, page)
            if after is None:
                print("  fleet exhausted before page budget.")
                break

    print(f"  wrote {jsonl_path}")
    return { "endpoints": total_endpoints, "findings": total_findings }


def ModePaging(client, args):
    '''
    3 pages at first: 5.  Asserts cursors advance, asserts zero duplicate
    node.id, prints hasNextPage at the tail.
    '''
    print("\n== paging ==  3 pages at first=5")
    after = None
    cursors = []
    seen_ids = {}
    duplicates = []
    pages = []
    total_endpoints = 0
    total_findings = 0
    client.BeginWalk()

    for page in range(1, 4):
        client.CheckCursorLifetime()
        endpoints = client.GetEndpointsPage(after=after, first=5)
        edges = (endpoints or {}).get("edges") or []
        page_info = (endpoints or {}).get("pageInfo") or {}

        page_cursors = [e.get("cursor") for e in edges]
        page_ids = []
        for edge in edges:
            node = edge.get("node") or {}
            node_id = node.get("id")
            page_ids.append(node_id)
            total_endpoints += 1
            total_findings += CountFindings(node)
            if node_id in seen_ids:
                duplicates.append({ "id": node_id, "first_page": seen_ids[node_id], "repeat_page": page })
            else:
                seen_ids[node_id] = page

        pages.append({
            "page": page,
            "after_sent": after,
            "count": len(edges),
            "node_ids": page_ids,
            "edge_cursors": page_cursors,
            "hasNextPage": page_info.get("hasNextPage"),
            "endCursor": page_info.get("endCursor")
        })
        cursors.append(page_info.get("endCursor"))
        print(f"  page {page}: {len(edges)} endpoints, endCursor={page_info.get('endCursor')!r}, hasNextPage={page_info.get('hasNextPage')}")

        after = NextCursor(page_info, page)
        if after is None:
            print("  fleet exhausted before 3 pages.")
            break

    non_null = [c for c in cursors if c is not None]
    cursors_advanced = len(set(non_null)) == len(non_null)

    print(f"\n  cursors advanced      : {'PASS' if cursors_advanced else 'FAIL'}")
    print(f"  duplicate node.id     : {'PASS (0)' if not duplicates else f'FAIL ({len(duplicates)})'}")
    print(f"  hasNextPage at tail   : {pages[-1]['hasNextPage'] if pages else None}")

    WriteJson(args.out, "paging.json", {
        "pages": pages,
        "cursors_advanced": cursors_advanced,
        "duplicate_node_ids": duplicates,
        "has_next_page_at_tail": pages[-1]["hasNextPage"] if pages else None
    })
    return { "endpoints": total_endpoints, "findings": total_findings }


def ShapeOf(container, key):
    '''
    Reports missing / null / empty / populated for one key, without collapsing them.

    The whole point of the census is that these four are different answers, and
    `if not x` turns all four into one.  Probe reports have to preserve the
    distinction for the same reason the walk does.
    '''
    if container is None:
        return "parent-missing"
    if key not in container:
        return "missing"
    value = container[key]
    if value is None:
        return "null"
    if len(value) == 0:
        return "empty"
    return f"populated({len(value)})"


def RunProbe(name, fn):
    '''
    Runs one probe and normalises however it failed into a report dict.

    Probes are diagnostics, not collection: a probe that raises must never take
    the census walk down with it.  A GraphQL error here is the finding, not an
    accident, so Errors is recorded verbatim rather than summarised.
    '''
    report = { "probe": name, "ok": False, "http_status": None,
               "graphql_errors": None, "error": None }
    try:
        result = fn()
        report.update(result)
        report["ok"] = result.get("ok", True)
        # Anything that returned through Post() came back HTTP 200; Post raises
        # on every other status, so claiming a code here would be inventing one.
        if report["http_status"] is None:
            report["http_status"] = 200
    except TaniumGraphQLException as e:
        report["graphql_errors"] = e.Errors
        report["http_status"] = 200
        report["error"] = str(e)
    except TaniumException as e:
        report["error"] = f"[{type(e).__name__}] {e}"
    except Exception as e:                              # noqa: BLE001 - probe must not abort census
        report["error"] = f"[{type(e).__name__}] {e}"
    return report


def ProbeFilterCost(client, sample_id, sample_serial, reps=3):
    '''
    Is `filter: {path:"id", op:EQ}` index-backed, or a scan?

    The whole per-endpoint fan-out design rests on a cheap point lookup.  Nothing
    in the schema answers this - Tanium does not disclose the backing store - so
    it can only be settled empirically.  Three queries, identical except for the
    filter:

      baseline    unfiltered first: 1        cheapest possible query
      id          filter on id               the one the design depends on
      serial      filter on serialNumber     control: almost certainly unindexed

    Reading it:
      id ~= baseline, serial much slower  ->  id is index-backed, design holds
      id ~= serial                        ->  both are scans, fan-out dies at scale

    On a small tenant absolute latency proves nothing: a scan over 200 endpoints
    and an index lookup are indistinguishable in wall-clock.  The serial control
    is the signal that survives that, because it is a *relative* measure.

    Runs are interleaved rather than batched per variant so warm-up and network
    drift land on all three equally.  Minimum is reported alongside median: the
    floor is the cleanest estimate of cost with noise removed.
    '''
    variants = {
        "baseline": ("", "", {}),
        "id":       (", $v: String!", '    filter: { path: "id", op: EQ, value: $v }\n',
                     {"v": str(sample_id)}),
        "serial":   (", $v: String!", '    filter: { path: "serialNumber", op: EQ, value: $v }\n',
                     {"v": str(sample_serial)}),
    }
    timings = {k: [] for k in variants}
    errors  = {}
    matched = {}

    for _ in range(reps):
        for name, (fvar, farg, extra) in variants.items():
            if name in errors:
                continue
            query = FILTER_COST_PROBE_QUERY % {"filter_var": fvar, "filter_arg": farg}
            variables = {"allNamespaces": client.all_namespaces, **extra}
            start = time.perf_counter()
            try:
                data = client.Post(query, variables)
                timings[name].append((time.perf_counter() - start) * 1000.0)
                edges = ((data.get("endpoints") or {}).get("edges")) or []
                matched[name] = len(edges)
            except TaniumGraphQLException as e:
                errors[name] = json.dumps(e.Errors)[:300]
            except Exception as e:                      # noqa: BLE001 - probe must not abort
                errors[name] = f"[{type(e).__name__}] {e}"

    def stats(vals):
        if not vals:
            return None
        s = sorted(vals)
        return {"min_ms": round(s[0], 1), "median_ms": round(s[len(s) // 2], 1),
                "runs": len(s)}

    report = {"probe": "filter_cost", "reps": reps,
              "sample_id": str(sample_id), "sample_serial": str(sample_serial),
              "timings": {k: stats(v) for k, v in timings.items()},
              "matched_edges": matched, "errors": errors or None,
              "verdict": None, "ok": False}

    base = report["timings"].get("baseline")
    idt  = report["timings"].get("id")
    ser  = report["timings"].get("serial")

    if not base or not idt:
        report["verdict"] = "inconclusive - baseline or id query did not complete"
    elif not ser:
        report["verdict"] = ("inconclusive - serialNumber filter unavailable, so there is no "
                             "control to compare against")
    else:
        b, i, s = base["min_ms"], idt["min_ms"], ser["min_ms"]
        report["id_vs_baseline"]    = round(i / b, 2) if b else None
        report["serial_vs_baseline"] = round(s / b, 2) if b else None
        report["serial_vs_id"]      = round(s / i, 2) if i else None
        if s >= i * 1.5:
            report["ok"] = True
            report["verdict"] = (f"id filter looks index-backed - serial is {round(s/i,2)}x the "
                                 f"cost of id. Per-endpoint fan-out is safe on this evidence.")
        else:
            report["verdict"] = (f"id and serial cost about the same ({round(s/i,2)}x) - both may "
                                 f"be scans. If the fleet here is small this is NOT conclusive; "
                                 f"re-run on a larger tenant before committing to fan-out.")
    return report


def ProbeByIdFilter(client, sample_id, walk_finding_count):
    '''
    P1: confirms on the live tenant that `endpoints(filter: {path:"id", op:EQ})`
    returns the same endpoint the walk returned.

    The schema says this works.  A failure here is therefore worth more than a
    pass - it means the fan-out architecture cannot be built on this tenant, and
    the verbatim GraphQL error is the only thing that explains why.
    '''
    def run():
        envelope = client.GetEndpointById(sample_id)
        if envelope is None:
            return { "ok": False, "detail": "response carried no 'endpoints' block" }

        total = envelope.get("totalRecords")
        edges = envelope.get("edges") or []
        node = (edges[0] or {}).get("node") if edges else None
        compliance = node.get("compliance") if node else None
        refetched = CountFindings(node) if node else None

        return {
            "ok": total == 1 and node is not None and refetched == walk_finding_count,
            "requested_id": str(sample_id),
            "total_records": total,
            "returned_id": node.get("id") if node else None,
            "id_matches": (str(node.get("id")) == str(sample_id)) if node else False,
            "compliance_shape": "null" if node is not None and compliance is None
                                else ("missing" if node is not None and "compliance" not in node
                                      else "present"),
            "cve_findings_shape": ShapeOf(compliance, "cveFindings"),
            "findings_from_walk": walk_finding_count,
            "findings_from_refetch": refetched,
            "finding_count_matches": refetched == walk_finding_count
        }
    return RunProbe("by_id_filter", run)


def ProbeUpdatedAfter(client):
    '''
    P2: tests whether UPDATED_AFTER is usable as a delta mechanism.

    The op's own docstring limits it to "fields from certain data sources", and
    the schema cannot say whether TDS compliance paths qualify.  Two attempts,
    then stop - the nested path first, then eidLastSeen as a second data point.

    This probe decides only whether delta collection is *possible*.  No delta
    logic is built on the result either way.
    '''
    since = (datetime.now(timezone.utc) - timedelta(days=1)).strftime("%Y-%m-%dT%H:%M:%SZ")
    attempts = []

    for path in ("compliance.cveFindings.lastFound", "eidLastSeen"):
        query = UPDATED_AFTER_PROBE_QUERY % { "path": path }
        attempt = { "path": path, "since": since }
        try:
            data = client.Post(query, { "allNamespaces": client.all_namespaces, "since": since })
            envelope = data.get("endpoints") or {}
            attempt.update({ "ok": True, "http_status": 200, "graphql_errors": None,
                             "total_records": envelope.get("totalRecords") })
        except TaniumGraphQLException as e:
            attempt.update({ "ok": False, "http_status": 200,
                             "total_records": None, "graphql_errors": e.Errors })
        except Exception as e:                          # noqa: BLE001 - probe must not abort census
            attempt.update({ "ok": False, "http_status": None,
                             "total_records": None, "error": f"[{type(e).__name__}] {e}" })
        attempts.append(attempt)
        if attempt.get("ok"):
            break                                       # first success settles it

    return { "probe": "updated_after", "ok": any(a.get("ok") for a in attempts),
             "attempts": attempts,
             "http_status": attempts[-1].get("http_status") if attempts else None,
             "graphql_errors": attempts[-1].get("graphql_errors") if attempts else None,
             "error": None }


def PrintProbeReport(report):
    ''' One compact block per probe, readable without opening census.json. '''
    verdict = "PASS" if report.get("ok") else "FAIL"
    print(f"\n  probe [{report.get('probe')}]: {verdict}")
    for key in ("requested_id", "total_records", "returned_id", "id_matches",
                "compliance_shape", "cve_findings_shape",
                "findings_from_walk", "findings_from_refetch", "finding_count_matches",
                "http_status", "detail"):
        if key in report and report[key] is not None:
            print(f"    {key:<24}: {report[key]}")
    for attempt in report.get("attempts") or []:
        state = "ok" if attempt.get("ok") else "error"
        print(f"    {attempt['path']:<34} {state}  totalRecords={attempt.get('total_records')}")
        if attempt.get("graphql_errors"):
            print(f"      errors: {json.dumps(attempt['graphql_errors'])[:400]}")
    if report.get("graphql_errors"):
        print(f"    graphql_errors          : {json.dumps(report['graphql_errors'])[:600]}")
    if report.get("error"):
        print(f"    error                   : {report['error']}")


def ModeCensus(client, args):
    '''
    Walks up to --limit endpoints and counts the things that block the mapping.
    Dumps one full example endpoint for each of six shapes.

    With --probe-extended the query carries the full experimental field list and
    the census additionally counts how many of those fields the tenant actually
    populates.  Schema presence and tenant population are different questions,
    and only the second one decides whether a field is worth mapping.
    '''
    if args.probe_extended:
        client.EnableExtendedFields(True)
    print(f"\n== census ==  limit={args.limit} probe_extended={args.probe_extended}")

    counters = {
        "endpoints_seen": 0,
        "compliance_is_null": 0,
        "cve_findings_is_null": 0,
        "cve_findings_is_empty": 0,
        "cve_findings_populated": 0,
        "endpoints_with_duplicate_cve_id": 0,
        "max_detected_products_len": 0,
        "max_cve_findings_len": 0,
        "summary_non_null": 0,
        "summary_null": 0
    }
    summary_lengths = []
    total_findings = 0

    examples = {
        "null_compliance": None,
        "null_cve_findings": None,
        "empty_findings": None,
        "multi_detected_products": None,
        "duplicate_cve_id": None,
        "highest_finding_count": None
    }
    highest_count = -1

    # Per-field population counts, only meaningful under --probe-extended but
    # cheap enough to always collect.
    probe_fields = client.RequestedFindingFields
    field_stats = { f: { "non_null": 0, "null": 0, "example": None } for f in probe_fields }

    # First endpoint off the walk, kept as the P1 probe's subject.  Using a real
    # walked endpoint rather than an arbitrary id is what makes the refetch
    # comparable - the probe checks the two paths agree, not merely that one works.
    probe_sample_id = None
    probe_sample_findings = None

    stop = False
    for page in client.GetEndpointsGenerator(first=args.first, resume_from=args.resume_from):
        for edge in (page.get("edges") or []):
            node = edge.get("node")
            if node is None:
                continue
            counters["endpoints_seen"] += 1

            if probe_sample_id is None and node.get("id") is not None:
                probe_sample_id = str(node.get("id"))
                probe_sample_findings = CountFindings(node)

            compliance = node.get("compliance")
            if compliance is None:
                counters["compliance_is_null"] += 1
                if examples["null_compliance"] is None:
                    examples["null_compliance"] = node
            else:
                findings = compliance.get("cveFindings")
                if findings is None:
                    counters["cve_findings_is_null"] += 1
                    if examples["null_cve_findings"] is None:
                        examples["null_cve_findings"] = node
                elif len(findings) == 0:
                    counters["cve_findings_is_empty"] += 1
                    if examples["empty_findings"] is None:
                        examples["empty_findings"] = node
                else:
                    counters["cve_findings_populated"] += 1
                    total_findings += len(findings)
                    counters["max_cve_findings_len"] = max(counters["max_cve_findings_len"], len(findings))

                    if len(findings) > highest_count:
                        highest_count = len(findings)
                        examples["highest_finding_count"] = node

                    cve_ids = []
                    for finding in findings:
                        for f in probe_fields:
                            value = finding.get(f)
                            if value is None:
                                field_stats[f]["null"] += 1
                            else:
                                field_stats[f]["non_null"] += 1
                                if field_stats[f]["example"] is None:
                                    field_stats[f]["example"] = value

                        products = finding.get("detectedProducts")
                        if products is not None:
                            counters["max_detected_products_len"] = max(
                                counters["max_detected_products_len"], len(products))
                            if len(products) > 1 and examples["multi_detected_products"] is None:
                                examples["multi_detected_products"] = node

                        summary = finding.get("summary")
                        if summary is None:
                            counters["summary_null"] += 1
                        else:
                            counters["summary_non_null"] += 1
                            summary_lengths.append(len(summary))

                        cve_ids.append(finding.get("cveId"))

                    if len(cve_ids) != len(set(cve_ids)):
                        counters["endpoints_with_duplicate_cve_id"] += 1
                        if examples["duplicate_cve_id"] is None:
                            examples["duplicate_cve_id"] = node

            if args.limit and counters["endpoints_seen"] >= args.limit:
                stop = True
                break
        if stop:
            break

    summary_stats = None
    if summary_lengths:
        summary_stats = {
            "count": len(summary_lengths),
            "min_len": min(summary_lengths),
            "max_len": max(summary_lengths),
            "mean_len": round(sum(summary_lengths) / len(summary_lengths), 1)
        }

    print(f"  endpoints seen                 : {counters['endpoints_seen']}")
    print(f"  compliance is None             : {counters['compliance_is_null']}")
    print(f"  cveFindings is None            : {counters['cve_findings_is_null']}")
    print(f"  cveFindings is []              : {counters['cve_findings_is_empty']}")
    print(f"  cveFindings populated          : {counters['cve_findings_populated']}")
    print(f"  max len(detectedProducts)      : {counters['max_detected_products_len']}   (open item 1)")
    print(f"  endpoints w/ duplicate cveId   : {counters['endpoints_with_duplicate_cve_id']}   (open item 1)")
    print(f"  max len(cveFindings)           : {counters['max_cve_findings_len']}   (open item 6)")
    print(f"  summary non-null / null        : {counters['summary_non_null']} / {counters['summary_null']}   (open item 4)")
    if summary_stats:
        print(f"  summary length min/mean/max    : {summary_stats['min_len']} / {summary_stats['mean_len']} / {summary_stats['max_len']}")

    if args.probe_extended:
        dropped = { k: v for k, v in client.DroppedFields.items() if v }
        print("\n  field population across walked findings:")
        print(f"    {'field':<26} {'non_null':>9} {'null':>9}  example")
        for f in probe_fields:
            s = field_stats[f]
            example = s["example"]
            if isinstance(example, (list, dict)):
                example = json.dumps(example, default=str)
            example = "" if example is None else str(example)
            if len(example) > 40:
                example = example[:37] + "..."
            print(f"    {f:<26} {s['non_null']:>9} {s['null']:>9}  {example}")
        if dropped:
            print(f"\n  not in schema (dropped before the query): {dropped}")

    print("\n  examples captured:")
    example_dir = os.path.join(args.out, "examples")
    for name, node in examples.items():
        if node is None:
            print(f"    {name}: NONE FOUND")
        else:
            print(f"    {name}: {node.get('id')}")
            WriteJson(example_dir, f"{name}.json", node)

    # ---- schema probes ---------------------------------------------------
    # Run after the walk so the census result is already complete: a probe that
    # blows up must cost the run its probe report, never its census.
    print("\n  == schema probes ==")
    probes = {}

    if probe_sample_id is None:
        probes["by_id_filter"] = { "probe": "by_id_filter", "ok": False,
                                   "error": "no endpoint walked, nothing to refetch" }
    else:
        probes["by_id_filter"] = ProbeByIdFilter(client, probe_sample_id, probe_sample_findings)
    PrintProbeReport(probes["by_id_filter"])

    probes["updated_after"] = RunProbe("updated_after", lambda: ProbeUpdatedAfter(client))
    PrintProbeReport(probes["updated_after"])

    print(f"\n  walk checkpoint (resume id)    : {client.CheckpointId}")

    # The empirical answer to "what page size does this instance tolerate".
    # Seed Page_Size_Start from this for future runs.
    print("\n  == page sizing ==")
    print(f"    locked page size             : {client.PageSize}  (locked={client.PageSizeLocked})")
    if client.ShrinkEvents:
        print(f"    shrink events                : {len(client.ShrinkEvents)}")
        for ev in client.ShrinkEvents:
            print(f"      {ev['from']:>5} -> {ev['to']:<5} {ev['reason']}")
        print(f"    ACTION                       : set Page_Size_Start to {client.PageSize} "
              f"in Config/Sources/TaniumClient.json to skip this discovery next run.")
    else:
        print(f"    shrink events                : none - {client.PageSize} served every page")

    WriteJson(args.out, "probes.json", probes)

    WriteJson(args.out, "census.json", {
        "probes": probes,
        "page_size_locked": client.PageSize,
        "page_size_shrink_events": client.ShrinkEvents,
        "checkpoint_id": client.CheckpointId,
        "resumed_from": args.resume_from,
        "counters": counters,
        "summary_stats": summary_stats,
        "total_findings": total_findings,
        "probe_extended": args.probe_extended,
        "requested_finding_fields": probe_fields,
        "dropped_fields": client.DroppedFields,
        "field_population": field_stats,
        "examples_found": { k: (v.get("id") if v else None) for k, v in examples.items() }
    })
    return { "endpoints": counters["endpoints_seen"], "findings": total_findings }


def ModeSizing(client, args):
    ''' Times and measures response bytes at first = 10, 50, 100, 250. '''
    print("\n== sizing ==")
    rows = []
    total_endpoints = 0
    total_findings = 0

    for first in (10, 50, 100, 250):
        started = time.monotonic()
        endpoints = client.GetEndpointsPage(first=first)
        elapsed = time.monotonic() - started

        edges = (endpoints or {}).get("edges") or []
        findings = sum(CountFindings(e.get("node") or {}) for e in edges)
        byte_count = client.LastResponseBytes
        total_endpoints += len(edges)
        total_findings += findings

        row = {
            "first": first,
            "elapsed_s": round(elapsed, 3),
            "response_bytes": byte_count,
            "endpoints_returned": len(edges),
            "findings_returned": findings,
            "bytes_per_endpoint": round(byte_count / len(edges)) if edges else None
        }
        rows.append(row)
        print(f"  first={first:>4}  {elapsed:6.2f}s  {byte_count:>10,} bytes  "
              f"{len(edges):>4} endpoints  {findings:>5} findings  "
              f"{row['bytes_per_endpoint'] or 0:>8,} b/endpoint")

    WriteJson(args.out, "sizing.json", rows)
    return { "endpoints": total_endpoints, "findings": total_findings }


def ModeIntrospect(client, args):
    '''
    Runs __type for --type, or dumps the full schema type list when omitted.

    Reference open item 9 lists the unexpanded nested fields by field name, not
    by type name, so with no --type we dump the schema rather than guessing at
    type names.
    '''
    if args.type:
        print(f"\n== introspect ==  type={args.type}")
        result = client.IntrospectType(args.type)
        if result is None:
            print(f"  schema has no type named {args.type!r}.")
        else:
            print(f"  {result.get('name')} ({result.get('kind')}) - {len(result.get('fields') or [])} fields")
            for field in (result.get("fields") or []):
                ftype = field.get("type") or {}
                name = ftype.get("name") or ((ftype.get("ofType") or {}).get("name"))
                print(f"    {field.get('name')}: {name} ({ftype.get('kind')})")
        WriteJson(args.out, f"introspect_{args.type}.json", result)
    else:
        print("\n== introspect ==  full schema type list")
        schema = client.IntrospectSchema()
        types = (schema or {}).get("types") or []
        object_types = [t for t in types if t.get("kind") == "OBJECT" and not (t.get("name") or "").startswith("__")]
        print(f"  {len(types)} types total, {len(object_types)} object types.")
        for t in sorted(object_types, key=lambda x: x.get("name") or ""):
            print(f"    {t.get('name')}  ({len(t.get('fields') or [])} fields)")
        WriteJson(args.out, "introspect_schema.json", schema)

    return { "endpoints": 0, "findings": 0 }


MODES = {
    "preflight": ModePreflight,
    "dump": ModeDump,
    "paging": ModePaging,
    "census": ModeCensus,
    "sizing": ModeSizing,
    "introspect": ModeIntrospect
}


# ---------------------------------------------------------------------------
# entry point
# ---------------------------------------------------------------------------

def ParseArgs(argv=None):
    parser = argparse.ArgumentParser(
        description="Tanium Gateway test runner. Read-only. Dumps raw payloads for mapping work.")
    parser.add_argument("--mode", choices=sorted(MODES.keys()), default="dump",
                        help="Which probe to run. Default: dump.")
    parser.add_argument("--pages", type=int, default=2, help="dump: number of pages to pull. Default: 2.")
    parser.add_argument("--first", type=int, default=None,
                        help="Page size. Defaults to the configured Tanium.Page_Size.")
    parser.add_argument("--limit", type=int, default=500, help="census: max endpoints to walk. Default: 500.")
    parser.add_argument("--resume-from", default=None, metavar="ID",
                        help="census: resume the walk after this endpoint id, using an id GT "
                             "filter and no cursor. Take the value from a previous run's "
                             "'walk checkpoint' line. Cursors die at 5min idle / 1hr absolute, "
                             "so a fleet too large for one walk is collected in segments.")
    parser.add_argument("--probe-extended", action="store_true",
                        help="census: request every experimental field the schema exposes and "
                             "report how many the tenant actually populates. Default: off.")
    parser.add_argument("--type", default=None, help="introspect: GraphQL type name. Omit for the full schema.")
    parser.add_argument("--out", default="./tanium_out/", help="Artifact directory. Default: ./tanium_out/.")
    parser.add_argument("--min-endpoints", type=int, default=0,
                        help="Exit 2 if fewer than N endpoints were seen. Default: 0 (off).")
    parser.add_argument("--min-findings", type=int, default=0,
                        help="Exit 2 if fewer than N findings were seen. Default: 0 (off).")
    return parser.parse_args(argv)


def Main(argv=None):
    args = ParseArgs(argv)

    app = Application(skipCleanFiles=True)
    client = TaniumClient(app.Settings)

    print("****************************")
    print(f"** Tanium runner - mode: {args.mode}")
    print(f"** base_url    : {client.base_url}")
    print(f"** page_size   : {client.page_size}")
    print(f"** allNamespaces: {client.all_namespaces}")
    print(f"** out         : {args.out}")
    print("****************************")

    exit_code = 0
    totals = { "endpoints": 0, "findings": 0 }
    try:
        totals = MODES[args.mode](client, args)
    except TaniumException as e:
        logging.error("[Tanium Runner] %s failed: [%s] %s", args.mode, type(e).__name__, e)
        print(f"\n  RUN FAILED: [{type(e).__name__}] {e}")
        exit_code = 1
    except KeyboardInterrupt:
        print("\n  interrupted.")
        exit_code = 130

    stats = client.run_stats
    print("\n****************************")
    print(f"** total endpoints : {totals.get('endpoints', 0)}")
    print(f"** total findings  : {totals.get('findings', 0)}")
    print(f"** requests made   : {stats['requests']}")
    print(f"** elapsed seconds : {stats['elapsed_s']}")
    print("****************************")

    # Thresholds turn this from an interactive probe into something a scheduler
    # can watch.  An empty cveFindings array returns HTTP 200 whether the fleet
    # is clean, the token lacks API Gateway permission, or the Comply CVE
    # Findings sensor is not deployed - so "succeeded and collected nothing" has
    # to be expressible as a failure.
    if exit_code == 0:
        unmet = []
        if args.min_endpoints and totals.get("endpoints", 0) < args.min_endpoints:
            unmet.append(f"endpoints {totals.get('endpoints', 0)} < --min-endpoints {args.min_endpoints}")
        if args.min_findings and totals.get("findings", 0) < args.min_findings:
            unmet.append(f"findings {totals.get('findings', 0)} < --min-findings {args.min_findings}")
        if unmet:
            for reason in unmet:
                print(f"\n  THRESHOLD NOT MET: {reason}")
            exit_code = 2

    if exit_code == 0 and not totals.get("findings", 0):
        print("\nNo findings collected. An empty cveFindings array returns HTTP 200 whether")
        print("the fleet is clean, the token lacks API Gateway permission, or the Comply CVE")
        print("Findings sensor is not deployed. Run --mode census to tell which.")

    return exit_code


if __name__ == "__main__":
    sys.exit(Main())
