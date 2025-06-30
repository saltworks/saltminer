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











