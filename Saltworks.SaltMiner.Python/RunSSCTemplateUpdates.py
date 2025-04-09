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
