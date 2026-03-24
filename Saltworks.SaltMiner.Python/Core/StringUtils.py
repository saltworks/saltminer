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

