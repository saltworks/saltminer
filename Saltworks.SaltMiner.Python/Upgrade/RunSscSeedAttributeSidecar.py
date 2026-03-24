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

# This upgrade utility program seeds the attributes sidecar so that it will be in sync before the first (or next) refresh.
# This utility can be run at any time and can be run multiple times if desired.
# The source data for this utility is the sscprojattr2 index - if this index is empty/missing (new install) then this utility will have no effect.

# Example call from command prompt:
# python -m Upgrade.RunSscSeedAttributeSidecar [sourcename]

import logging
import os
import json
import sys
import time

from Core.Application import Application
from Sources.SSC.SyncExtractor import SyncExtractor

prog = os.path.splitext(os.path.basename(__file__))[0]
hlp = '''
Program syntax:
python -m Upgrade.RunSscSeedAttributeSidecar [sourcename]

[sourcename] should be the name of a valid SSC source.
NOTE: the SSC source will not be contacted as part of this upgrade.
'''

# Setup
if not len(sys.argv) == 2:
    print(hlp)
    exit(0)
srcname = sys.argv[1]
app = Application()
es = app.GetElasticClient()
sync = SyncExtractor(app.Settings, srcname)

# Main
count1 = 0
count2 = 0
attribs = []
scroller = es.SearchScroll("sscprojattr2", { "sort": [ { "projectVersionId": { "order": "asc" } } ] }, 200)
logging.info("Sidecar update started.")
if not scroller or not scroller.Results:
    logging.warning("No data found, nothing to do.")
    exit(0)
cpvid = scroller.Results[0]['_source']['projectVersionId']
while scroller.Results:
    for thing in scroller.Results:
        attrib = thing['_source']
        if attrib['projectVersionId'] == cpvid:
            attribs.append(attrib)
        else:
            count2 += 1
            sync.UpdateSidecarAttributes(attribs)
            cpvid = attrib['projectVersionId']
            attribs = [attrib]
        count1 += 1
    logging.info("Side car updates processed: %s project versions, %s attributes", count2, count1)
    scroller.GetNext()
if len(attribs):
    count2 += 1
    sync.UpdateSidecarAttributes(attribs)
logging.info("Side car updates processed (total): %s project versions (some PVs may have no attributes and not get counted), %s attributes", count2, count1)
sync.UpdateSidecarAttributes(None) # flush any remaining in queue
sync.Cleanup()
logging.info("Sidecar update complete.")
