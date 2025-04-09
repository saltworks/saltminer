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

from Core.Application import Application
from Utility.SettingsHelper import SettingsHelper

module = os.path.splitext(os.path.basename(__file__))[0]

def MainTest():
    # Arrange
    sh = SettingsHelper(Application())
    key = "testsetting"
    val = "testval"
    val2 = "testval2"

    # Act/Assert
    fn = sys._getframe().f_code.co_name
    try:
        setting = sh.Get(key)
        assert setting == None, "Setting should be none when doesn't exist."
        sh.SetValue(key, val)
        time.sleep(1)
        setting = sh.Get(key)
        assert setting != None, "Setting should be present after setting it."
        setting.Value = val2
        sh.Set(setting)
        time.sleep(1)
        setting = sh.Get(key)
        assert setting.Value == val2, f"Setting value '{setting.Value}', but expected '{val2}'"
        print(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        print(f"[TEST FAILURE] {module}:{fn}: {e}")
    finally:
        # cleanup
        try:
            sh.DeleteByKey(key)
        except:
            pass


MainTest()