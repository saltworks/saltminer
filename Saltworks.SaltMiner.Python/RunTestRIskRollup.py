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

# 4/7/23 - DWH, CMC
# AKA the Risk Roller https://www.youtube.com/watch?v=dQw4w9WgXcQ


import logging
import os
import time
import datetime 
import json
from datetime import timedelta
from Core.Application import Application
from Utility.RiskRoller import RiskRoller
from Core.ElasticClient import ElasticClient

timers = {}

app = Application()
prog = os.path.splitext(os.path.basename(__file__))[0]

now = datetime.datetime.now() - timedelta(hours=1)
stringTime = datetime.datetime.strftime(now, "%Y-%m-%dT%H:%M:%S.%fZ")
trueTime = datetime.datetime.strptime(stringTime, "%Y-%m-%dT%H:%M:%S.%fZ")

def StartTimer(key):
    timers[key] = time.perf_counter()


def EndTimer(key, prt=True):
    if key in timers.keys() and timers[key]:
        elapsed = time.perf_counter() - timers[key]
        if prt:
            logging.info(f"%s completed in %s sec", key, round(elapsed, 3))
        return elapsed
    else:
        raise ValueError(f"[%s] Invalid timer key '{key}'", prog)

logging.info("[%s] Starting", prog)

try:
    StartTimer("RunTestRiskRoller")
    rr = RiskRoller(settings=app.Settings, trueTime=trueTime, delayTime=5)
    rr.testFunctions('compliance')
    rr.testFunctions('risk')
    EndTimer("RunTestRiskRoller")
except Exception as e:
    logging.critical("[%s] Exception: [%s] %s", prog, type(e).__name__, e)
    raise

logging.info("[%s] Processing complete", prog)





