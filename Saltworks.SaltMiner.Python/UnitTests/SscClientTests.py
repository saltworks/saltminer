''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-06-30
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

import sys
import os
import time
import logging

from Core.Application import Application
from Core.SscClient import SscClient

module = os.path.splitext(os.path.basename(__file__))[0]
app = Application()

def ConnectionTest(app):
    s = app.Settings
    x,y = SscClient.TestConnection(s, 'SSC1')
    fn = sys._getframe().f_code.co_name
    if not x:
        print(f"SSC Connection failed: {y}")
        logging.info(f"[TEST FAILURE] {module}:{fn}")
    else:
        print("SSC Connection succeeded.")
        logging.info(f"[TEST SUCCESS] {module}:{fn}")

# DEPENDENT ON DATA, MAY NEED MODIFICATION TO WORK PROPERLY
def ScanTests(app):
    # Arrange
    c = SscClient(app.Settings, 'SSC1')
    fn = sys._getframe().f_code.co_name

    # Act / Assert
    try:
        data = c.GetProjectVersionScans(10005, 5)
        assert len(data) == 16, "Expected 16 scans for avid 10005 (if data has changed this may be wrong)"
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")

def BulkIssuePullTest(app):
    # Arrange
    s = app.Settings
    c = SscClient(s, 'SSC1')
    fn = sys._getframe().f_code.co_name

    # Act / Assert
    try:
        list = c.GetProjectVersions(limit = 1)
        assert len(list) == 1, "Should have returned 1 project version"
        id = list[0]['id']
        filter = c.GetProjectVersionDefaultFilterset(id)
        assert filter, "Should have returned default filterset guid for project version"
        rsp = c.GetProjectVersionIssues(id, filter, 1)
        assert rsp['count'] > 1, "Should have shown more than 1 issue for project version"
        assert len(rsp['data']) == 1, "Should have returned 1 issue for project version"
        iid = rsp['data'][0]['id']
        rsp = c.GetProjectVersionIssues(id, filter, 1)
        assert len(rsp['data']) == 1, "Should have returned 1 issue for project version (2nd call)"
        assert rsp['data'][0]['id'] != iid, "Should have returned a different issue on the 2nd call"
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")

def MainTests(app):
    # Arrange
    s = app.Settings
    c = SscClient(s, 'SSC1')
    fn = sys._getframe().f_code.co_name

    # Act/Assert
    try:
        list = c.GetProjectVersions(limit = 10)
        assert len(list) == 10, "Project version list should have 10 items"
        list = c.GetUsers()
        assert len(list) > 1, "User list should have more than 1 item"
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")

def CacheTests(app):
    # Arrange
    s = app.Settings
    c = SscClient(s, 'SSC1')
    fn = sys._getframe().f_code.co_name

    # Act
    st = time.perf_counter()
    list = c.GetProjectVersions(forceRefresh = True, limit = 50)
    et1 = time.perf_counter() - st
    st = time.perf_counter()
    c1 = len(list)
    list = c.GetProjectVersions(forceRefresh = False, limit = 50)
    et2 = time.perf_counter() - st
    c2 = len(list)
    print(f"et1: {et1}, et2: {et2}")

    # Assert
    try:
        assert c1 == c2, f"Expected list to contain the same number of elements each time, but the second time was different from the first ({c1} vs. {c2})."
        assert et1 / 2 > et2, f"Expected cache hit to take less than half the time of the original request, but it didn't ({et1} vs. {et2})."
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")

def BulkIssueDetails(app):
    # Arrange
    s = app.Settings
    c = SscClient(s, 'SSC1')
    lst = c.GetProjectVersions(limit = 50)
    issues = None
    for pv in lst:
        filter = c.GetProjectVersionDefaultFilterset(pv['id'])
        r = c.GetProjectVersionIssuesV2(pv['id'], filter, 'id', 10, restartScroll=True)
        if 'data' in r.keys() and r['data']:
            if 'count' in r.keys() and r['count'] >= 10:
                issues = r['data']
                break
    if not issues or len(issues) < 10:
        print("First 50 pvs don't have at least 10 issues, can't perform bulk test")
        return
    fn = sys._getframe().f_code.co_name
    uri = c.BaseUrl + "api/v1/issueDetails"

    # Act/Assert
    try:
        requests = []
        for iss in issues:
            requests.append(c.BulkRequest(f"{uri}/{iss['id']}"))
        list = c.Bulk(requests)
        assert len(list) == len(issues), f"Bulk response count ({len(list)}) doesn't match issues count ({len(issues)})"
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")

# Don't enable unless data is in good shape
#ScanTests(app)
BulkIssueDetails(app)
ConnectionTest(app)
MainTests(app)
CacheTests(app)
BulkIssuePullTest(app)
