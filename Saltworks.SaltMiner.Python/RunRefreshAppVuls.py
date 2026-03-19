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











