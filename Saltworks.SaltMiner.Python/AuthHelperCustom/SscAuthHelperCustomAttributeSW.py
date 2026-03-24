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
