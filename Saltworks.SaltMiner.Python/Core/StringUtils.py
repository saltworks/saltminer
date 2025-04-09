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

import re

class StringUtils(object):
    """
    Static string utility methods
    """
    def __init__(self):
        pass

    @staticmethod
    def SnakeCase(text):
        """
        Converts CamelCase to snake_case

        """
        str1 = re.sub('(.)([A-Z][a-z]+)', r'\1_\2', text)
        return re.sub('([a-z0-9])([A-Z])', r'\1_\2', str1).lower()

    @staticmethod
    def CamelCase(text):
        """
        Converts snake_case to CamelCase

        """
        if text.find("_") == -1:
            return text
        return ''.join(x.capitalize() or '_' for x in text.split('_'))

