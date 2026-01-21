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
using System;
using Saltworks.SaltMiner.ElasticClient.EsClient;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests;

[TestClass]
public class IndexTests
{
    private static IElasticClient Client = null;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        Helpers.ValidateSettingsAndConnect();
        var c = Helpers.SettingsConfig();
        Client = Helpers.GetElasticClient(c);
    }

		[TestMethod]
		public void CheckIndexTemplate()
		{
			// Arrange
			var indexName = "queue_asset";

			// Act
			var result = Client.IndexTemplateExists(indexName);

			// Assert
			Assert.IsTrue(result.IsSuccessful);
		}

		[TestMethod]
		public void GetIndexMapping()
		{
			// Arrange
			var indexName = "queue_issues";

			// Act
			var result = Client.IndexMappingGet(indexName);

			// Assert
			Assert.IsTrue(result != null);
		}

		[TestMethod]
		public void ReIndex()
		{
			// Arrange
			var indexName = "queue_issues";
			var newIndexName = indexName + "_test87789";

			// Act
			Client.IndexReindex(indexName, newIndexName);
			var result = Client.IndexExists(newIndexName);
			Client.IndexDelete(newIndexName);

			// Assert
			Assert.IsTrue(result != null);
		}

		[TestMethod]
		public void GetAllIndexes()
		{
			// Act
			var result = Client.IndexGetAll();

			// Assert
			Assert.IsTrue(result != null);
		}

		[TestMethod]
		public void GetIndexTemplate()
		{
			// Arrange
			var indexName = "queue_asset";

			// Act
			var result = Client.IndexTemplateGet(indexName);

			// Assert
			Assert.IsTrue(result != null);
		}

		[TestMethod]
		public void RefreshTest()
		{
			// Arrange
			var index = "queue_asset";

			// Act
			var result = Client.IndexRefresh(index);

			// Assert
			Assert.IsTrue(result.IsSuccessful);
        }

        [TestMethod]
        public void FlushTest()
        {
            // Arrange
            var index = "queue_asset";

            // Act
            var result = Client.IndexFlush(index);

            // Assert
            Assert.IsTrue(result.IsSuccessful);
        }

        [TestMethod]
        public void Create_Delete_Index()
        {
            // Arrange
            var mapping = MAPPING;
            var index = "test-index";

            // Act
            Client.IndexCreate(index, mapping);
            Client.IndexDelete(index);

            // Assert
            Assert.IsTrue(true, "No exceptions up to this point == good");
        }

        public const string MAPPING = @"
		{
			""mappings"": {
				""dynamic"": ""false"",
				""properties"": {
					""id"": {
						""type"": ""keyword""
					},
					""number"": {
						""type"": ""integer""
					},
					""name"": {
						""type"": ""keyword""
					},
					""timestamp"": {
						""type"": ""date"",
						""format"": ""date_time""
					}
				}
			}
		}";

        [TestMethod]
        public void CheckActiveIssueAlias_ChecksForAlias()
        {
            // Arrange
            var indexName = "queue_issue";

            // Act
            var result = Client.CheckActiveIssueAlias(indexName);

            // Assert
            Assert.IsNotNull(result);
            // Result will be true or false depending on whether alias exists
        }

        [TestMethod]
        public void GetIndexMapping_RetrievesMapping()
        {
            // Arrange
            var indexName = "queue_issue";

            // Act
            var result = Client.IndexMappingGet(indexName);

            // Assert
            Assert.IsNotNull(result);
            // Result should be JSON string with mappings
        }

        [TestMethod]
        public void UpdateIndexMapping_RemapsIndex()
        {
            // Arrange - create a temporary index with simple mapping
            var tempIndex = $"test_remap_{Guid.NewGuid()}";
            var simpleMapping = @"
			{
			""mappings"": {
				""properties"": {
				""field1"": { ""type"": ""keyword"" }
				}
			}
			}";

            Client.IndexCreate(tempIndex);

            // Act
            var newIndexName = $"{tempIndex}_remapped";
            var result = Client.IndexMappingUpdate(tempIndex, simpleMapping, newIndexName);

            // Assert
            Assert.IsNotNull(result);
            // Clean up if successful
            try { Client.IndexDelete(newIndexName); }
            catch (Exception) { /* cleanup attempt */ }
            try { Client.IndexDelete(tempIndex); }
            catch (Exception) { /* cleanup attempt */ }
        }

        [TestMethod]
        public void UpdateIndexName_RenamesIndex()
        {
            // Arrange - create a temporary index
			var tempIndex = $"test_rename_{Guid.NewGuid().ToString()[0..8]}";
			var createResult = Client.IndexCreate(tempIndex);
			Assert.IsTrue(createResult.IsSuccessful, $"Failed to create source index: {createResult.Message}");
			Client.IndexRefresh(tempIndex, 500);
			
			// Add a test document so the index isn't empty
			var testDoc = new ThrowawayEntity { Id = "test-doc", Name = "test" };
			Client.AddUpdate(testDoc, tempIndex);
			Client.IndexRefresh(tempIndex, 500);
			
			var exists = Client.IndexExists(tempIndex);
			Assert.IsTrue(exists.IsSuccessful && exists.CountAffected == 1, $"Source index should exist before rename - success:{exists.IsSuccessful}, affected:{exists.CountAffected}, message:{exists.Message}");

            // Act
            var newIndexName = $"{tempIndex}_renamed";
            var result = Client.IndexRename(tempIndex, newIndexName);
			Assert.IsTrue(result.IsSuccessful, $"IndexRename failed: {result.Message}");
			Client.IndexRefresh(newIndexName, 500);
			var oldExists = Client.IndexExists(tempIndex);
			var newExists = Client.IndexExists(newIndexName);

            // Assert
            Assert.IsNotNull(result);
			Assert.IsTrue(oldExists.IsSuccessful && oldExists.CountAffected == 0, $"Old index should not exist - call success: {oldExists.IsSuccessful}, affected: {oldExists.CountAffected}");
			Assert.IsTrue(newExists.IsSuccessful && newExists.CountAffected == 1, $"New index should exist - call success: {newExists.IsSuccessful}, affected: {newExists.CountAffected}");
            // Clean up if successful
            try { Client.IndexDelete(newIndexName); }
            catch (Exception) { /* cleanup attempt */ }
            try { Client.IndexDelete(tempIndex); }
            catch (Exception) { /* cleanup attempt */ }
        }

		[TestMethod]
		public void Search_WithJsonSearchRequest_ReturnsJsonResults()
		{
			var tempIndex = $"test_search_{Guid.NewGuid():N}";

			var createResult = Client.IndexCreate(tempIndex);
			Assert.IsTrue(createResult.IsSuccessful, $"IndexCreate failed: {createResult.Message}");
			Client.IndexRefresh(tempIndex, 500);

			var doc1 = new TestItem { Id = "doc1", Name = "alpha", Value = 1 };
			var doc2 = new TestItem { Id = "doc2", Name = "beta", Value = 2 };
			var doc3 = new TestItem { Id = "doc3", Name = "gamma", Value = 3 };

			Assert.IsTrue(Client.AddUpdate(doc1, tempIndex).IsSuccessful);
			Assert.IsTrue(Client.AddUpdate(doc2, tempIndex).IsSuccessful);
			Assert.IsTrue(Client.AddUpdate(doc3, tempIndex).IsSuccessful);
			Client.IndexRefresh(tempIndex, 500);

			var searchRequest = new JsonSearchRequest
			{
				TypeName = typeof(TestItem).FullName,
				Filter = new Filter(),
				PagingInfo = new PagingInfo { Size = 10 },
				SortKeys = new System.Collections.Generic.Dictionary<string, bool> { { "timestamp", true } }
			};

			var result = Client.Search(tempIndex, searchRequest);

			Assert.IsNotNull(result, "Search result should not be null");
			Assert.IsTrue(result.IsSuccessful, $"Search should succeed. Message: {result.Message}");
			Assert.IsNotNull(result.Results, "Search results should not be null");
			Assert.IsTrue(result.CountAffected >= 3, $"Expected at least 3 docs. Count: {result.CountAffected}");

			try { Client.IndexDelete(tempIndex); }
			catch (Exception) { /* cleanup attempt */ }
		}

		[TestMethod]
		public void Search_WithInvalidType_ThrowsException()
		{
			var tempIndex = $"test_search_invalid_{Guid.NewGuid():N}";
			Client.IndexCreate(tempIndex);
			Client.IndexRefresh(tempIndex, 500);

			var searchRequest = new JsonSearchRequest
			{
				TypeName = "NonExistentType",
				PagingInfo = new PagingInfo { Size = 10 }
			};

			Assert.ThrowsException<EsClientException>(
				() => Client.Search(tempIndex, searchRequest),
				"Should throw EsClientException for invalid type");

			try { Client.IndexDelete(tempIndex); }
			catch (Exception) { /* cleanup attempt */ }
		}
}
