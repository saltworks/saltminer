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
