''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-06-30
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

import logging
import datetime
import time
import os
import sys

import requests

from Core.Application import Application
from Sources.SSC.AppVulsProcessor import AppVulsProcessor as AppVulsSsc

timers = {}

if len(sys.argv) < 2 or len(sys.argv) > 4:
    logging.error("Invalid parameters.")
    logging.info("Syntax: 'python[3] RunUtilSyncSscQuickRefresh.py [sourcename] [includerefresh=True] [-loginstance:xyz]")
    logging.info("sourcename is the name of the source as found in the source config.  For example in Ssc1.json you would probably have '\"SourceName\": \"SSC1\",'")
    logging.info("includerefresh determines if refresh processing will be included in this run after the queue is loaded.  Defaults to True.")
    exit(1)

LOG_SWITCH = "-loginstance"
logInstance = None
for i in range(1, len(sys.argv)):
    if sys.argv[i].lower().startswith(LOG_SWITCH):
        logInstance = sys.argv[i].lower().replace(LOG_SWITCH, "")
        if not logInstance.startswith(":") or len(logInstance) == 1:
            print(f"[PRELOG] [ERROR] {LOG_SWITCH} switch must include a colon and then the alphanumeric value to use.")
            exit(1)
        logInstance = logInstance.replace(":", "")

app = Application(loggingInstance=logInstance)
prog = os.path.splitext(os.path.basename(__file__))[0]
prmSourceName = None
prmRunRefresh = True
v2Index = "sscprojissues"
v3Index = "issues_app_saltworks.ssc_ssc1"

if len(sys.argv) > 1:
    prmSourceName = sys.argv[1]
if len(sys.argv) > 2 and not sys.argv[2].lower().startswith(LOG_SWITCH):
    prmRunRefresh = True if sys.argv[2].lower().startswith("t") else False

logging.info("This utility does a very fast comparison by issue count between sscprojissues and v3 issues, adding project versions to the update queue and then optionally processing the updates.")
logging.info("[%s] Starting SSC quick refresh, processing 'source %s'", prog, prmSourceName)
es = app.GetElasticClient()
ssc = AppVulsSsc(app.Settings, prmSourceName)

def GetV3Count(avid):
    lookupQry = { "query": { "term": { "saltminer.asset.source_id" : { "value": avid } } } }
    c = es.Count(v3Index, lookupQry)
    if c and c > 0:
        return c
    else:
        return 0

def WriteUpdateQueueDoc(avid):
    es.Index("sscupdatequeue", {
        'processedDateTime' : datetime.datetime.now().strftime("%Y-%m-%dT%H:%M:%S"),
        'projectVersionId' : avid,
        'updateType' : 'U',
        'updateReason' : 'Quick refresh',
        'completedDateTime' : '1900-01-01T00:00:00.000-0000'
    })


def StartTimer(key):
    timers[key] = time.perf_counter()

def EndTimer(key, prt=True):
    if key in timers.keys() and timers[key]:
        elapsed = time.perf_counter() - timers[key]
        if prt:
            print(f"{key}: {elapsed}")
        return elapsed
    else:
        raise ValueError(f"Invalid timer key '{key}'")

StartTimer("main")

try:
    v2Qry = {
        "aggs": {
            "avid": {
                "terms": {
                    "field": "projectVersionId",
                    "size": 100000,
                    "order": {
                        "_key": "asc"
                    }
                }
            }
        },
        "size": 0
    }

    logging.info("Comparison of v2 (%s) to v3 (%s) app version counts starting", v2Index, v3Index)
    dto = es.Search(v2Index, v2Qry, 0, False)
    if not dto or not "aggregations" in dto.keys() or not "avid" in dto['aggregations'].keys():
        logging.warning("Invalid or empty aggregation query response, unable to continue.")
        exit(0)

    counts = dto['aggregations']['avid']['buckets']
    processed = 0
    for count in counts:
        v3 = GetV3Count(count['key'])
        v2 = count['doc_count']
        processed += 1
        if processed % 200 == 0:
            logging.info("%s of %s, %s%% complete", processed, len(counts), int(processed / len(counts) * 100))
        if v3 == 0:
            logging.info("App version ID %s missing in v3 index (%s), v2 count %s", count['key'], v3Index, v2)
        if v3 != 0 and v2 != v3:
            logging.warning("App version ID %s, v2: %s, v3: %s, delta: %s", count['key'], v2, v3, abs(v3 - v2))
        if v3 == 0 or v2 != v3:
            WriteUpdateQueueDoc(count['key'])
    if prmRunRefresh:
        logging.info("Comparison complete. Updates needed: %s  Begin refresh...", processed)
        es.FlushIndex('sscupdatequeue')
        ssc.PopulateVuls()
    else:
        logging.info("Comparison complete.  Refresh disabled by parameter, so process is complete.")

except requests.exceptions.ConnectionError as e:
    logging.error("Error connecting to elasticsearch server: connection failed or timed out")

except Exception as e:
    logging.error(f"Error: [{type(e).__name__}] {e}")

EndTimer("main")
