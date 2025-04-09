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
import os
import sys

from Core.Application import Application
from Core.SscClient import SscClient
from Sources.SSC.AppVulsProcessor import AppVulsProcessor as AppVulsSsc
from Sources.SSC.SyncExtractor import SyncExtractor

timers = {}

app = Application()
prog = os.path.splitext(os.path.basename(__file__))[0]
prmSourceName = None
v2Index = "sscprojissues"
v3Index = "issues_app_saltworks.ssc"

if len(sys.argv) > 1:
    prmSourceName = sys.argv[1]

if len(sys.argv) < 2:
    logging.info("Syntax: 'python[3] RunUtilSscExtractAllScans.py [sourcename]")
    logging.info("sourcename is the name of the source as found in the source config.  For example in Ssc1.json you would probably have '\"SourceName\": \"SSC1\",'")
    raise RuntimeError("Requires source name to be passed when called.")

logging.info("This utility pulls all scans for all project versions - typical use case is when we are missing purged scans.")
logging.info("[%s] Starting SSC scan reload, processing 'source %s'", prog, prmSourceName)
es = app.GetElasticClient()
sync = SyncExtractor(app.Settings, prmSourceName)
ssc = sync.__SscUtils.SscClient

projectversions = ssc.GetProjectVersions('id', 'project', 'name')
for pv in projectversions:
    projid = pv['id']
    projname = pv['project']['name']
    projversion = pv['name']
    projectScans = ssc.GetProjectVersionScans(projid)

    scnCount = 0
    pvAssessmentTypes = {}
                
    logging.info("Adding scans for PV ID %s to sscprojscans", projid)
    for artifact in projectScans:

        # artifacts can have multiple scans. map and return as list
        scans = sync.ArtifactToScans(artifact, projid, holdprojectname, holdname)

        for scan in scans:
            # set a dict entry to indicate presence of this scan type (and its configured assessment type)
            atype = self.__GetAssessmentType(scan['type'])
            if not scan['type'] in pvAssessmentTypes.keys():
                pvAssessmentTypes[scan['type']] = atype

            # write the scan to Elastic
            self.__ElasticClient.Index('sscprojscans', json.dumps(scan))
            scnCount = scnCount + 1


def StartTimer(key):
    timers[key] = time.perf_counter()

def EndTimer(key, prt=True):
    if key in timers.keys() and timers[key]:
        elapsed = time.perf_counter() - timers[key]
        if prt:
            print(f"{key}: {elapsed}")
        return elapsed
    else:
        raise ValueError(f"Invalid timer key '{key}'")

StartTimer("main")

try:

except requests.exceptions.ConnectionError as e:
    logging.error("Error connecting to elasticsearch server: connection failed or timed out")

except Exception as e:
    logging.error(f"Error: [{type(e).__name__}] {e}")

EndTimer("main")
