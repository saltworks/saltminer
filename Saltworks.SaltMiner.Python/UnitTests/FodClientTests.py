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
