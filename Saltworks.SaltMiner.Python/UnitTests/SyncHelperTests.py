''' --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
*
* ----
'''

import os
import logging
import time
import datetime
import unittest

from Core.Application import Application
from Utility.SyncQueueHelper import SyncQueueHelper
from Core.SscClient import SscClient

module = os.path.splitext(os.path.basename(__file__))[0]


class SyncHelperTests(unittest.TestCase):
    """Unit tests for SyncQueueHelper functionality."""

    @classmethod
    def setUpClass(cls):
        """[UnitTest] Set up test fixtures before running tests in this class."""
        cls.app = Application(skipCleanFiles=True)
        cls.source_name = "SSC1"
        cls.ssc = SscClient(cls.app.Settings, cls.source_name)
        cls.reader = cls.ssc.GetProjectVersions(fields='id')
        cls.sqh = SyncQueueHelper(cls.app.Settings, cls.source_name)
        cls.sqh.BatchSize = 10
        cls.es = cls.app.GetElasticClient()

    def test_clear_and_load_queue(self):
        """Test clearing and loading sync queue."""
        # WARNING: this test will clear and reload sync queue for the specified source name
        # Act/Assert
        self.sqh.ClearSyncQueue(True, True)
        count = 0
        id_list = []
        for itm in self.reader:
            if count % 10 == 0:
                self.sqh.InsertQueueBatch(id_list, False)
                id_list = []
            id_list.append(itm['id'])
            count += 1
        if len(id_list) > 0:
            self.sqh.InsertQueueBatch(id_list, False)
        self.assertEqual(count, len(self.reader), f"Should have loaded {len(self.reader)} sync queues, but counted {count} instead.")
        logging.info(f"[TEST SUCCESS] {module}:test_clear_and_load_queue")

    def test_get_lock_complete(self):
        """Test getting, locking, and completing queue items."""
        # Act/Assert
        result = self.sqh.GetSyncQueueBatch()
        q_batch = result[0]
        total = result[1]
        self.assertGreater(total, 0, "Expected results but no data returned.")
        for q_item in q_batch:
            logging.info("Testing target %s", q_item.SyncQueueDoc.TargetId)
            r = self.sqh.SetInProgress(q_item)
            self.assertIsNotNone(r, "Expected successful lock, but None returned from SetInProgress().")
            self.assertEqual(r.SyncQueueDoc.LockId, self.sqh.SessionId, f"Expected lock id for sync queue with id {r.SyncQueueDoc.TargetId} to match session id '{self.sqh.SessionId}', but found '{r.SyncQueueDoc.LockId}' instead.")
            r = self.sqh.SetComplete(q_item)
            self.assertIsNotNone(r, "Expected successful completion, but None returned from SetComplete().")
            self.assertFalse(r.SyncQueueDoc.LockId and not r.SyncQueueDoc.Completed, f"Expected no lock id and a completed date for sync queue with id {r.SyncQueueDoc.TargetId}, but found lock id '{r.SyncQueueDoc.LockId}' and completed '{r.SyncQueueDoc.Completed}'.")

        logging.info(f"[TEST SUCCESS] {module}:test_get_lock_complete")

    def test_cleanup_history(self):
        """Test cleanup of queue history."""
        # Act/Assert
        q_item = self.sqh.GetSyncQueueBatch()[0][0]  # first item in batch
        doc = q_item.SyncQueueDoc.Dto()
        doc['completed'] = (datetime.datetime.utcnow() - datetime.timedelta(days=8)).isoformat()
        qid = q_item.Id
        self.es.IndexWithId(self.sqh.Index, qid, doc)
        self.sqh.CleanupQueueHistory()
        self.es.FlushIndex(self.sqh.Index)
        logging.info("Slight delay to complete flush...")
        time.sleep(1)
        r = self.es.Get(self.sqh.Index, qid, False)
        self.assertFalse(r['found'], "Should not have found the removed queue item.")

        logging.info(f"[TEST SUCCESS] {module}:test_cleanup_history")

    def test_clear_session(self):
        """Test clearing session locks."""
        # Arrange
        self.sqh.ClearSession()  # initial clear

        # Act/Assert
        q_item = self.sqh.GetSyncQueueBatch()[0][0]  # first item in batch
        qid = q_item.Id
        self.assertIsNotNone(self.sqh.SetInProgress(q_item), "Expected successful lock, but None returned from SetInProgress().")
        self.es.FlushIndex(self.sqh.Index)
        logging.info("Slight delay to complete flush...")
        time.sleep(1)
        self.sqh.ClearSession()
        self.es.FlushIndex(self.sqh.Index)
        logging.info("Slight delay to complete flush...")
        time.sleep(1)
        r = self.es.Get(self.sqh.Index, qid, False)
        self.assertTrue(r['found'] and not r['_source']['lock_id'], "Queue item not found in data or lock id is still set.")

        logging.info(f"[TEST SUCCESS] {module}:test_clear_session")

    def test_priority(self):
        """Test priority queue functionality."""
        # WARNING: this test will clear and reload sync queue and reservations for the specified source name
        # Arrange
        self.sqh.ClearSyncQueue(True, True)
        self.sqh.ClearPriorityReservations()
        self.es.FlushIndex(self.sqh.Index)
        logging.info("Slight delay to complete flush (this test method requires several of these)...")
        time.sleep(2)

        # Act/Assert
        # normal priority items, id order
        id_list = ["1", "2", "3", "4"]
        self.sqh.InsertQueueBatch(id_list)
        self.es.FlushIndex(self.sqh.Index)
        logging.info("Slight delay to complete flush...")
        time.sleep(2)
        response = self.sqh.GetSyncQueueBatch()
        q_item = response[0][0].SyncQueueDoc  # first item in batch
        self.assertEqual(q_item.TargetId, id_list[0], f"Expected first queue item ID to be {id_list[0]} before adding priority items.")

        # add priority items
        self.sqh.InsertQueueBatch(["7"], 4)
        self.sqh.InsertQueueBatch(["6"], 3, permanent=True)
        self.sqh.InsertQueueBatch(["5"], 4)
        self.es.FlushIndex(self.sqh.Index)
        logging.info("Slight delay to complete flush...")
        time.sleep(2)
        response = self.sqh.GetSyncQueueBatch()
        q_item1 = response[0][0].SyncQueueDoc  # first item in batch
        q_item2 = response[0][1].SyncQueueDoc  # second item in batch
        q_item3 = response[0][2].SyncQueueDoc  # third item in batch
        self.assertEqual(q_item1.TargetId, "6", "Expected first queue item ID to be 6 after adding priority items.")
        self.assertEqual(q_item2.TargetId, "5", "Expected second queue item ID to be 5 after adding priority items.")
        self.assertEqual(q_item3.TargetId, "7", "Expected third queue item ID to be 7 after adding priority items.")

        # check permanent reservation
        id_list = ["1", "2", "3", "4", "5", "6"]
        self.sqh.ClearSyncQueue(True, True)
        self.es.FlushIndex(self.sqh.Index)
        logging.info("Slight delay to complete flush (clear sync queue)...")
        time.sleep(2)
        self.sqh.InsertQueueBatch(id_list)
        self.es.FlushIndex(self.sqh.Index)
        logging.info("Slight delay to complete flush (load sync queue)...")
        time.sleep(2)
        response = self.sqh.GetSyncQueueBatch()
        q_item1 = response[0][0].SyncQueueDoc  # first item in batch
        q_item2 = response[0][1].SyncQueueDoc  # second item in batch
        self.assertEqual(q_item1.TargetId, "6", "Expected first queue item ID to be 6 when checking permanent priority items.")
        self.assertEqual(q_item2.TargetId, "1", "Expected second queue item ID to be 1 when checking permanent priority items.")

        # clean up
        self.sqh.ClearSyncQueue(True, True)
        self.sqh.ClearPriorityReservations()

        logging.info(f"[TEST SUCCESS] {module}:test_priority")


if __name__ == '__main__':
    unittest.main()