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

from Core.Application import Application
from Utility import SmApiClient

timers = {}

app = Application()
prog = os.path.splitext(os.path.basename(__file__))[0]
prmSourceName = None
resetIndices = ["issues_app_saltworks.ssc_ssc1", "assets_app_saltworks.ssc_ssc1", "scans_app_saltworks.ssc_ssc1"]

if len(sys.argv) > 1:
    prmSourceName = sys.argv[1]

if len(sys.argv) < 2:
    logging.info("Syntax: 'python[3] RunUtilSscHardRefresh.py [sourcename]")
    logging.info("sourcename is the name of the source as found in the source config.  For example in Ssc1.json you would probably have '\"SourceName\": \"SSC1\",'")
    logging.error("Requires source name to be passed when called, but makes minimal/no calls to the source for this operation.")
    exit(1)

logging.info("This utility removes v3 SSC indices and then runs a 'forcerefresh' RunPopulateAppVuls to reload all v3 indices from local elasticsearch SSC data.")
logging.info("[%s] Starting SSC hard refresh, processing 'source %s'", prog, prmSourceName)
es = app.GetElasticClient()

def StartTimer(key):
    timers[key] = time.perf_counter()

def EndTimer(key, prt=True):
    if key in timers.keys() and timers[key]:
        elapsed = time.perf_counter() - timers[key]
        logging.info("[Timer] %s: %s", key, elapsed)
        return elapsed
    else:
        logging.error("[Timer] Invalid timer key '%s'", key)

StartTimer("HardRefresh")

try:
    for idx in resetIndices:
        es.DeleteAllByQuery(idx)

except Exception as e:
    logging.exception("Index reset failed.  See log for details.")
    exit(1)

logging.info("Reset complete. Beginning full refresh.")

try:
    sys.argv.append("-forcerefresh")
    import RunPopulateAppVuls
except:
    logging.exception("Critical failure when running SSC hard refresh")

EndTimer("HardRefresh")
