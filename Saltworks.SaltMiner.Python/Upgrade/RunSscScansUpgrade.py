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

# This upgrade utility program converts an existing sscprojscans index into the latest "flat" data structure.
# The utility can be safely run on either version of the index structure, or even a mix of the two.

# Example call from command prompt:
# python -m Upgrade.RunSscScansUpgrade [sourcename]

import logging
import os
import json
import sys
import time

from Core.Application import Application
from Sources.SSC.SyncExtractor import SyncExtractor

def CleanupAndExit(isError=False):
    if sync:
        sync.Cleanup()
    exit(1 if isError else 0)

sync = None
prog = os.path.splitext(os.path.basename(__file__))[0]
debug = True
hlp = '''
Program syntax:
python -m Upgrade.RunSscScansUpgrade sourcename [-silent]

sourcename should be the name of a valid SSC source.  Required parameter.
-silent will suppress the interrupt help (and input requirement) for automation purposes.  Defaults to False if not present.
NOTE: the SSC source will not be contacted as part of this upgrade.
'''
if not len(sys.argv) >= 2:
    print(hlp)
    CleanupAndExit()
srcname = sys.argv[1]
silent = False
if len(sys.argv) == 3 and sys.argv[2].lower() == "-silent":
    silent = True

app = Application()
es = app.GetElasticClient()
sync = SyncExtractor(app.Settings, srcname)
sscProjectVersions = {}

if es.IndexExists("sscprojscans.bak"):
    logging.error("Please remove the sscprojscans.bak index to continue.")
    CleanupAndExit(True)
    
def GetPvNames(pvid):
    try:
        if pvid in sscProjectVersions.keys():
            return sscProjectVersions[pvid]['prjName'], sscProjectVersions[pvid]['verName']
        else:
            logging.warning('Project version %s could not be found in loaded buffer and will be skipped', pvid)
    except Exception as e:
        logging.warning('Error locating project version %s in loaded buffer (skipping): [%s] %s', pvid, type(e).__name__, e)
    return None, None

msg = '''

**************************************************************************************
You can interrupt the processing at any time by pressing CTRL-C. 
This may be a good idea if the converted amount isn't increasing.

Press ENTER to continue, CTRL-C to abort.
**************************************************************************************
'''
try:
    if not silent:
        input(msg)
except KeyboardInterrupt:
    logging.info("Aborting")
    CleanupAndExit()


#*************************************************************************************
#                       MAIN PROGRAM
#*************************************************************************************

# Setup
try:
    mapping = es.GetMapping("sscprojscans")
    es.MapIndexWithMapping("ssctmp", mapping, True)
except Exception as e:
    logging.critical("[%s - Setup] Exception: [%s] %s", prog, type(e).__name__, e)
    if debug:
        raise
    CleanupAndExit(True)

# Load ssc project versions into memory
try:
    count = 0
    scroller = es.SearchScroll("sscprojects", None, 200)
    total = scroller.TotalHits
    while scroller.Results:
        for thing in scroller.Results:
            pv = thing['_source']
            sscProjectVersions[pv['id']] = { "prjName": pv['project']['name'], "verName": pv['name'] }
            count += 1
        logging.info("Loaded %s/%s SSC project versions from local sscprojects", count, total)
        scroller.GetNext()
    logging.info("Project versions loaded.  Beginning scan processing")
except Exception as e:
    logging.critical("[%s - PV load] Exception: [%s] %s", prog, type(e).__name__, e)
    if debug:
        raise
    CleanupAndExit(True)

# Scan processing
try:
    scroller = es.SearchScroll("sscprojscans", None, 200)
    count = 0
    converted = 0
    if not scroller.Results:
        logging.info("Nothing to do - no data.")
        CleanupAndExit()
    total = scroller.TotalHits
    while scroller.Results:
        for thing in scroller.Results:
            if 'scanrec' in thing['_source'].keys():
                artifact = thing['_source']['scanrec']
                pvid = thing['_source']['projectVersionId']
                prjName, verName = GetPvNames(pvid)
                if not prjName or not verName:
                    continue

                # artifacts can have multiple scans. map and return as list
                scans = sync.ArtifactToScans(artifact, pvid, prjName, verName)
                converted += len(scans)
            else:
                scans = [thing['_source']]

            for scan in scans:
                # write the scan to Elastic
                es.Index('ssctmp', json.dumps(scan))
                count += 1
                if count % 200 == 0:
                    logging.info("Processed %s/%s scans, converted %s", count, total, converted)
            # end for
        # end for
        scroller.GetNext()
    # end while
    logging.info("Processed %s scans total", count)
except KeyboardInterrupt:
    logging.info("Canceling due to keyboard interrupt (ctrl-c), remove ssctmp index before restarting.")
    CleanupAndExit()
except Exception as e:
    logging.critical("[%s - Processing] Exception: [%s] %s", prog, type(e).__name__, e)
    if debug:
        raise
    CleanupAndExit(True)


# Finish up
try:
    logging.info("Scan processing complete.  Reindexing...")
    es.Reindex("sscprojscans", "sscprojscans.bak")
    es.DeleteIndex("sscprojscans")
    time.sleep(5) # need this to make sure the delete goes through before attempting the reindex
    mapping = es.GetMapping("sscprojscans")
    es.MapIndexWithMapping("sscprojscans", mapping, True)
    es.Reindex("ssctmp", "sscprojscans")
    es.DeleteIndex("ssctmp")
    sync.Cleanup()
    logging.info("Reindexing complete.  Original data backed up to sscprojscans.bak index, which can be safely removed.")
except Exception as e:
    logging.critical("[%s - Final reindexing] Exception: [%s] %s", prog, type(e).__name__, e)
    if debug:
        raise
    CleanupAndExit(True)
