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
import os
import sys
import json

from Core.Application import Application
from Core.DataClient import DataClient
from Core.QueueClient import QueueClient
from Core.SscClient import SscClient
from Sources.SyncWorker import SyncQueueType
from Utility.QueueLoader import format_item

# Queue the new (python) sync agent - RunAgentService.py - reads from.  Pass the optional
# queue tag argument to sweep webhook events into a different queue instead (ex: the legacy
# queue tag configured under SyncAgent/queue_index_pattern_tag).
DEFAULT_QUEUE_TAG = "sync"

# Whether webhook-queued items bypass change detection during sync.  A webhook event means SSC
# already told us something changed, so today we force.  Flip to False to let the sync agent's
# own change detection decide instead.
WEBHOOKS_SHOULD_FORCE = True

# Parameters
prog = os.path.splitext(os.path.basename(__file__))[0]
prm_webhook_src = None
prm_src_name = 'all'
prm_log_instance = None
prm_queue_tag = DEFAULT_QUEUE_TAG
enable_drop_check = True
if len(sys.argv) > 1:
    prm_webhook_src = sys.argv[1]      # Webhook source ID (i.e. ssc1)
if len(sys.argv) > 2:
    prm_src_name = sys.argv[2]         # Source name (i.e. SSC1)
if len(sys.argv) > 3:
    prm_log_instance = sys.argv[3]     # Custom logging instance
if len(sys.argv) > 4 and sys.argv[4].strip():
    prm_queue_tag = sys.argv[4].strip()  # Destination queue index pattern tag

msg = ("Usage:\n\npython3 RunWebhookPull.py whsrc src [loginst] [queuetag]\n\n"
       ":whsrc: Webhook source ID, i.e ssc1\n"
       ":src: Source name, i.e. SSC1\n"
       ":loginst: logging instance number, defaults to none\n"
       f":queuetag: destination queue index pattern tag, defaults to '{DEFAULT_QUEUE_TAG}' "
       "(sm_queue_sync_*, the queue the sync agent reads)")
if len(sys.argv) < 3:
    logging.warning(msg)
    sys.exit(1)

# Setup
app = Application(loggingCustomTag=prm_log_instance)
es = app.GetElasticClient()
api = DataClient(app, validate_on_init=True)
qcli = QueueClient(app, prm_queue_tag)
logging.info("[Webhook Pull] Webhook events will be swept into queue '%s'.", qcli.index_pattern)
ssc = None
ssc_inactives = []
try:
    ssc = SscClient(app.Settings, prm_src_name)
except Exception:
    logging.warning("[Webhook Pull] '%s' appears to not be an SSC source name, inactive release checking disabled.", prm_src_name)
    ssc = None
try:
    if ssc:
        ssc_inactives = ssc.GetInactiveProjectVersionIds()
except Exception as ex:
    logging.error("[Webhook Pull] Error retrieving SSC inactive version list: %s", ex)
    ssc_inactives = []

MAX_LOOPS = 500
max_loops = app.Settings.Get("main", "WebhookMaxBatches", MAX_LOOPS)

# Let's go
ssc_data = {}
seen_ids = []
found_some = False
if max_loops > MAX_LOOPS:
    logging.warning("[Webhook Pull] WebhookMaxBatches set to %s, but max supported currently are %s, which will be used.", max_loops, MAX_LOOPS)
    max_loops = MAX_LOOPS
cur_loop = 1
while cur_loop < max_loops:
    data = api.webhook_get(prm_webhook_src)
    if not data:
        if not found_some:
            logging.info("[Webhook Pull] No webhook data returned.  Processing complete")
        else:
            logging.info("[Webhook Pull] %s unique IDs processed. Processing complete.", len(seen_ids))
        sys.exit(0)
    found_some = True

    logging.info("[Webhook Pull] Processing %s webhook (queue sync) items.", len(data))
    count = 0
    for data_item in data:
        did_something = False
        if not data_item['payload']:
            logging.error("[Webhook Pull] Invalid/missing payload for webhook (queue sync item) ID %s, skipping.", data_item['id'])
            continue
        payload = json.loads(data_item['payload'])
        if not ('events' in payload and payload['events'] and 'projectVersionId' in payload['events'][0]):
            continue

        # SSC
        did_something = True
        for evt in payload['events']:
            if 'projectVersionId' in evt:
                event = "?" if 'event' not in evt else evt['event']
                user = "?" if 'username' not in evt else evt['username']
                logging.info("[Webhook Pull] SSC update event '%s' found for project version %s, tagged with username %s.", event, evt['projectVersionId'], user)
                ssc_id = evt['projectVersionId']
                if ssc_id in ssc_inactives:
                    logging.info("[Webhook Pull] SSC projectVersion ID %s is inactive and will not be syned.", ssc_id)
                    continue
                if event not in ['APP_VERSION_DELETED'] and ssc_id not in seen_ids:
                    key, item = format_item(ssc_id, SyncQueueType.SSC, prm_src_name, WEBHOOKS_SHOULD_FORCE)
                    ssc_data[key] = item
                    seen_ids.append(ssc_id)
                else:
                    logging.debug("[Webhook Pull] SSC project version ID %s already queued for update, skipping.", ssc_id)
            else:
                logging.error("[Webhook Pull] SSC update event may be malformed (missing project version ID), encountered in webhook (queue sync item) ID %s. Skipping.", data_item['id'])

        if not did_something:
            logging.info("[Webhook Pull] Unknown type of webhook (queue sync item) with ID '%s'.", data_item['id'])

        count += 1
        if count % 100 == 0:
            logging.info("[Webhook Pull] %s/%s webhook (queue sync) items processed.", count, len(data))

    if len(ssc_data) > 0:
        logging.info("[Webhook Pull] %s total SSC IDs to queue for updates.", len(ssc_data))
        qcli.insert_queue("SSC Webhook", ssc_data, change_reason="SSC webhook event", change_trigger="webhook")
        ssc_data = {}
    cur_loop += 1
# end while
logging.warning("[Webhook Pull] Max data loops occurred (%s), stopping processing.", max_loops)
