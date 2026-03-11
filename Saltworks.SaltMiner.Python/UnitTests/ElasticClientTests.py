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
import datetime
import time
import unittest

from elasticsearch.helpers.actions import BulkIndexError
from elasticsearch import NotFoundError

from Core.Application import Application
from Core.DictUtils import DictUtils

module = os.path.splitext(os.path.basename(__file__))[0]


class ElasticClientTests(unittest.TestCase):
    """Unit tests for ElasticClient functionality."""

    @classmethod
    def setUpClass(cls):
        """[UnitTest] Set up test fixtures before running tests in this class."""
        cls.app = Application()
        cls.es = cls.app.GetElasticClient()

    def setUp(self):
        """[UnitTest] Set up test fixtures before each test method."""
        pass

    def tearDown(self):
        """[UnitTest] Clean up after each test method."""
        pass

    def _get_test_index_name(self):
        """Generate a unique index name for the current test method."""
        test_name = self._testMethodName
        return f"test_py_elastic_client_{test_name}_remove_me"

    def test_connection(self):
        """Test Elasticsearch connection."""
        self.assertTrue(self.es.PingServer(), "Ping failed")
        logging.info("Elasticsearch connection succeeded.")
        logging.info(f"[TEST SUCCESS] {module}:test_connection")

    def test_update_by_query(self):
        """Test UpdateByQuery functionality."""
        # Create a temporary test index and populate with documents
        idx = self._get_test_index_name()
        self.es.DeleteIndex(idx)
        try:
            for i in range(1, 6):
                self.es.IndexWithId(idx, str(i), { "id": i, "status": "old", "value": i })
            self.es.IndexWithId(idx, str(i), { "id": 7, "status": "nope", "value": 7 })
            self.es.FlushIndex(idx)
            time.sleep(2)

            # Build an UpdateByQuery body that includes both script and query nodes
            body = {
                "script": {
                    "source": "ctx._source.status = 'updated'",
                    "lang": "painless"
                },
                "query": {"term": { "status": "old" } }
            }

            rsp = self.es.UpdateByQuery(idx, body)
            # Some clients return the full response; check completed when present
            if isinstance(rsp, dict) and 'completed' in rsp:
                self.assertTrue(rsp['completed'], "UpdateByQuery should complete successfully")

            # Verify all documents were updated
            self.es.FlushIndex(idx)
            results = self.es.Search(idx, navToData=True)
            self.assertGreater(len(results), 0, "Should have documents to verify update")
            found = False
            for doc in results:
                self.assertNotEqual(DictUtils.GetValue(doc, '_source.status'), 'old', "Document status should have been updated")
                if DictUtils.GetValue(doc, '_source.status') == 'nope':
                    found = True
            self.assertTrue(found, "Document with status 'nope' should remain unchanged")

            logging.info(f"[TEST SUCCESS] {module}:test_update_by_query")
        finally:
            try:
                self.es.DeleteIndex(idx)
            except Exception:
                pass

    def test_search_scroll(self):
        """Test SearchScroll functionality."""
        # Create a temporary index and populate enough documents to require multiple scroll batches
        idx = self._get_test_index_name()
        self.es.DeleteIndex(idx)
        try:
            total_docs = 120
            for i in range(1, total_docs + 1):
                self.es.BulkSendBatch(idx, {"id": i, "name": f"proj_{i}"}, batchSize=1000)
            self.es.BulkSendBatch(None, None)  # finalize bulk
            self.es.FlushIndex(idx)
            time.sleep(2) # give it a chance to settle...

            scroll = self.es.SearchScroll(idx, scrollSize=50)
            batch = 1
            total_seen = 0
            while scroll.Results:
                total_seen += len(scroll.Results)
                logging.info(f"Batch {batch}, {len(scroll.Results)} result(s). First project id: '{scroll.Results[0]['_source']['id']}'")
                scroll.GetNext()
                batch += 1
            self.assertGreater(batch, 1, "Should have returned more than 1 batch in scroller")
            self.assertEqual(total_seen, total_docs, "Should have seen all indexed documents via scroller")
            logging.info(f"[TEST SUCCESS] {module}:test_search_scroll")
        finally:
            try:
                self.es.DeleteIndex(idx)
            except Exception:
                pass

    def test_search_scroll_after(self):
        """Test SearchScroll with after parameter."""
        # Create a temporary index and populate documents for scroller using 'after' behavior
        idx = self._get_test_index_name()
        self.es.DeleteIndex(idx)
        try:
            total_docs = 120
            for i in range(1, total_docs + 1):
                self.es.BulkSendBatch(idx, {"id": i, "name": f"proj_{i}"}, batchSize=1000)
            self.es.BulkSendBatch(None, None)  # finalize bulk
            self.es.FlushIndex(idx)
            time.sleep(2) # give it a chance to settle...

            scroll = self.es.SearchScroll(idx, scrollSize=50, scrollTimeout=None)
            batch = 1
            startId = scroll.Results[0]['_id'] if scroll.Results else None
            endId = None
            while scroll.Results:
                logging.info(f"Batch {batch}, {len(scroll.Results)} result(s). First project id: '{scroll.Results[0]['_source']['id']}'")
                endId = scroll.Results[0]['_id']
                scroll.GetNext()
                batch += 1
            self.assertGreater(batch, 1, "Should have returned more than 1 batch in scroller")
            self.assertIsNotNone(startId, "Start ID should not be None")
            self.assertIsNotNone(endId, "End ID should not be None")
            self.assertNotEqual(startId, endId, "Scroller not returning successive batches")
            logging.info(f"[TEST SUCCESS] {module}:test_search_scroll_after")
        finally:
            try:
                self.es.DeleteIndex(idx)
            except Exception:
                pass

    def test_create_only(self):
        """Test IndexWithId with createOnly parameter."""
        # Arrange
        idx = self._get_test_index_name()
        doc = {"field": "value", "id": 1}
        self.es.DeleteIndex(idx)  # just in case it exists

        try:
            # Act/Assert
            rsp = self.es.IndexWithId(idx, "1", doc, createOnly=True)
            self.assertEqual(rsp['result'], 'created', "First index operation should have succeeded")
            rsp = self.es.IndexWithId(idx, "1", doc, createOnly=True)
            self.assertEqual(rsp['result'], 'noop', "Second index operation should have generated a noop result")
            logging.info(f"[TEST SUCCESS] {module}:test_create_only")
        finally:
            # Cleanup
            try:
                self.es.DeleteIndex(idx)
            except Exception as e:
                logging.info(f"[TEST CLEANUP FAILURE] {module}:test_create_only: {e}")

    def test_search_and_lock(self):
        """Test search with locking information and update with locking."""
        # Arrange
        idx = self._get_test_index_name()
        doc = {"field": "value", "id": 1}
        self.es.DeleteIndex(idx)  # just in case it exists
        self.es.IndexWithId(idx, "1", doc)
        time.sleep(2)

        try:
            # Act/Assert
            rsp = self.es.Search(idx, includeLockingInfo=True)
            dto = rsp[0]
            doc = dto['_source']
            doc['locked'] = datetime.datetime.utcnow().isoformat()
            seq = dto['_seq_no']
            pri = dto['_primary_term']
            upd = self.es.UpdateWithLocking(idx, doc, "1", seq, pri)
            self.assertEqual(upd['result'], "updated", "First update should succeed")
            upd = self.es.UpdateWithLocking(idx, doc, "1", seq, pri)
            self.assertEqual(upd['result'], "noop", "Second update should fail")
            logging.info(f"[TEST SUCCESS] {module}:test_search_and_lock")
        finally:
            # Cleanup
            try:
                self.es.DeleteIndex(idx)
            except Exception as e:
                logging.info(f"[TEST CLEANUP FAILURE] {module}:test_search_and_lock: {e}")

    def test_bulk(self):
        """Test bulk insert operations."""
        # Arrange
        idx = self._get_test_index_name()
        docs = [
            self.es.BulkInsertDocument(idx, {"field": "value1", "id": 1}, "1", "create"),
            self.es.BulkInsertDocument(idx, {"field": "value2", "id": 2}, "2", "create")
        ]
        self.es.DeleteIndex(idx)  # just in case it exists

        try:
            # Act/Assert
            rsp = self.es.BulkInsert(docs)
            self.assertEqual(rsp[0], 2, "Response should indicate 2 & 0")
            rsp = self.es.BulkInsert(docs, False)
            self.assertEqual(rsp[0], 0, "Response on second insert should indicate 0 & 2")
            self.assertEqual(rsp[1], 2, "Response on second insert should indicate 0 & 2")
            logging.info(f"[TEST SUCCESS] {module}:test_bulk")
        except BulkIndexError as e:
            self.fail(f"BulkIndexError: {e.args[0]}")
        finally:
            # Cleanup
            try:
                self.es.DeleteIndex(idx)
            except Exception as e:
                logging.info(f"[TEST CLEANUP FAILURE] {module}:test_bulk: {e}")

    def test_bulk_send_batch(self):
        """Test bulk send batch operations."""
        # Arrange
        idx = self._get_test_index_name()
        self.es.DeleteIndex(idx)  # just in case it exists

        try:
            # Act/Assert
            for i in range(1, 10000):
                self.es.BulkSendBatch(idx, {"field": "value", "id": i}, batchSize=3000)
            time.sleep(2)
            count = self.es.Count(idx)
            self.assertEqual(count, 9000, "Count should be 9k after loop but before final send")
            self.es.BulkSendBatch(None, None)
            time.sleep(2)
            count = self.es.Count(idx)
            self.assertEqual(count, 9999, "Count should be 10k after final send")
            logging.info(f"[TEST SUCCESS] {module}:test_bulk_send_batch")
        except BulkIndexError as e:
            self.fail(f"BulkIndexError: {e.args[0]}")
        finally:
            # Cleanup
            try:
                self.es.DeleteIndex(idx)
            except Exception as e:
                logging.info(f"[TEST CLEANUP FAILURE] {module}:test_bulk_send_batch: {e}")

    def test_crud_operations(self):
        """Test Create, Read, Update, Delete operations and related functionality."""
        # Arrange
        idx = self._get_test_index_name()
        self.es.DeleteIndex(idx)  # just in case it exists

        try:
            # CREATE operations
            # Test 1: Index existence check (before creation)
            exists = self.es.IndexExists(idx)
            self.assertFalse(exists, "Index should not exist before creation")

            # Test 2: Index with ID (Create)
            doc_id_1 = "test_doc_1"
            doc_1 = {"field": "value", "id": 1, "name": "test_document"}
            result = self.es.IndexWithId(idx, doc_id_1, doc_1)
            self.assertEqual(result['result'], 'created', "Document should be created")
            time.sleep(1)  # Allow indexing to complete

            # Test 3: Index existence check (after creation)
            exists = self.es.IndexExists(idx)
            self.assertTrue(exists, "Index should exist after document is indexed")

            # Test 4: Index without ID (auto-generated ID)
            doc_auto = {"field": "value", "id": 2, "name": "auto_id_doc"}
            result_auto = self.es.Index(idx, doc_auto)
            self.assertIn('result', result_auto, "Result should contain 'result' key")
            self.assertIn(result_auto['result'], ['created', 'updated'], "Result should be 'created' or 'updated'")
            self.assertIn('_id', result_auto, "Result should contain '_id' key")
            auto_id = result_auto['_id']
            self.assertIsNotNone(auto_id, "Auto-generated ID should not be None")
            time.sleep(1)

            # Test 5: Index with createOnly=True
            doc_create_only = {"field": "value2", "id": 3}
            result_create_only = self.es.Index(idx, doc_create_only, createOnly=True)
            self.assertEqual(result_create_only['result'], 'created', "First index with createOnly should succeed")
            time.sleep(1)

            # Test 6: Index multiple documents for GetIndex testing
            for i in range(4, 7):
                self.es.IndexWithId(idx, str(i), {"id": i, "name": f"doc_{i}", "value": i * 10})
            time.sleep(1)

            # READ operations
            # Test 7: Get document by ID
            result_get = self.es.Get(idx, doc_id_1)
            self.assertTrue(result_get['found'], "Document should be found")
            self.assertEqual(result_get['_id'], doc_id_1, "Document ID should match")
            self.assertEqual(result_get['_source']['field'], doc_1['field'], "Document field should match")
            self.assertEqual(result_get['_source']['name'], doc_1['name'], "Document name should match")

            # Test 8: Get document with auto-generated ID
            get_result_auto = self.es.Get(idx, auto_id)
            self.assertTrue(get_result_auto['found'], "Document with auto-generated ID should be found")
            self.assertEqual(get_result_auto['_source']['name'], doc_auto['name'], "Document content should match")

            # Test 9: Get with raiseNotFoundError=False
            result_not_found = self.es.Get(idx, "nonexistent_id", raiseNotFoundError=False)
            self.assertFalse(result_not_found['found'], "Document should not be found")

            # Test 10: Get with raiseNotFoundError=True (should raise exception)
            with self.assertRaises(Exception):
                self.es.Get(idx, "nonexistent_id", raiseNotFoundError=True)

            # Test 11: GetIndex with default parameters
            results = DictUtils.GetValue(self.es.GetIndex(idx).body, 'hits.hits', [])
            self.assertIsNotNone(results, "Results should not be None")
            self.assertGreater(len(results), 0, "Should return at least one document")

            # Test 12: GetIndex with limit
            results_limited = DictUtils.GetValue(self.es.GetIndex(idx, limit=3).body, 'hits.hits', [])
            self.assertLessEqual(len(results_limited), 3, "Should return at most 3 documents with limit=3")

            # Test 13: GetIndex with sort descending
            results_sorted_desc = DictUtils.GetValue(self.es.GetIndex(idx, sort="id:desc", limit=10).body, 'hits.hits', [])
            self.assertIsNotNone(results_sorted_desc, "Sorted results should not be None")
            if len(results_sorted_desc) > 1:
                first_id = results_sorted_desc[0]['_source']['id']
                second_id = results_sorted_desc[1]['_source']['id']
                self.assertGreaterEqual(first_id, second_id, "Results should be sorted in descending order")

            # Test 14: GetIndex with sort ascending
            results_sorted_asc = DictUtils.GetValue(self.es.GetIndex(idx, sort="id:asc", limit=10).body, 'hits.hits', [])
            if len(results_sorted_asc) > 1:
                first_id = results_sorted_asc[0]['_source']['id']
                second_id = results_sorted_asc[1]['_source']['id']
                self.assertLessEqual(first_id, second_id, "Results should be sorted in ascending order")

            # UPDATE operations
            # Test 15: Update document
            doc_1_updated = doc_1.copy()
            doc_1_updated['name'] = "updated_document"
            result_update = self.es.UpdateDoc(idx, doc_id_1, doc_1_updated)
            self.assertIn('result', result_update, "Update result should contain 'result' key")
            time.sleep(1)

            # Verify update
            result_get_updated = self.es.Get(idx, doc_id_1)
            self.assertEqual(result_get_updated['_source']['name'], "updated_document", "Document should be updated")

            # DELETE operations
            # Test 16: Delete by ID
            doc_id_to_delete = "4"
            delete_result = self.es.DeleteById(idx, doc_id_to_delete)
            self.assertEqual(delete_result['result'], 'deleted', "Document should be deleted")
            time.sleep(1)  # Allow deletion to complete

            # Test 17: Verify document is gone
            get_result_deleted = self.es.Get(idx, doc_id_to_delete, raiseNotFoundError=False)
            self.assertFalse(get_result_deleted['found'], "Document should not exist after deletion")

            # Test 18: Delete with ignoreNotFound=True
            try:
                self.es.DeleteById(idx, "nonexistent_id", ignoreNotFound=True)
            except:
                self.assertFalse(True, "Should not raise error if document doesn't exist and ignoreNotFound=True")

            # Test 19: Delete with ignoreMissingIndex=True
            try:
                self.es.DeleteById("nonexistent_index", doc_id_1, ignoreMissingIndex=True)
            except:
                self.assertFalse(True, "Should not raise error when index doesn't exist and ignoreMissingIndex=True")

            logging.info(f"[TEST SUCCESS] {module}:test_crud_operations")
        finally:
            # Cleanup
            try:
                self.es.DeleteIndex(idx)
            except Exception as e:
                logging.info(f"[TEST CLEANUP FAILURE] {module}:test_crud_operations: {e}")

    def test_delete_by_query(self):
        """Test delete by query operations."""
        # Arrange
        idx = self._get_test_index_name()
        self.es.DeleteIndex(idx)  # just in case it exists

        # Index multiple documents with different field values
        for i in range(1, 11):
            self.es.IndexWithId(idx, str(i), {"id": i, "category": "test" if i % 2 == 0 else "other", "value": i * 10})
        time.sleep(1)  # Allow indexing to complete

        try:
            # Act/Assert - Test DeleteByQuery
            # Delete documents with category="test" (should delete even numbers: 2, 4, 6, 8, 10)
            query_body = {"query": {"match": {"category": "test"}}}
            result = self.es.DeleteByQuery(idx, query_body, flushAfter=True, wait=True)
            self.assertIsNotNone(result, "DeleteByQuery should return a result")
            time.sleep(1)

            # Verify deletion - should have 5 documents remaining (odd numbers)
            count = self.es.Count(idx)
            self.assertEqual(count, 5, "Should have 5 documents remaining after deleting 'test' category")

            # Test DeleteByMatchQuery
            # Delete documents with value=30 (should delete document with id=3)
            filters = {"value": 30}
            self.es.DeleteByMatchQuery(idx, filters)
            time.sleep(1)

            # Verify deletion - should have 4 documents remaining
            count = self.es.Count(idx)
            self.assertEqual(count, 4, "Should have 4 documents remaining after DeleteByMatchQuery")

            # Test DeleteByMatchQuery with ignoreMissingIndex=True
            result = self.es.DeleteByMatchQuery("nonexistent_index", {"field": "value"}, ignoreMissingIndex=True)
            self.assertIsNone(result, "Should return None when index doesn't exist and ignoreMissingIndex=True")

            # Test DeleteAllByQuery
            # Delete all remaining documents
            self.es.DeleteAllByQuery(idx, ignoreMissingIndex=False)
            time.sleep(1)

            # Verify all documents are deleted
            count = self.es.Count(idx)
            self.assertEqual(count, 0, "Should have 0 documents after DeleteAllByQuery")

            # Test DeleteByQuery with ignoreMissingIndex=True
            result = self.es.DeleteByQuery("nonexistent_index", {"query": {"match_all": {}}}, ignoreMissingIndex=True)
            self.assertIsNone(result, "Should return None when index doesn't exist and ignoreMissingIndex=True")

            logging.info(f"[TEST SUCCESS] {module}:test_delete_by_query")
        finally:
            # Cleanup
            try:
                self.es.DeleteIndex(idx)
            except Exception as e:
                logging.info(f"[TEST CLEANUP FAILURE] {module}:test_delete_by_query: {e}")

    def test_search_operations(self):
        """Test search operations with various parameters."""
        # Arrange
        idx = self._get_test_index_name()
        self.es.DeleteIndex(idx)  # just in case it exists

        # Index multiple documents
        for i in range(1, 6):
            self.es.IndexWithId(idx, str(i), {"id": i, "name": f"doc_{i}", "category": "A" if i % 2 == 0 else "B", "value": i * 10})
        time.sleep(1)  # Allow indexing to complete

        try:
            # Act/Assert - Test Search with navToData=False (for aggregations)
            query_body = {
                "query": {"match_all": {}},
                "aggs": {
                    "categories": {
                        "terms": {"field": "category.keyword"}
                    }
                }
            }
            result = self.es.Search(idx, queryBody=query_body, navToData=False)
            self.assertIsNotNone(result, "Search result should not be None")
            self.assertIn('hits', result, "Result should contain 'hits' key when navToData=False")
            self.assertIn('aggregations', result, "Result should contain 'aggregations' key when navToData=False")

            # Test Search with custom queryBody
            query_body_match = {"query": {"match": {"category": "A"}}}
            result_match = self.es.Search(idx, queryBody=query_body_match, size=10)
            self.assertIsNotNone(result_match, "Search with custom query should return results")
            self.assertGreater(len(result_match), 0, "Should return at least one matching document")
            # Verify all results have category="A" (even numbers: 2, 4)
            for doc in result_match:
                self.assertEqual(doc['_source']['category'], "A", "All results should have category='A'")

            # Test Search with custom size
            result_small = self.es.Search(idx, size=2)
            self.assertLessEqual(len(result_small), 2, "Should return at most 2 documents with size=2")

            # Test RunMatchQuery
            filters = {"category": "B"}
            result_match_query = self.es.RunMatchQuery(idx, filters)
            self.assertIsNotNone(result_match_query, "RunMatchQuery should return results")
            # Verify all results have category="B" (odd numbers: 1, 3, 5)
            hits = DictUtils.GetValue(result_match_query.body, 'hits.hits', [])
            for doc in hits:
                self.assertEqual(DictUtils.GetValue(doc, '_source.category', ''), "B", "All results should have category='B'")

            # Test RunMatchQuery with multiple filters
            filters_multi = {"category": "A", "value": 20}
            result_multi = self.es.RunMatchQuery(idx, filters_multi)
            self.assertIsNotNone(result_multi, "RunMatchQuery with multiple filters should return results")

            logging.info(f"[TEST SUCCESS] {module}:test_search_operations")
        finally:
            # Cleanup
            try:
                self.es.DeleteIndex(idx)
            except Exception as e:
                logging.info(f"[TEST CLEANUP FAILURE] {module}:test_search_operations: {e}")

    def test_index_management(self):
        """Test index management operations."""
        # Arrange
        idx = self._get_test_index_name()
        self.es.DeleteIndex(idx)  # just in case it exists

        # Create index by indexing a document
        self.es.IndexWithId(idx, "1", {"field": "value", "id": 1})
        time.sleep(1)  # Allow indexing to complete

        try:
            # Act/Assert - Test FlushIndex
            self.es.FlushIndex(idx)
            # Verify index still exists after flush
            exists = self.es.IndexExists(idx)
            self.assertTrue(exists, "Index should still exist after flush")

            # Test RefreshIndex
            self.es.RefreshIndex(idx)
            # Verify index still exists after refresh
            exists = self.es.IndexExists(idx)
            self.assertTrue(exists, "Index should still exist after refresh")

            # Test GetIndexMapping
            mapping = self.es.GetIndexMapping(idx)
            self.assertIsNotNone(mapping, "GetIndexMapping should return a mapping")
            self.assertIn(idx, mapping, "Mapping should contain the index name")
            self.assertIn('mappings', mapping[idx], "Mapping should contain 'mappings' key")

            # Add more documents and test flush/refresh again
            self.es.IndexWithId(idx, "2", {"field": "value2", "id": 2})
            self.es.FlushIndex(idx)
            self.es.RefreshIndex(idx)
            time.sleep(1)

            # Verify both documents exist
            count = self.es.Count(idx)
            self.assertEqual(count, 2, "Should have 2 documents after flush and refresh")

            logging.info(f"[TEST SUCCESS] {module}:test_index_management")
        finally:
            # Cleanup
            try:
                self.es.DeleteIndex(idx)
            except Exception as e:
                logging.info(f"[TEST CLEANUP FAILURE] {module}:test_index_management: {e}")

    def test_count(self):
        """Test Count functionality."""
        # Arrange
        idx = self._get_test_index_name()
        self.es.DeleteIndex(idx)  # just in case it exists

        try:
            # Act/Assert - Test Count on non-existent index (should return 0)
            try:
                self.es.Count(idx)
            except Exception as e:
                self.assertIsInstance(e, NotFoundError, "Should raise NotFoundError for non-existent index")
            count_empty = self.es.Count(idx, suppressErrorOnMissingIndex=True)
            self.assertEqual(count_empty, 0, "Count should return 0 for non-existent index")

            # Index multiple documents
            for i in range(1, 111):
                self.es.BulkSendBatch(idx, {"id": i, "category": "A" if i % 2 == 0 else "B", "value": i * 10}, batchSize=1000)
            self.es.BulkSendBatch(None, None)  # finalize bulk
            self.es.FlushIndex(idx)
            time.sleep(1)  # Allow indexing to complete

            # Test Count without query (all documents)
            count_all = self.es.Count(idx)
            self.assertEqual(count_all, 110, "Count should return 110 after indexing 110 documents")

            # Test Count with query body (filter for category="A")
            query_body = {"query": {"match": {"category": "A"}}}
            count_filtered = self.es.Count(idx, query_body)
            self.assertEqual(count_filtered, 55, "Count should return 55 for documents with category='A'")

            # Test Count with different filter (value > 50)
            query_range = {"query": {"range": {"value": {"gt": 50}}}}
            count_range = self.es.Count(idx, query_range)
            self.assertEqual(count_range, 105, "Count should return 105 for documents with value > 50")

            logging.info(f"[TEST SUCCESS] {module}:test_count")
        finally:
            # Cleanup
            try:
                self.es.DeleteIndex(idx)
            except Exception as e:
                logging.info(f"[TEST CLEANUP FAILURE] {module}:test_count: {e}")

    def test_aliases(self):
        """Test alias operations."""
        # Arrange
        idx = self._get_test_index_name()
        alias_name = "test_alias_remove_me"
        self.es.DeleteIndex(idx)  # just in case it exists

        # Create index by indexing a document
        self.es.IndexWithId(idx, "1", {"field": "value", "id": 1})
        time.sleep(1)  # Allow indexing to complete

        try:
            # Act/Assert - Test PutAlias (create alias)
            alias_body = {"filter": {"term": {"field": "value"}}}
            self.es.PutAlias(idx, alias_name, alias_body, force=True)
            time.sleep(1)

            # Test GetAlias
            alias_data = self.es.GetAlias(idx, name=alias_name)
            self.assertIsNotNone(alias_data, "GetAlias should return alias data")
            self.assertIn(idx, alias_data, "Alias data should contain the index")
            self.assertIn('aliases', alias_data[idx], "Alias data should contain 'aliases' key")
            self.assertIn(alias_name, alias_data[idx]['aliases'], "Alias should be found in aliases")

            # Test GetAlias without name (get all aliases for index)
            all_aliases = self.es.GetAlias(idx)
            self.assertIsNotNone(all_aliases, "GetAlias should return alias data even without name parameter")

            # Test PutAlias with empty body
            alias_name2 = "test_alias2_remove_me"
            self.es.PutAlias(idx, alias_name2, '', force=True)
            time.sleep(1)

            # Verify second alias exists
            alias_data2 = self.es.GetAlias(idx, name=alias_name2)
            self.assertIsNotNone(alias_data2, "Second alias should be created")

            # Test PutAlias with force=False when alias already exists (should not update)
            # This should not raise an error, but also shouldn't update
            self.es.PutAlias(idx, alias_name, alias_body, force=False)
            time.sleep(1)

            # Verify alias still exists
            alias_data_check = self.es.GetAlias(idx, name=alias_name)
            self.assertIsNotNone(alias_data_check, "Alias should still exist after PutAlias with force=False")

            logging.info(f"[TEST SUCCESS] {module}:test_aliases")
        finally:
            # Cleanup - Note: Aliases are automatically deleted when index is deleted
            try:
                self.es.DeleteIndex(idx)
            except Exception as e:
                logging.info(f"[TEST CLEANUP FAILURE] {module}:test_aliases: {e}")

    def test_pit_open_close(self):
        """Test OpenPit returns a pit_id string and ClosePit closes it without error."""
        idx = self._get_test_index_name()
        self.es.DeleteIndex(idx)
        try:
            self.es.IndexWithId(idx, "1", {"id": 1, "name": "test"})
            self.es.FlushIndex(idx)

            pit_id = self.es.OpenPit(idx)
            self.assertIsNotNone(pit_id, "OpenPit should return a pit_id")
            self.assertIsInstance(pit_id, str, "pit_id should be a string")
            self.assertGreater(len(pit_id), 0, "pit_id should not be empty")

            self.es.ClosePit(pit_id)
            logging.info(f"[TEST SUCCESS] {module}:test_pit_open_close")
        finally:
            try:
                self.es.DeleteIndex(idx)
            except Exception:
                pass

    def test_resilient_search_with_pit(self):
        """Test ResilientSearch accepts pitId and returns a response with pit_id."""
        idx = self._get_test_index_name()
        self.es.DeleteIndex(idx)
        try:
            for i in range(1, 6):
                self.es.IndexWithId(idx, str(i), {"id": i, "name": f"doc_{i}"})
            self.es.FlushIndex(idx)

            pit_id = self.es.OpenPit(idx)
            try:
                response = self.es.ResilientSearch(None, {}, size=10, pitId=pit_id, pitKeepAlive="1m")
                self.assertIsNotNone(response, "ResilientSearch with pit should return a response")
                self.assertIn("pit_id", response, "Response should contain pit_id when using PIT")
                hits = response.get("hits", {}).get("hits", [])
                self.assertGreater(len(hits), 0, "Should return documents when searching with PIT")
                updated_pit_id = response["pit_id"]
                self.assertIsInstance(updated_pit_id, str, "Updated pit_id should be a string")
            finally:
                self.es.ClosePit(pit_id)

            logging.info(f"[TEST SUCCESS] {module}:test_resilient_search_with_pit")
        finally:
            try:
                self.es.DeleteIndex(idx)
            except Exception:
                pass

    def test_search_with_pit_returns_tuple(self):
        """Test Search with pitId returns (hits, pit_id) tuple."""
        idx = self._get_test_index_name()
        self.es.DeleteIndex(idx)
        try:
            for i in range(1, 4):
                self.es.IndexWithId(idx, str(i), {"id": i, "name": f"doc_{i}"})
            self.es.FlushIndex(idx)

            pit_id = self.es.OpenPit(idx)
            try:
                result = self.es.Search(None, size=10, pitId=pit_id, pitKeepAlive="1m")
                self.assertIsInstance(result, tuple, "Search with pitId should return a tuple")
                self.assertEqual(len(result), 2, "Tuple should have 2 elements: (hits, pit_id)")
                hits, new_pit_id = result
                self.assertIsNotNone(hits, "hits should not be None")
                self.assertIsInstance(hits, list, "hits should be a list")
                self.assertGreater(len(hits), 0, "Should return documents")
                self.assertIsNotNone(new_pit_id, "new_pit_id should not be None")
                self.assertIsInstance(new_pit_id, str, "new_pit_id should be a string")
            finally:
                self.es.ClosePit(pit_id)

            # Verify Search without pitId still returns a plain list (no regression)
            plain_result = self.es.Search(idx, size=10)
            self.assertIsInstance(plain_result, list, "Search without pitId should still return a list")

            logging.info(f"[TEST SUCCESS] {module}:test_search_with_pit_returns_tuple")
        finally:
            try:
                self.es.DeleteIndex(idx)
            except Exception:
                pass

    def test_getindex_with_pit(self):
        """Test GetIndex with pitId returns raw response containing pit_id."""
        idx = self._get_test_index_name()
        self.es.DeleteIndex(idx)
        try:
            for i in range(1, 4):
                self.es.IndexWithId(idx, str(i), {"id": i, "name": f"doc_{i}"})
            self.es.FlushIndex(idx)

            pit_id = self.es.OpenPit(idx)
            try:
                response = self.es.GetIndex(None, pitId=pit_id, pitKeepAlive="1m")
                self.assertIsNotNone(response, "GetIndex with pitId should return a response")
                self.assertIn("pit_id", response, "Response should contain pit_id when using PIT")
            finally:
                self.es.ClosePit(pit_id)

            logging.info(f"[TEST SUCCESS] {module}:test_getindex_with_pit")
        finally:
            try:
                self.es.DeleteIndex(idx)
            except Exception:
                pass


if __name__ == '__main__':
    unittest.main()

