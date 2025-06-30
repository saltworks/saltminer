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

import time 
import datetime
import logging
from Utility.IndexSwap import IndexSwap

timers = {}
targetIndex = "target_index"
def StartTimer(process):
    timers[process] = time.perf_counter()

def EndTimer(process):
    if process in timers.keys() and timers[process]:
        elapsed = time.perf_counter() - timers[process]
        logging.info(f"{process}: {elapsed}")
        return elapsed
    else:
        raise ValueError(f"Invalid timer key '{process}'")
    
process= "process"
logging.info("Index Swap begin %s", datetime.datetime.utcnow().isoformat())
IS = IndexSwap()
StartTimer(process)
IS.runIndexSwap(targetIndex, withMapping=False)
EndTimer(process)
logging.info("Index Swap complete.")