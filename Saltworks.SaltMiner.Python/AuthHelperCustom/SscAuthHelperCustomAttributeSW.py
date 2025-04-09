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

# SAMPLE CUSTOM ATTRIBUTE CLASS

from Sources.SSC.AuthHelper import *
import re

class AuthHelperCustomAttribute(AuthHelperCustomAttribute):
    def __init__(self):
        pass

    def GetAttribute(self, appVersion):
        # attempt to find UID-xxxx at the beginning of the app version application name
        m = re.search("^UID-\d\d\d\d", appVersion['project']['name'])
        if m:
            return m.string
        else:
            return ""
