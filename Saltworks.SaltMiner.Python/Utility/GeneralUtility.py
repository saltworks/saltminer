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