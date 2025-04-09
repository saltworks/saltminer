''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-04-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

import logging
import sys
import os

from Core.Application import Application
from Utility.SyncQueueHelper import SyncQueueHelper

sourceNames = []
prog = os.path.splitext(os.path.basename(__file__))[0]
prmSourceName = None
prmIdList= []
prmPriority = 5
prmPerm = False

if len(sys.argv) > 1:
    prmSourceName = sys.argv[1]         # Source name
if len(sys.argv) > 2:
    prmIdList = sys.argv[2]             # Comma-delimited list of IDs to add to the sync queue
if len(sys.argv) > 3:
    prmPriority = sys.argv[3]           # Priority
if len(sys.argv) > 4:
    prmPerm = str(sys.argv[4]).lower().startswith('t')  # Whether the priority setting should be permanent

msg = "Usage:\n\npython3 RunSyncQueuePriority.py src idList priority permanent\n\n:src: Source name, i.e. SSC1\n:idList: Comma-delimited list of IDs to add to the sync queue\n:priority: 1-9, 9 is least priority\n:permanent: True to remember priority for the IDs"
if len(sys.argv) < 4:
    logging.warning(msg)
    exit(1)
if prmPriority not in range(1, 9):
    raise ValueError(f"Invalid priority '{prmPriority}', expected a single digit 1-9.")
app = Application()

sourceType = app.Settings.GetSource(prmSourceName, "Source", "")
if not sourceType in ["FOD", "SSC"]:
    raise ValueError(f"Invalid source '%s'.  Check source configuration.", prmSourceName)

sqh = SyncQueueHelper(app.Settings, prmSourceName)
sqh.InsertQueueBatch(prmIdList.split(","), prmPriority, False, False)

logging.info("[%s] Processing complete.", prog)
