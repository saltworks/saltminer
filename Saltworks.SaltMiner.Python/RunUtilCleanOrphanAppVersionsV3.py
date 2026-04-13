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
from Utility.SmApiClient import SmApiClient

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
        logging.info(f"{key}: {elapsed}")
        return elapsed
    else:
        raise ValueError(f"Invalid timer key '{key}'")

def RunOne(sourceIndex, sourceIdField, sourceType, key, assetId):
    # replaced count with single doc search, as search behaves with timeouts better and size of 1 means faster
    data = es.Search(sourceIndex, { "query": { "term": { sourceIdField: { "value": str(key) } } } }, 1)
    if not data or len(data) == 0:
        logging.info("Asset with source ID %s not found in local data, attempting to remove in V3", key)
        if apiClient.DeleteAssetAll(assetId, str(key), sourceType):
            logging.info("Removed asset successfully")
        else:
            logging.warning("Error or asset not found while attempting to remove asset with source ID %s in V3", key)
        return 1
    # end if
    return 0

sourceNames = app.Settings.GetSourceNames()
if len(sys.argv) > 1:
    sourceNames = [sys.argv[1]]

logging.info("Starting orphan app version removal (v3).")
StartTimer("process")

# Query issues using a bucket query to pull out all the app version ids
query = {
    "aggs": {
        "avid": {
            "terms": {
            "field": "saltminer.asset.source_id",
            "size": 100000
            },
            "aggs": {
                "id": {
                    "terms": {
                        "field": "saltminer.asset.id",
                        "size": 1
                    }
                }
            }
        }
    },
    "size": 0
}

for sourceName in sourceNames:
    source = app.Settings.GetSource(sourceName, "Source")
    sourceType = "Saltworks." + source
    apiClient = SmApiClient(app.Settings, sourceName)
    sourceIndex = "sscprojects" if sourceType == "Saltworks.SSC" else "fodreleases"
    sourceIdField = "id" if sourceType == "Saltworks.SSC" else "releaseId"
    iIdx = "issues_app_" + sourceType.lower() + sourceName.lower()
    rIdx = "assets_app_" + sourceType.lower() + sourceName.lower()

    iIdx = f"issues_app_{sourceType.lower()}_{sourceName.lower()}"
    rIdx = f"assets_app_{sourceType.lower()}_{sourceName.lower()}"

    logging.info("Orphan removal (v3) starting for source name '%s'", sourceName)
    ri = es.Search(iIdx, query, navToData=False)
    query['aggs']['avid']['aggs']['id']['terms']['field'] = 'id'
    ra = es.Search(rIdx, query, navToData=False)
    buckets = ri['aggregations']['avid']['buckets']
        
    dc = 0
    try:
        c = 0
        for b in buckets:
            dc += RunOne(sourceIndex, sourceIdField, sourceType, b['key'], b['id']['buckets'][0]['key'])
            c += 1
            if c % 100 == 0:
                logging.info("Processed %s of %s app versions found in issues (pass 1)", c, len(buckets))
        # end for
        logging.info("Processed %s of %s app versions found in issues (pass 1)", c, len(buckets))
        buckets = ra['aggregations']['avid']['buckets']
        c = 0
        for b in buckets:
            dc += RunOne(sourceIndex, sourceIdField, sourceType, b['key'], b['id']['buckets'][0]['key'])
            c += 1
            if c % 100 == 0:
                logging.info("Processed %s of %s app versions found in assets (pass 2)", c, len(buckets))
        # end for
        logging.info("Processed %s of %s app versions found in assets (pass 2)", c, len(buckets))
    except KeyboardInterrupt:
        logging.info("Processed %s app versions for source %s", c, source)
        exit(0)
    logging.info("Orphan removal (v3) complete for source '%s'.  Removed %s assets", sourceName, dc)

EndTimer("process")
logging.info("Orphan removal processing (v3) complete.")
