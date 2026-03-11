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
using Saltworks.SaltMiner.Core.Data;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests;

[TestClass]
public class CrudTests
{
    private const string SOURCE_TYPE = "ElasticClient";
    private static IElasticClient Client = null;
    private static void RegisterDeleteIndex(string index) => Helpers.RegisterDeleteIndex(index);

    [ClassInitialize]
    public static void Initialize(TestContext _)
    {
        Helpers.ValidateSettingsAndConnect();
        var c = Helpers.SettingsConfig();
        Client = Helpers.GetElasticClient(c);
    }

    // Per-class index cleanup not needed; indices are cleaned up centrally in AssemblyHooks

    [TestMethod]
    public void FuzzySearchTest()
    {
        var kvp = new Dictionary<string, string>
        {
            { "saltminer.name", "Test***" }
        };

        var request = new SearchRequest
        {
            Filter = new Filter
            {
                FilterMatches = kvp
            }
        };

        var results = Client.Search<Engagement>(Engagement.GenerateIndex(), request);
        Assert.IsTrue(results.IsSuccessful);
    }

    [TestMethod]
    public void QueueScanAddTest()
    {
        var json = @"
        {
            ""Timestamp"": ""2022-08-02T16:10:20"",
            ""Saltminer"": {
            ""Internal"": {
                ""IssueCount"": -1,
                ""CurrentQueueScanId"": null,
                ""QueueStatus"": ""Loading""
            },
            ""Scan"": {
                ""AssessmentType"": ""SAST"",
                ""ProductType"": ""Fortify SCA"",
                ""Product"": ""Fortify SCA"",
                ""Vendor"": ""Fortify"",
                ""ReportId"": ""2022-08-02T15:53:48.615+00:00~485"",
                ""ScanDate"": ""2022-08-02T15:53:48"",
                ""SourceType"": ""Saltworks.SSC"",
                ""IsSaltMinerSource"": true,
                ""Instance"": ""SSC1"",
                ""AssetType"": ""App"",
                ""Rulepacks"": [
                {
                    ""Id"": ""9C48678C-09B6-474D-B86D-97EE94D38F17"",
                    ""Name"": ""Fortify Secure Coding Rules, Extended, Content"",
                    ""Version"": ""2018.1.0.0007""
                },
                {
                    ""Id"": ""BD292C4E-4216-4DB8-96C7-9B607BFD9584"",
                    ""Name"": ""Fortify Secure Coding Rules, Core, JavaScript"",
                    ""Version"": ""2018.1.0.0007""
                },
                {
                    ""Id"": ""C4D1969E-B734-47D3-87D4-73962C1D32E2"",
                    ""Name"": ""Fortify Secure Coding Rules, Extended, JavaScript"",
                    ""Version"": ""2018.1.0.0007""
                },
                {
                    ""Id"": ""CA8013D5-11DE-44EF-9563-182F9FCB87BC"",
                    ""Name"": ""Fortify Secure Coding Rules, Core, Ruby"",
                    ""Version"": ""2018.1.0.0007""
                },
                {
                    ""Id"": ""CD6959FC-0C37-45BE-9637-BAA43C3A4D56"",
                    ""Name"": ""Fortify Secure Coding Rules, Extended, Configuration"",
                    ""Version"": ""2018.1.0.0007""
                }
                ]
            }
            }
        }";
        var qs = System.Text.Json.JsonSerializer.Deserialize<QueueScan>(json);
        var r = Client.AddUpdate(qs, "queue_scans");
        var rd = Client.Delete<QueueScan>(r.Result.Document.Id, r.Result.Index);
        Assert.IsTrue(r.IsSuccessful);
        Assert.IsTrue(rd.IsSuccessful);
    }

