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
import unittest

from Core.Application import Application
from Core.FodClient import FodClient

module = os.path.splitext(os.path.basename(__file__))[0]


class FodClientTests(unittest.TestCase):
    """Unit tests for FodClient functionality."""

    @classmethod
    def setUpClass(cls):
        """[UnitTest] Set up test fixtures before running tests in this class."""
        cls.app = Application(skipCleanFiles=True)
        cls.source_name = "FOD1"

    def setUp(self):
        """[UnitTest] Set up test fixtures before each test method."""
        self.fod = FodClient(self.app.Settings, self.source_name)

    def test_connection(self):
        """Test FOD connection."""
        x, y = FodClient.TestConnection(self.app.Settings, self.source_name)
        self.assertTrue(x, f"Connection test failed: {y}")
        logging.info(f"[TEST SUCCESS] {module}:test_connection")

    def test_get_paged(self):
        """Test paged retrieval of releases."""
        # Arrange
        limit = 102

        # Act/Assert
        r = self.fod.GetReleases(limit)
        total = r.Content['totalCount']
        if total >= limit:
            total = limit
        self.assertEqual(len(r.Content['items']), total, f"Should be returning {total} items.")
        logging.info(f"[TEST SUCCESS] {module}:test_get_paged")

    def test_scroll(self):
        """Test scroll functionality for releases."""
        # Arrange
        lst = []

        # Act/Assert
        r = self.fod.GetReleases(scroller=True)
        r.GetAll()
        total = r.TotalHits
        self.assertTrue(total and total > 0, "TotalHits should be > 0 from GetAll")
        self.assertEqual(len(r.Results), total, f"Should be returning {total} items from GetAll.")

        r = self.fod.GetReleases(scroller=True)
        total = 57
        r.GetAll(total)
        self.assertEqual(len(r.Results), total, f"Should be returning {total} items from GetAll(limit).")

        r = self.fod.GetReleases(scroller=True)
        r.GetNext()
        total = r.TotalHits
        self.assertTrue(total and total > 0, "TotalHits should be > 0 from GetNext")
        while r.Results:
            lst.extend(r.Results)
            r.GetNext()
        self.assertEqual(len(lst), total, f"Should be returning {total} items on GetNext loop.")

        logging.info(f"[TEST SUCCESS] {module}:test_scroll")


if __name__ == '__main__':
    unittest.main()
