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

import json
import os
import sys

from Core.RestClient import RestClient

module = os.path.splitext(os.path.basename(__file__))[0]

def GetObject():
    return { "what": "stringy", "howmany": 1, "really": False }

def Get():
    # Arrange
    c = RestClient("https://postman-echo.com")
    d = GetObject()
    fn = sys._getframe().f_code.co_name

    # Act
    r1 = c.Get("get")
    
    # Assert
    try:
        assert r1.ok, "Should have been a successful GET, but wasn't"
        print(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        print(f"[TEST FAILURE] {module}:{fn}: {e}")

def Delete():
    # Arrange
    c = RestClient("https://postman-echo.com")
    d = GetObject()
    fn = sys._getframe().f_code.co_name

    # Act
    r1 = c.Delete("delete")
    
    # Assert
    try:
        assert r1.ok, "Should have been a successful DELETE, but wasn't"
        print(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        print(f"[TEST FAILURE] {module}:{fn}: {e}")

def Post():
    # Arrange
    c = RestClient("https://postman-echo.com")
    d = GetObject()
    fn = sys._getframe().f_code.co_name

    # Act
    r1 = c.Post("post", json.dumps(d))
    d2 = None
    if r1.text:
        d2 = json.loads(json.loads(json.loads(r1.text)['data']))
    
    # Assert
    try:
        assert r1.ok, "Should have been a successful POST, but wasn't"
        assert d['really'] == d2['really'], "Resulting post data doesn't match on field 'really'"
        assert d['what'] == d2['what'], "Resulting post data doesn't match on field 'what'"
        assert d['howmany'] == d2['howmany'], "Resulting post data doesn't match on field 'howmany'"
        print(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        print(f"[TEST FAILURE] {module}:{fn}: {e}")

def Put():
    # Arrange
    c = RestClient("https://postman-echo.com")
    d = GetObject()
    fn = sys._getframe().f_code.co_name

    # Act
    r1 = c.Put("put", json.dumps(d))
    d2 = None
    if r1.text:
        d2 = json.loads(json.loads(json.loads(r1.text)['data']))
    
    # Assert
    try:
        assert r1.ok, "Should have been a successful PUT, but wasn't"
        assert d['really'] == d2['really'], "Resulting put data doesn't match on field 'really'"
        assert d['what'] == d2['what'], "Resulting put data doesn't match on field 'what'"
        assert d['howmany'] == d2['howmany'], "Resulting put data doesn't match on field 'howmany'"
        print(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        print(f"[TEST FAILURE] {module}:{fn}: {e}")

def Perf():
    # Arrange
    c = RestClient("https://postman-echo.com")
    d = GetObject()
    fn = sys._getframe().f_code.co_name

    # Act
    r1 = c.Get("get")
    d1 = c.RequestLastDuration()
    a1 = c.RequestAvgDuration()
    r2 = c.Get("get")
    d2 = c.RequestLastDuration()
    a2 = c.RequestAvgDuration()
    r3 = c.Get("get")
    d3 = c.RequestLastDuration()
    r4 = c.Get("get")
    d4 = c.RequestLastDuration()
    ct = c.RequestCount()
    avg = c.RequestAvgDuration()
    cavg = (d1 + d2 + d3 + d4) / 4
    
    # Assert
    try:
        assert r1.ok, "Should have been a successful GET (1), but wasn't"
        assert r2.ok, "Should have been a successful GET (2), but wasn't"
        assert r3.ok, "Should have been a successful GET (3), but wasn't"
        assert r4.ok, "Should have been a successful GET (4), but wasn't"
        assert d1 == a1, f"Expected 1st avg duration to be {d1}, but found it to be {a1}"
        assert ct == 4, f"Should have been 4 requests, but instead returned {ct}"
        assert avg == cavg, f"Expected avg of {cavg}, but instead returned {avg}"
        print(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        print(f"[TEST FAILURE] {module}:{fn}: {e}")


# Run tests
Get()
Post()
Put()
Delete()
Perf()