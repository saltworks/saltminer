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

import time
import unittest

from Utility.ProgressLogger import ProgressLogger
from Core.Application import Application


class ProgressLoggerTests(unittest.TestCase):
    """Unit tests for ProgressLogger functionality."""

    @classmethod
    def setUpClass(cls):
        """[UnitTest] Set up test fixtures before running tests in this class."""
        cls.es = Application().GetElasticClient()

    def setUp(self):
        """[UnitTest] Set up test fixtures before each test method."""
        self.progress_logger = ProgressLogger(self.es)

    def test_progress(self):
        """Test progress logging functionality."""
        self.progress_logger.Start("test", 20)
        for i in range(1, 20, 1):
            time.sleep(5)
            self.progress_logger.Progress(i)
        self.progress_logger.Finish()

    def test_get_index(self):
        """Test retrieving progress log index."""
        # Uncomment if needed for testing index retrieval
        # r = self.es.GetIndex(ProgressLogger.IndexName(), "id:ascending,timestamp:ascending")
        # for x in r:
        #     print("{}".format(x))
        pass


if __name__ == '__main__':
    unittest.main()
