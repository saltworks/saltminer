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

import json
import os
import unittest

from Core.RestClient import RestClient

module = os.path.splitext(os.path.basename(__file__))[0]


class RestClientTests(unittest.TestCase):
    """Unit tests for RestClient functionality."""

    def setUp(self):
        """[UnitTest] Set up test fixtures before each test method."""
        self.client = RestClient("https://postman-echo.com")
        self.test_object = {"what": "stringy", "howmany": 1, "really": False}

    def test_get(self):
        """Test GET request."""
        # Act
        r1 = self.client.Get("get")

        # Assert
        self.assertTrue(r1.ok, "Should have been a successful GET, but wasn't")
        print(f"[TEST SUCCESS] {module}:test_get")

    def test_post(self):
        """Test POST request."""
        # Act
        r1 = self.client.Post("post", json.dumps(self.test_object))
        d2 = None
        if r1.text:
            d2 = json.loads(json.loads(json.loads(r1.text)['data']))

        # Assert
        self.assertTrue(r1.ok, "Should have been a successful POST, but wasn't")
        self.assertEqual(self.test_object['really'], d2['really'], "Resulting post data doesn't match on field 'really'")
        self.assertEqual(self.test_object['what'], d2['what'], "Resulting post data doesn't match on field 'what'")
        self.assertEqual(self.test_object['howmany'], d2['howmany'], "Resulting post data doesn't match on field 'howmany'")
        print(f"[TEST SUCCESS] {module}:test_post")

    def test_put(self):
        """Test PUT request."""
        # Act
        r1 = self.client.Put("put", json.dumps(self.test_object))
        d2 = None
        if r1.text:
            d2 = json.loads(json.loads(json.loads(r1.text)['data']))

        # Assert
        self.assertTrue(r1.ok, "Should have been a successful PUT, but wasn't")
        self.assertEqual(self.test_object['really'], d2['really'], "Resulting put data doesn't match on field 'really'")
        self.assertEqual(self.test_object['what'], d2['what'], "Resulting put data doesn't match on field 'what'")
        self.assertEqual(self.test_object['howmany'], d2['howmany'], "Resulting put data doesn't match on field 'howmany'")
        print(f"[TEST SUCCESS] {module}:test_put")

    def test_delete(self):
        """Test DELETE request."""
        # Act
        r1 = self.client.Delete("delete")

        # Assert
        self.assertTrue(r1.ok, "Should have been a successful DELETE, but wasn't")
        print(f"[TEST SUCCESS] {module}:test_delete")

    def test_perf(self):
        """Test performance tracking."""
        # Act
        r1 = self.client.Get("get")
        d1 = self.client.RequestLastDuration()
        a1 = self.client.RequestAvgDuration()
        r2 = self.client.Get("get")
        d2 = self.client.RequestLastDuration()
        a2 = self.client.RequestAvgDuration()
        r3 = self.client.Get("get")
        d3 = self.client.RequestLastDuration()
        r4 = self.client.Get("get")
        d4 = self.client.RequestLastDuration()
        ct = self.client.RequestCount()
        avg = self.client.RequestAvgDuration()
        cavg = (d1 + d2 + d3 + d4) / 4

        # Assert
        self.assertTrue(r1.ok, "Should have been a successful GET (1), but wasn't")
        self.assertTrue(r2.ok, "Should have been a successful GET (2), but wasn't")
        self.assertTrue(r3.ok, "Should have been a successful GET (3), but wasn't")
        self.assertTrue(r4.ok, "Should have been a successful GET (4), but wasn't")
        self.assertEqual(d1, a1, f"Expected 1st avg duration to be {d1}, but found it to be {a1}")
        self.assertEqual(ct, 4, f"Should have been 4 requests, but instead returned {ct}")
        self.assertEqual(avg, cavg, f"Expected avg of {cavg}, but instead returned {avg}")
        print(f"[TEST SUCCESS] {module}:test_perf")


if __name__ == '__main__':
    unittest.main()