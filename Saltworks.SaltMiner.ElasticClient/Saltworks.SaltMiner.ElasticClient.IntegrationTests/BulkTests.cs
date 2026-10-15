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

    // Per-class index cleanup not needed; indices are cleaned up centrally in AssemblyHooks

    [TestMethod]
    public void AddUpdateBulk_ThrowawayEntities()
    {
        // Arrange
        var indexName = "test_addupdate_bulk_throwaway";
        RegisterDeleteIndex(indexName);
        
        var entities = new List<ThrowawayEntity>();
        var entityCount = 10;
        
        for (int i = 0; i < entityCount; i++)
        {
            var entity = new ThrowawayEntity 
            { 
                Id = i % 2 == 0 ? "" : Guid.NewGuid().ToString(), // Test auto-ID generation for some entities
                Name = $"Entity_{i}",
                Number = i
            };
            entities.Add(entity);
        }

        // Act
        var result = Client.BulkAddUpdate(entities, indexName);
        Assert.IsTrue(result.IsSuccessful, "Initial bulk insert failed");
        Assert.AreEqual(entityCount, result.CountAffected, "Not all entities were added");

        // Modify entities
        foreach (var entity in entities)
        {
            entity.Name += "_Updated";
        }
        
        // Act - Update same entities
        result = Client.BulkAddUpdate(entities, indexName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccessful, $"Bulk operation failed: {result.Message}");
        Assert.AreEqual(entityCount, result.CountAffected, "Not all entities were updated");
        
        // Verify all entities have IDs assigned
        Assert.IsTrue(entities.All(e => !string.IsNullOrEmpty(e.Id)), "Some entities don't have IDs assigned");
    }

    [TestMethod]
    public async Task AddUpdateBulkQueueIssues()
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
        var result = Client.BulkQueueAddUpdate(queuedIssues);
        Client.IndexRefresh(QueueIssue.GenerateIndex(), 500);

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(issueCount, result.CountAffected);

        // Clean Up
        var issueDelete = Client.DeleteByQuery<QueueIssue>(new Core.Data.SearchRequest
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
        var indexName = "test_bulk_partial_update_with_script";
        RegisterDeleteIndex(indexName);
        
        var entities = new List<ThrowawayEntity>();
        for (int i = 0; i < 3; i++)
        {
            var entity = new ThrowawayEntity { Id = Guid.NewGuid().ToString() };
            entities.Add(entity);
            Client.AddUpdate(entity, indexName);
        }
        Client.IndexRefresh(indexName, 500);

        var script = "ctx._source.test_field = params.update.test_value";
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
        var indexName = "test_delete_bulk_multiple_docs";
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
        var result = Client.BulkDelete<ThrowawayEntity>(ids, indexName);

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(3, result.CountAffected);
    }
}
