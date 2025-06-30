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
import time

from elasticsearch.helpers.actions import BulkIndexError

from Core.Application import Application

module = os.path.splitext(os.path.basename(__file__))[0]
app = Application()

def ConnectionTest():
    es = app.GetElasticClient()
    fn = sys._getframe().f_code.co_name
    e = "Ping failed"
    if not es.PingServer():
        logging.info("Elasticsearch connection failed.")
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")
    else:
        logging.info("Elasticsearch connection succeeded.")
        logging.info(f"[TEST SUCCESS] {module}:{fn}")

def UpdateByQueryTest():
    es = app.GetElasticClient()
    fn = sys._getframe().f_code.co_name
    try:
        rsp = es.UpdateByQuery("issues_app_saltworks.ssc_ssc1", {}, noWait=False)
        assert rsp['completed'] == True
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")

def SearchScrollTest():
    es = app.GetElasticClient()
    fn = sys._getframe().f_code.co_name
    try:
        scroll = es.SearchScroll("sscprojects", scrollSize=50)
        batch = 1
        while scroll.Results:
            logging.info(f"Batch {batch}, {len(scroll.Results)} result(s). First project id: '{scroll.Results[0]['_source']['id']}'")
            scroll.GetNext()
            batch += 1
        assert batch > 1, "Should have returned more than 1 batch in scroller"
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")

def SearchScrollAfterTest():
    es = app.GetElasticClient()
    fn = sys._getframe().f_code.co_name
    try:
        scroll = es.SearchScroll("sscprojects", scrollSize=50, scrollTimeout=None)
        batch = 1
        startId = scroll.Results[0]['_id'] if scroll.Results else None
        while scroll.Results:
            logging.info(f"Batch {batch}, {len(scroll.Results)} result(s). First project id: '{scroll.Results[0]['_source']['id']}'")
            endId = scroll.Results[0]['_id']
            scroll.GetNext()
            batch += 1
        assert batch > 1, "Should have returned more than 1 batch in scroller"
        assert startId and endId and startId != endId, "Scroller not returning successive batches"
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")

def RoleTests():
    # Arrange
    es = app.GetElasticClient()
    fn = sys._getframe().f_code.co_name
    r = "TempTestRoleUniqueIgnoreMe76523765"
    rm = "TestRoleMappingUniqueIgnoreMe9879263"

    # Act/Assert
    try:
        es.RoleFromTemplate(r, "SecurityRoleTemplate", {"AppVerId": "10077"})
        es.RoleMapping(rm, [r], ["CN=Travis Dickson,OU=SM Users,DC=sm,DC=local"])
        list = es.GetRoles()
        assert len(list) > 0, "Roles list should have at least one item"
        assert r in list, "Roles list doesn't include the new role"
        list = es.GetRoleMappings()
        assert len(list) > 0, "Role mappings list should have at least one item"
        assert rm in list, "Role mappings list doesn't include the new role mapping"
        es.DeleteRoleMapping(rm)
        es.DeleteRole(r)
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")

def CreateOnlyTest():
    # Arrange
    es = app.GetElasticClient()
    fn = sys._getframe().f_code.co_name
    idx = "test_index_elastic_client_remove_me_whenever"
    doc = { "field": "value", "id": 1 }
    es.DeleteIndex(idx) # just in case it exists

    # Act/Assert
    try:
        rsp = es.IndexWithId(idx, "1", doc, createOnly=True)
        assert rsp['result'] == 'created', "First index operation should have succeeded"
        rsp = es.IndexWithId(idx, "1", doc, createOnly=True)
        assert rsp['result'] == 'noop', "Second index operation should have generated a noop result"
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")
    try:
        es.DeleteIndex(idx)
    except Exception as e:
        logging.info(f"[TEST CLEANUP FAILURE] {module}:{fn}: {e}")

def SearchAndLockTest():
    # Arrange
    es = app.GetElasticClient()
    fn = sys._getframe().f_code.co_name
    idx = "test_index_elastic_client_remove_me_whenever"
    doc = { "field": "value", "id": 1 }
    es.DeleteIndex(idx) # just in case it exists
    es.IndexWithId(idx, "1", doc)
    time.sleep(2)

    # Act/Assert
    try:
        rsp = es.Search(idx, includeLockingInfo=True)
        dto = rsp[0]
        doc = dto['_source']
        doc['locked'] = datetime.datetime.utcnow().isoformat()
        seq = dto['_seq_no']
        pri = dto['_primary_term']
        upd = es.UpdateWithLocking(idx, doc, "1", seq, pri)
        assert upd['result'] == "updated", "First update should succeed"
        upd = es.UpdateWithLocking(idx, doc, "1", seq, pri)
        assert upd['result'] == "noop", "Second update should fail"
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")
    try:
        es.DeleteIndex(idx)
    except Exception as e:
        logging.info(f"[TEST CLEANUP FAILURE] {module}:{fn}: {e}")

def BulkTest():
    # Arrange
    es = app.GetElasticClient()
    fn = sys._getframe().f_code.co_name
    idx = "test_index_elastic_client_remove_me_whenever"
    docs = [
        es.BulkInsertDocument(idx, { "field": "value1", "id": 1 }, "1", "create"),
        es.BulkInsertDocument(idx, { "field": "value2", "id": 2 }, "2", "create")
    ]
    es.DeleteIndex(idx) # just in case it exists

    # Act/Assert
    try:
        rsp = es.BulkInsert(docs)
        assert rsp[0] == 2, "Response should indicate 2 & 0"
        rsp = es.BulkInsert(docs, False)
        assert rsp[0] == 0 and rsp[1] == 2, "Response on second insert should indicate 0 & 2"
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except BulkIndexError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e.args[0]}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")
    try:
        es.DeleteIndex(idx)
    except Exception as e:
        logging.info(f"[TEST CLEANUP FAILURE] {module}:{fn}: {e}")

def BulkTest2():
    # Arrange
    es = app.GetElasticClient()
    fn = sys._getframe().f_code.co_name
    idx = "test_index_elastic_client_remove_me_whenever"
    es.DeleteIndex(idx) # just in case it exists

    # Act/Assert
    try:
        for i in range(1, 10000):
            es.BulkSendBatch(idx, { "field": "value", "id": i }, batchSize=3000)
        time.sleep(2)
        count = es.Count(idx)
        assert count == 9000, "Count should be 9k after loop but before final send"
        es.BulkSendBatch(None, None)
        time.sleep(2)
        count = es.Count(idx)
        assert count == 9999, "Count should be 10k after final send"
        es.DeleteIndex(idx)
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except BulkIndexError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e.args[0]}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")
    try:
        es.DeleteIndex(idx)
    except Exception as e:
        logging.info(f"[TEST CLEANUP FAILURE] {module}:{fn}: {e}")

ConnectionTest()
#UpdateByQueryTest()  # Leave commented unless needed; takes a while to complete if any data in system
#RoleTests()
SearchScrollTest()
SearchScrollAfterTest()
CreateOnlyTest()
BulkTest()
BulkTest2()
SearchAndLockTest()

