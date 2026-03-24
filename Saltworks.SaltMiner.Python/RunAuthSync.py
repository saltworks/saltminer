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

from Core.Application import Application
from Sources.SSC.AuthHelper import AuthHelper as SscAuthHelper
from Sources.FOD.AuthHelper import AuthHelper as FodAuthHelper

app = Application(loggingInstance="authsync")
s = app.Settings
SSC_AUTH_CONFIG = "SscAuth"
FOD_AUTH_CONFIG = "FodAuth"

# Determine which auth integration is enabled and initialize helpers
helpers = []
if s.Get(SSC_AUTH_CONFIG, "Enabled", False):
    try:
        helpers.append(SscAuthHelper(s, s.Get(SSC_AUTH_CONFIG, "SourceName")))
    except:
        logging.error("SSC auth helper failed to initialize.", exc_info=True)
if s.Get(FOD_AUTH_CONFIG, "Enabled", False):
    try:
        helpers.append(FodAuthHelper(s, s.Get(FOD_AUTH_CONFIG, "SourceName")))
    except:
        logging.error("FOD auth helper failed to initialize.", exc_info=True)


if len(helpers) == 0:
    logging.warning("Auth helpers not configured or not enabled.")
    exit(0)

for helper in helpers:
    try:
        helper.Sync() # config determines mode
    except Exception as e:
        app.HandleException(e, "Error occurred during auth sync with SSC")
