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

# 10/8/2021 TD
# Originally CheckForFODUpdates.py and SyncSsc.py - renamed and refactored into one program that processes enabled and supported sources.

import logging
import sys
import os
import traceback
import time

from Core.Application import Application
from Sources.FOD.SyncExtractor import SyncExtractor as SyncExtractorFod
from Sources.SSC.SyncExtractor import SyncExtractor as SyncExtractorSsc

sourceNames = []
prog = os.path.splitext(os.path.basename(__file__))[0]
prmSourceName = None
prmAction = 'all'
prmLogInstance = None
enableDropCheck = True
if len(sys.argv) > 1:
    prmSourceName = sys.argv[1]         # Source name
if len(sys.argv) > 2:
    prmAction = sys.argv[2]             # Action ('sync', 'loadqueue', 'checkdrop', 'all'), defaults to 'all'
if len(sys.argv) > 3:
    prmLogInstance = sys.argv[3]        # Custom logging instance

msg = "Usage:\n\npython3 RunSync.py src [action] [lognum]\n\n:src: Source name, i.e. SSC1\n:action: sync, loadqueue, or all, defaults to all\n:lognum: logging instance number, defaults to none"
if len(sys.argv) == 0:
    logging.warning(msg)
    exit(1)
if prmAction not in ['sync', 'loadqueue', 'all', 'checkdrop']:
    raise ValueError(f"Invalid action '{prmAction}', expected 'sync', 'loadqueue', 'checkdrop', or 'all'.")
app = Application(loggingInstance=prmLogInstance)
logging.info(f"[{prog}] Starting, processing {'all sources' if prmSourceName is None else 'source ' + prmSourceName}, using '{prmAction}' action.")

try:
    for s in app.Settings.GetSourceNames():
        if app.Settings.GetSource(s, "Enabled", False) and app.Settings.GetSource(s, "Source", "") in ["FOD", "SSC"] and (not prmSourceName or s == prmSourceName):
            sourceNames.append(s)
    if len(sourceNames) == 0:
        logging.warning(f"[{prog}] No enabled source of a supported type found in configuration.  {prog} Exiting.")
        exit(0)
except Exception as e:
    logging.critical(f"[{prog}] failure loading configuration due to exception {e}", exc_info = e)


for sourceName in sourceNames:
    source = app.Settings.GetSource(sourceName, "Source")
    logging.info(f"[{prog}] Processing source '{sourceName}'")
    try:
        if source == "SSC":
            e = SyncExtractorSsc(app.Settings, sourceName)
            if prmAction in ['loadqueue', 'all']:
                e.ReloadSyncQueue("completed")
                if prmAction == 'all':
                    logging.info("Delaying a few seconds to allow queue changes to become queryable...")
                    time.sleep(3)
            if prmAction in ['sync', 'all']:
                e.Process(cleanupAfter=False)
            if prmAction in ['checkdrop', 'all']:
                e.CheckSscDropProjects()
            e.Cleanup()
        if source == "FOD":
            e = SyncExtractorFod(app.Settings, sourceName)
            if prmAction in ['loadqueue', 'all']:
                e.ReloadSyncQueue("completed")
                if prmAction == 'all':
                    logging.info("Delaying a few seconds to allow queue changes to become queryable...")
                    time.sleep(3)
            if prmAction in ['sync', 'all']:
                e.Process()
            if prmAction in ['checkdrop', 'all']:
                e.CheckDrop()
    except KeyboardInterrupt:
        logging.info("Keyboard interrupt, cancelling")
    except Exception as e:
        error_msg = traceback.format_exc()
        logging.critical(error_msg)
        raise Exception(f"[{prog}] Exception: [{type(e).__name__}] {e}")
    logging.info(f"[{prog}] Completed processing source '{sourceName}'")
