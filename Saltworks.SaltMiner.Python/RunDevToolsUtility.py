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

# This is the runner for the devToolsScript Reader. 
# In order to use this runner you just need to put the path to the config file and the path to the .txt file with your Dev Tool commands
# into the Dev Script reader function on line 28. If no config changes are needed then use the path provided.

import datetime
import time
import logging
import sys
import os

from Utility.DevToolsUtility import DevToolsUtility
from Core.Application import Application

timers = {}
prmFilePath = None
hlpMsg = '''
Syntax:

python3 RunDevToolsUtility.py [Dev tools script file path]
'''
app = Application()

if not len(sys.argv) >= 2:
    logging.error("No file specified.\n %s", hlpMsg)
    exit(1)
prmFilePath = sys.argv[1]

if len(sys.argv) == 3:
    logFilePath = sys.argv[2]

else:
    logFilePath = None

if not prmFilePath or not os.path.exists(prmFilePath) or not os.path.isfile(prmFilePath):
    logging.error("File path '%s' could not be found.", prmFilePath)
    exit(1)
if logFilePath and (not os.path.exists(logFilePath) or not os.path.isfile(logFilePath)):
    logging.error("File path '%s' could not be found.", logFilePath)
    

def StartTimer(key):
    timers[key] = time.perf_counter()

def EndTimer(key):
    if key in timers.keys() and timers[key]:
        elapsed = time.perf_counter() - timers[key]
        print(f"{key}: {elapsed}")
        return elapsed
    else:
        raise ValueError(f"Invalid timer key '{key}'")
    
prcKey = "process"

logging.info("Dev Tools Utility starting -%s", datetime.datetime.utcnow().isoformat())
StartTimer(prcKey)
#add path to config file and to dev tool script.txt file 
DevToolsUtility(app.Settings).ExecuteDevScriptFile(prmFilePath, logFilepath=logFilePath)
EndTimer(prcKey)