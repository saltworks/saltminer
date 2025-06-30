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
from Utility.PipelineParamsUtility import PipelineParamsUtility
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
    StartTimer("RunRiskRoller")
    parameters = PipelineParamsUtility(app.Settings, "RiskRollerParams")
    parameters.PipelineParamsUtility()
    helper = RiskRoller(settings=app.Settings, trueTime=trueTime, delayTime= 10)
    helper.riskRollup('compliance')
    helper.riskRollup('risk')
    EndTimer("RunRiskRoller")
except Exception as e:
    logging.critical("[%s] Exception: [%s] %s", prog, type(e).__name__, e)
    raise

logging.info("[%s] Processing complete", prog)





