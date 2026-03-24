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
