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

import logging
import time
from datetime import datetime
from Core.Application import Application


class CleanQueueHelper():
    """Removes aged queue scans (and their child docs) plus any orphaned queue_issues/queue_assets."""

    def __init__(self, app:Application=None):
        """
        :app: optional Application to use; if not supplied a new one is created (logging instance 'qcln').
        """
        self.app = app if app is not None else Application(loggingInstance="qcln")
        self.es = self.app.GetElasticClient()
        self.bsize = 500
        self.bodies = {}

        # Complete, keep for 12 hrs
        self.bodies['complete'] = {
          "query": {
            "bool": {
              "must": [
                { "term": { "saltminer.internal.queue_status": { "value": "Complete" } } },
                { "range": { "timestamp": { "lte": "now-12h" } } }
              ],
              "must_not": [
                { "exists": { "field": "saltminer.internal.current_queue_scan_id"}}
              ]
            }
          },
          "sort": [ "id" ],
          "_source": False
        }
        # Error, keep for 48 hrs, don't remove PEN
        self.bodies['error'] = {
          "query": {
            "bool": {
              "must": [
                { "term": { "saltminer.internal.queue_status": { "value": "Error" } } },
                { "range": { "timestamp": { "lte": "now-48h" } } }
              ],
              "must_not": [
                { "exists": { "field": "saltminer.internal.current_queue_scan_id"}},
                { "exists": { "field": "saltminer.engagement.id"}},
                { "term": { "saltminer.scan.source_type": { "value": "Saltworks.PenTest" } } }
              ]
            }
          },
          "sort": [ "id" ],
          "_source": False
        }
        # Processing, keep for 12 hrs, don't remove PEN
        self.bodies['processing'] = {
          "query": {
            "bool": {
              "must": [
                { "term": { "saltminer.internal.queue_status": { "value": "Processing" } } },
                { "range": { "timestamp": { "lte": "now-12h" } } }
              ],
              "must_not": [
                { "exists": { "field": "saltminer.internal.current_queue_scan_id"}},
                { "exists": { "field": "saltminer.engagement.id"}},
                { "term": { "saltminer.scan.source_type": { "value": "Saltworks.PenTest" } } }
              ]
            }
          },
          "sort": [ "id" ],
          "_source": False
        }
        # Loading (NOT PEN!!), give up after 48 hrs
        self.bodies['loading'] = {
          "query": {
            "bool": {
              "must": [
                { "term": { "saltminer.internal.queue_status": { "value": "Loading" } } },
                { "range": { "timestamp": { "lte": "now-48h" } } }
              ],
              "must_not": [
                { "exists": { "field": "saltminer.internal.current_queue_scan_id"}},
                { "exists": { "field": "saltminer.engagement.id"}},
                { "term": { "saltminer.scan.source_type": { "value": "Saltworks.PenTest" } } }
              ]
            }
          },
          "sort": [ "id" ],
          "_source": False
        }

    def throttle(self):
        """Pause page processing while Elasticsearch is saturated with tasks."""
        wait = 0
        tasks = self.es.GetTaskCount()
        while tasks > self.tasksMax and wait < 10:
            logging.info("Elasticsearch task count is %s, waiting until it drops down below %s...", tasks, self.tasksMax)
            time.sleep(10)
            tasks = self.es.GetTaskCount()
            wait += 1
        if wait >= 10:
            logging.error("Elasticsearch task count failed to drop under %s after %s wait cycles (last count: %s)", self.tasksMax, wait, tasks)
            exit(1)

    def clean_orphans(self, child_index, scan_id_field):
        """Remove child docs whose referenced queue scan no longer exists in queue_scans.

        Enumerates the distinct scan ids referenced by child_index via a composite aggregation,
        checks each page against queue_scans.id, and deletes the child docs for any missing scans.
        """
        if not self.es.IndexExists(child_index):
            logging.info("Index %s not found; skipping orphan cleanup.", child_index)
            return
        logging.info("Scanning %s for orphaned docs (linked by %s)...", child_index, scan_id_field)
        start = datetime.now()
        after = None
        checked = 0
        removed = 0
        orphan_batch = []
        while True:
            agg_body = {
                "size": 0,
                "query": { "exists": { "field": scan_id_field } },
                "aggs": {
                    "sids": {
                        "composite": {
                            "size": 1000,
                            "sources": [ { "sid": { "terms": { "field": scan_id_field } } } ]
                        }
                    }
                }
            }
            if after is not None:
                agg_body["aggs"]["sids"]["composite"]["after"] = after
            rsp = self.es.Search(child_index, agg_body, size=0, navToData=False)
            agg = (rsp or {}).get("aggregations", {}).get("sids", {})
            buckets = agg.get("buckets", [])
            if not buckets:
                break
            after = agg.get("after_key")
            scan_ids = [b["key"]["sid"] for b in buckets]
            checked += len(scan_ids)

            # Which of these referenced scan ids still exist as a queue_scans.id?
            found_body = {
                "size": 0,
                "query": { "terms": { "id": scan_ids } },
                "aggs": { "found": { "terms": { "field": "id", "size": len(scan_ids) } } }
            }
            frsp = self.es.Search("queue_scans", found_body, size=0, navToData=False)
            existing = { b["key"] for b in (frsp or {}).get("aggregations", {}).get("found", {}).get("buckets", []) }

            for sid in scan_ids:
                if sid not in existing:
                    orphan_batch.append(sid)
                    if len(orphan_batch) >= self.bsize:
                        b = { "query": { "terms": { scan_id_field: orphan_batch } } }
                        self.es.DeleteByQuery(child_index, b, False, False, ignoreConflictError=True)
                        removed += len(orphan_batch)
                        orphan_batch = []

            self.throttle()
            if after is None:
                break

        if len(orphan_batch):
            b = { "query": { "terms": { scan_id_field: orphan_batch } } }
            self.es.DeleteByQuery(child_index, b, False, False, timeout=30, ignoreConflictError=True)
            removed += len(orphan_batch)

        elapsed = int((datetime.now() - start).total_seconds())
        logging.info("Checked %s distinct scan ids referenced by %s in %s sec; removed orphaned docs for %s missing scans.", checked, child_index, elapsed, removed)

    def run(self):
        """Remove aged queue scans (and their child docs), then sweep any orphaned child docs."""
        gtotal = 0
        self.tasksMax = self.es.GetTaskCount() + 250

        for key, body in self.bodies.items():
            count = 1
            start = datetime.now()
            scroller = self.es.SearchScroll("queue_scans", body, 10000, None)
            total = scroller.TotalHits
            gtotal += total
            batch = []
            for dto in scroller.Generator():
                if count % (self.bsize * 5) == 0:
                    self.throttle()

                batch.append(dto['_id'])
                if len(batch) >= self.bsize:
                    b = { "query": { "terms": { "saltminer.queue_scan_id": batch } } }
                    self.es.DeleteByQuery("queue_issues", b, False, False, ignoreConflictError=True)
                    b = { "query": { "terms": { "saltminer.current.queue_scan_id": batch } } }
                    self.es.DeleteByQuery("queue_assets", b, False, False, ignoreConflictError=True)
                    b = { "query": { "terms": { "saltminer.internal.current_queue_scan_id": batch } } }
                    self.es.DeleteByQuery("queue_scans", b, False, False, ignoreConflictError=True)
                    b = { "query": { "terms": { "id": batch } } }
                    self.es.DeleteByQuery("queue_scans", b, False, False, ignoreConflictError=True)
                    batch = []

                count += 1
                if count % (self.bsize * 10) == 0:
                    elapsed = int((datetime.now() - start).total_seconds())
                    remaining = int((total - count) / (count / elapsed))
                    logging.info("Processed %s / %s queue scans with %s status.  Elapsed: %s sec, Remaining: %s sec, Rate: %s/sec", count, total, key, elapsed, remaining, int(count / elapsed))

            # remainder
            if len(batch):
                b = { "query": { "terms": { "saltminer.queue_scan_id": batch } } }
                self.es.DeleteByQuery("queue_issues", b, False, False, timeout=30, ignoreConflictError=True)
                b = { "query": { "terms": { "saltminer.current.queue_scan_id": batch } } }
                self.es.DeleteByQuery("queue_assets", b, False, False, timeout=30, ignoreConflictError=True)
                b = { "query": { "terms": { "saltminer.internal.current_queue_scan_id": batch } } }
                self.es.DeleteByQuery("queue_scans", b, False, False, timeout=30, ignoreConflictError=True)
                b = { "query": { "terms": { "id": batch } } }
                self.es.DeleteByQuery("queue_scans", b, False, False, timeout=30, ignoreConflictError=True)
            logging.info("Processed %s total queue scans with %s status.", count, key)

        logging.info("Processed %s total queue scans. Queue scan removal complete.", gtotal)

        # Remove orphaned child docs (issues/assets whose parent queue scan is already gone).
        # Requires queue_scans to verify against - skip entirely if it's missing to avoid deleting everything.
        if self.es.IndexExists("queue_scans"):
            self.clean_orphans("queue_issues", "saltminer.queue_scan_id")
            self.clean_orphans("queue_assets", "saltminer.current.queue_scan_id")
            logging.info("Orphan cleanup complete.")
        else:
            logging.warning("queue_scans index not found; skipping orphan cleanup.")


if __name__ == "__main__":
    CleanQueueHelper().run()
