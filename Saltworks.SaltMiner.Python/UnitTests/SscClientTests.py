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
import logging
import unittest

from Core.Application import Application
from Core.SscClient import SscClient

module = os.path.splitext(os.path.basename(__file__))[0]


class SscClientTests(unittest.TestCase):
    """Unit tests for SscClient functionality."""

    @classmethod
    def setUpClass(cls):
        """[UnitTest] Set up test fixtures before running tests in this class."""
        cls.app = Application()
        cls.source_name = 'SSC1'

    def setUp(self):
        """[UnitTest] Set up test fixtures before each test method."""
        self.client = SscClient(self.app.Settings, self.source_name)

    def test_connection(self):
        """Test SSC connection."""
        x, y = SscClient.TestConnection(self.app.Settings, self.source_name)
        if not x:
            print(f"SSC Connection failed: {y}")
            self.fail(f"Connection test failed: {y}")
        print("SSC Connection succeeded.")
        logging.info(f"[TEST SUCCESS] {module}:test_connection")

    def test_scans(self):
        """Test scan retrieval functionality."""
        # DEPENDENT ON DATA, MAY NEED MODIFICATION TO WORK PROPERLY
        data = self.client.GetProjectVersionScans(10005, 5)
        self.assertEqual(len(data), 16, "Expected 16 scans for avid 10005 (if data has changed this may be wrong)")
        logging.info(f"[TEST SUCCESS] {module}:test_scans")

    def test_bulk_issue_pull(self):
        """Test bulk issue pull functionality."""
        # Act / Assert
        pv_list = self.client.GetProjectVersions(limit=1)
        self.assertEqual(len(pv_list), 1, "Should have returned 1 project version")
        pv_id = pv_list[0]['id']
        filter_set = self.client.GetProjectVersionDefaultFilterset(pv_id)
        self.assertTrue(filter_set, "Should have returned default filterset guid for project version")
        rsp = self.client.GetProjectVersionIssues(pv_id, filter_set, 1)
        self.assertGreater(rsp['count'], 1, "Should have shown more than 1 issue for project version")
        self.assertEqual(len(rsp['data']), 1, "Should have returned 1 issue for project version")
        iid = rsp['data'][0]['id']
        rsp = self.client.GetProjectVersionIssues(pv_id, filter_set, 1)
        self.assertEqual(len(rsp['data']), 1, "Should have returned 1 issue for project version (2nd call)")
        self.assertNotEqual(rsp['data'][0]['id'], iid, "Should have returned a different issue on the 2nd call")
        logging.info(f"[TEST SUCCESS] {module}:test_bulk_issue_pull")

    def test_main(self):
        """Test main functionality - project versions and users."""
        # Act/Assert
        pv_list = self.client.GetProjectVersions(limit=10)
        self.assertEqual(len(pv_list), 10, "Project version list should have 10 items")
        user_list = self.client.GetUsers()
        self.assertGreater(len(user_list), 1, "User list should have more than 1 item")
        logging.info(f"[TEST SUCCESS] {module}:test_main")

    def test_cache(self):
        """Test caching functionality."""
        # Act
        st = time.perf_counter()
        pv_list = self.client.GetProjectVersions(forceRefresh=True, limit=50)
        et1 = time.perf_counter() - st
        st = time.perf_counter()
        c1 = len(pv_list)
        pv_list = self.client.GetProjectVersions(forceRefresh=False, limit=50)
        et2 = time.perf_counter() - st
        c2 = len(pv_list)
        print(f"et1: {et1}, et2: {et2}")

        # Assert
        self.assertEqual(c1, c2, f"Expected list to contain the same number of elements each time, but the second time was different from the first ({c1} vs. {c2}).")
        self.assertGreater(et1 / 2, et2, f"Expected cache hit to take less than half the time of the original request, but it didn't ({et1} vs. {et2}).")
        logging.info(f"[TEST SUCCESS] {module}:test_cache")

    def test_bulk_issue_details(self):
        """Test bulk issue details retrieval."""
        # Arrange
        pv_list = self.client.GetProjectVersions(limit=50)
        issues = None
        for pv in pv_list:
            filter_set = self.client.GetProjectVersionDefaultFilterset(pv['id'])
            r = self.client.GetProjectVersionIssuesV2(pv['id'], filter_set, 'id', 10, restartScroll=True)
            if 'data' in r.keys() and r['data']:
                if 'count' in r.keys() and r['count'] >= 10:
                    issues = r['data']
                    break
        if not issues or len(issues) < 10:
            self.skipTest("First 50 pvs don't have at least 10 issues, can't perform bulk test")
            return

        uri = self.client.BaseUrl + "api/v1/issueDetails"

        # Act/Assert
        requests = []
        for iss in issues:
            requests.append(self.client.BulkRequest(f"{uri}/{iss['id']}"))
        result_list = self.client.Bulk(requests)
        self.assertEqual(len(result_list), len(issues), f"Bulk response count ({len(result_list)}) doesn't match issues count ({len(issues)})")
        logging.info(f"[TEST SUCCESS] {module}:test_bulk_issue_details")


if __name__ == '__main__':
    unittest.main()
