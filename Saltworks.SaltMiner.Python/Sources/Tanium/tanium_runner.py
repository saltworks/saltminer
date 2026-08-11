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

    python Sources/Tanium/tanium_runner.py --mode dump --pages 2 --first 5 --out ./tanium_out/
    python Sources/Tanium/tanium_runner.py --mode census --limit 500
    python Sources/Tanium/tanium_runner.py --mode introspect --type EndpointOS
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
    AUTH_SESSION,
    AUTH_BEARER
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


def IterFindings(node):
    '''
    Yields findings for one endpoint node without collapsing null into empty.
    Returns nothing for both cases; callers that care must inspect the node.
    '''
    compliance = node.get("compliance")
    if compliance is None:
        return
    findings = compliance.get("cveFindings")
    if not findings:
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

def ModeAuth(client, args):
    '''
    Resolves open item T-1.  Tries `session`, then `Authorization: Bearer`,
    at first: 1.  Prints which worked and the raw error for the one that did not.
    '''
    print("\n== auth ==")
    results = {}
    for scheme in (AUTH_SESSION, AUTH_BEARER):
        label = "session: <token>" if scheme == AUTH_SESSION else "Authorization: Bearer <token>"
        try:
            endpoints = client.GetEndpointsPage(first=1, scheme=scheme)
            results[scheme] = {
                "header": label,
                "ok": True,
                "total_records": (endpoints or {}).get("totalRecords"),
                "error": None
            }
            print(f"  OK      {label}  totalRecords={(endpoints or {}).get('totalRecords')}")
        except TaniumException as e:
            results[scheme] = {
                "header": label,
                "ok": False,
                "total_records": None,
                "error": f"[{type(e).__name__}] {e}"
            }
            print(f"  FAILED  {label}")
            print(f"          [{type(e).__name__}] {e}")

    working = [s for s, r in results.items() if r["ok"]]
    print(f"\n  configured scheme: {client.auth_header}")
    print(f"  working scheme(s): {', '.join(working) if working else 'NONE'}")

    WriteJson(args.out, "auth.json", results)
    return { "endpoints": 0, "findings": 0 }


def ModeDump(client, args):
    '''
    Pulls --pages pages at --first.  Writes each full response envelope
    unmodified, plus a flattened findings.jsonl.
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
            if not page_info.get("hasNextPage"):
                print("  fleet exhausted before page budget.")
                break
            after = page_info.get("endCursor")

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

        if not page_info.get("hasNextPage"):
            print("  fleet exhausted before 3 pages.")
            break
        after = page_info.get("endCursor")

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
    Dumps one full example endpoint for each of five shapes.
    '''
    print(f"\n== census ==  limit={args.limit}")

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
        "empty_findings": None,
        "multi_detected_products": None,
        "duplicate_cve_id": None,
        "highest_finding_count": None
    }
    highest_count = -1

    for node in client.GetEndpointsGenerator(first=args.first):
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
    type names like "EndpointOS".
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
    "auth": ModeAuth,
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
    parser.add_argument("--type", default=None, help="introspect: GraphQL type name. Omit for the full schema.")
    parser.add_argument("--out", default="./tanium_out/", help="Artifact directory. Default: ./tanium_out/.")
    return parser.parse_args(argv)


def Main(argv=None):
    args = ParseArgs(argv)

    app = Application(skipCleanFiles=True)
    client = TaniumClient(app.Settings)

    print("****************************")
    print(f"** Tanium runner - mode: {args.mode}")
    print(f"** base_url    : {client.base_url}")
    print(f"** auth scheme : {client.auth_header}")
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
    print("\nAn empty cveFindings array returns HTTP 200 whether the fleet is clean,")
    print("the token lacks API Gateway permission, or the Comply CVE Findings sensor")
    print("is not deployed. The counts above are how you tell which.")

    return exit_code


if __name__ == "__main__":
    sys.exit(Main())
