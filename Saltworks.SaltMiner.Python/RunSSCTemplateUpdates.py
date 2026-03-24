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

from Core.SscClient import SscClient
from Core.Application import Application

app = Application()
SSC =SscClient(app.Settings, sourceName='SSC1')
prid = None
Template = "Prioritized-HighRisk-Project-Template"
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
logging.info("Project version template update begin %s", datetime.datetime.utcnow().isoformat())
StartTimer(process)

PV = SSC.GetProjectVersions()

for projectVersion in PV:
    if prid != None:
        if projectVersion['project']['id'] == prid:
                    projectVersion['issueTemplateId'] = Template
                    
                    SSC.PutProjectVersionTemplate(projectVersionId=projectVersion['id'], data=projectVersion)
                    logging.info("Updated template for project version %s", projectVersion['name'])
    
    else:
                
        projectVersion['issueTemplateId'] = Template
        SSC.PutProjectVersionTemplate(projectVersionId=projectVersion['id'], data=projectVersion)
        logging.info("Updated template for project version %s", projectVersion['name'])




EndTimer(process)
logging.info("Project version template update complete.")
