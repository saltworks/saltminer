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
        result["tokens"] = tokens
        print("\n  token metadata:")
        for t in (tokens if isinstance(tokens, list) else [tokens]):
            if isinstance(t, dict):
                print(f"    id={t.get('id')} expires={t.get('expiration')} "
                      f"trustedIPs={t.get('trustedIPAddresses')}")
    else:
        print("\n  token metadata          : unavailable (needs 'Token - View')")

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

    stop = False
    for page in client.GetEndpointsGenerator(first=args.first):
        for edge in (page.get("edges") or []):
            node = edge.get("node")
            if node is None:
                continue
            counters["endpoints_seen"] += 1

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

    WriteJson(args.out, "census.json", {
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
