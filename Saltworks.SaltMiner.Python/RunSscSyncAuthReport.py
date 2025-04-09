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
import os

from Sources.SSC.SscAuthHelper import SscAuthHelper
from Core.Application import Application

prog = os.path.splitext(os.path.basename(__file__))[0]
logging.info(f"{prog} starting")
a = Application()

# SSC user project version assignment CSV report
sah = SscAuthHelper(a.Settings, a.Settings.Get("SscAuth", "SscSourceName"))
sah.UserProjectAssignmentCsv()

logging.info(f"{prog} finished")