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
import logging
import time
import datetime
import unittest

from Core.Application import Application
from Utility.AsyncQueueHelper import AsyncQueueHelper
from Core.SscClient import SscClient

module = os.path.splitext(os.path.basename(__file__))[0]


class AsyncHelperTests(unittest.TestCase):
    """Unit tests for AsyncQueueHelper functionality."""

    @classmethod
    def setUpClass(cls):
        """[UnitTest] Set up test fixtures before running tests in this class."""
        cls.app = Application(skipCleanFiles=True)
        cls.source_name = "SSC1"
        cls.ssc = SscClient(cls.app.Settings, cls.source_name)
        cls.reader = cls.ssc.GetProjectVersions(fields='id')
        cls.aqh = AsyncQueueHelper(cls.app.Settings)
        cls.aqh.TargetType = "SSC"
        cls.aqh.TargetInstance = cls.source_name
        cls.aqh.BatchSize = 10
        cls.es = cls.app.GetElasticClient()

    def test_clear_and_load_queue(self):
        """Test clearing and loading async queue."""
        # WARNING: this test will clear and reload async queue for the specified source name
        # Act/Assert
        self.aqh.ClearAsyncQueue(True, True)
        count = 0
        id_list = []
        for itm in self.reader:
            if count % 10 == 0:
                self.aqh.InsertQueueBatch(id_list, False)
                id_list = []
            id_list.append(itm['id'])
            count += 1
        if len(id_list) > 0:
            self.aqh.InsertQueueBatch(id_list, False)
        self.assertEqual(count, len(self.reader), f"Should have loaded {len(self.reader)} async queues, but counted {count} instead.")
        logging.info(f"[TEST SUCCESS] {module}:test_clear_and_load_queue")

    def test_get_lock_complete(self):
        """Test getting, locking, and completing async queue items."""
        # Act/Assert
        result = self.aqh.GetAsyncQueueBatch()
        q_batch = result[0]
        total = result[1]
        self.assertGreater(total, 0, "Expected results but no data returned.")
        for q_item in q_batch:
            logging.info("Testing target %s", q_item.AsyncQueueDoc.TargetId)
            r = self.aqh.SetInProgress(q_item)
            self.assertIsNotNone(r, "Expected successful lock, but None returned from SetInProgress().")
            self.assertEqual(r.AsyncQueueDoc.LockId, self.aqh.SessionId, f"Expected lock id for async queue with id {r.AsyncQueueDoc.TargetId} to match session id '{self.aqh.SessionId}', but found '{r.AsyncQueueDoc.LockId}' instead.")
            r = self.aqh.SetComplete(q_item)
            self.assertIsNotNone(r, "Expected successful completion, but None returned from SetComplete().")
            self.assertFalse(r.AsyncQueueDoc.LockId and not r.AsyncQueueDoc.Completed, f"Expected no lock id and a completed date for async queue with id {r.AsyncQueueDoc.TargetId}, but found lock id '{r.AsyncQueueDoc.LockId}' and completed '{r.AsyncQueueDoc.Completed}'.")

        logging.info(f"[TEST SUCCESS] {module}:test_get_lock_complete")

    def test_cleanup_history(self):
        """Test cleanup of async queue history."""
        # Act/Assert
        q_item = self.aqh.GetAsyncQueueBatch()[0][0]  # first item in batch
        doc = q_item.AsyncQueueDoc.Dto()
        doc['completed'] = (datetime.datetime.utcnow() - datetime.timedelta(days=8)).isoformat()
        qid = q_item.Id
        self.es.IndexWithId(self.aqh.Index, qid, doc)
        self.aqh.CleanupQueueHistory()
        self.es.FlushIndex(self.aqh.Index)
        logging.info("Slight delay to complete flush...")
        time.sleep(1)
        r = self.es.Get(self.aqh.Index, qid, False)
        self.assertFalse(r['found'], "Should not have found the removed async queue item.")

        logging.info(f"[TEST SUCCESS] {module}:test_cleanup_history")

    def test_clear_session(self):
        """Test clearing session locks."""
        # Arrange
        self.aqh.ClearSession()  # initial clear

        # Act/Assert
        q_item = self.aqh.GetAsyncQueueBatch()[0][0]  # first item in batch
        qid = q_item.Id
        self.assertIsNotNone(self.aqh.SetInProgress(q_item), "Expected successful lock, but None returned from SetInProgress().")
        self.es.FlushIndex(self.aqh.Index)
        logging.info("Slight delay to complete flush...")
        time.sleep(1)
        self.aqh.ClearSession()
        self.es.FlushIndex(self.aqh.Index)
        logging.info("Slight delay to complete flush...")
        time.sleep(1)
        r = self.es.Get(self.aqh.Index, qid, False)
        self.assertTrue(r['found'] and not r['_source']['lock_id'], "Async queue item not found in data or lock id is still set.")

        logging.info(f"[TEST SUCCESS] {module}:test_clear_session")

    def test_priority(self):
        """Test priority queue functionality."""
        # WARNING: this test will clear and reload async queue and reservations for the specified source name
        # Arrange
        self.aqh.ClearAsyncQueue(True, True)
        self.aqh.ClearPriorityReservations()
        self.es.FlushIndex(self.aqh.Index)
        logging.info("Slight delay to complete flush (this test method requires several of these)...")
        time.sleep(2)

        # Act/Assert
        # normal priority items, id order
        id_list = ["1", "2", "3", "4"]
        self.aqh.InsertQueueBatch(id_list)
        self.es.FlushIndex(self.aqh.Index)
        logging.info("Slight delay to complete flush...")
        time.sleep(2)
        response = self.aqh.GetAsyncQueueBatch()
        q_item = response[0][0].AsyncQueueDoc  # first item in batch
        self.assertEqual(q_item.TargetId, id_list[0], f"Expected first queue item ID to be {id_list[0]} before adding priority items.")

        # add priority items
        self.aqh.InsertQueueBatch(["7"], 4)
        self.aqh.InsertQueueBatch(["6"], 3, permanent=True)
        self.aqh.InsertQueueBatch(["5"], 4)
        self.es.FlushIndex(self.aqh.Index)
        logging.info("Slight delay to complete flush...")
        time.sleep(2)
        response = self.aqh.GetAsyncQueueBatch()
        q_item1 = response[0][0].AsyncQueueDoc  # first item in batch
        q_item2 = response[0][1].AsyncQueueDoc  # second item in batch
        q_item3 = response[0][2].AsyncQueueDoc  # third item in batch
        self.assertEqual(q_item1.TargetId, "6", "Expected first queue item ID to be 6 after adding priority items.")
        self.assertEqual(q_item2.TargetId, "7", "Expected third queue item ID to be 7 after adding priority items.")
        self.assertEqual(q_item3.TargetId, "5", "Expected second queue item ID to be 5 after adding priority items.")
        

        # check permanent reservation
        id_list = ["1", "2", "3", "4", "5", "6"]
        self.aqh.ClearAsyncQueue(True, True)
        self.es.FlushIndex(self.aqh.Index)
        logging.info("Slight delay to complete flush (clear async queue)...")
        time.sleep(2)
        self.aqh.InsertQueueBatch(id_list)
        self.es.FlushIndex(self.aqh.Index)
        logging.info("Slight delay to complete flush (load async queue)...")
        time.sleep(2)
        response = self.aqh.GetAsyncQueueBatch()
        q_item1 = response[0][0].AsyncQueueDoc  # first item in batch
        q_item2 = response[0][1].AsyncQueueDoc  # second item in batch
        self.assertEqual(q_item1.TargetId, "6", "Expected first queue item ID to be 6 when checking permanent priority items.")
        self.assertEqual(q_item2.TargetId, "1", "Expected second queue item ID to be 1 when checking permanent priority items.")

        # clean up
        self.aqh.ClearAsyncQueue(True, True)
        self.aqh.ClearPriorityReservations()

        logging.info(f"[TEST SUCCESS] {module}:test_priority")

    def test_target_switching(self):
        """Test that TargetType and TargetInstance can be changed between operations."""
        original_type = self.aqh.TargetType
        original_instance = self.aqh.TargetInstance

        # Switch to a different target and verify properties update
        self.aqh.TargetType = "FOD"
        self.aqh.TargetInstance = "Fod1"
        self.assertEqual(self.aqh.TargetType, "FOD", "Expected TargetType to be 'FOD' after setting.")
        self.assertEqual(self.aqh.TargetInstance, "Fod1", "Expected TargetInstance to be 'Fod1' after setting.")

        # Restore original values
        self.aqh.TargetType = original_type
        self.aqh.TargetInstance = original_instance
        self.assertEqual(self.aqh.TargetType, original_type, "Expected TargetType to be restored to original value.")
        self.assertEqual(self.aqh.TargetInstance, original_instance, "Expected TargetInstance to be restored to original value.")

        logging.info(f"[TEST SUCCESS] {module}:test_target_switching")


if __name__ == '__main__':
    unittest.main()
