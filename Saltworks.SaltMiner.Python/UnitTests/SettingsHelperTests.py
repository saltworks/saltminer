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