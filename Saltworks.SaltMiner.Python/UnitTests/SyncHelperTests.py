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
import logging
import time
import datetime

from Core.Application import Application
from Utility.SyncQueueHelper import SyncQueueHelper
from Core.SscClient import SscClient

module = os.path.splitext(os.path.basename(__file__))[0]
sourceName = "SSC1"
app = Application(skipCleanFiles=True)
ssc = SscClient(app.Settings, sourceName)
rdr = ssc.GetProjectVersions(fields='id')
sqh = SyncQueueHelper(app.Settings, sourceName)
sqh.BatchSize = 10
es = app.GetElasticClient()

def ClearAndLoadQueueTest():
    # Arrange
    fn = sys._getframe().f_code.co_name

    # Act/Assert
    try:
        sqh.ClearSyncQueue(True, True)
        count = 0
        idList = []
        for itm in rdr:
            if count % 10 == 0:
                sqh.InsertQueueBatch(idList, False)
                idList = []
            idList.append(itm['id'])
            count += 1
        if len(idList) > 0:
            sqh.InsertQueueBatch(idList, False)
        assert count == len(rdr), f"Should have loaded {len(rdr)} sync queues, but counted {count} instead."
        # Report
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")

def GetLockCompleteTest():
    # Arrange
    fn = sys._getframe().f_code.co_name
    # Act/Assert
    try:
        result = sqh.GetSyncQueueBatch()
        qBatch = result[0]
        total = result[1]
        assert total > 0, "Expected results but no data returned."
        for qItem in qBatch:
            logging.info("Testing target %s", qItem.SyncQueueDoc.TargetId)
            r = sqh.SetInProgress(qItem)
            assert r !=  None, "Expected successful lock, but None returned from SetInProgress()."
            assert r.SyncQueueDoc.LockId == sqh.SessionId, "Expected lock id for sync queue with id {r.SyncQueueDoc.TargetId} to match session id '{sqh.SessionId}', but found '{r.SyncQueueDoc.LockId}' instead."
            r = sqh.SetComplete(qItem)
            assert r !=  None, "Expected successful completion, but None returned from SetComplete()."
            assert not r.SyncQueueDoc.LockId and r.SyncQueueDoc.Completed, "Expected no lock id and a completed date for sync queue with id {r.SyncQueueDoc.TargetId}, but found lock id '{r.SyncQueueDoc.LockId}' and completed '{r.SyncQueueDoc.Completed}'."
            
        # Report
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")

def CleanupHistoryTest():
    # Arrange
    fn = sys._getframe().f_code.co_name
    # Act/Assert
    try:
        qItem = sqh.GetSyncQueueBatch()[0][0] # first item in batch
        doc = qItem.SyncQueueDoc.Dto()
        doc['completed'] = (datetime.datetime.utcnow() - datetime.timedelta(days=8)).isoformat()
        qid = qItem.Id
        es.IndexWithId(sqh.Index, qid, doc)
        sqh.CleanupQueueHistory()
        es.FlushIndex(sqh.Index)
        logging.info("Slight delay to complete flush...")
        time.sleep(1)
        r = es.Get(sqh.Index, qid, False)
        assert not r['found'], "Should not have found the removed queue item."
            
        # Report
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")

def ClearSessionTest():
    # Arrange
    fn = sys._getframe().f_code.co_name
    sqh.ClearSession() # initial clear

    # Act/Assert
    try:
        qItem = sqh.GetSyncQueueBatch()[0][0] # first item in batch
        qid = qItem.Id
        assert sqh.SetInProgress(qItem) != None, "Expected successful lock, but None returned from SetInProgress()."
        es.FlushIndex(sqh.Index)
        logging.info("Slight delay to complete flush...")
        time.sleep(1)
        sqh.ClearSession()
        es.FlushIndex(sqh.Index)
        logging.info("Slight delay to complete flush...")
        time.sleep(1)
        r = es.Get(sqh.Index, qid, False)
        assert r['found'] and not r['_source']['lock_id'], "Queue item not found in data or lock id is still set."
            
        # Report
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")

def PriorityTest():
    # Arrange
    fn = sys._getframe().f_code.co_name
    sqh.ClearSyncQueue(True, True)
    sqh.ClearPriorityReservations()
    es.FlushIndex(sqh.Index)
    logging.info("Slight delay to complete flush (this test method requires several of these)...")
    time.sleep(2)

    # Act/Assert
    try:
        # normal priority items, id order
        idList = ["1", "2", "3", "4"]
        sqh.InsertQueueBatch(idList)
        es.FlushIndex(sqh.Index)
        logging.info("Slight delay to complete flush...")
        time.sleep(2)
        response = sqh.GetSyncQueueBatch()
        qItem = response[0][0].SyncQueueDoc # first item in batch
        assert qItem.TargetId == idList[0], f"Expected first queue item ID to be {idList[0]} before adding priority items."
        
        # add priority items
        sqh.InsertQueueBatch(["7"], 4)
        sqh.InsertQueueBatch(["6"], 3, permanent=True)
        sqh.InsertQueueBatch(["5"], 4)
        es.FlushIndex(sqh.Index)
        logging.info("Slight delay to complete flush...")
        time.sleep(2)
        response = sqh.GetSyncQueueBatch()
        qItem1 = response[0][0].SyncQueueDoc # first item in batch
        qItem2 = response[0][1].SyncQueueDoc # second item in batch
        qItem3 = response[0][2].SyncQueueDoc # third item in batch
        assert qItem1.TargetId == "6", f"Expected first queue item ID to be 6 after adding priority items."
        assert qItem2.TargetId == "5", f"Expected second queue item ID to be 5 after adding priority items."
        assert qItem3.TargetId == "7", f"Expected third queue item ID to be 7 after adding priority items."

        # check permanent reservation
        idList = ["1", "2", "3", "4", "5", "6"]
        sqh.ClearSyncQueue(True, True)
        es.FlushIndex(sqh.Index)
        logging.info("Slight delay to complete flush (clear sync queue)...")
        time.sleep(2)
        sqh.InsertQueueBatch(idList)
        es.FlushIndex(sqh.Index)
        logging.info("Slight delay to complete flush (load sync queue)...")
        time.sleep(2)
        response = sqh.GetSyncQueueBatch()
        qItem1 = response[0][0].SyncQueueDoc # first item in batch
        qItem2 = response[0][1].SyncQueueDoc # second item in batch
        assert qItem1.TargetId == "6", f"Expected first queue item ID to be 6 when checking permanent priority items."
        assert qItem2.TargetId == "1", f"Expected second queue item ID to be 1 when checking permanent priority items."

        # clean up        
        sqh.ClearSyncQueue(True, True)
        sqh.ClearPriorityReservations()
            
        # Report
        logging.info(f"[TEST SUCCESS] {module}:{fn}")
    except AssertionError as e:
        logging.info(f"[TEST FAILURE] {module}:{fn}: {e}")

# WARNING: these tests will clear and reload sync queue and reservations for the specified source name
#ClearAndLoadQueueTest()
#logging.info("2 sec delay...")
#time.sleep(2)
#GetLockCompleteTest()
#CleanupHistoryTest()
#ClearSessionTest()
PriorityTest()