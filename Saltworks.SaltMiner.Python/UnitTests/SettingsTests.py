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
import shutil
import os
import json

from Core.ApplicationSettings import ApplicationSettings

module = os.path.splitext(os.path.basename(__file__))[0]
configTestDir = "ConfigTestTmp"

def SetupConfigTest():
    spath = os.path.join(configTestDir, "Sources")
    if not os.path.isdir(configTestDir):
        os.mkdir(configTestDir)
    if not os.path.isdir(spath):
        os.mkdir(spath)

def ConfigFileList(filenames, isSource=False):
    list = []
    for f in filenames:
        list.append(os.path.join(configTestDir, "Sources", f) if isSource else os.path.join(configTestDir, f))
    return list

def SaveConfig(config, filename, isSource=False):
    if isSource:
        path = os.path.join(configTestDir, "Sources", filename)
    else:
        path = os.path.join(configTestDir, filename)
    with open(path, "w") as f:
        f.write(json.dumps(config))

def CleanupConfigTest():
    spath = os.path.join(".", configTestDir)
    if os.path.isdir(spath):
        shutil.rmtree(spath)

def MainTests():
    # Arrange
    SetupConfigTest()
    c1 = { "Setting1": "Value1", "Setting2": "Value2", "Password": "1234" }
    cf1 = "Config1.json"
    SaveConfig(c1, cf1)
    c2 = { "Setting1": "Value1.2", "Setting3": "Value3" }
    cf2 = "Config2.json"
    SaveConfig(c2, cf2)
    c3 = { "SourceName": "SSC1", "Setting4": "Value4" }
    cf3 = "Source1.json"
    SaveConfig(c3, cf3, True)

    # Act/Assert
    fn = sys._getframe().f_code.co_name
    try:
        s = ApplicationSettings(ConfigFileList([cf1, cf2]), ConfigFileList([cf3], True))
        v1 = s.Get("Config1", "Setting1")
        assert v1 == c1["Setting1"], f"Config1 basic Get failed, expected '{c1['Setting1']}' but got '{v1}'"
        v2 = s.Get("Config1", "Nope", "Nope")
        assert v2 == "Nope", f"Config1 basic Get failed to return default value (returned '{v2}' instead)"
        print(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        print(f"[TEST FAILURE] {module}:{fn}: {e}")

    # Away with the test env
    CleanupConfigTest()


MainTests()