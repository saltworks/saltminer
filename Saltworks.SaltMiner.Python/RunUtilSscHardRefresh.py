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
