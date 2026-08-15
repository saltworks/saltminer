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

from Core.QueueClient import QueueClient
from Sources.SyncWorker import SyncQueueType

BATCH_SIZE = 1000
# A whole-source load can be six figures of IDs - log the first few individually in a
# dry run, then fall back to per-batch logging.
DRY_RUN_LOG_LIMIT = 20


def format_item(pvid, target_type: str, src_name: str, force: bool) -> tuple:
    '''
    Format a single ID into the (key, payload) pair expected by
    QueueClient.insert_queue(), matching the key convention used by
    RunWebhookPull.py and the payload fields read by SyncQueueData.

    :pvid: ID (str or int, stringified here since target_id is mapped as a
        keyword field and must be consistent across all callers)
    :target_type: SyncQueueType.SSC or SyncQueueType.FOD
    :src_name: source instance name, becomes target_instance
    :force: written into the payload, bypasses change detection in ProcessOne
    '''
    pvid = str(pvid).strip()
    return f"{target_type}|{src_name}|{pvid}", {
        "target_id": pvid,
        "target_type": target_type,
        "target_instance": src_name,
        "force": force,
    }


def load_queue_items(qcli: QueueClient, pvids, target_type: str, src_name: str,
                     priority: int = 5, force: bool = False,
                     source: str = "Manual Reinject", change_reason: str = None,
                     dry_run: bool = False) -> dict:
    '''
    Format and insert target IDs into the sync queue, submitting a batch as soon as
    one fills rather than materializing the whole set first.

    :qcli: constructed QueueClient
    :pvids: iterable of IDs - a normalized list (see normalize_pvids()) or a lazy
        generator such as Sources.IdLoader.iter_all_target_ids()
    :target_type: SyncQueueType.SSC or SyncQueueType.FOD
    :src_name: source instance name, becomes target_instance
    :priority: lower is processed sooner; QueueClient default is 5
    :force: written into each payload, bypasses change detection
    :source: recorded on each queue doc for later auditing
    :change_reason: free text recorded on each queue doc
    :dry_run: format and log, but do not write

    Returns a summary dict: total, unique, batches, skipped_duplicates.
    '''
    if not SyncQueueType.is_valid(target_type):
        raise ValueError(f"Invalid target_type '{target_type}', expected one of SSC/FOD.")
    if not src_name:
        raise ValueError("src_name is required.")
    if pvids is None:
        raise ValueError("pvids is required.")

    summary = {"total": 0, "unique": 0, "batches": 0, "skipped_duplicates": 0}
    # Keys already handled, so duplicates are dropped across the whole run and not just
    # within a batch.  Even a six-figure source costs only a few MB of keys here.
    seen = set()
    batch = {}

    def submit(batch):
        summary["batches"] += 1
        if dry_run:
            logging.info("[dry-run] Would submit batch %s (%s items) at priority %s, force=%s",
                         summary["batches"], len(batch), priority, force)
            return
        qcli.insert_queue(source, batch, priority=priority, change_reason=change_reason,
                          change_trigger="manual")
        logging.info("Batch %s submitted (%s items)", summary["batches"], len(batch))

    for pvid in pvids:
        summary["total"] += 1
        key, payload = format_item(pvid, target_type, src_name, force)
        if key in seen:
            summary["skipped_duplicates"] += 1
            continue
        seen.add(key)
        if dry_run and len(seen) <= DRY_RUN_LOG_LIMIT:
            logging.info("[dry-run] Would submit %s", key)
        elif dry_run and len(seen) == DRY_RUN_LOG_LIMIT + 1:
            logging.info("[dry-run] ... further individual IDs not logged.")
        batch[key] = payload
        if len(batch) >= BATCH_SIZE:
            submit(batch)
            batch = {}

    if batch:
        submit(batch)

    summary["unique"] = len(seen)
    if summary["total"] == 0:
        raise ValueError("No IDs supplied - nothing to queue.")

    logging.info("%s IDs supplied, %s unique (%s duplicates dropped)",
                 summary["total"], summary["unique"], summary["skipped_duplicates"])
    logging.info("%s%s items submitted across %s batch(es) of up to %s at priority %s, force=%s.",
                 "[dry-run] " if dry_run else "Done. ", summary["unique"], summary["batches"],
                 BATCH_SIZE, priority, force)
    return summary
