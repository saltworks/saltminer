''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

class DictUtils(object):
    """
    Static dict utility methods
    """
    def __init__(self):
        pass

    @staticmethod
    def GetValue(obj:dict, path:str, default:any=None):
        """
        Returns the value from a given dict & path or the default if not found

        """
        keys = path.split(".")
        cur = obj
        for key in keys:
            if not isinstance(cur, dict) or key not in cur:
                return default
            cur = cur[key]
        return cur
