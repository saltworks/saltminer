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
