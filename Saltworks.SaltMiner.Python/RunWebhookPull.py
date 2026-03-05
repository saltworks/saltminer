''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

import logging
import os
import sys
import json

from Core.Application import Application
from Utility.SmApiClient import SmApiClient
from Utility.SyncQueueHelper import SyncQueueHelper
from Core.SscClient import SscClient

# Parameters
prog = os.path.splitext(os.path.basename(__file__))[0]
prmWebhookSource = None
prmSourceName = 'all'
prmLogInstance = None
enableDropCheck = True
if len(sys.argv) > 1:
    prmWebhookSource = sys.argv[1]      # Webhook source ID (i.e. ssc1)
if len(sys.argv) > 2:
    prmSourceName = sys.argv[2]         # Source name (i.e. SSC1)
if len(sys.argv) > 3:
    prmLogInstance = sys.argv[3]        # Custom logging instance

msg = "Usage:\n\npython3 RunWebhookPull.py whsrc src [loginst]\n\n:whsrc: Webhook source ID, i.e ssc1\n:src: Source name, i.e. SSC1\n:loginst: logging instance number, defaults to none"
if len(sys.argv) < 3:
    logging.warning(msg)
    sys.exit(1)

# Setup
app = Application(loggingInstance=prmLogInstance)
es = app.GetElasticClient()
api = SmApiClient(app.Settings, prmSourceName)
sqh = SyncQueueHelper(app.Settings, prmSourceName)
ssc = None
sscInactives = []
try:
    ssc = SscClient(app.Settings, prmSourceName)
except Exception:
    logging.warning("[Webhook Pull] '%' appears to not be an SSC source name, inactive release checking disabled.")
    ssc = None
try:
    if ssc:
        sscInactives = ssc.GetInactiveProjectVersionIds()
except Exception as ex:
    logging.error("[Webhook Pull] Error retrieving SSC inactive version list: %s", ex)
    sscInactives = []

MAX_LOOPS = 500
maxLoops = app.Settings.Get("main", "WebhookMaxBatches", MAX_LOOPS)

# Let's go
sscIds = []
foundSome = False
if maxLoops > MAX_LOOPS:
    logging.warning("[Webhook Pull] WebhookMaxBatches set to %s, but max supported currently are %s, which will be used.", maxLoops, MAX_LOOPS)
    maxLoops = MAX_LOOPS
curLoop = 1
while curLoop < maxLoops:
    data = api.GetWebhookEvents(prmWebhookSource)
    if not data:
        if not foundSome:
            logging.info("[Webhook Pull] No webhook data returned.  Processing complete")
        logging.info("[Webhook Pull] No more webhook data found.")
        sys.exit(0)
    foundSome = True

    logging.info("[Webhook Pull] Processing %s webhook (queue sync) items.", len(data))
    count = 0
    for dataItm in data:
        didSomething = False
        if not dataItm['payload']:
            logging.error("[Webhook Pull] Invalid/missing payload for webhook (queue sync item) ID %s, skipping.", dataItm['id'])
            continue
        payload = json.loads(dataItm['payload'])

    # SSC
    if 'events' in payload and payload['events'] and 'projectVersionId' in payload['events'][0]:
        didSomething = True
        for evt in payload['events']:
            if 'projectVersionId' in evt:
                event = "?" if 'event' not in evt else evt['event']
                user = "?" if 'username' not in evt else evt['username']
                logging.info("[Webhook Pull] SSC update event '%s' found for project version %s, tagged with username %s.", event, evt['projectVersionId'], user)
                sscId = evt['projectVersionId']
                if sscId not in sscInactives:
                    sscIds.append(sscId)
                else:
                    logging.info("[Webhook Pull] SSC projectVersion ID %s is inactive and will not be syned.", sscId)
            else:
                logging.error("[Webhook Pull] SSC update event may be malformed (missing projectVersionId), encountered in webhook (queue sync item) ID %s. Skipping.", dataItm['id'])

        if not didSomething:
            logging.info("[Webhook Pull] Unknown type of webhook (queue sync item) with ID '%s'.", dataItm['id'])

        count += 1
        if count % 100 == 0:
            logging.info("[Webhook Pull] %s/%s webhook (queue sync) items processed.", count, len(data))

    if len(sscIds) > 0:
        logging.info("[Webhook Pull] %s total SSC IDs to queue for updates.", len(sscIds))
        sqh.InsertQueueBatch(sscIds, force=True)
    curLoop += 1
logging.warning("[Webhook Pull] Max data loops occurred (%s), stopping processing.", maxLoops)
