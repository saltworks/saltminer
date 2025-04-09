''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-04-09
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
from Core.FodClient import FodClient

module = os.path.splitext(os.path.basename(__file__))[0]
app = Application(skipCleanFiles=True)
sourceName = "FOD1"

def ConnectionTest():
    fn = sys._getframe().f_code.co_name
    s = app.Settings
    x,y = FodClient.TestConnection(s, sourceName)
    if not x:
        logging.info(f"[TEST FAILURE] {module}:{fn}")
    else:
        logging.info(f"[TEST SUCCESS] {module}:{fn}")

def GetPagedTest():
    # Arrange
    fn = sys._getframe().f_code.co_name
    fod = FodClient(app.Settings, sourceName)
    limit = 102
    # Act/Assert
    try:
        r = fod.GetReleases(limit)
        total = r.Content['totalCount']
        if total >= limit:
            total = limit
        assert len(r.Content['items']) == total, f"Should be returning {total} items."
        # Report
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")

def ScrollTest():
    # Arrange
    fn = sys._getframe().f_code.co_name
    fod = FodClient(app.Settings, sourceName)
    lst = []
    # Act/Assert
    try:
        r = fod.GetReleases(scroller=True)
        r.GetAll()
        total = r.TotalHits
        assert total and total > 0, "TotalHits should be > 0 from GetAll"
        assert len(r.Results) == total, f"Should be returning {total} items from GetAll."

        r = fod.GetReleases(scroller=True)
        total = 57
        r.GetAll(total)
        assert len(r.Results) == total, f"Should be returning {total} items from GetAll(limit)."

        r = fod.GetReleases(scroller=True)
        items = r.GetNext()
        total = r.TotalHits
        assert total and total > 0, "TotalHits should be > 0 from GetNext"
        while r.Results:
            lst.extend(r.Results)
            r.GetNext()
        assert len(lst) == total, f"Should be returning {total} items on GetNext loop."

        # Report
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")

ConnectionTest()
GetPagedTest()
# Needs FOD releases to succeed
ScrollTest()
