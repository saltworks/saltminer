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

import os
import logging
import datetime
import unittest

from Utility.GeneralUtility import GeneralUtility
from Core.DictUtils import DictUtils

module = os.path.splitext(os.path.basename(__file__))[0]


class GeneralTests(unittest.TestCase):
    """Unit tests for general functionality."""

    def test_date_time(self):
        """Test date time formatting functionality."""
        # Arrange
        dt1 = "2023-04-23T03:48:29.673237"
        dt2 = "nsdlk"
        dt3 = datetime.datetime.utcnow()
        dt4 = ""

        # Act/Assert
        c1 = GeneralUtility.GetFormattedDateString(dt1)
        c2 = False
        try:
            GeneralUtility.GetFormattedDateString(dt2)
        except ValueError:
            c2 = True
        c3 = GeneralUtility.GetFormattedDateString(dt3)
        c4 = GeneralUtility.GetFormattedDateString(dt4)

        self.assertEqual(c1[0:18], dt1[0:18], f"Expected dt1 to be '{dt1[0:18]}', but instead found '{c1[0:18]}'")
        self.assertTrue(c2, "Should have thrown a value error")
        self.assertGreater(len(c3), 17, "Should have returned a value")
        self.assertIsNone(c4, "Empty value should result in None")
        logging.info(f"[TEST SUCCESS] {module}:test_date_time")


    def test_dict_utils_get_value(self):
        """Test DictUtils.GetValue functionality."""
        # Arrange
        test_dict = {
            "level1": {
                "level2": {
                    "level3": "value"
                },
                "level2b": "value2"
            }
        }

        # Act/Assert
        v1 = DictUtils.GetValue(test_dict, "level1.level2.level3")
        v2 = DictUtils.GetValue(test_dict, "level1.level2b")
        v3 = DictUtils.GetValue(test_dict, "level1.levelX", default="default_value")
        v4 = DictUtils.GetValue(test_dict, "level1.level2.levelX", default=None)

        self.assertEqual(v1, "value", f"Expected 'value', but got '{v1}'")
        self.assertEqual(v2, "value2", f"Expected 'value2', but got '{v2}'")
        self.assertEqual(v3, "default_value", f"Expected 'default_value', but got '{v3}'")
        self.assertIsNone(v4, f"Expected None, but got '{v4}'")
        logging.info(f"[TEST SUCCESS] {module}:test_dict_utils_get_value")


if __name__ == '__main__':
    unittest.main()