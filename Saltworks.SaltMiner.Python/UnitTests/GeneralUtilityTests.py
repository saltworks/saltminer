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
import logging
import datetime

from Core.Application import Application
from Utility.GeneralUtility import GeneralUtility

module = os.path.splitext(os.path.basename(__file__))[0]
app = Application(skipCleanFiles=True)

def DateTimeTests():
    # Arrange
    fn = sys._getframe().f_code.co_name
    dt1 = "2023-04-23T03:48:29.673237"
    dt2 = "nsdlk"
    dt3 = datetime.datetime.utcnow()
    dt4 = ""

    # Act/Assert
    try:
        c1 = GeneralUtility.GetFormattedDateString(dt1)
        c2 = False
        try:
            GeneralUtility.GetFormattedDateString(dt2)
        except ValueError:
            c2 = True
        c3 = GeneralUtility.GetFormattedDateString(dt3)
        c4 = GeneralUtility.GetFormattedDateString(dt4)

        assert c1[0:18] == dt1[0:18], f"Expected dt1 to be '{dt1[0:18]}', but instead found '{c1[0:18]}'"
        assert c2, "Should have thrown a value error"
        assert len(c3) > 17, "Should have returned a value"
        assert c4 == None, "Empty value should result in None"
        # Report
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")


DateTimeTests()