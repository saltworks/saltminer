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