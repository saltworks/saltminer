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