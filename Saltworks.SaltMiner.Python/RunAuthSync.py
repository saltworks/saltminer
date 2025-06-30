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
