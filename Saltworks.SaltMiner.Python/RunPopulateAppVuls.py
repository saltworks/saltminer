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

# 10/8/2021 TD
# Originally PopulateAppVuls.py

import logging
import sys
import os

from Core.Application import Application
from Sources.SSC.AppVulsProcessor import AppVulsProcessor as AppVulsSSC
from Sources.FOD.AppVulsProcessor import AppVulsProcessor as AppVulsFOD
from Sources.SSC.RefreshSSC import RefreshSSC
from Sources.FOD.RefreshFOD import RefreshFOD

# setup logging instance if switch present
LOG_SWITCH = "-loginstance"
logInstance = None
for i in range(1, len(sys.argv)):
    if sys.argv[i].lower().startswith(LOG_SWITCH):
        logInstance = sys.argv[i].lower().replace(LOG_SWITCH, "")
        if not logInstance.startswith(":") or len(logInstance) == 1:
            print("[PRELOG] [ERROR] -loginstance switch must include a colon and then the alphanumeric value to use.")
            exit(1)
        logInstance = logInstance.replace(":", "")

# basic init
app = Application(loggingInstance=logInstance)
sources = []
prog = os.path.splitext(os.path.basename(__file__))[0]

# setup targeted source name if passed
prmSourceName = None
for i in range(1, len(sys.argv)):
    if not sys.argv[i].startswith("-"):
        prmSourceName = sys.argv[i]
        break

logging.info(f"{prog} starting, processing {'all sources' if prmSourceName is None else 'source ' + prmSourceName}")

# get source config(s)
try:
    for s in app.Settings.GetSourceNames():
        if app.Settings.GetSource(s, "Enabled", False) and app.Settings.GetSource(s, "Source", "") in ["FOD", "SSC"] and (not prmSourceName or s == prmSourceName):
            sources.append({ "source": app.Settings.GetSource(s, "Source", ""), "name": s })
    if len(sources) == 0:
        logging.warning(f"No enabled source(s) of supported types found in configuration.  {prog} Exiting.")
        exit(0)
except Exception as e:
    logging.critical(f"{prog} failed while loading configuration due to exception {e}", exc_info = e)
    exit(1)

# setup force refresh params if switch present
PVAM_SWITCH = "-forcerefresh"
forceRefresh = None
for i in range(1, len(sys.argv)):
    # backward compatibility
    sys.argv[i] = sys.argv[i].lower().replace("populateappvulsmanual", PVAM_SWITCH)
    if sys.argv[i].lower().startswith(PVAM_SWITCH):
        forceRefresh = sys.argv[i]

try:   
    #
    # Note: This is the file that is run as part of an automatic
    # This should not have any use input required.
    #
    # For things that require input please use PopulateAppVulsManual.py
    #
    
    for source in sources:
        sourceName = source['name']
        logging.info(f"[{prog}] Processing source '{sourceName}'")
        if source['source'] == "SSC":
            if forceRefresh:
                logging.info("Manual switch detected, forcing refresh for source '{sourceName}'")
                sscRefresh = RefreshSSC(app.Settings, sourceName)
                if forceRefresh.lower() != PVAM_SWITCH.lower():
                    # Only force refresh on selected PV ID
                    sscRefresh.ForceRefresh(int(forceRefresh.lower().replace(PVAM_SWITCH.lower() + ":", "")))
                else:
                    # Force refresh for all
                    sscRefresh.ForceRefresh()
            else:
                print(f'Not running reset.  Adding {PVAM_SWITCH} will force a full reset of the data set, and {PVAM_SWITCH}:99999 will force one PV to be reset.  Only compatible with SSC currently.')

            ssc = AppVulsSSC(app.Settings, sourceName)
            try:
                ssc.PopulateVuls()
            except:
                try:
                    ssc.Cleanup()
                except Exception as e:
                    # eat any cleanup error
                    logging.error("Failed to cleanup AppVulsSSC due to exception %s", e, exc_info=e)
                raise # raise the original exception
        if source['source'] == "FOD":
            if forceRefresh:
                logging.info("Manual switch detected, forcing refresh for source '{sourceName}'")
                fodRefresh = RefreshFOD(app.Settings, sourceName)
                # Force refresh for all
                fodRefresh.ForceRefresh()
            else:
                print(f'Not running reset.  Adding {PVAM_SWITCH} will force a full reset of the data set.')

            fod = AppVulsFOD(app.Settings, sourceName)
            fod.PopulateVuls()
        logging.info(f"[{prog}] Completed processing source '{sourceName}'")

except Exception as e:
    logging.critical(f"{prog} failed due to exception {e}", exc_info = e)
