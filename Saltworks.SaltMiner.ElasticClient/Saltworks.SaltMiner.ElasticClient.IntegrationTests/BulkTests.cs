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

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests;

[TestClass]
public class BulkTests
{
    private static IElasticClient Client = null;
    private static readonly List<string> _indicesToDelete = [];

    private static void RegisterDeleteIndex(string index)
    {
        if (!_indicesToDelete.Contains(index))
            _indicesToDelete.Add(index);
    }

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        Helpers.ValidateSettingsAndConnect();
        var c = Helpers.SettingsConfig();
        Client = Helpers.GetElasticClient(c);
    }

    [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
    public static void Cleanup()
    {
        foreach (var index in _indicesToDelete)
        {
            try
            {
                Client.DeleteIndex(index);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting index {index}: {ex.Message}");
            }
        }
    }

    [TestMethod]
    public async Task AddUpdateBulkQueue_MultipleQueueIssues()
    {
        // Arrange
        var queuedIssues = new List<QueueIssue>();
        var issueCount = 5;
        var scanId = Guid.NewGuid().ToString();

        for (var i = 0; i < issueCount; i++)
        {
            var qi = Mock.QueueIssue();
            qi.Id = "";
            qi.Saltminer.QueueScanId = scanId;
            queuedIssues.Add(qi);
        }

        // Act
        var result = Client.AddUpdateBulkQueue(queuedIssues);
        await Task.Delay(2000);

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(issueCount, result.CountAffected);

        // Clean Up
        var issueDelete = Client.DeleteByQuery<QueueIssue>(new Saltworks.SaltMiner.Core.Data.SearchRequest
        {
            Filter = new()
            {
                FilterMatches = new Dictionary<string, string> { { "saltminer.queue_scan_id", scanId } }
            }
        }, QueueIssue.GenerateIndex());
        Assert.AreEqual(issueCount, issueDelete.CountAffected);
    }

    [TestMethod]
    public void BulkPartialUpdate_WithScript()
    {
        // Arrange
        var indexName = $"throwaway_{Guid.NewGuid()}";
        RegisterDeleteIndex(indexName);
        
        var entities = new List<ThrowawayEntity>();
        for (int i = 0; i < 3; i++)
        {
            var entity = new ThrowawayEntity { Id = Guid.NewGuid().ToString() };
            entities.Add(entity);
            Client.AddUpdate(entity, indexName);
        }
        System.Threading.Thread.Sleep(1000);

        var script = "ctx._source.test_field = params.update";
        var updateObj = new { test_value = "updated" };

        // Act
        var result = Client.BulkPartialUpdate(entities, e => indexName, script, updateObj, "update");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccessful);
    }

    [TestMethod]
    public void DeleteBulk_MultipleDocs()
    {
        // Arrange
        var indexName = $"throwaway_{Guid.NewGuid()}";
        RegisterDeleteIndex(indexName);

        var entities = new List<ThrowawayEntity>();
        for (int i = 0; i < 3; i++)
        {
            var entity = new ThrowawayEntity { Id = Guid.NewGuid().ToString() };
            entities.Add(entity);
            Client.AddUpdate(entity, indexName);
        }

        var ids = entities.Select(e => e.Id).ToList();

        // Act
        var result = Client.DeleteBulk<ThrowawayEntity>(ids, indexName);

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(3, result.CountAffected);
    }
}
