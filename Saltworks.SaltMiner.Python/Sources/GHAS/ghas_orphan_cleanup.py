#!/usr/bin/env python3
"""
ghas_orphan_cleanup.py
======================
Companion cleanup for the GHAS adapter's "Category-2" orphans: SaltMiner data
for repositories that no longer exist in GitHub (deleted, renamed, or
transferred). The adapter cannot tombstone these in-band because it can no
longer enumerate them from the org — so they are purged here, out of band,
using the run-tag the adapter stamps on every queued document.

HOW IT WORKS
------------
Every asset and issue the adapter queues carries two attributes:
    saltminer.attributes.ghas_run_id   (per-run UUID)
    saltminer.attributes.ghas_run_ts   (per-run ISO8601 timestamp)
On each run, every repo GitHub still reports is re-queued and gets the CURRENT
run's tag. Therefore any document still carrying an OLD ghas_run_id for a given
instance belongs to a repo the latest run did not touch — i.e. an orphan.

This script:
  1. Finds, per instance (SourceType+Instance → index), the most recent
     ghas_run_ts present in the index (the "latest run").
  2. Identifies documents whose ghas_run_ts is older than that latest run
     (optionally older by a safety margin — see --min-age-minutes).
  3. In --apply mode, deletes those documents. By DEFAULT it is a DRY RUN and
     only reports what it would delete.

SAFETY
------
- DRY RUN by default. Nothing is deleted unless you pass --apply.
- Refuses to run if it cannot positively determine a latest run (avoids
  deleting everything when the tag is missing, e.g. pre-run-tag data).
- Scopes strictly to GHAS indices (SourceType "Saltworks.GHAS").
- A configurable safety margin (--min-age-minutes, default 120) prevents
  deleting data from a run that is merely in-progress or very recent.
- Prints a per-instance summary before any deletion.

CONNECTION (.env)
-----------------
Reads Elasticsearch connection from environment (e.g. your host .env):
    ELASTICSEARCH_URL    full URL, preferred  (e.g. https://es.internal:443)
    ELASTIC_HOST         host (used if URL absent)  e.g. es.internal
    ELASTIC_PORT         port (used if URL absent)  e.g. 443
    ELASTIC_USERNAME     defaults to "elastic" if blank
    ELASTIC_PASSWORD     required
TLS verification is OFF by default (SaltMiner ES typically uses a self-signed
cert). Pass --verify-certs and optionally --ca-cert /path if you have a CA.

USAGE
-----
    # dry run, all GHAS instances:
    python3 ghas_orphan_cleanup.py

    # dry run, single instance:
    python3 ghas_orphan_cleanup.py --instance ghas2

    # actually delete (after reviewing the dry run):
    python3 ghas_orphan_cleanup.py --instance ghas2 --apply

Requires: elasticsearch Python client (pip install "elasticsearch>=8,<9"
matching your stack 8.19.x). If unavailable, the script falls back to raw
REST via urllib so it can run anywhere.

This script does NOT touch the adapter, its state file, or GitHub. It only
removes already-orphaned documents from Elasticsearch.
"""

import argparse
import json
import os
import ssl
import sys
import urllib.request
import urllib.error
from datetime import datetime, timezone, timedelta


# ── Connection ─────────────────────────────────────────────────────────────

def _env(name, default=None):
    v = os.environ.get(name)
    return v if v not in (None, "") else default


def resolve_es_base_url() -> str:
    url = _env("ELASTICSEARCH_URL")
    if url:
        return url.rstrip("/")
    host = _env("ELASTIC_HOST")
    port = _env("ELASTIC_PORT", "443")
    if not host:
        sys.exit(
            "ERROR: set ELASTICSEARCH_URL, or ELASTIC_HOST (+ELASTIC_PORT), in the environment/.env."
        )
    scheme = "https" if str(port) in ("443", "9243") else "http"
    return f"{scheme}://{host}:{port}"