    [TestMethod]
    public void SimpleSearchTest()
    {
        var idx = ThrowawayEntity.GenerateIndex("Test_SimpleSearch");
        RegisterDeleteIndex(idx);
        var result = Client.Search<ThrowawayEntity>(idx, new SearchRequest
        {
            Filter = new Filter
            {
                FilterMatches = new Dictionary<string, string>
                    {
                        { "index", idx }
                    },
            },
        });
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task SearchWithUiPagingTest()
    {
        var count = 51;
        var pageSize = 10;
        var pageCount = count / pageSize;
        if (count % pageSize > 0)
            pageCount++;
        var testIndex = TestItem.GenerateIndex($"ui_paging_test");
        RegisterDeleteIndex(testIndex);
        var docs = new List<TestItem>();

        for (var i = 0; i < count; i++)
        {
            var doc = new TestItem()
            {
                Name = $"TestItem_{i}",
                Category = "UiPagingTest"
            };
            docs.Add(doc);
        }

        var rsp = Client.BulkAddUpdate(docs, testIndex);
        Assert.AreEqual(count, rsp.CountAffected, $"Bulk insert failed during test setup: {rsp.Message}");
        Client.IndexRefresh(testIndex);
        await Task.Delay(500); // Wait for indexing

        var searchRequest = new SearchRequest()
        {
            PagingInfo = new PagingInfo(pageSize)
        };

        var response = Client.Search<TestItem>(testIndex, searchRequest);
        var lastPageFirstValue = "----";
        var totalPages = 0;
        var processed = 0;
        while (response?.Results != null && response.Results.Any())
        {
            Assert.AreNotEqual(response.Results.First().Document.Name, lastPageFirstValue, "First item should not be same as the previous page.");
            lastPageFirstValue = response.Results.First().Document.Name;
            totalPages++;
            processed += response.Results.Count();
            Assert.IsFalse(processed > count, "Processed more entities than expected.");
            // continue previous via scrolling with only page number (no keys)
            searchRequest.PagingInfo = new PagingInfo(pageSize) { Page = searchRequest.PagingInfo.Page + 1 };
            response = Client.Search<TestItem>(testIndex, searchRequest);
        }

        Assert.AreEqual(count, processed, $"Expected {count} entities but found {processed}");
        Assert.AreEqual(pageCount, totalPages, $"Expected {pageCount} pages but found {totalPages}");
    }

    [TestMethod]
    public async Task SearchTest()
    {
        var sourceType = SOURCE_TYPE;
        var mockIssue = Mock.Issue(sourceType);
        var indexName = Issue.GenerateIndex(mockIssue.Saltminer.Asset.AssetType, mockIssue.Saltminer.Asset.SourceType, mockIssue.Saltminer.Asset.Instance);
        RegisterDeleteIndex(indexName);
        mockIssue.Saltminer.Low = 0;
        var kvps = new Dictionary<string, string>
        {
            ["Saltminer.Source.IssueStatus"] = "SearchTest",
            ["Saltminer.Low"] = "0"
        }; 
        
        var request = new SearchRequest
        {
        };

        Client.DeleteByQuery<Issue>(request, indexName);

        request.Filter = new()
        {
            FilterMatches = kvps
        };

        Client.DeleteByQuery<Issue>(request, indexName);

        var issues = new List<Issue>();
        var issueCount = 11;

        for (var index = 0; index < issueCount; index++)
        {
            var issue = Mock.Issue(sourceType);
            issue.Id = "";
            issue.Saltminer.Source.IssueStatus = "SearchTest";
            issues.Add(issue);
        }

        Client.BulkAddUpdate(issues, indexName);
        await Task.Delay(2000); // Will fail if search happens right after inserts
        request = new SearchRequest
        {
            Filter = new()
            {
                FilterMatches = kvps
            },
            PagingInfo = new PagingInfo(10) { EnablePit = true }
        };
        var result = Client.Search<Issue>(indexName, request);

        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(10, result.Results.Count());

        //Clean Up
        request = new SearchRequest
        {
            Filter = new()
            {
                FilterMatches = kvps
            }
        };
        var issueDelete = Client.DeleteByQuery<Issue>(request, indexName);
        Assert.AreEqual(issueCount, issueDelete.CountAffected);

    }

    [TestMethod]
    public async Task CountTest()
    {
        var sourceType = SOURCE_TYPE;
        var mockIssue = Mock.Issue(sourceType);
        var indexName = Issue.GenerateIndex(mockIssue.Saltminer.Asset.AssetType, mockIssue.Saltminer.Asset.SourceType, mockIssue.Saltminer.Asset.Instance);
        RegisterDeleteIndex(indexName);
        var kvps = new Dictionary<string, string>
        {
            ["Saltminer.Asset.Name"] = "CountTest"
        };

        var request = new SearchRequest
        {
            Filter = new()
            {
                FilterMatches = kvps
            }
        };
        Client.DeleteByQuery<Issue>(request, indexName);

        var issues = new List<Issue>();
        var issueCount = 5;

        for (var index = 0; index < issueCount; index++)
        {
            var issue = Mock.Issue(sourceType);
            issue.Saltminer.Asset.Name = "CountTest";
            issue.Id = "";
            issues.Add(issue);
        }

        var r = Client.BulkAddUpdate(issues, indexName);
        await Task.Delay(2000); //Will fail if count happens right after update
        var result = Client.Count<Issue>(request, indexName);

        Assert.AreEqual(issueCount, r.CountAffected);
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(issueCount, result.CountAffected);

        //Clean Up
        var issueDelete = Client.DeleteByQuery<Issue>(request, indexName).CountAffected;
        Assert.AreEqual(issueCount, issueDelete);
    }

    [TestMethod]
    public async Task SearchWithScrollingTest()
    {
        var sourceType = "ElasticClient";
        var mockIssue = Mock.Issue(sourceType);
        var indexName = Issue.GenerateIndex(mockIssue.Saltminer.Asset.AssetType, mockIssue.Saltminer.Asset.SourceType, mockIssue.Saltminer.Asset.Instance);
        RegisterDeleteIndex(indexName);
        var kvps = new Dictionary<string, string>() {
            { "Saltminer.Asset.Name", "SearchWithScrollingTest" },
            { "Vulnerability.Severity", "Critical" }
        };


        var request = new SearchRequest
        {
            Filter = new()
            {
                FilterMatches = kvps
            }
        };
        Client.DeleteByQuery<Issue>(request, indexName);

        var issues = new List<Issue>();
        var issueCount = 15;

        for (var index = 0; index < issueCount; index++)
        {
            var issue = Mock.Issue(sourceType);
            issue.Id = "";
            issue.Vulnerability.Severity = "Critical";
            issue.Saltminer.Asset.Name = "SearchWithScrollingTest";
            issues.Add(issue);
        }

        Client.BulkAddUpdate(issues, indexName);

        await Task.Delay(2000); //Will fail if search happens right after inserts


        request = new SearchRequest
        {
            Filter = new()
            {
                FilterMatches = kvps
            },
            PagingInfo = new PagingInfo(5) { EnablePit = true }
        };
        var result = Client.Search<Issue>(indexName, request);
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(5, result.Results.Count());

        request.PagingInfo = result.PagingInfo.NextPage();
        result = Client.Search<Issue>(indexName, request);
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(5, result.Results.Count());

        request.PagingInfo = result.PagingInfo.NextPage();
        result = Client.Search<Issue>(indexName, request);
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(5, result.Results.Count());

        request.PagingInfo = result.PagingInfo.NextPage();
        result = Client.Search<Issue>(indexName, request);
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(0, result.Results.Count());

        //Clean Up
        request.PagingInfo = null;
        var issueDelete = Client.DeleteByQuery<Issue>(request, indexName).CountAffected;
        Assert.AreEqual(issueCount, issueDelete);
    }

    [TestMethod]
    public async Task UpdateByQuery()
    {
        var queued = new List<ThrowawayEntity>();
        var count = 5;
        var oddCount = count % 2 == 0 ? count / 2 : (count / 2) + 1;
        var name = "odd";
        var newName = "oddball";
        var nameField = "name";

        foreach (var i in Enumerable.Range(1, count).ToArray())
        {
            var itm = new ThrowawayEntity
            {
                Number = i,
                Id = "",
                Name = i % 2 == 0 ? "even" : "odd"
            };
            queued.Add(itm);
        }

        var idx = ThrowawayEntity.GenerateIndex("test_updatebyquery");
        RegisterDeleteIndex(idx);
        // Clean up any existing data from previous test runs - won't fail if index doesn't exist
        Client.IndexDelete(idx);
        
        var result = Client.BulkAddUpdate(queued, idx);
        Client.IndexRefresh(idx, 1000);

        Assert.IsTrue(result.IsSuccessful, "Bulk insert failed");
        Assert.AreEqual(count, result.CountAffected, "Bulk insert count mismatch");

        var searchRequest = new SearchRequest(nameField, name, 5);
        var updateRequest = new UpdateQueryRequest<ThrowawayEntity>
        {
            Filter = new()
            {
                FilterMatches = new Dictionary<string, string> { { nameField, name } }
            },
            ScriptUpdates = new Dictionary<string, object> { 
                { nameField, newName }
            }
        };

        var srch = Client.Count<ThrowawayEntity>(searchRequest, idx);

        Assert.IsTrue(srch.IsSuccessful);
        Assert.AreEqual(oddCount, srch.CountAffected, $"Should find {oddCount} documents with name='{name}'");

        Client.UpdateByQuery(updateRequest, idx);
        Client.IndexRefresh(idx, 500);
        
        // After update, search for old name should return 0
        srch = Client.Count<ThrowawayEntity>(searchRequest, idx);
        Assert.IsTrue(srch.IsSuccessful);
        Assert.AreEqual(0, srch.CountAffected, "Should find 0 documents with name='odd' after update");
        
        // Search for new name should return oddCount
        var newSearchRequest = new SearchRequest(nameField, newName, 5);
        srch = Client.Count<ThrowawayEntity>(newSearchRequest, idx);
        Assert.IsTrue(srch.IsSuccessful);
        Assert.AreEqual(oddCount, srch.CountAffected, $"Should find {oddCount} documents with name='{newName}' after update");
    }

    [TestMethod]
    public void DeleteManyTests()
    {
        var queueLogs = new List<QueueLog>();
        var queueLog = Mock.QueueLog();
        var indexName = QueueLog.GenerateIndex();
        RegisterDeleteIndex(indexName);

        queueLogs.Add(queueLog);
        Client.AddUpdate(queueLog, indexName);

        queueLog = Mock.QueueLog();
        queueLogs.Add(queueLog);
        Client.AddUpdate(queueLog, indexName);

        var result = Client.BulkDelete<QueueLog>(queueLogs.Select(ql => ql.Id), indexName);
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(2, result.CountAffected);
    }

    [TestMethod]
    public void DeleteTests()
    {
        var queueLog = Mock.QueueLog();
        var indexName = QueueLog.GenerateIndex();
        RegisterDeleteIndex(indexName);
        var result = Client.AddUpdate(queueLog, indexName);
        Assert.IsTrue(result.IsSuccessful);
        result = Client.Delete<QueueLog>(queueLog.Id, indexName);
        Assert.IsTrue(result.IsSuccessful);

        //Clean Up
        var deleteQueueLog = Client.Delete<QueueLog>(queueLog.Id, indexName).CountAffected;
        Assert.AreEqual(1, deleteQueueLog);
    }

    [TestMethod]
    public void DeleteByQueryNoFilterTest()
    {
        var indexName = "throwaway";
        var request = new SearchRequest(); // crux of the test, will it break with no search stuff?
        Client.AddUpdate(new ThrowawayEntity { Id = "hi" }, indexName);
        Task.Delay(2000).Wait(); // Will fail unless we give elastic time to process
        var booboo = false;
        var msg = "";
        IElasticClientResponse<ThrowawayEntity> result = null;
        try { result = Client.DeleteByQuery<ThrowawayEntity>(request, indexName); }
        catch (Exception ex) { booboo = true; msg = ex.Message; }
        Assert.IsFalse(booboo, "DeleteByQuery had a boo-boo with no searchy stuff. Msg: {0}", msg);
        Assert.AreEqual(1, result.CountAffected);
        Client.IndexDelete(indexName);
    }

    [TestMethod]
    public void DeleteByQueryTest()
    {
        var sourceType = SOURCE_TYPE;
        var issue = Mock.Issue(sourceType);
        var issueCount = 2;
        var issueIndex = Issue.GenerateIndex(issue.Saltminer.Asset.AssetType, issue.Saltminer.Asset.SourceType, issue.Saltminer.Asset.Instance);
        RegisterDeleteIndex(issueIndex);
        var reportId = "DeleteByQueryTest_ReportId";
        var kvps = new Dictionary<string, string>
        {
            ["Saltminer.Scan.ReportId"] = reportId
        };

        var request = new SearchRequest
        {
            Filter = new()
            {
                FilterMatches = kvps
            }
        };
        Client.DeleteByQuery<Issue>(request, issueIndex);

        for (int i = 0; i < issueCount; i++)
        {
            var newIssue = Mock.Issue(sourceType);
            newIssue.Id = "";
            newIssue.Saltminer.Scan.ReportId = reportId;
            Client.AddUpdate(newIssue, issueIndex);
        }

        Task.Delay(2000).Wait(); // Will fail if happens right after inserts
        var result = Client.DeleteByQuery<Issue>(request, issueIndex);
        Assert.AreEqual(2, result.CountAffected);
    }

    [TestMethod]
    public void UpdateWithLocking()
    {
        var queueLog = Mock.QueueLog();
        var idx = QueueLog.GenerateIndex();
        RegisterDeleteIndex(idx);
        var result = Client.AddUpdate(queueLog, idx);

        var queueDescription = "A different description";
        queueLog.QueueDescription = queueDescription;

        result = Client.UpdateWithLocking(queueLog, idx, result.Result.Primary, result.Result.Sequence);
        Assert.IsTrue(result.IsSuccessful);

        result = Client.Get<QueueLog>(queueLog.Id, idx);
        Assert.AreEqual(queueDescription, result.Result.Document.QueueDescription);

        //Clean Up
        var delete = Client.Delete<QueueLog>(result.Result.Document.Id, idx).CountAffected;
        Assert.AreEqual(1, delete);
    }

    [TestMethod]
    public void UpdateWithLocking_Error()
    {
        var queueLog = Mock.QueueLog();
        var index = QueueLog.GenerateIndex();
        var result = Client.AddUpdate(queueLog, index);
        RegisterDeleteIndex(index);

        Assert.IsTrue(result.IsSuccessful);

        var get = Client.Get<QueueLog>(result.Result.Document.Id, index);
        Assert.IsTrue(get.IsSuccessful);

        get.Result.Document.Message = "test";
        var update = Client.Update(get.Result.Document, index);
        Assert.IsTrue(update.IsSuccessful);

        update.Result.Document.Message = "test2";
        var update2 = Client.UpdateWithLocking(update.Result.Document, index, get.Result.Primary, get.Result.Sequence);
        Assert.IsFalse(update2.IsSuccessful);

        //Clean Up
        var delete = Client.Delete<QueueLog>(result.Result.Document.Id, index).CountAffected;
        Assert.AreEqual(1, delete);
    }

    [TestMethod]
    public void QueueLog_AddUpdate()
    {
        var queueLog = Mock.QueueLog();
        var index = QueueLog.GenerateIndex();
        var result = Client.AddUpdate(queueLog, index);
        Assert.IsTrue(result.IsSuccessful);

        queueLog.Read = true;

        result = Client.AddUpdate(queueLog, index);
        Assert.IsTrue(result.IsSuccessful);

        queueLog = Client.Get<QueueLog>(queueLog.Id, index).Result.Document;
        Assert.IsTrue(queueLog.Read);

        //Clean Up
        var delete = Client.Delete<QueueLog>(result.Result.Document.Id, index).CountAffected;
        Assert.AreEqual(1, delete);
    }

    [TestMethod]
    public void AssetIssue_AddUpdate()
    {
        var sourceType = SOURCE_TYPE;
        var issue = Mock.Issue(sourceType);
        var issueIndex = Issue.GenerateIndex(issue.Saltminer.Asset.AssetType, issue.Saltminer.Asset.SourceType, issue.Saltminer.Asset.Instance);
        RegisterDeleteIndex(issueIndex);
        var result = Client.AddUpdate(issue, issueIndex);
        Assert.IsTrue(result.IsSuccessful);

        issue.Message = "Updated Message";
        issue.Labels["customer_specific_key1"] = "newly updated value";

        result = Client.AddUpdate(issue, issueIndex);
        Assert.IsTrue(result.IsSuccessful);

        //Clean Up
        var delete = Client.Delete<Issue>(result.Result.Document.Id, issueIndex).CountAffected;
        Assert.AreEqual(1, delete);
    }

    [TestMethod]
    public void AssetQueueIssue_AddUpdate()
    {
        var queueIssue = Mock.QueueIssue();
        var result = Client.AddUpdate(queueIssue, QueueIssue.GenerateIndex());
        Assert.IsTrue(result.IsSuccessful);

        queueIssue.Saltminer.Attributes = null;
        queueIssue.Labels = [];
        queueIssue.Tags = [ "newtag1", "newtag2" ];

        result = Client.AddUpdate(queueIssue, QueueIssue.GenerateIndex());
        Assert.IsTrue(result.IsSuccessful);

        //Clean Up
        var delete = Client.Delete<Issue>(result.Result.Document.Id, QueueIssue.GenerateIndex()).CountAffected;
        Assert.AreEqual(1, delete);
    }

    [TestMethod]
    public void AssetQueueScan_AddUpdate()
    {
        var queueScan = Mock.QueueScan();
        var result = Client.AddUpdate(queueScan, QueueScan.GenerateIndex());
        Assert.IsTrue(result.IsSuccessful);

        queueScan.Saltminer.Scan.AssessmentType = null;

        result = Client.AddUpdate(queueScan, QueueScan.GenerateIndex());
        Assert.IsTrue(result.IsSuccessful);

        //Clean Up
        var delete = Client.Delete<QueueScan>(result.Result.Document.Id, QueueScan.GenerateIndex()).CountAffected;
        Assert.AreEqual(1, delete);
    }

    [TestMethod]
    public void AssetScan_AddUpdate()
    {
        var sourceType = SOURCE_TYPE;
        var scan = Mock.Scan(sourceType);
        var scanIndex = Scan.GenerateIndex(scan.Saltminer.Asset.AssetType, scan.Saltminer.Asset.SourceType, scan.Saltminer.Asset.Instance);
        RegisterDeleteIndex(scanIndex);
        var result = Client.AddUpdate(scan, scanIndex);
        Assert.IsTrue(result.IsSuccessful);

        result = Client.AddUpdate(scan, scanIndex);
        Assert.IsTrue(result.IsSuccessful);

        //Clean Up
        var delete = Client.Delete<Scan>(result.Result.Document.Id, scanIndex).CountAffected;
        Assert.AreEqual(1, delete);
    }

    [TestMethod]
    public void AssetInventory_AddUpdate()
    {
        var assetInv = Mock.InventoryAsset();
        var result = Client.AddUpdate(assetInv, InventoryAsset.GenerateIndex());
        Assert.IsTrue(result.IsSuccessful);

        assetInv = new ();

        result = Client.AddUpdate(assetInv, InventoryAsset.GenerateIndex());
        Assert.IsTrue(result.IsSuccessful);

        //Clean Up
        var delete = Client.Delete<InventoryAsset>(result.Result.Document.Id, InventoryAsset.GenerateIndex()).CountAffected;
        Assert.AreEqual(1, delete);
    }

    public static void Snapshot_AddUpdate()
    {
        var assetSnapshot = Mock.Snapshot();
        var assetSnapshotIndex = Snapshot.GenerateIndex(assetSnapshot.Saltminer.Asset.AssetType, false);
        var result = Client.AddUpdate(assetSnapshot, assetSnapshotIndex);
        Assert.IsTrue(result.IsSuccessful);

        result = Client.AddUpdate(assetSnapshot, assetSnapshotIndex);
        Assert.IsTrue(result.IsSuccessful);

        //Clean Up
        var delete = Client.Delete<Snapshot>(result.Result.Document.Id, assetSnapshotIndex).CountAffected;
        Assert.AreEqual(1, delete);

        var indexDelete = Client.IndexDelete(assetSnapshotIndex).IsSuccessful;
        Assert.IsTrue(indexDelete);
    }

    [TestMethod]
    public void Asset_AddUpdate()
    {
        var sourceType = SOURCE_TYPE;
        var asset = Mock.Asset(sourceType);
        var assetIndex = Asset.GenerateIndex(asset.Saltminer.Asset.AssetType, asset.Saltminer.Asset.SourceType, asset.Saltminer.Asset.Instance);
        RegisterDeleteIndex(assetIndex);
        var result = Client.AddUpdate(asset, assetIndex);
        Assert.IsTrue(result.IsSuccessful);
        
        result = Client.AddUpdate(asset, assetIndex);
        Assert.IsTrue(result.IsSuccessful);

        //Clean Up
        var delete = Client.Delete<Asset>(result.Result.Document.Id, assetIndex).CountAffected;
        Assert.AreEqual(1, delete);
    }

    [TestMethod]
    public void QueueScanDateRangeSearch()
    {
        var sourceTpye = "Qualys";
        var kvps = new Dictionary<string, string>
        {
            ["Saltminer.Scan.ScanDate"] = "2021-10-18||2021-10-18",
            ["Saltminer.Scan.SourceType"] = sourceTpye,
        };
        var results = Client.Search<QueueScan>(QueueScan.GenerateIndex(), new SearchRequest
        {
            Filter = new()
            {
                FilterMatches = kvps
            },
            PagingInfo = new PagingInfo(300) { EnablePit = true }
        });

        Assert.IsNotNull(results);
    }

    [TestMethod]
    public async Task UpdateByQuery_WithStringQuery()
    {
        var sourceType = SOURCE_TYPE;
        var issue = Mock.Issue(sourceType);
        var indexName = Issue.GenerateIndex(issue.Saltminer.Asset.AssetType, issue.Saltminer.Asset.SourceType, issue.Saltminer.Asset.Instance);
        RegisterDeleteIndex(indexName);
        
        // Add test document
        issue.Saltminer.Low = 1;
        Client.AddUpdate(issue, indexName);
        await Task.Delay(1000);

        // Act
        var updateScript = "ctx._source.saltminer_low = 5";
        var result = Client.UpdateByQuery<Issue>("*", indexName, updateScript);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccessful);
        Assert.IsTrue(result.CountAffected > 0);

        // Clean Up
        Client.DeleteByQuery<Issue>(new SearchRequest(), indexName);
    }

    [TestMethod]
    public async Task UpdateByQuery_WithRequest()
    {
        var sourceType = SOURCE_TYPE;
        var issue = Mock.Issue(sourceType);
        var indexName = Issue.GenerateIndex(issue.Saltminer.Asset.AssetType, issue.Saltminer.Asset.SourceType, issue.Saltminer.Asset.Instance);
        RegisterDeleteIndex(indexName);
        
        // Add test document
        issue.Saltminer.Medium = 3;
        Client.AddUpdate(issue, indexName);
        await Task.Delay(1000);

        // Act
        var updateRequest = new UpdateQueryRequest<Issue>
        {
            Filter = new()
            {
                FilterMatches = new Dictionary<string, string> { { "saltminer.asset.name", issue.Saltminer.Asset.Name } }
            },
            ScriptUpdates = new Dictionary<string, object> { { "Saltminer.Medium", 10 } }
        };
        var result = Client.UpdateByQuery<Issue>(updateRequest, indexName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccessful);

        // Clean Up
        Client.DeleteByQuery<Issue>(new SearchRequest(), indexName);
    }
}