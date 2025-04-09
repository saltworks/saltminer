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
# Replaces RefreshFODAppVuls.py and RefreshSSCAppVuls.py - takes an optional Source Name now

import logging
import sys
import os

from Sources.FOD.RefreshFOD import RefreshFOD
from Sources.SSC.RefreshSSC import RefreshSSC
from Core.Application import Application

prog = os.path.splitext(os.path.basename(__file__))[0]
app = Application()
prmSourceName = None
if len(sys.argv) > 1:
    prmSourceName = sys.argv[1]

logging.info(f"{prog} starting, processing {'all sources' if prmSourceName is None else 'source ' + prmSourceName}")

list = []
okSources = ["SSC", "FOD"]
for sourceName in app.Settings.GetSourceNames():
    if app.Settings.GetSource(sourceName, "Source", "") in okSources and app.Settings.GetSource(sourceName, "Enabled", False) and (sourceName == prmSourceName or prmSourceName is None):
        list.append(sourceName)

logging.info(f"Found {len(list)} source(s) to process.")

for sourceName in list:
    logging.info(f"Processing source {sourceName}")
    if app.Settings.GetSource(sourceName, "Source") == "FOD":
        fod = RefreshFOD(app.Settings, sourceName)
        fod.ForceRefresh()
    if app.Settings.GetSource(sourceName, "Source") == "SSC":
        ssc = RefreshSSC(app.Settings, sourceName)
        ssc.ForceRefresh()
    logging.info(f"Source {sourceName} complete")

logging.info(f"{prog} complete")











