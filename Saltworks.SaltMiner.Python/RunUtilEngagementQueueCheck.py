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
Finds Draft, Queued and Error engagements missing their queue_scan, queue_assets or queue_issues, and
repairs one engagement - the queue scan from the engagement itself, assets and issues from the
published (assets/issues_pen_*) copy.

Only those three statuses are in scope.  A Draft lives entirely in the queue indices, a Queued
engagement can't be published without its queue data, and an Error engagement may have failed to
publish because of it.  An engagement whose queue data is all present is left alone - for Error, the
failure had some other cause.  Published and Historical are read from the published indices, and
Processing is in the manager's hands.

Usage (run from Saltworks.SaltMiner.Python/):
  python3 RunUtilEngagementQueueCheck.py check [--status Draft,Queued,Error] [--all]
  python3 RunUtilEngagementQueueCheck.py repair <engagement-id> [--dry-run]

  check        Read-only.  Reports each in-scope engagement missing any of the three, or with more
               than one queue scan.  A Draft with no queue_issues is not flagged - a draft with
               no findings yet is normal; Queued/Error with none is.  --all lists every in-scope
               engagement, not just the problem ones.
  repair       Works on the one engagement given.  Only fills in what is absent:
                 queue_assets  none, and published assets exist          -> one per published asset
                 queue_issues  none, and published issues exist          -> one per published issue
                 queue_scan    missing                                   -> built from the engagement
                               and a queue asset, as the UI builds it: scan_date = engagement
                               timestamp (its creation), new report_id, source/asset type and
                               instance from the asset.
               Once all three are present, the queue scan's status is set for the engagement:
                 Draft          stays Loading
                 Queued, Error  -> Loading, and the engagement goes back to Draft (as Reset Publish
                                   does), so the rebuilt data is reviewed and re-published from the UI
               If anything is still missing, statuses are left alone and a new queue scan stays Loading.
               --dry-run reports what would be written, and writes nothing.
  --suffix     Published index suffix, default pen_saltworks.pentest_pen1
               (so scans_pen_saltworks.pentest_pen1, assets_..., issues_...).

