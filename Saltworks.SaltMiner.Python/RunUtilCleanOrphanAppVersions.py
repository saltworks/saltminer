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

import sys
import logging
import time

from Core.Application import Application
from Sources.SSC.SyncExtractor import SyncExtractor as SyncExtractorSsc
from Sources.FOD.SyncExtractor import SyncExtractor as SyncExtractorFod

prmForce = True if "-force" in sys.argv else False
prmLogInstance = None
for arg in sys.argv:
    if arg.lower().startswith("-loginstance:"):
        prmLogInstance = arg.split(":")[1]

timers = {}

app = Application(loggingCustomTag=prmLogInstance)
es = app.GetElasticClient()

def StartTimer(key):
    timers[key] = time.perf_counter()

def EndTimer(key):
    if key in timers.keys() and timers[key]:
        elapsed = time.perf_counter() - timers[key]
        logging.info(f"{key}: {int(elapsed)} sec")
        return elapsed
    else:
        raise ValueError(f"Invalid timer key '{key}'")

# Query issues using a bucket query to pull out all the app version ids
query = {
    "aggs": {
        "avid": {
            "terms": {
            "field": "application_version_id",
            "size": 100000
            }
        }
    },
    "size": 0
}
sourceNames = app.Settings.GetSourceNames()
if len(sys.argv) > 1:
    sourceNames = [sys.argv[1]]

logging.info("Starting orphan app version removal.")
StartTimer("process")

for sourceName in sourceNames:
    source = app.Settings.GetSource(sourceName, "Source")
    sourceIndex = "sscprojects" if source == "SSC" else "fodreleases"
    sourceIdField = "id" if source == "SSC" else "releaseId"

    logging.info("Orphan removal starting for source name '%s'", sourceName)
    if source == "SSC":
        ses = SyncExtractorSsc(app.Settings, sourceName)
        ses.RemoveSyncOrphans(prmForce)
    if source == "FOD":
        fes = SyncExtractorFod(app.Settings, sourceName)
        fes.CheckDrop(prmForce)

    logging.info("Orphan removal complete for source name '%s'", sourceName)

EndTimer("process")
logging.info("Orphan removal processing complete.")
