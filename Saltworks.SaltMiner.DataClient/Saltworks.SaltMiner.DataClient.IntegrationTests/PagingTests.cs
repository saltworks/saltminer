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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests;

[TestClass]
public class PagingTests
{
    private static DataClient Client = null;
    private const string TestCategory = "PagingTest";

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        if (context == null)
        {
            return;
        }
        Client = Helpers.GetDataClient<PagingTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(true)));
    }

    [TestMethod]
    public void PitPaging()
    {
        // Arrange
        var count = 100;
        var pageSize = 3;
        var testIndex = TestItem.GenerateIndex($"pit_paging");
        Helpers.RegisterDeleteIndex(testIndex);
        var bulkResponse = Helpers.BulkAddUpdateTestEntities(Client, testIndex, count, TestCategory);
        Assert.AreEqual(count, bulkResponse.Affected, $"Bulk insert failed during test setup: {bulkResponse.Message}");
        Client.RefreshIndex(testIndex);
        Task.Delay(500).Wait(); // Wait for indexing

        // Act - paginate through results using PIT
        var processed = 0;
        var request = new SearchRequest
        {
            Filter = new()
            {
                FilterMatches = new Dictionary<string, string> { { "Category", TestCategory } }
            },
            PagingInfo = new PagingInfo(pageSize) { EnablePit = true }
        };

        var response = Client.IndexSearch<TestItem>(request, testIndex);
        Assert.IsTrue(response.Success, $"Initial search failed: {response.Message}");

        while (response.Data.Any())
        {
            foreach (var entity in response.Data)
            {
                processed++;
            }
            request.PagingInfo = response.PagingInfo.NextPage();
            response = Client.IndexSearch<TestItem>(request, testIndex);
            Assert.IsTrue(response.Success, $"Paging search failed: {response.Message}");
        }
        
        // Assert
        Assert.AreEqual(count, processed, $"Expected {count} entities but processed {processed}");
    }

    [TestMethod]
    public void Paging()
    {
        // Arrange
        var count = 500;
        var processed = 0;
        var pageSize = 20;
        var pageCount = count / pageSize;
        var totalPages = 0;
        var testIndex = TestItem.GenerateIndex($"paging");
        var bulkResponse = Helpers.BulkAddUpdateTestEntities(Client, testIndex, count, TestCategory);
        Assert.AreEqual(count, bulkResponse.Affected, $"Bulk insert failed during test setup: {bulkResponse.Message}");
        Client.RefreshIndex(testIndex);
        Task.Delay(500).Wait(); // Wait for indexing

        // Act - paginate through results
        var searchRequest = new SearchRequest(new(pageSize));
        var response = Client.IndexSearch<TestItem>(searchRequest, testIndex);
        while (response?.Data != null && response.Data.Any())
        {
            totalPages++;
            foreach (var issue in response.Data)
                processed++;
            // continue previous via scrolling
            searchRequest.PagingInfo = response.PagingInfo.NextPage();
            response = Client.IndexSearch<TestItem>(searchRequest, testIndex); 
        }

        Assert.AreEqual(count, processed, $"Expected {count} entities but found {processed}");
        Assert.AreEqual(pageCount, totalPages, $"Expected {pageCount} pages but found {totalPages}");
    }
}
