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

import datetime
from dateutil.parser import parse as dtparse

class GeneralUtility(object):
    def __init__(self):
        pass

    # Might need to convert dates into UTC as well.  Decided not to for now.
    @staticmethod
    def GetFormattedDateString(dt):
        if not dt: return None
        if isinstance(dt, datetime.date):
            return dt.isoformat()
        try:
            return GeneralUtility.ParseDate(str(dt)).isoformat()
        except Exception as e:
            raise ValueError(f"Date string '{dt}' is incorrect") from e

    @staticmethod
    def ParseDate(dtString, preserveTime=False):
        if not dtString: return None
        if isinstance(dtString, datetime.date):
            return dtString
        ds = str(dtString)
        i = ds.find(".")
        if i > -1 and preserveTime == False:
            ds = ds[0:i]
        try:
            return dtparse(ds)
        except Exception as e:
            raise ValueError(f"Date string '{ds}' is incorrect") from e