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
import time
import os
import sys

from Core.Application import Application
from Sources.SSC.SyncExtractor import SyncExtractor

timers = {}

app = Application()
prog = os.path.splitext(os.path.basename(__file__))[0]
prmSourceName = None
if len(sys.argv) > 1:
    prmSourceName = sys.argv[1]

if len(sys.argv) < 2:
    logging.info("Syntax: 'python[3] RunUtilSyncSscSidecar.py [sourcename]")
    logging.info("sourcename is the name of the source as found in the source config.  For example in Ssc1.json you would probably have '\"SourceName\": \"SSC1\",'")
    raise RuntimeError("Requires source name to be passed when called.")

logging.info("[%s] Starting SSC sidecar sync, processing 'source %s'", prog, prmSourceName)

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
ex = SyncExtractor(app.Settings, prmSourceName)
ex.SynchronizeSidecarAttributes()
EndTimer("main")