def es_auth():
    user = _env("ELASTIC_USERNAME", "elastic")
    pw = _env("ELASTIC_PASSWORD")
    if not pw:
        sys.exit("ERROR: ELASTIC_PASSWORD is not set in the environment/.env.")
    return user, pw


# ── Minimal REST client (urllib) — no third-party dependency required ───────

class ESRest:
    """Tiny Elasticsearch REST client over urllib so this script runs anywhere.
    Only implements the few operations the cleanup needs (search, indices, and
    delete_by_query)."""

    def __init__(self, base_url, user, password, verify_certs=False, ca_cert=None):
        self.base = base_url.rstrip("/")
        self.user = user
        self.password = password
        if verify_certs:
            self.ctx = ssl.create_default_context(cafile=ca_cert) if ca_cert else ssl.create_default_context()
        else:
            self.ctx = ssl._create_unverified_context()

    def _req(self, method, path, body=None, params=None):
        url = self.base + path
        if params:
            from urllib.parse import urlencode
            url += "?" + urlencode(params)
        data = json.dumps(body).encode("utf-8") if body is not None else None
        req = urllib.request.Request(url, data=data, method=method)
        req.add_header("Content-Type", "application/json")
        import base64
        token = base64.b64encode(f"{self.user}:{self.password}".encode()).decode()
        req.add_header("Authorization", f"Basic {token}")
        try:
            with urllib.request.urlopen(req, timeout=60, context=self.ctx) as r:
                return json.load(r)
        except urllib.error.HTTPError as e:
            detail = e.read().decode()[:500]
            raise RuntimeError(f"ES {method} {path} → HTTP {e.code}: {detail}") from e

    def list_ghas_indices(self):
        """Return GHAS issue/asset index names. SaltMiner index convention:
        issues_app_saltworks.ghas_<instance>. We match the saltworks.ghas prefix."""
        res = self._req("GET", "/_cat/indices/*saltworks.ghas*", params={"format": "json", "h": "index"})
        return sorted({row["index"] for row in res})

    def latest_run_ts(self, index):
        """Max ghas_run_ts in this index, or None if the field is absent."""
        body = {
            "size": 0,
            "aggs": {"latest": {"max": {"field": "saltminer.attributes.ghas_run_ts"}}},
        }
        res = self._req("POST", f"/{index}/_search", body=body)
        val = (res.get("aggregations", {}).get("latest", {}) or {}).get("value_as_string")
        return val

    def count_older_than(self, index, cutoff_iso):
        body = {
            "query": {
                "bool": {
                    "must": [{"exists": {"field": "saltminer.attributes.ghas_run_ts"}}],
                    "filter": [{"range": {"saltminer.attributes.ghas_run_ts": {"lt": cutoff_iso}}}],
                }
            }
        }
        res = self._req("POST", f"/{index}/_count", body=body)
        return res.get("count", 0)

    def sample_orphan_repos(self, index, cutoff_iso, size=25):
        body = {
            "size": 0,
            "query": {"range": {"saltminer.attributes.ghas_run_ts": {"lt": cutoff_iso}}},
            "aggs": {"repos": {"terms": {"field": "saltminer.attributes.ghas_repo_full_name", "size": size}}},
        }
        res = self._req("POST", f"/{index}/_search", body=body)
        buckets = res.get("aggregations", {}).get("repos", {}).get("buckets", [])
        return [(b["key"], b["doc_count"]) for b in buckets]

    def delete_older_than(self, index, cutoff_iso):
        body = {"query": {"range": {"saltminer.attributes.ghas_run_ts": {"lt": cutoff_iso}}}}
        res = self._req("POST", f"/{index}/_delete_by_query",
                        body=body, params={"conflicts": "proceed", "refresh": "true"})
        return res.get("deleted", 0)


# ── Core logic ──────────────────────────────────────────────────────────────

def parse_iso(ts):
    if not ts:
        return None
    try:
        return datetime.fromisoformat(ts.replace("Z", "+00:00"))
    except ValueError:
        return None


