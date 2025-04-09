''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-04-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

# This is a custom template runner program.  It can be used as a starting point for a new custom program.
# Pattern: 
# 1. Use runner to "setup" helper class for a custom utility.
# 2. Call utility Run() or main method.  
# 3. Runner should include outer try...except block.
# 4. Runner should include start and end timer, and should log start and complete messages.

# Example call from command prompt:
# python -m Custom.RunCustomTemplate

import logging
import os
import time

from Core.Application import Application
from Custom.CustomTemplateHelper import CustomTemplateHelper

timers = {}

app = Application()
prog = os.path.splitext(os.path.basename(__file__))[0]


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
    StartTimer("RunCustomTemplate")
    helper = CustomTemplateHelper(app.Settings)
    helper.Run()
    EndTimer("RunCustomTemplate")
except Exception as e:
    logging.critical("[%s] Exception: [%s] %s", prog, type(e).__name__, e)
    raise

logging.info("[%s] Processing complete", prog)