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

from Utility.Remapper import Remapper
from Core.Application import Application


timers = {}

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
app = Application()

logging.info("Remap process begin %s", datetime.datetime.utcnow().isoformat())

RM = Remapper(appSettings= app.Settings)

StartTimer(process)
#add the path to your new mapping file and the index that you would like to remap 
RM.Remap(jsonFile="./Utility/asset.json", targetIndex="issues_app_saltworks.ssc")

EndTimer(process)
logging.info("Remap processing complete.")










