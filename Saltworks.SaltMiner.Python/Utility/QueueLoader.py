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

BATCH_SIZE = 100


def format_items(pvids: list, target_type: str, src_name: str, force: bool) -> dict:
    '''
    Format a list of already-normalized ID strings into the {key: payload} shape
    expected by QueueClient.insert_queue(), matching the key convention used by
    RunWebhookPull.py and the payload fields read by SyncQueueData.

    :pvids: list of ID strings, already normalized (stripped, non-empty)
    :target_type: SyncQueueType.SSC or SyncQueueType.FOD
    :src_name: source instance name, becomes target_instance
    :force: written into each payload, bypasses change detection in ProcessOne

    Returns a dict of {key: payload}. Duplicate IDs collapse to a single entry.
    '''
    items = {}
    for pvid in pvids:
        key = f"{target_type}|{src_name}|{pvid}"
        items[key] = {
            "target_id": pvid,
            "target_type": target_type,
            "target_instance": src_name,
            "force": force,
        }
    return items


def chunk_items(items: dict, batch_size: int = BATCH_SIZE) -> list:
    '''
    Split an {key: payload} dict into a list of same-shaped dicts, each with at
    most batch_size entries. insert_queue() does no internal batching, so this
    is required before calling it with a large item list.
    '''
    keys = list(items.keys())
    return [
        {k: items[k] for k in keys[i:i + batch_size]}
        for i in range(0, len(keys), batch_size)
    ]


def load_queue_items(qcli: QueueClient, pvids: list, target_type: str, src_name: str,
                     priority: int = 5, force: bool = False,
                     source: str = "Manual Reinject", change_reason: str = None,
                     dry_run: bool = False) -> dict:
    '''
    Format and insert a list of target IDs into the sync queue.

    :qcli: constructed QueueClient
    :pvids: list of already-normalized ID strings (see normalize_ids())
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
    if not isinstance(pvids, list) or len(pvids) == 0:
        raise ValueError("pvids must be a non-empty list.")

    items = format_items(pvids, target_type, src_name, force)
    batches = chunk_items(items)
    summary = {
        "total": len(pvids),
        "unique": len(items),
        "batches": len(batches),
        "skipped_duplicates": len(pvids) - len(items),
    }

    logging.info("%s IDs supplied, %s unique (%s duplicates dropped)",
                 summary["total"], summary["unique"], summary["skipped_duplicates"])

    if dry_run:
        for key in items.keys():
            logging.info("[dry-run] Would submit %s", key)
        logging.info("[dry-run] Would submit %s batch(es) of up to %s at priority %s, force=%s",
                     len(batches), BATCH_SIZE, priority, force)
        return summary

    logging.info("Submitting %s batch(es) of up to %s at priority %s, force=%s",
                 len(batches), BATCH_SIZE, priority, force)
    for i, batch in enumerate(batches, start=1):
        qcli.insert_queue(source, batch, priority=priority, change_reason=change_reason,
                          change_trigger="manual")
        logging.info("Batch %s/%s submitted (%s items)", i, len(batches), len(batch))

    logging.info("Done. %s items submitted across %s batch(es).", summary["unique"], summary["batches"])
    return summary
