/* --[auto-generated, do not modify this block]--
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
*/

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System;
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
        var pageSize = 30;
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
                FilterMatches = new Dictionary<string, string> { { "category", TestCategory } }
            },
            PagingInfo = new PagingInfo(pageSize) { EnablePit = true },
            SortKeys = new() { { "id", true } }
        };

        var response = Client.IndexSearch<TestItem>(request, testIndex);
        Assert.IsTrue(response.Success, $"Initial search failed: {response.Message}");
        try {
            while (response.Data.Any())
            {
                foreach (var entity in response.Data)
                {
                    processed++;
                }
                request.PagingInfo = response.PagingInfo.NextPage();
                response = Client.IndexSearch<TestItem>(request, testIndex);
                Assert.IsTrue(response.Success, $"Paging search failed: {response.Message}");
                Assert.IsFalse(processed > count, "Processed more entities than expected.");  // break potential infinite loop
            }
        }
        catch (Exception ex)
        {
            Assert.Fail($"Exception during PIT paging: {ex.Message}");
        }
        finally
        {
            if (response?.PagingInfo?.PitPagingToken != null)
            {
                Client.ClosePitSearch(response.PagingInfo.PitPagingToken);
            }
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
        var pageSize = 200;
        var pageCount = count / pageSize;
        if (count % pageSize > 0)
            pageCount++;
        var totalPages = 0;
        var testIndex = TestItem.GenerateIndex($"paging");
        Helpers.RegisterDeleteIndex(testIndex);
        var bulkResponse = Helpers.BulkAddUpdateTestEntities(Client, testIndex, count, TestCategory);
        Assert.AreEqual(count, bulkResponse.Affected, $"Bulk insert failed during test setup: {bulkResponse.Message}");
        Client.RefreshIndex(testIndex);
        Task.Delay(500).Wait(); // Wait for indexing

        // Act - paginate through results
        var searchRequest = new SearchRequest(new(pageSize))
        {
            SortKeys = new() { { "id", true } }
        };
        var response = Client.IndexSearch<TestItem>(searchRequest, testIndex);
        while (response?.Data != null && response.Data.Any())
        {
            totalPages++;
            processed += response.Data.Count();
            Assert.IsFalse(processed > count, "Processed more entities than expected.");
            // continue previous via scrolling
            searchRequest.PagingInfo = response.PagingInfo.NextPage();
            response = Client.IndexSearch<TestItem>(searchRequest, testIndex); 
        }

        Assert.AreEqual(count, processed, $"Expected {count} entities but found {processed}");
        Assert.AreEqual(pageCount, totalPages, $"Expected {pageCount} pages but found {totalPages}");
    }

    [TestMethod]
    public void Ui_Paging()
    {
        // Arrange
        var count = 500;
        var processed = 0;
        var pageSize = 200;
        var pageCount = count / pageSize;
        if (count % pageSize > 0)
            pageCount++;
        var totalPages = 0;
        var testIndex = TestItem.GenerateIndex($"ui-paging");
        Helpers.RegisterDeleteIndex(testIndex);
        var bulkResponse = Helpers.BulkAddUpdateTestEntities(Client, testIndex, count, TestCategory);
        Assert.AreEqual(count, bulkResponse.Affected, $"Bulk insert failed during test setup: {bulkResponse.Message}");
        Client.RefreshIndex(testIndex);
        Task.Delay(500).Wait(); // Wait for indexing

        // Act - paginate through results
        var searchRequest = new SearchRequest(new(pageSize))
        {
            SortKeys = new() { { "id", true } },
            PagingInfo = new PagingInfo(pageSize)
        };
        var response = Client.IndexSearch<TestItem>(searchRequest, testIndex);
        var lastPageFirstValue = "----";
        while (response?.Data != null && response.Data.Any())
        {
            Assert.AreNotEqual(response.Data.First().Name, lastPageFirstValue, "First item should not be same as the previous page.");
            lastPageFirstValue = response.Data.First().Name;
            totalPages++;
            processed += response.Data.Count();
            Assert.IsFalse(processed > count, "Processed more entities than expected.");
            // continue previous via scrolling with only page number (no keys)
            searchRequest.PagingInfo = new PagingInfo(pageSize) { Page = searchRequest.PagingInfo.Page + 1 };
            response = Client.IndexSearch<TestItem>(searchRequest, testIndex);
        }

        Assert.AreEqual(count, processed, $"Expected {count} entities but found {processed}");
        Assert.AreEqual(pageCount, totalPages, $"Expected {pageCount} pages but found {totalPages}");
    }
}
