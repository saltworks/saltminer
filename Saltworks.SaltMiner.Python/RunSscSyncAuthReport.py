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