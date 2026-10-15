''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2026 Saltworks Security, LLC
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

import datetime
import logging
import os
import time
import unittest

from Core.Application import Application
from Core.QueueClient import QueueClient, QueueClientDoc, QueueClientDto, QueueClientException, QueueClientStatus

module = os.path.splitext(os.path.basename(__file__))[0]

class QueueClientTests(unittest.TestCase):
    """Integration tests for QueueClient functionality."""

    SM_QUEUE_TEMPLATE = {
        "index_patterns": ["sm_queue*"],
        "template": {
            "settings": {
                "number_of_shards": 1,
                "number_of_replicas": 0,
                "analysis": {
                    "normalizer": {
                        "lc_normalizer": {
                            "type": "custom",
                            "char_filter": [],
                            "filter": ["lowercase", "asciifolding"]
                        }
                    }
                }
            },
            "mappings": {
                "properties": {
                    "id":             {"type": "keyword"},
                    "source":         {"type": "keyword"},
                    "key":            {"type": "keyword"},
                    "status":         {"type": "keyword"},
                    "stage":          {"type": "keyword"},
                    "status_reason":  {"type": "text"},
                    "data":           {"type": "object"},
                    "created":        {"type": "date"},
                    "completed":      {"type": "date"},
                    "locked":         {"type": "date"},
                    "lock_id":        {"type": "keyword"},
                    "priority":       {"type": "integer"},
                    "change_reason":  {"type": "text"},
                    "change_trigger": {"type": "keyword"}
                }
            }
        }
    }

    @classmethod
    def setUpClass(cls):
        """Set up shared Application instance before all tests."""
        cls.app = Application()
        cls.es = cls.app.GetElasticClient()
        cls.TEST_TAG = "test"
        cls.SMALL_BATCH = 3
        # Ensure the sm_queue index template is installed so fields get proper keyword/date mappings.
        cls.es.PutTemplate("sm_queue", cls.SM_QUEUE_TEMPLATE)

    def _get_queue_client(self, batch_size=None):
        """Create a QueueClient with the test tag and optional batch size override."""
        return QueueClient(self.app, self.TEST_TAG, batch_size=batch_size or self.SMALL_BATCH)

    def _search_all(self, qc, **kwargs):
        """Call get_search and return results as a list (empty list if None)."""
        result = qc.get_search(**kwargs)
        return list(result) if result else []

    def _es_search_dtos(self, qc, size=1000, **kwargs):
        """
        Search directly via es.Search (one page, avoids search_after pagination issues)
        using get_search_body() to build the query.  Returns list of QueueClientDtos.
        """
        body = qc.get_search_body(**kwargs)
        hits = self.es.Search(qc.index_pattern, body, size, True)
        return [QueueClientDto(h) for h in hits] if hits else []

    def _find_by_key(self, items, key):
        """Find first QueueClientDto in items whose doc.key matches key."""
        return next((i for i in items if i.doc.key == key), None)

    def test_dto_init(self):
        """Test QueueClientDto initialization from Search and SearchScroll results."""
        idx = f"sm_queue_{self.TEST_TAG}_dto_test_removeme"
        self.es.DeleteIndex(idx)
        try:
            # Index a QueueClientDoc directly so we can test DTO mapping
            doc = QueueClientDoc.new("test_source", "test_key_dto_init", stage="init_stage")
            self.es.IndexWithId(idx, "test_doc_1", doc.dto())
            self.es.FlushIndex(idx)
            time.sleep(2)

            # --- Test from Search (includeLockingInfo=True) ---
            hits = self.es.Search(idx, includeLockingInfo=True)
            self.assertIsNotNone(hits, "Search should return results")
            self.assertEqual(len(hits), 1, "Should have exactly 1 document")

            dto_from_search = QueueClientDto(hits[0])
            self.assertIsNotNone(dto_from_search.doc, "QueueClientDto.doc should not be None")
            self.assertEqual(dto_from_search.doc.key, "test_key_dto_init", "doc.key should match")
            self.assertEqual(dto_from_search.doc.source, "test_source", "doc.source should match")
            self.assertEqual(dto_from_search.doc.stage, "init_stage", "doc.stage should match")
            self.assertEqual(dto_from_search.doc.status, QueueClientStatus.NEW, "doc.status should default to New")
            self.assertIsNotNone(dto_from_search.sequence_number, "sequence_number should be set from Search")
            self.assertIsNotNone(dto_from_search.primary_term, "primary_term should be set from Search")
            self.assertIsNotNone(dto_from_search.id, "id should be set")
            self.assertIsNotNone(dto_from_search.index, "index should be set")

            # --- Test from SearchScroll (includeLockingInfo=True) ---
            scroller = self.es.SearchScroll(idx, queryBody={"sort": [{"created": "asc"}]}, scrollSize=10, scrollTimeout="60s", includeLockingInfo=True)
            self.assertIsNotNone(scroller, "SearchScroll should return a scroller")
            self.assertGreater(len(scroller.Results), 0, "Scroller should have results")

            dto_from_scroll = QueueClientDto(scroller.Results[0])
            self.assertIsNotNone(dto_from_scroll.doc, "QueueClientDto.doc from scroller should not be None")
            self.assertEqual(dto_from_scroll.doc.key, "test_key_dto_init", "doc.key from scroller should match")
            self.assertEqual(dto_from_scroll.doc.source, "test_source", "doc.source from scroller should match")
            # Note: IncludeLockingInfo is set on the scroller after __init__ calls GetNext(), so the
            # first batch is fetched without locking info. Seq/pri will be None for the first page.
            # Locking info IS available from Search (tested above) and is used by get_next_queue_batch.

            logging.info(f"[TEST SUCCESS] {module}:test_dto_init")
        finally:
            try:
                self.es.DeleteIndex(idx)
            except Exception:
                pass

    def test_main_flow(self):
        """
        Integration test covering the full queue lifecycle in sequence:
          0  Init validation
          1  Insert first batch
          2  Search - verify new items
          3  get_next_queue_batch - first batch
          4  Verify locked items in progress
          5  get_next_queue_batch - second batch (locked excluded)
          6  Insert with duplicates
          7  Verify count (no duplicates added)
          8  set_progress
          9  set_complete
          9b get_search exclusions (exclude_completed, exclude_locked)
          10 Insert with completed key (new item, old not overwritten)
          11 clear_session
          12 Cleanup (in finally)
        """
        qc = self._get_queue_client()
        test_index = qc.index
        self.es.DeleteIndex(test_index)  # clean start

        try:
            # ------------------------------------------------------------------
            # Step 0: Init validation
            # ------------------------------------------------------------------
            today = datetime.datetime.now(datetime.UTC).strftime("%Y%m%d")
            self.assertEqual(qc.index, f"sm_queue_{self.TEST_TAG}_{today}",
                             "Index name should follow sm_queue_{tag}_{YYYYMMDD} pattern")
            self.assertEqual(qc.batch_size, self.SMALL_BATCH,
                             "batch_size should match constructor argument")

            with self.assertRaises(QueueClientException, msg="Empty tag should raise QueueClientException"):
                QueueClient(self.app, "")

            # batch_size=0 is handled gracefully (logged warning, default from config kept)
            qc_bad_batch = QueueClient(self.app, self.TEST_TAG, batch_size=0)
            self.assertGreater(qc_bad_batch.batch_size, 0,
                               "Invalid batch_size=0 should be ignored, falling back to config default")

            # Verify sm_queue_ prefix is stripped (index name should be identical)
            qc_prefixed = QueueClient(self.app, f"sm_queue_{self.TEST_TAG}", batch_size=self.SMALL_BATCH)
            self.assertEqual(qc_prefixed.index, qc.index,
                             "sm_queue_ prefix in tag should be stripped to produce same index name")

            # ------------------------------------------------------------------
            # Step 1: Insert 7 items (enough for 2+ batches of size 3)
            # ------------------------------------------------------------------
            keys_first = {f"key{i:02d}": {} for i in range(1, 8)}  # key01..key07
            qc.insert_queue("test", keys_first)
            self.es.FlushIndex(test_index)
            time.sleep(2)

            # ------------------------------------------------------------------
            # Step 2: Search - verify 7 new items are found and properly populated
            # ------------------------------------------------------------------
            all_items = self._es_search_dtos(qc)
            self.assertEqual(len(all_items), 7, "Should have 7 new items after first insert")
            for item in all_items:
                self.assertIsNotNone(item.doc, "Each result should have a populated doc")
                self.assertIsNotNone(item.doc.key, "Each doc should have a key")
                self.assertEqual(item.doc.status, QueueClientStatus.NEW, "All items should start as New")
            # Verify get_search() generator works and yields QueueClientDtos
            gs_items = self._search_all(qc)
            self.assertGreater(len(gs_items), 0, "get_search() should yield results")
            self.assertIsInstance(gs_items[0], QueueClientDto, "get_search() should yield QueueClientDto instances")

            # ------------------------------------------------------------------
            # Step 3: get_next_queue_batch - retrieve first batch
            # ------------------------------------------------------------------
            batch1, total = qc.get_next_queue_batch(stage="processing")
            self.assertIsNotNone(batch1, "Batch should not be None")
            self.assertEqual(len(batch1), self.SMALL_BATCH,
                             f"First batch should contain {self.SMALL_BATCH} items")
            self.assertIsNotNone(total, "Total count should not be None")
            self.assertEqual(total, 7, "Total should reflect all 7 available items")
            batch1_keys = {dto.doc.key for dto in batch1}

            # ------------------------------------------------------------------
            # Step 4: Verify locked items are In Progress
            # ------------------------------------------------------------------
            self.es.FlushIndex(test_index)
            time.sleep(2)
            all_items = self._es_search_dtos(qc)
            in_progress = [i for i in all_items if i.doc.status == QueueClientStatus.IN_PROGRESS]
            self.assertEqual(len(in_progress), self.SMALL_BATCH,
                             f"Should have {self.SMALL_BATCH} in-progress items after first batch lock")
            for item in in_progress:
                self.assertEqual(str(item.doc.lock_id), str(qc.session_id),
                                 "Locked items should carry the current session_id")
                self.assertIsNotNone(item.doc.locked, "Locked items should have a locked timestamp")
                self.assertEqual(item.doc.stage, "processing", "Locked items should have the requested stage")

            # ------------------------------------------------------------------
            # Step 5: get_next_queue_batch again - locked items must not appear
            # ------------------------------------------------------------------
            batch2, total2 = qc.get_next_queue_batch()
            self.assertIsNotNone(batch2, "Second batch should not be None")
            self.assertEqual(len(batch2), self.SMALL_BATCH,
                             f"Second batch should contain {self.SMALL_BATCH} items")
            batch2_keys = {dto.doc.key for dto in batch2}
            self.assertEqual(len(batch1_keys & batch2_keys), 0,
                             "First and second batches should not share any keys")
            self.assertIsNotNone(total2, "Total2 should not be None")
            # 7 total, 3 locked by batch1 → 4 remaining available
            self.assertEqual(total2, 4, "Total should be 4 remaining unlocked items")

            # Flush to make batch2 locks visible before duplicate check
            self.es.FlushIndex(test_index)
            time.sleep(2)

            # ------------------------------------------------------------------
            # Step 6: Insert second batch with duplicates
            #   2 keys already in queue as IN_PROGRESS + 5 new keys
            # ------------------------------------------------------------------
            dup_keys_list = list(batch2_keys)[:2]
            new_keys = [f"key{i:02d}" for i in range(8, 13)]  # key08..key12
            insert_mixed = {k: {} for k in dup_keys_list + new_keys}
            qc.insert_queue("test", insert_mixed)
            self.es.FlushIndex(test_index)
            time.sleep(2)

            # ------------------------------------------------------------------
            # Step 7: Verify total count excludes duplicates
            #   7 original + 5 new = 12 (2 duplicate IN_PROGRESS keys not re-inserted)
            # ------------------------------------------------------------------
            all_items = self._es_search_dtos(qc)
            self.assertEqual(len(all_items), 12,
                             "Should have 12 items: 7 original + 5 new (2 duplicate keys skipped)")

            # ------------------------------------------------------------------
            # Step 8: set_progress on a locked item
            # ------------------------------------------------------------------
            target = batch1[0]
            result = qc.set_progress(target, stage="stage2", data={"progress": 50})
            self.assertIsNotNone(result, "set_progress should return the updated DTO")
            self.es.FlushIndex(test_index)
            time.sleep(2)
            all_items = self._es_search_dtos(qc)
            updated = self._find_by_key(all_items, target.doc.key)
            self.assertIsNotNone(updated, "Updated item should be found via search")
            self.assertEqual(updated.doc.stage, "stage2", "Stage should be updated")
            self.assertEqual(updated.doc.data, {"progress": 50}, "Data should be updated")

            # ------------------------------------------------------------------
            # Step 9: set_complete on another locked item
            # ------------------------------------------------------------------
            to_complete = batch1[1]
            result = qc.set_complete(to_complete, stage="done")
            self.assertIsNotNone(result, "set_complete should return the updated DTO")
            self.es.FlushIndex(test_index)
            time.sleep(2)
            all_items = self._es_search_dtos(qc)
            completed_item = next(
                (i for i in all_items
                 if i.doc.key == to_complete.doc.key and i.doc.status == QueueClientStatus.COMPLETE),
                None
            )
            self.assertIsNotNone(completed_item, "Completed item should appear in search results")
            self.assertIsNotNone(completed_item.doc.completed, "completed timestamp should be set")
            self.assertIsNone(completed_item.doc.lock_id, "lock_id should be cleared on completion")

            # ------------------------------------------------------------------
            # Step 9b: get_search with exclude_completed / exclude_locked
            # Uses _es_search_dtos (via get_search_body) for reliable full-result verification.
            # ------------------------------------------------------------------
            completed_key = to_complete.doc.key

            no_completed = self._es_search_dtos(qc, exclude_completed=True)
            false_positives = [
                i for i in no_completed
                if i.doc.key == completed_key and i.doc.status == QueueClientStatus.COMPLETE
            ]
            self.assertEqual(len(false_positives), 0,
                             "Completed item should be excluded when exclude_completed=True")

            with_completed = self._es_search_dtos(qc, exclude_completed=False)
            found_completed = [
                i for i in with_completed
                if i.doc.key == completed_key and i.doc.status == QueueClientStatus.COMPLETE
            ]
            self.assertGreater(len(found_completed), 0,
                               "Completed item should appear when exclude_completed=False")

            no_locked = self._es_search_dtos(qc, exclude_locked=True)
            still_locked_in_results = [i for i in no_locked if i.doc.lock_id is not None]
            self.assertEqual(len(still_locked_in_results), 0,
                             "Locked items should be excluded when exclude_locked=True")

            # Also verify get_search() generator respects the exclusion flags
            gs_no_completed = self._search_all(qc, exclude_completed=True)
            gs_false_pos = [i for i in gs_no_completed if i.doc.status == QueueClientStatus.COMPLETE]
            self.assertEqual(len(gs_false_pos), 0,
                             "get_search(exclude_completed=True) must not yield completed items")

            # ------------------------------------------------------------------
            # Step 10: Insert with the completed item's key
            #   A NEW item should be created; the old COMPLETE item must not be overwritten
            # ------------------------------------------------------------------
            qc.insert_queue("test", {completed_key: {}})
            self.es.FlushIndex(test_index)
            time.sleep(2)
            all_items = self._es_search_dtos(qc)
            items_for_completed_key = [i for i in all_items if i.doc.key == completed_key]
            self.assertEqual(len(items_for_completed_key), 2,
                             "Should have 2 items for the completed key: old COMPLETE + new NEW")
            statuses = {i.doc.status for i in items_for_completed_key}
            self.assertIn(QueueClientStatus.COMPLETE, statuses,
                          "Old completed item should still exist")
            self.assertIn(QueueClientStatus.NEW, statuses,
                          "New item with same key should have been inserted")

            # ------------------------------------------------------------------
            # Step 11: clear_session - all session-locked items should be unlocked
            # ------------------------------------------------------------------
            still_locked_before = [i for i in self._es_search_dtos(qc) if i.doc.lock_id is not None]
            self.assertGreater(len(still_locked_before), 0,
                               "Should have locked items remaining before clear_session")

            qc.clear_session()
            self.es.FlushIndex(test_index)
            time.sleep(2)

            remaining_locked = [i for i in self._es_search_dtos(qc) if i.doc.lock_id is not None]
            self.assertEqual(len(remaining_locked), 0,
                             "No items should have a lock_id after clear_session")

            logging.info(f"[TEST SUCCESS] {module}:test_main_flow")

        finally:
            # Step 12: Cleanup test indexes
            try:
                self.es.DeleteIndex(test_index)
            except Exception:
                pass


if __name__ == "__main__":
    unittest.main()