Recreated docs reuse the published doc's id (queue asset id = published asset id, queue issue id =
published issue id).  That makes a repeated repair overwrite rather than duplicate, and lines the queue
docs up with the comments the manager rewrote against the published ids at publish time.  The queue
scan reuses the id its surviving queue assets/issues still point at, when there is one.
'''

import argparse
import logging
import sys
import uuid
from datetime import datetime, timezone

from Core.Application import Application

ENGAGEMENTS = "engagements"
QSCANS = "queue_scans"
QASSETS = "queue_assets"
QISSUES = "queue_issues"
ENG_FIELD = "saltminer.engagement.id"

# The only engagement statuses checked or repaired.
IN_SCOPE_STATUSES = ["Draft", "Queued", "Error"]


def issues_required(status):
    """A Draft with no issues yet is normal; a Queued/Error engagement with none has lost them."""
    return status != "Draft"


def now_iso():
    return datetime.now(timezone.utc).isoformat()


class EngagementQueueCheck():
    """Reports and repairs engagements whose queue_scan / queue_assets / queue_issues are missing."""

    def __init__(self, suffix, app:Application=None):
        self.app = app if app is not None else Application(loggingCustomTag="engq", skipCleanFiles=True)
        self.es = self.app.GetElasticClient()
        self.scans = f"scans_{suffix}"
        self.assets = f"assets_{suffix}"
        self.issues = f"issues_{suffix}"

    # ---- reads ----------------------------------------------------------------------------------

    def counts_by_engagement(self, index):
        """{engagement id: doc count} for every engagement referenced by index.  {} if index is absent."""
        if not self.es.IndexExists(index):
            logging.warning("Index %s not found; treating it as empty.", index)
            return {}
        counts = {}
        after = None
        while True:
            body = {
                "size": 0,
                "aggs": { "eng": { "composite": {
                    "size": 1000,
                    "sources": [ { "id": { "terms": { "field": ENG_FIELD } } } ]
                } } }
            }
            if after:
                body["aggs"]["eng"]["composite"]["after"] = after
            rsp = self.es.Search(index, body, size=0, navToData=False)
            agg = (rsp or {}).get("aggregations", {}).get("eng", {})
            for b in agg.get("buckets", []):
                counts[b["key"]["id"]] = b["doc_count"]
            after = agg.get("after_key")
            if not agg.get("buckets") or not after:
                return counts

    def all_docs(self, index, query):
        """Every doc (hit) in index matching query; [] if the index is absent."""
        if not self.es.IndexExists(index):
            return []
        return list(self.es.SearchScroll(index, { "query": query }, 1000, None).Generator())

    def by_engagement(self, index, engagement_id):
        return self.all_docs(index, { "term": { ENG_FIELD: engagement_id } })

    # ---- check ----------------------------------------------------------------------------------

    def check(self, statuses=None, show_all=False):
        """Prints one line per engagement with a missing (or duplicated) queue scan/assets/issues."""
        qs, qa, qi = (self.counts_by_engagement(i) for i in (QSCANS, QASSETS, QISSUES))
        ps, pa, pi = (self.counts_by_engagement(i) for i in (self.scans, self.assets, self.issues))

        statuses = [s for s in (statuses or IN_SCOPE_STATUSES) if s in IN_SCOPE_STATUSES]
        query = { "terms": { "saltminer.engagement.status": statuses } }

        header = (f"{'engagement id':38} {'status':11} {'q scan':>6} {'q ast':>6} {'q iss':>6} | "
                  f"{'p scan':>6} {'p ast':>6} {'p iss':>6}  problem / name")
        print(f"\n{'All' if show_all else 'Missing queue data -'} {'/'.join(statuses)} engagements "
              f"(q = queue_*, p = published {self.scans} etc.)\n")
        print(header)
        print("-" * len(header))
        total = problems = repairable = 0
        for hit in self.all_docs(ENGAGEMENTS, query):
            src = hit["_source"]
            eid = src.get("id") or hit["_id"]
            eng = src.get("saltminer", {}).get("engagement", {})
            status = eng.get("status") or "?"
            c = [qs.get(eid, 0), qa.get(eid, 0), qi.get(eid, 0), ps.get(eid, 0), pa.get(eid, 0), pi.get(eid, 0)]
            total += 1

            issues = []
            if c[0] == 0:
                issues.append("no queue_scan")
            elif c[0] > 1:
                issues.append(f"{c[0]} queue_scans")
            if c[1] == 0:
                issues.append("no queue_assets")
            need_issues = issues_required(status)
            if c[2] == 0 and need_issues:
                issues.append("no queue_issues")
            # queue scan: built from the engagement + a queue asset (existing, or rebuilt from published)
            fixable = (c[0] == 0 and (c[1] or c[4])) or (c[1] == 0 and c[4]) or (c[2] == 0 and need_issues and c[5])
            if issues:
                problems += 1
                repairable += 1 if fixable else 0
            if issues or show_all:
                note = ", ".join(issues) or "ok"
                if issues and fixable:
                    note += " [repairable]"
                print(f"{eid:38} {status:11} {c[0]:>6} {c[1]:>6} {c[2]:>6} | {c[3]:>6} {c[4]:>6} {c[5]:>6}  "
                      f"{note} / {eng.get('name', '')}")

        if not problems and not show_all:
            print("(none)")
        print(f"\nEngagements checked:          {total}  (status {', '.join(statuses)})")
        print(f"Missing queue data:           {problems}")
        print(f"  repairable:                 {repairable}")
        if repairable:
            print("Repair one with:  python3 RunUtilEngagementQueueCheck.py repair <engagement-id> --dry-run")
        return problems

    # ---- repair ---------------------------------------------------------------------------------

    def engagement_info(self, eng, fallback):
        """The saltminer.engagement block carried on queue docs, from the engagement doc itself."""
        return {
            "id": eng["id"],
            "name": eng["name"],
            "customer": eng.get("customer", fallback.get("customer")),
            "subtype": eng.get("subtype", fallback.get("subtype")),
            "publish_date": eng.get("publish_date", fallback.get("publish_date")),
            "attributes": eng.get("attributes", fallback.get("attributes")),
        }

    def repair(self, engagement_id, dry_run=False):
        hit = self.es.Get(ENGAGEMENTS, engagement_id, raiseNotFoundError=False)
        if not hit or not hit.get("found", True) or "_source" not in hit:
            print(f"Engagement {engagement_id} not found in {ENGAGEMENTS}.")
            return 1
        eng = hit["_source"].get("saltminer", {}).get("engagement", {})
        eng["id"] = engagement_id
        status = eng.get("status", "")
        mode = "DRY RUN - nothing will be written" if dry_run else "REPAIR"
        print(f"\n{mode}: engagement {engagement_id} '{eng.get('name')}' (status {status})")
        if status not in IN_SCOPE_STATUSES:
            print(f"  Engagement is {status}; only {' and '.join(IN_SCOPE_STATUSES)} engagements are repaired.")
            return 1

        created = hit["_source"].get("timestamp")
        qscans = self.by_engagement(QSCANS, engagement_id)
        qassets = self.by_engagement(QASSETS, engagement_id)
        qissues = self.by_engagement(QISSUES, engagement_id)
        passets = self.by_engagement(self.assets, engagement_id)
        pissues = self.by_engagement(self.issues, engagement_id)
        print(f"  queue:     {len(qscans)} scan(s), {len(qassets)} asset(s), {len(qissues)} issue(s)")
        print(f"  published: {len(passets)} asset(s), {len(pissues)} issue(s)  [{self.assets} / {self.issues}]")

        if len(qscans) > 1:
            print(f"  ! {len(qscans)} queue scans for one engagement - not repairing automatically: "
                  + ", ".join(h["_id"] for h in qscans))
            return 1
        if qscans and qassets and (qissues or not issues_required(status)):
            print("  Queue data is all present - nothing to repair." +
                  ("  The Error was not caused by missing queue data; status left as is." if status == "Error" else ""))
            return 0

        bulk = self.es.NewBulkHelper(500)
        written = 0
        stamp = now_iso()

        # -- queue scan id -------------------------------------------------------------------------
        # Settled first so recreated assets/issues can point at it; the doc itself is built below,
        # once there is a queue asset to take its source type / asset type / instance from.
        need_scan = not qscans
        if qscans:
            qscan_id = qscans[0]["_id"]
            print(f"  queue_scan present: {qscan_id} ({qscans[0]['_source'].get('saltminer', {}).get('internal', {}).get('queue_status')})")
        else:
            # Prefer the id surviving children still reference, so they re-attach
            orphan_ids = {h["_source"].get("saltminer", {}).get("internal", {}).get("queue_scan_id") for h in qassets} \
                | {h["_source"].get("saltminer", {}).get("queue_scan_id") for h in qissues}
            orphan_ids.discard(None)
            if len(orphan_ids) > 1:
                print(f"  ! surviving queue docs point at {len(orphan_ids)} different queue scans ({', '.join(orphan_ids)}); using the first.")
            qscan_id = sorted(orphan_ids)[0] if orphan_ids else str(uuid.uuid4())

        # -- queue assets -------------------------------------------------------------------------
        # published asset id -> queue asset id, for the issues below
        asset_map = {}
        assets_created = []
        if qassets:
            by_source = { (h["_source"]["saltminer"].get("asset", {}).get("source_id")): h["_id"] for h in qassets }
            for h in passets:
                qid = by_source.get(h["_source"]["saltminer"].get("asset", {}).get("source_id"))
                if qid:
                    asset_map[h["_id"]] = qid
            print(f"  queue_assets present: {len(qassets)}")
        elif not passets:
            print("  ! queue_assets missing and the engagement has no published assets - cannot rebuild them.")
        else:
            for h in passets:
                p = h["_source"]
                sm = p.get("saltminer", {})
                asset = dict(sm.get("asset", {}))
                doc = {
                    "id": h["_id"],
                    "timestamp": p.get("timestamp", stamp),
                    "last_updated": stamp,
                    "saltminer": {
                        "asset": asset,
                        "engagement": self.engagement_info(eng, sm.get("engagement") or {}),
                        "inventory_asset": { "key": (sm.get("inventory_asset") or {}).get("key") },
                        "internal": { "queue_scan_id": qscan_id },
                    }
                }
                asset_map[h["_id"]] = h["_id"]
                assets_created.append(doc)
                print(f"  + queue_asset {h['_id']} '{asset.get('name')}'")
                if not dry_run:
                    bulk.add(QASSETS, doc, h["_id"])
                written += 1

        # -- queue issues -------------------------------------------------------------------------
        issue_total = len(qissues)
        if qissues:
            print(f"  queue_issues present: {len(qissues)}")
            dangling = [h["_id"] for h in qissues if h["_source"].get("saltminer", {}).get("queue_asset_id") not in
                        {a["_id"] for a in qassets} | set(asset_map.values())]
            if dangling:
                print(f"  ! {len(dangling)} queue issue(s) reference a queue asset that does not exist (not re-pointed).")
        elif not pissues:
            if issues_required(status):
                print("  ! queue_issues missing and the engagement has no published issues - nothing to copy.")
        else:
            skipped = 0
            for h in pissues:
                p = h["_source"]
                sm = p.get("saltminer", {})
                qasset_id = asset_map.get((sm.get("asset") or {}).get("id"))
                if not qasset_id:
                    skipped += 1
                    continue
                doc = {
                    "id": h["_id"],
                    "timestamp": p.get("timestamp", stamp),
                    "last_updated": stamp,
                    "message": p.get("message"),
                    "labels": p.get("labels") or {},
                    "tags": p.get("tags") or [],
                    "is_processed": False,
                    "is_cloned": False,
                    "vulnerability": p.get("vulnerability", {}),
                    "saltminer": {
                        "queue_asset_id": qasset_id,
                        "queue_scan_id": qscan_id,
                        "custom_data": sm.get("custom_data"),
                        "attributes": sm.get("attributes") or {},
                        "is_historical": sm.get("is_historical", False),
                        "source": sm.get("source") or {},
                        "issue_type": sm.get("issue_type"),
                        "engagement": self.engagement_info(eng, sm.get("engagement") or {}),
                    }
                }
                if not dry_run:
                    bulk.add(QISSUES, doc, h["_id"])
                written += 1
                issue_total += 1
            print(f"  + {len(pissues) - skipped} queue_issue(s) copied from published issues")
            if skipped:
                print(f"  ! {skipped} published issue(s) skipped: their asset has no matching queue asset.")

        # Assets and issues must be searchable before the queue scan is written or changed, so the
        # issue count the manager validates against matches what it can find.
        if not dry_run:
            bulk.flush(refresh="wait_for")

        # -- queue scan doc, from the engagement plus one queue asset -------------------------------
        # Mirrors EngagementHelper.CreateEngagementQueueScan.  report_id is a fresh GUID, as the UI
        # makes it (the original is random and unrecoverable; it only matters once published).
        # scan_date is the engagement's creation timestamp: the UI sets both in the same request.
        new_scan = None
        if need_scan:
            src_asset = next((a["_source"]["saltminer"].get("asset", {}) for a in qassets), None) \
                or next((d["saltminer"]["asset"] for d in assets_created), None)
            if not src_asset:
                print("  ! queue_scan missing and there is no queue asset to build it from - not rebuilt.")
            elif not created:
                print("  ! queue_scan missing and the engagement has no timestamp to use as scan_date - not rebuilt.")
            else:
                new_scan = {
                    "id": qscan_id,
                    "timestamp": created,
                    "last_updated": stamp,
                    "saltminer": {
                        "scan": {
                            "report_id": str(uuid.uuid4()),
                            "assessment_type": "Pen",
                            "product_type": "AppSec",
                            "product": "SaltMiner",
                            "vendor": "Saltworks",
                            "scan_date": created,
                            "source_type": src_asset.get("source_type"),
                            "asset_type": src_asset.get("asset_type"),
                            "instance": src_asset.get("instance"),
                            "is_saltminer_source": True,
                        },
                        "engagement": self.engagement_info(eng, {}),
                        # queue_status and issue_count are set below
                        "internal": { "queue_status": "Loading", "issue_count": 0 },
                    }
                }
                print(f"  + queue_scan {qscan_id} from the engagement (scan_date {created}) and queue asset "
                      f"'{src_asset.get('name')}' ({src_asset.get('source_type')} / {src_asset.get('asset_type')} / "
                      f"{src_asset.get('instance')})")

        still_missing = [name for name, n in (("queue_scan", 0 if need_scan and not new_scan else 1),
                                              ("queue_assets", len(qassets) + len(assets_created)),
                                              ("queue_issues", issue_total if issues_required(status) else 1)) if not n]
        if still_missing:
            print(f"  ! still no {', '.join(still_missing)} after the repair; engagement status left as {status}.")

        # Queue scan status once the data is complete: Draft stays Loading; Queued and Error go back
        # to Draft with the scan Loading, as Reset Publish does, so the rebuilt data is reviewed and
        # re-published from the UI rather than published unseen.  Incomplete, statuses are left alone.
        reset_to_draft = not still_missing and status in ("Queued", "Error")
        target = "Loading" if reset_to_draft else None
        current = (qscans[0]["_source"].get("saltminer", {}).get("internal", {}).get("queue_status")
                   if qscans else None)

        # -- queue scan, carrying the final issue count --------------------------------------------
        if new_scan:
            new_scan["saltminer"]["internal"]["issue_count"] = issue_total
            new_scan["saltminer"]["internal"]["queue_status"] = target or "Loading"
            print(f"  + queue_scan {qscan_id} written with queue_status {target or 'Loading'}")
            if not dry_run:
                self.es.IndexWithId(QSCANS, qscan_id, new_scan)
            written += 1
        elif qscan_id:
            update = {}
            if issue_total != len(qissues):
                update["issue_count"] = issue_total
            if target and target != current:
                update["queue_status"] = target
            if update:
                print(f"  ~ queue_scan {qscan_id} " + ", ".join(f"{k} -> {v}" for k, v in update.items()))
                if not dry_run:
                    self.es.UpdateDoc(QSCANS, qscan_id, { "saltminer": { "internal": update } })
        if not dry_run and qscan_id:
            self.es.RefreshIndex(QSCANS)

        if reset_to_draft:
            print(f"  ~ engagement status {status} -> Draft")
            if not dry_run:
                self.es.UpdateDoc(ENGAGEMENTS, engagement_id,
                                  { "last_updated": now_iso(), "saltminer": { "engagement": { "status": "Draft" } } })
                self.es.RefreshIndex(ENGAGEMENTS)
            written += 1

        if dry_run:
            print(f"\nDry run: {written} document(s) would be written.")
        else:
            print(f"\n{written} document(s) written.")
        return 0


def main():
    parser = argparse.ArgumentParser(description="Check/repair engagement queue_scan, queue_assets and queue_issues.")
    parser.add_argument("--suffix", default="pen_saltworks.pentest_pen1",
                        help="published index suffix (scans_<suffix>, assets_<suffix>, issues_<suffix>)")
    sub = parser.add_subparsers(dest="cmd", required=True)
    c = sub.add_parser("check", help="report engagements with missing queue data (read-only)")
    c.add_argument("--status", help="comma-separated engagement statuses to include, e.g. Draft,Queued")
    c.add_argument("--all", action="store_true", help="list every engagement, not only problem ones")
    r = sub.add_parser("repair", help="rebuild missing queue data for one engagement from its published copy")
    r.add_argument("engagement_id")
    r.add_argument("--dry-run", action="store_true", help="report what would be written; write nothing")
    args = parser.parse_args()

    util = EngagementQueueCheck(args.suffix)
    if args.cmd == "check":
        statuses = [s.strip() for s in args.status.split(",")] if args.status else None
        util.check(statuses, args.all)
        return 0
    return util.repair(args.engagement_id, args.dry_run)


if __name__ == "__main__":
    sys.exit(main())
