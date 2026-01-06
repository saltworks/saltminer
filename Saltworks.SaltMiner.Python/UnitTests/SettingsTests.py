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

import shutil
import os
import json
import unittest

from Core.ApplicationSettings import ApplicationSettings

module = os.path.splitext(os.path.basename(__file__))[0]
config_test_dir = "ConfigTestTmp"


class SettingsTests(unittest.TestCase):
    """Unit tests for ApplicationSettings functionality."""

    @classmethod
    def setUpClass(cls):
        """[UnitTest] Set up test fixtures before running tests in this class."""
        cls._setup_config_test()

    @classmethod
    def tearDownClass(cls):
        """[UnitTest] Clean up after all tests in this class."""
        cls._cleanup_config_test()

    @staticmethod
    def _setup_config_test():
        """Set up test configuration directory."""
        spath = os.path.join(config_test_dir, "Sources")
        if not os.path.isdir(config_test_dir):
            os.mkdir(config_test_dir)
        if not os.path.isdir(spath):
            os.mkdir(spath)

    @staticmethod
    def _cleanup_config_test():
        """Clean up test configuration directory."""
        spath = os.path.join(".", config_test_dir)
        if os.path.isdir(spath):
            shutil.rmtree(spath)

    @staticmethod
    def _config_file_list(filenames, is_source=False):
        """Create list of config file paths."""
        file_list = []
        for f in filenames:
            if is_source:
                file_list.append(os.path.join(config_test_dir, "Sources", f))
            else:
                file_list.append(os.path.join(config_test_dir, f))
        return file_list

    @staticmethod
    def _save_config(config, filename, is_source=False):
        """Save config to file."""
        if is_source:
            path = os.path.join(config_test_dir, "Sources", filename)
        else:
            path = os.path.join(config_test_dir, filename)
        with open(path, "w") as f:
            f.write(json.dumps(config))

    def test_main(self):
        """Test ApplicationSettings get functionality."""
        # Arrange
        c1 = {"Setting1": "Value1", "Setting2": "Value2", "Password": "1234"}
        cf1 = "Config1.json"
        self._save_config(c1, cf1)
        c2 = {"Setting1": "Value1.2", "Setting3": "Value3"}
        cf2 = "Config2.json"
        self._save_config(c2, cf2)
        c3 = {"SourceName": "SSC1", "Setting4": "Value4"}
        cf3 = "Source1.json"
        self._save_config(c3, cf3, True)

        # Act/Assert
        s = ApplicationSettings(self._config_file_list([cf1, cf2]), self._config_file_list([cf3], True))
        v1 = s.Get("Config1", "Setting1")
        self.assertEqual(v1, c1["Setting1"], f"Config1 basic Get failed, expected '{c1['Setting1']}' but got '{v1}'")
        v2 = s.Get("Config1", "Nope", "Nope")
        self.assertEqual(v2, "Nope", f"Config1 basic Get failed to return default value (returned '{v2}' instead)")
        print(f"[TEST SUCCESS] {module}:test_main")


if __name__ == '__main__':
    unittest.main()