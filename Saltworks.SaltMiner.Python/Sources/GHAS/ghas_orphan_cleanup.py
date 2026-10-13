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
- RETIRE-001: issues carrying vulnerability.removed_date are NEVER deleted.
  The adapter now retires closed GHAS alerts (setting RemovedDate from
  GitHub's own closure timestamp) instead of having the Manager hard-delete
  them. Run-tags are stamped only on documents the adapter queues, and the
  Manager's RemoveIssue path sets RemovedDate without refreshing the tag — so
  any retired issue the adapter stops re-queueing (alert deleted at GitHub,
  engine skipped under rate limit, repo newly excluded by config) carries a
  FROZEN run-tag and would otherwise be swept up here, destroying the very
  closure history the retirement change exists to preserve. This guard applies
  to issue indices only; asset and scan indices are purged as before.

- PBI-102: findings of an ARCHIVED repository are NEVER deleted. The adapter
  keeps them on purpose, marked vulnerability.is_suppressed=true and
  saltminer.attributes.issue_status=archived, and they carry a stale run-tag
  (the adapter queues nothing for an archived repository), so without this
  guard they would match the stale-tag selection. The marker is read from each
  document's _source, not by a query, so the guard does not depend on the index
  mapping the attribute. The archived repository's ASSET document is kept too:
  an asset is kept when any issue in the sibling issue index is marked archived
  for its source id. The issue index is therefore processed before the asset
  index of the same instance.

  Deliberate consequence: a scope whose alerts GitHub deleted outright leaves
  stale issues that were never retired. Those have no removed_date, so this
  script still collects them — which is what keeps that case covered now that
  the adapter no longer hard-deletes empty scopes.

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

    @staticmethod
    def is_asset_index(index):
        """True for the asset indices (assets_app_saltworks.ghas_<instance>)."""
        return index.startswith("assets")

    @staticmethod
    def sibling_issue_index(asset_index):
        """issues_... index of the same instance as an assets_... index."""
        return "issues" + asset_index[len("assets"):]

    @staticmethod
    def is_issue_index(index):
        """True for the issue indices (issues_app_saltworks.ghas_<instance>).

        Only issue documents carry vulnerability.removed_date, so only they
        get the retirement guard; asset and scan indices are untouched.
        """
        return index.startswith("issues")

    @staticmethod
    def _orphan_query(cutoff_iso, protect_retired):
        """Documents older than the cutoff, optionally excluding retired ones.

        RETIRE-001: the adapter now marks closed alerts with
        vulnerability.removed_date rather than having the Manager hard-delete
        them, and the Manager's own RemoveIssue path never refreshes the
        ghas_run_ts tag. A retired issue the adapter stops re-queueing
        therefore carries a FROZEN run tag and would be swept up here as an
        orphan, destroying exactly the closure history the retirement change
        exists to preserve. Excluding removed_date keeps that history.
        """
        query = {
            "bool": {
                "must": [{"exists": {"field": "saltminer.attributes.ghas_run_ts"}}],
                "filter": [{"range": {"saltminer.attributes.ghas_run_ts": {"lt": cutoff_iso}}}],
            }
        }
        if protect_retired:
            query["bool"]["must_not"] = [
                {"exists": {"field": "vulnerability.removed_date"}}
            ]
        return query

    # ── PBI-102: archived findings and assets are kept ─────────────────────

    def scan(self, index, query, source_fields, page_size=1000):
        """Yield every hit (dict with _id and _source) for `query`, paging with a
        point in time and search_after, so the 10,000-result window never applies."""
        pit = self._req("POST", f"/{index}/_pit", params={"keep_alive": "2m"})["id"]
        try:
            after = None
            while True:
                body = {
                    "size": page_size,
                    "query": query,
                    "_source": source_fields,
                    "pit": {"id": pit, "keep_alive": "2m"},
                    "sort": [{"_shard_doc": "asc"}],
                }
                if after is not None:
                    body["search_after"] = after
                res = self._req("POST", "/_search", body=body)
                pit = res.get("pit_id", pit)
                hits = res.get("hits", {}).get("hits", [])
                for h in hits:
                    yield h
                if len(hits) < page_size:
                    return
                after = hits[-1].get("sort")
        finally:
            try:
                self._req("DELETE", "/_pit", body={"id": pit})
            except Exception:
                pass

    def candidate_ids(self, index, cutoff_iso, protect_retired=False):
        """Issue-index orphan candidates, minus archived findings.

        Returns (ids_to_delete, archived_kept, repo_counts). The archived marker
        is read from _source, which works whether or not the index maps it.
        """
        ids, kept, repos = [], 0, {}
        for h in self.scan(index, self._orphan_query(cutoff_iso, protect_retired),
                           [ISSUE_STATUS_FIELD, REPO_FIELD]):
            src = h.get("_source") or {}
            if _dig(src, ISSUE_STATUS_FIELD) == ARCHIVED:
                kept += 1
                continue
            ids.append(h["_id"])
            repo = _dig(src, REPO_FIELD)
            repos[repo] = repos.get(repo, 0) + 1
        return ids, kept, repos

    def archived_source_ids(self, issue_index):
        """Source ids (GitHub repo ids) that have at least one finding marked
        issue_status=archived. Uses mapped fields only for the filter (source_type
        and is_suppressed; every archived finding is suppressed), then reads the
        marker from _source. Independent of the cutoff and of removed_date, so a
        repository whose archived findings are all closed still keeps its asset."""
        query = {"bool": {"filter": [
            {"term": {"saltminer.asset.source_type": "Saltworks.GHAS"}},
            {"term": {"vulnerability.is_suppressed": True}},
        ]}}
        found = set()
        for h in self.scan(issue_index, query, [SOURCE_ID_FIELD, ISSUE_STATUS_FIELD]):
            src = h.get("_source") or {}
            if _dig(src, ISSUE_STATUS_FIELD) == ARCHIVED:
                sid = _dig(src, SOURCE_ID_FIELD)
                if sid not in (None, ""):
                    found.add(str(sid))
        return found

    def candidate_assets(self, index, cutoff_iso, protected_source_ids):
        """Asset-index orphan candidates, minus assets of archived repositories.
        Returns (ids_to_delete, archived_assets_kept, name_counts)."""
        ids, kept, names = [], 0, {}
        for h in self.scan(index, self._orphan_query(cutoff_iso, False),
                           [SOURCE_ID_FIELD, ASSET_NAME_FIELD]):
            src = h.get("_source") or {}
            sid = _dig(src, SOURCE_ID_FIELD)
            if sid is not None and str(sid) in protected_source_ids:
                kept += 1
                continue
            ids.append(h["_id"])
            name = _dig(src, ASSET_NAME_FIELD)
            names[name] = names.get(name, 0) + 1
        return ids, kept, names

    def fetch_sources(self, index, ids, fields):
        """Re-read the current _source of `ids` (a point-in-time-free, live read)."""
        body = {"size": len(ids), "query": {"ids": {"values": list(ids)}}, "_source": fields}
        res = self._req("POST", f"/{index}/_search", body=body)
        return {h["_id"]: (h.get("_source") or {}) for h in res.get("hits", {}).get("hits", [])}

    def delete_ids(self, index, ids, is_protected=None, guard_suppressed=False, chunk=500):
        """Delete documents by _id, in chunks, re-checking protection at delete time.

        The ids were chosen from an earlier snapshot, and the adapter may mark a
        finding archived (or a repository may become archived) after that. So,
        chunk by chunk, immediately before each delete: re-read the current
        _source of the chunk, drop every document `is_protected(source)` now
        reports as protected, and delete the rest.

        guard_suppressed (issue index): the delete query itself also excludes
        vulnerability.is_suppressed=true, a mapped field, so a finding the
        adapter marks archived between the re-read and the delete is still
        spared, atomically. Documents the re-read found suppressed but not
        archived (SARIF suppressed findings, which the cleanup always removed
        when orphaned) are deleted by a second request without that exclusion.

        Returns (deleted, skipped_at_delete_time).
        """
        deleted = skipped = 0
        params = {"conflicts": "proceed", "refresh": "true"}
        for i in range(0, len(ids), chunk):
            part = ids[i:i + chunk]
            if is_protected is None:
                current = None
                keep = part
            else:
                current = self.fetch_sources(index, part, [ISSUE_STATUS_FIELD, SOURCE_ID_FIELD, SUPPRESSED_FIELD])
                keep = [d for d in part if d in current and not is_protected(current[d])]
                skipped += len(part) - len(keep)
            if not keep:
                continue
            if guard_suppressed and current is not None:
                plain = [d for d in keep if _dig(current[d], SUPPRESSED_FIELD) is True]
                guarded = [d for d in keep if d not in set(plain)]
            else:
                plain, guarded = keep, []
            if guarded:
                res = self._req("POST", f"/{index}/_delete_by_query", body={"query": {"bool": {
                    "filter": [{"ids": {"values": guarded}}],
                    "must_not": [{"term": {SUPPRESSED_FIELD: True}}]}}}, params=params)
                deleted += res.get("deleted", 0)
            if plain:
                res = self._req("POST", f"/{index}/_delete_by_query",
                                body={"query": {"ids": {"values": plain}}}, params=params)
                deleted += res.get("deleted", 0)
        return deleted, skipped

    def count_older_than(self, index, cutoff_iso, protect_retired=False):
        body = {"query": self._orphan_query(cutoff_iso, protect_retired)}
        res = self._req("POST", f"/{index}/_count", body=body)
        return res.get("count", 0)

    def count_retired_skipped(self, index, cutoff_iso):
        """Stale-tagged issues shielded from deletion by their removed_date."""
        body = {
            "query": {
                "bool": {
                    "must": [
                        {"exists": {"field": "saltminer.attributes.ghas_run_ts"}},
                        {"exists": {"field": "vulnerability.removed_date"}},
                    ],
                    "filter": [{"range": {"saltminer.attributes.ghas_run_ts": {"lt": cutoff_iso}}}],
                }
            }
        }
        res = self._req("POST", f"/{index}/_count", body=body)
        return res.get("count", 0)

    def sample_orphan_repos(self, index, cutoff_iso, protect_retired=False, size=25):
        # Same query as the delete, so the sample always reflects what would
        # actually be removed rather than a superset of it.
        body = {
            "size": 0,
            "query": self._orphan_query(cutoff_iso, protect_retired),
            "aggs": {"repos": {"terms": {"field": "saltminer.attributes.ghas_repo_full_name", "size": size}}},
        }
        res = self._req("POST", f"/{index}/_search", body=body)
        buckets = res.get("aggregations", {}).get("repos", {}).get("buckets", [])
        return [(b["key"], b["doc_count"]) for b in buckets]

    def delete_older_than(self, index, cutoff_iso, protect_retired=False):
        body = {"query": self._orphan_query(cutoff_iso, protect_retired)}
        res = self._req("POST", f"/{index}/_delete_by_query",
                        body=body, params={"conflicts": "proceed", "refresh": "true"})
        return res.get("deleted", 0)


# ── Core logic ──────────────────────────────────────────────────────────────

ARCHIVED = "archived"
ISSUE_STATUS_FIELD = "saltminer.attributes.issue_status"
REPO_FIELD = "saltminer.attributes.ghas_repo_full_name"
SOURCE_ID_FIELD = "saltminer.asset.source_id"
ASSET_NAME_FIELD = "saltminer.asset.name"
SUPPRESSED_FIELD = "vulnerability.is_suppressed"


def _dig(src, dotted):
    """Read a dotted path from a nested _source, or a literal dotted key."""
    if not isinstance(src, dict):
        return None
    if dotted in src:
        return src[dotted]
    cur = src
    for part in dotted.split("."):
        if not isinstance(cur, dict) or part not in cur:
            return None
        cur = cur[part]
    return cur


def parse_iso(ts):
    if not ts:
        return None
    try:
        return datetime.fromisoformat(ts.replace("Z", "+00:00"))
    except ValueError:
        return None


def process_index(es, index, instance_filter, min_age_minutes, apply, protected_source_ids=None,
                  refresh_protected=None):
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

    # RETIRE-001: issue indices exclude retired issues from deletion so that
    # closure history survives the adapter no longer re-queueing them.
    protect_retired = es.is_issue_index(index)
    is_asset = es.is_asset_index(index)

    retired_skipped = es.count_retired_skipped(index, cutoff_iso) if protect_retired else 0
    archived_kept = 0
    ids = None
    if protect_retired:
        ids, archived_kept, repos = es.candidate_ids(index, cutoff_iso, protect_retired)
        count = len(ids)
        sample = sorted(repos.items(), key=lambda kv: -kv[1])[:25]
    elif is_asset:
        ids, archived_kept, names = es.candidate_assets(index, cutoff_iso, protected_source_ids or set())
        count = len(ids)
        sample = sorted(names.items(), key=lambda kv: -kv[1])[:25]
    else:
        count = es.count_older_than(index, cutoff_iso, protect_retired)
        sample = es.sample_orphan_repos(index, cutoff_iso, protect_retired) if count else []

    print(f"  [{index}]")
    print(f"      latest run ts : {latest}")
    print(f"      cutoff (<)    : {cutoff_iso}  (latest − {min_age_minutes}m safety margin)")
    print(f"      orphan docs   : {count}")
    if protect_retired:
        print(f"      retired kept  : {retired_skipped}  "
              f"(stale run-tag but removed_date set — closure history, not orphans)")
        print(f"      archived kept : {archived_kept}  "
              f"(issue_status=archived — suppressed on purpose, not orphans)")
    if is_asset:
        print(f"      archived assets kept : {archived_kept}  "
              f"(repository has findings marked issue_status=archived)")

    result = {"index": index, "orphans": count, "deleted": 0,
              "retired_skipped": retired_skipped, "archived_kept": archived_kept}
    if count == 0:
        return result

    for repo, n in sample:
        print(f"        - {repo or '(no repo attr)'}: {n}")

    if apply:
        if ids is not None and protect_retired:
            result["deleted"], skipped = es.delete_ids(
                index, ids,
                is_protected=lambda src: _dig(src, ISSUE_STATUS_FIELD) == ARCHIVED,
                guard_suppressed=True)
            result["archived_kept"] += skipped
        elif ids is not None:
            # Re-derive the protected repositories now, not from the earlier
            # snapshot, then re-check each asset against them at delete time.
            fresh = refresh_protected() if refresh_protected else set(protected_source_ids or ())
            result["deleted"], skipped = es.delete_ids(
                index, ids,
                is_protected=lambda src: str(_dig(src, SOURCE_ID_FIELD)) in fresh
                if _dig(src, SOURCE_ID_FIELD) is not None else False)
            result["archived_kept"] += skipped
        else:
            result["deleted"] = es.delete_older_than(index, cutoff_iso, protect_retired)
        print(f"      DELETED       : {result['deleted']}")
        if result.get("archived_kept", 0) > archived_kept:
            print(f"      spared at delete time : {result['archived_kept'] - archived_kept}  "
                  f"(became archived after the candidates were selected)")
    else:
        print(f"      DRY RUN       : would delete {count} (pass --apply to delete)")
    return result


def order_indices(indices):
    """Group by instance and run the issue index before the asset index (then
    anything else), so the archived guard for an asset is decided from the
    issue index of the same instance. Plain sorting would put assets_ first."""
    def key(idx):
        suffix = idx.split("saltworks.ghas", 1)[-1]
        rank = 0 if idx.startswith("issues") else 1 if idx.startswith("assets") else 2
        return (suffix, rank, idx)
    return sorted(indices, key=key)


def run_all(es, indices, instance, min_age_minutes, apply):
    results = []
    for idx in order_indices(indices):
        try:
            protected = None
            refresh = None
            if es.is_asset_index(idx):
                sibling = es.sibling_issue_index(idx)
                if sibling in indices:
                    protected = es.archived_source_ids(sibling)
                    refresh = lambda sibling=sibling: es.archived_source_ids(sibling)
                else:
                    protected = set()
            r = process_index(es, idx, instance, min_age_minutes, apply, protected, refresh)
            if r:
                results.append(r)
        except Exception as exc:
            print(f"  [{idx}] ERROR: {exc}")
    return results


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

    results = run_all(es, indices, args.instance, args.min_age_minutes, args.apply)

    print("=" * 64)
    total_orphans = sum(r["orphans"] for r in results)
    total_deleted = sum(r["deleted"] for r in results)
    total_retired_kept = sum(r.get("retired_skipped", 0) for r in results)
    if total_retired_kept:
        print(f"  Retired issues preserved (not orphans): {total_retired_kept}")
    total_archived_kept = sum(r.get("archived_kept", 0) for r in results)
    if total_archived_kept:
        print(f"  Archived findings and assets preserved (not orphans): {total_archived_kept}")
    if args.apply:
        print(f"  DONE — deleted {total_deleted} orphan doc(s) across {len(results)} index(es).")
    else:
        print(f"  DRY RUN — {total_orphans} orphan doc(s) would be deleted across {len(results)} index(es).")
        print("  Re-run with --apply to delete.")
    print("=" * 64)


if __name__ == "__main__":
    main()