def process_index(es, index, instance_filter, min_age_minutes, apply):
    if instance_filter and not index.endswith(instance_filter.lower()):
        return None

    latest = es.latest_run_ts(index)
    latest_dt = parse_iso(latest)
    if latest_dt is None:
        print(f"  [{index}] SKIP — no ghas_run_ts found (pre-run-tag data?). "
              f"Refusing to purge without a positive latest-run marker.")
        return None

    # Cutoff = latest run minus safety margin. Anything strictly older than the
    # latest run AND older than the margin is an orphan from a prior run.
    cutoff_dt = latest_dt - timedelta(minutes=min_age_minutes)
    cutoff_iso = cutoff_dt.astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

    count = es.count_older_than(index, cutoff_iso)
    print(f"  [{index}]")
    print(f"      latest run ts : {latest}")
    print(f"      cutoff (<)    : {cutoff_iso}  (latest − {min_age_minutes}m safety margin)")
    print(f"      orphan docs   : {count}")

    if count == 0:
        return {"index": index, "orphans": 0, "deleted": 0}

    for repo, n in es.sample_orphan_repos(index, cutoff_iso):
        print(f"        - {repo or '(no repo attr)'}: {n}")

    deleted = 0
    if apply:
        deleted = es.delete_older_than(index, cutoff_iso)
        print(f"      DELETED       : {deleted}")
    else:
        print(f"      DRY RUN       : would delete {count} (pass --apply to delete)")
    return {"index": index, "orphans": count, "deleted": deleted}


def main():
    ap = argparse.ArgumentParser(description="Purge GHAS orphan docs (deleted/renamed repos) from Elasticsearch.")
    ap.add_argument("--instance", default=None,
                    help="Only process the index for this instance SourceName (e.g. ghas2). Default: all GHAS indices.")
    ap.add_argument("--apply", action="store_true",
                    help="Actually delete. Default is a DRY RUN (report only).")
    ap.add_argument("--min-age-minutes", type=int, default=120,
                    help="Safety margin: only purge docs older than (latest run − this many minutes). Default 120.")
    ap.add_argument("--verify-certs", action="store_true",
                    help="Verify TLS certs (default off — SaltMiner ES is usually self-signed).")
    ap.add_argument("--ca-cert", default=None, help="Path to CA bundle (implies --verify-certs).")
    args = ap.parse_args()

    base = resolve_es_base_url()
    user, pw = es_auth()
    es = ESRest(base, user, pw,
                verify_certs=args.verify_certs or bool(args.ca_cert),
                ca_cert=args.ca_cert)

    print("=" * 64)
    print("  GHAS ORPHAN CLEANUP" + ("  (DRY RUN)" if not args.apply else "  (APPLY — deletions will occur)"))
    print(f"  ES: {base}")
    print(f"  instance filter: {args.instance or 'ALL GHAS indices'}")
    print(f"  safety margin  : {args.min_age_minutes} minutes")
    print("=" * 64)

    try:
        indices = es.list_ghas_indices()
    except Exception as exc:
        sys.exit(f"ERROR listing indices: {exc}")

    if not indices:
        print("No GHAS indices found (looked for *saltworks.ghas*). Nothing to do.")
        return

    results = []
    for idx in indices:
        try:
            r = process_index(es, idx, args.instance, args.min_age_minutes, args.apply)
            if r:
                results.append(r)
        except Exception as exc:
            print(f"  [{idx}] ERROR: {exc}")

    print("=" * 64)
    total_orphans = sum(r["orphans"] for r in results)
    total_deleted = sum(r["deleted"] for r in results)
    if args.apply:
        print(f"  DONE — deleted {total_deleted} orphan doc(s) across {len(results)} index(es).")
    else:
        print(f"  DRY RUN — {total_orphans} orphan doc(s) would be deleted across {len(results)} index(es).")
        print("  Re-run with --apply to delete.")
    print("=" * 64)


if __name__ == "__main__":
    main()
