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

from Core.Application import Application
from Utility.SCWImport import SCWCode

timers = {}

def StartTimer(key):
    timers[key] = time.perf_counter()

def EndTimer(key):
    if key in timers.keys() and timers[key]:
        elapsed = time.perf_counter() - timers[key]
        logging.info(f"{key}: {elapsed}")
        return elapsed
    else:
        raise ValueError(f"Invalid timer key '{key}'")
    
key= "process"
app = Application()


logging.info("ScwData update - %s", datetime.datetime.utcnow().isoformat())
SCW = SCWCode(app.Settings)

StartTimer(key)
SCW.methodPicker()
EndTimer(key)
logging.info("SCW processing complete.")