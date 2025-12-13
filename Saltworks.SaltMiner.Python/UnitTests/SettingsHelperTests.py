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
import time
import unittest

from Core.Application import Application
from Utility.SettingsHelper import SettingsHelper

module = os.path.splitext(os.path.basename(__file__))[0]


class SettingsHelperTests(unittest.TestCase):
    """Unit tests for SettingsHelper functionality."""

    def setUp(self):
        """[UnitTest] Set up test fixtures before each test method."""
        self.sh = SettingsHelper(Application())
        self.key = "testsetting"
        self.val = "testval"
        self.val2 = "testval2"

    def tearDown(self):
        """[UnitTest] Clean up after each test method."""
        # cleanup
        try:
            self.sh.DeleteByKey(self.key)
        except:
            pass

    def test_main(self):
        """Test settings get, set, and update functionality."""
        # Act/Assert
        setting = self.sh.Get(self.key)
        self.assertIsNone(setting, "Setting should be none when doesn't exist.")
        self.sh.SetValue(self.key, self.val)
        time.sleep(1)
        setting = self.sh.Get(self.key)
        self.assertIsNotNone(setting, "Setting should be present after setting it.")
        setting.Value = self.val2
        self.sh.Set(setting)
        time.sleep(1)
        setting = self.sh.Get(self.key)
        self.assertEqual(setting.Value, self.val2, f"Setting value '{setting.Value}', but expected '{self.val2}'")
        print(f"[TEST SUCCESS] {module}:test_main")


if __name__ == '__main__':
    unittest.main()