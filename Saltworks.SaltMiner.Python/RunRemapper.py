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










