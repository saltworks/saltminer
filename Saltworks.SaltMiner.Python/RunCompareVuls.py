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
# Replaces CompareSSCVuls.py and CompareWSVuls.py
# Refactored to now require source type and name as parameters
# Needs testing

import logging
import sys
import os

from Sources.SSC.VulComparer import VulComparer as VulComparerSsc
from Core.Application import Application

if len(sys.argv) < 3:
    raise RuntimeError("Requires source type and source name to be passed when called.")
else:
    sourceType = sys.argv[1]
    sourceName = sys.argv[2]
prog = os.path.splitext(os.path.basename(__file__))[0]
app = Application()

logging.info(f"{prog} starting for source '{sourceName}' and type '{sourceType}'")

if sourceType not in ["SSC"]:
    raise RuntimeError(f"Source type must be SSC (not '{sourceType}'")

if sourceType == 'SSC':
    SSC = VulComparerSsc(app.Settings)
    SSC.CompareAppVuls()

logging.info(f"{prog} finished")











