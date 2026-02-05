/* --[auto-generated, do not modify this block]--
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
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class IndexTests
    {
        private static DataClient Client = null;
        
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }
            Client = Helpers.GetDataClient<AssetTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(false, true)));
        }

        [TestMethod]
        public void Refresh()
        {
            var response = Client.RefreshIndex(QueueAsset.GenerateIndex());
            Assert.IsTrue(response.Success);
        }

        /// <summary>
        /// Tests both: 
        /// 1. That test indices are cleaned up after test completion
        /// 2. That TestItem entities are properly serialized with all fields (not as base SaltMinerEntity)
        /// </summary>
        [TestMethod]
        public void TestIndexCleanupAndProperSerialization()
        {
            // Create an admin client for index operations (IndexBulk requires Admin role)
            var adminClient = Helpers.GetDataClient<IndexTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(admin: true)));
            
            // Generate a unique test index
            var testIndex = TestItem.GenerateIndex("test_cleanup_serialization");
            
            // Register the index for deletion after test run
            Helpers.RegisterDeleteIndex(testIndex);
            
            // Create the index and populate with TestItem entities (should have Category field)
            const int testCount = 5;
            const string testCategory = "TestCategory";
            var bulkResponse = Helpers.BulkAddUpdateTestEntities(adminClient, testIndex, testCount, testCategory);
            
            Assert.IsTrue(bulkResponse.Success, $"Bulk add failed: {bulkResponse.Message}");
            Assert.AreEqual(testCount, bulkResponse.Affected, "Not all items were added");
            
            // Refresh the index to make documents searchable
            var refreshResponse = Client.RefreshIndex(testIndex);
            Assert.IsTrue(refreshResponse.Success, "Index refresh failed");
            
            // Give Elasticsearch a moment to complete the refresh
            Task.Delay(5000).Wait();
            
            // Verify the index exists
            var existsResponse = Client.CheckForIndex(testIndex);
            Assert.IsTrue(existsResponse.Success, "Index should exist after creation");
            Assert.AreEqual(1, existsResponse.Affected, "Index should report as existing");
            
            // Search for the items to verify they were serialized correctly with all fields
            var searchRequest = new SearchRequest 
            { 
                Filter = new Filter(),
                PagingInfo = new PagingInfo { Size = 10 }
            };
            var searchResponse = adminClient.IndexSearch<TestItem>(searchRequest, testIndex);
            
            Assert.IsTrue(searchResponse.Success, "Search failed");
            Assert.AreEqual(testCount, searchResponse.Data.Count(), "Should find all items");
            
            // CRITICAL: Verify that the Category field is populated (proves proper serialization)
            foreach (var item in searchResponse.Data)
            {
                Assert.IsNotNull(item.Category, "Category field should not be null - indicates serialization issue");
                Assert.AreEqual(testCategory, item.Category, 
                    "Category field should match - if empty/null, entities were serialized as base SaltMinerEntity");
                Assert.IsFalse(string.IsNullOrEmpty(item.Name), "Name field should be populated");
                Assert.IsTrue(item.Value > 0, "Value field should be populated");
            }
            
            // The index will be cleaned up by AssemblyHooks after all tests complete
            // To verify cleanup works, run this test and check that the index is removed
            // You can manually verify by checking Elasticsearch after test run completes
        }
    }
}
