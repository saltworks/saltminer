/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-04-09
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using Saltworks.SaltMiner.Core.Data;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests
{
    [TestClass]
    public class CRUDTests
    {
        private static IElasticClient Client = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            var c = Helpers.SettingsConfig();
            Client = Helpers.GetElasticClient(c);
        }

        [ClassCleanup]
        public static void CleanUp()
        {
            Helpers.CleanIndex(Client, "issue");
            Helpers.CleanIndex(Client, "scan");
            Helpers.CleanIndex(Client, "asset");
        }

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

            var results = Client.Search<Engagement>(request, Engagement.GenerateIndex());
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
            var result = Client.Search<IndexMeta>(new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                        {
                            { "index", "queue_issue" }
                        },
                },
            }, IndexMeta.GenerateIndex());

            Assert.IsTrue(result != null);
        }

        [TestMethod]
        public void SearchTest()
        {
            var sourceType = "ElasticClient";
            var mockIssue = Mock.Issue(sourceType);
            var indexName = Issue.GenerateIndex(mockIssue.Saltminer.Asset.AssetType, mockIssue.Saltminer.Asset.SourceType, mockIssue.Saltminer.Asset.Instance);
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

            Client.AddUpdateBulk(issues, indexName);
            Thread.Sleep(2000); // Will fail if search happens right after inserts
            request = new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = kvps
                },
                PitPagingInfo = new PitPagingInfo(10)
            };
            var result = Client.Search<Issue>(request, indexName);
 
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
        public void CountTest()
        {
            var sourceType = "ElasticClient";
            var mockIssue = Mock.Issue(sourceType);
            var indexName = Issue.GenerateIndex(mockIssue.Saltminer.Asset.AssetType, mockIssue.Saltminer.Asset.SourceType, mockIssue.Saltminer.Asset.Instance);
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

            var r = Client.AddUpdateBulk(issues, indexName);
            Thread.Sleep(2000); //Will fail if count happens right after update
            var result = Client.Count<Issue>(request, indexName);

            Assert.AreEqual(issueCount, r.CountAffected);
            Assert.IsTrue(result.IsSuccessful);
            Assert.AreEqual(issueCount, result.CountAffected);

            //Clean Up
            var issueDelete = Client.DeleteByQuery<Issue>(request, indexName).CountAffected;
            Assert.AreEqual(issueCount, issueDelete);
        }

        [TestMethod]
        public void SearchWithScrollingTest()
        {
            var sourceType = "ElasticClient";
            var mockIssue = Mock.Issue(sourceType);
            var indexName = Issue.GenerateIndex(mockIssue.Saltminer.Asset.AssetType, mockIssue.Saltminer.Asset.SourceType, mockIssue.Saltminer.Asset.Instance);
            var kvps = new Dictionary<string, string>() {
                { "Saltminer.Asset.Name", "SearchWithScrollingTest" },
                { "Vulnerability.Severity", "Critically" }
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
                issue.Vulnerability.Severity = "Critically";
                issue.Saltminer.Asset.Name = "SearchWithScrollingTest";
                issues.Add(issue);
            }

            Client.AddUpdateBulk(issues, indexName);

            Thread.Sleep(2000);//Will fail if search happens right after inserts


            request = new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = kvps
                },
                PitPagingInfo = new PitPagingInfo(5, true)
            };
            var result = Client.Search<Issue>(request, indexName);
            Assert.IsTrue(result.IsSuccessful);
            Assert.AreEqual(5, result.Results.Count());

            request.AfterKeys = result.AfterKeys;
            request.PitPagingInfo = result.PitPagingInfo;
            result = Client.Search<Issue>(request, indexName);
            Assert.IsTrue(result.IsSuccessful);
            Assert.AreEqual(5, result.Results.Count());

            request.AfterKeys = result.AfterKeys;
            request.PitPagingInfo = result.PitPagingInfo;
            result = Client.Search<Issue>(request, indexName);
            Assert.IsTrue(result.IsSuccessful);
            Assert.AreEqual(5, result.Results.Count());

            request.AfterKeys = result.AfterKeys;
            request.PitPagingInfo = result.PitPagingInfo;
            result = Client.Search<Issue>(request, indexName);
            Assert.IsTrue(result.IsSuccessful);
            Assert.AreEqual(0, result.Results.Count());

            //Clean Up

            request.PitPagingInfo = null;
            var issueDelete = Client.DeleteByQuery<Issue>(request, indexName).CountAffected;
            Assert.AreEqual(issueCount, issueDelete);
        }

        [TestMethod]
        public void AddUpdateBulkQueueIssues()
        {
            var queuedIssues = new List<QueueIssue>();
            var issueCount = 5;
            var scanId = Guid.NewGuid().ToString();

            for (var index = 0; index < issueCount; index++)
            {
                var qi = Mock.QueueIssue();
                qi.Id = "";
                qi.Saltminer.QueueScanId = scanId;
                queuedIssues.Add(qi);
            }

            var result = Client.AddUpdateBulk(queuedIssues, QueueIssue.GenerateIndex());
            Thread.Sleep(2000); // gimme a sec or two to save that

            Assert.IsTrue(result.IsSuccessful);
            Assert.AreEqual(issueCount, result.CountAffected);

            //Clean Up
            var issueDelete = Client.DeleteByQuery<QueueIssue>(new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string> { { "saltminer.queue_scan_id", scanId } }
                }
            }, QueueIssue.GenerateIndex());
            Assert.AreEqual(issueCount, issueDelete.CountAffected);
        }

        [TestMethod]
        public void UpdateByQueryueueIssues()
        {
            var queuedIssues = new List<QueueIssue>();
            var issueCount = 5;
            var scanId = Guid.NewGuid().ToString();
            var name = "test";
            var newName = "NewName";
            var location = "test";
            var newLocaion = "NewLocation";

            for (var index = 0; index < issueCount; index++)
            {
                var qi = Mock.QueueIssue();
                qi.Id = "";
                qi.Saltminer.QueueScanId = scanId;
                qi.Vulnerability.Name = name;
                qi.Vulnerability.LocationFull = name;
                qi.Vulnerability.Location = location;
                qi.Vulnerability.RemovedDate = DateTime.UtcNow;
                qi.Vulnerability.IsSuppressed = true;
                qi.Vulnerability.FoundDate = DateTime.UtcNow;
                qi.Saltminer.Engagement.Attributes = null;
               
                queuedIssues.Add(qi);
            }

            var result = Client.AddUpdateBulk(queuedIssues, QueueIssue.GenerateIndex());
            Thread.Sleep(2000); // gimme a sec or two to save that

            Assert.IsTrue(result.IsSuccessful);
            Assert.AreEqual(issueCount, result.CountAffected);

            var searchRequest = new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string> { { "saltminer.queue_scan_id", scanId } }
                }
            };

            var updateRequest = new UpdateQueryRequest<QueueIssue>
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string> { { "saltminer.queue_scan_id", scanId } }
                },
                ScriptUpdates = new Dictionary<string, object> { 
                    { "Vulnerability.Name", newName }, 
                    { "Vulnerability.LocationFull", newName }, 
                    { "Vulnerability.Location", newLocaion }, 
                    { "Vulnerability.RemovedDate", null }, 
                    { "Vulnerability.IsSuppressed", false },
                    { "Vulnerability.FoundDate", DateTime.UtcNow },
                    { "Saltminer.Engagement.Attributes", new Dictionary<string, string> { { "Eddie", "test" } } }
                }
            };

            var newIssues = Client.Search<QueueIssue>(searchRequest, QueueIssue.GenerateIndex());

            Assert.IsTrue(newIssues.IsSuccessful);
            Assert.AreEqual(issueCount, newIssues.CountAffected);
            Assert.AreEqual(newIssues.Results.First().Document.Vulnerability.Name, name);
            Assert.AreEqual(newIssues.Results.First().Document.Vulnerability.Location, location);
            Assert.AreEqual(newIssues.Results.First().Document.Vulnerability.LocationFull, name);
            Assert.IsTrue(newIssues.Results.First().Document.Vulnerability.RemovedDate != null);
            Assert.AreEqual(newIssues.Results.First().Document.Vulnerability.IsSuppressed, true);
            Assert.AreEqual(newIssues.Results.First().Document.Vulnerability.IsActive, false);
            Assert.AreEqual(newIssues.Results.First().Document.Saltminer.Engagement.Attributes, null);

            Client.UpdateByQuery<QueueIssue>(updateRequest, QueueIssue.GenerateIndex());

            Client.RefreshIndex(QueueIssue.GenerateIndex());

            newIssues = Client.Search<QueueIssue>(searchRequest, QueueIssue.GenerateIndex());

            Assert.IsTrue(newIssues.IsSuccessful);
            Assert.AreEqual(issueCount, newIssues.CountAffected);
            Assert.AreEqual(newIssues.Results.First().Document.Vulnerability.Name, newName);
            Assert.AreEqual(newIssues.Results.First().Document.Vulnerability.Location, newLocaion);
            Assert.AreEqual(newIssues.Results.First().Document.Vulnerability.LocationFull, newName);
            Assert.IsTrue(newIssues.Results.First().Document.Vulnerability.RemovedDate == null);
            Assert.IsTrue(newIssues.Results.First().Document.Saltminer.Engagement.Attributes.Count == 1);
            Assert.AreEqual(newIssues.Results.First().Document.Vulnerability.IsSuppressed, false);
            Assert.AreEqual(newIssues.Results.First().Document.Vulnerability.IsActive, true);

            //Clean Up
            var issueDelete = Client.DeleteByQuery<QueueIssue>(new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string> { { "saltminer.queue_scan_id", scanId } }
                }
            }, QueueIssue.GenerateIndex());
            Assert.AreEqual(issueCount, issueDelete.CountAffected);
        }

        [TestMethod]
        public void DeleteManyTests()
        {
            var queueLogs = new List<QueueLog>();
            var queueLog = Mock.QueueLog();

            queueLogs.Add(queueLog);
            Client.AddUpdate(queueLog, QueueLog.GenerateIndex());

            queueLog = Mock.QueueLog();
            queueLogs.Add(queueLog);
            Client.AddUpdate(queueLog, QueueLog.GenerateIndex());

            var result = Client.DeleteBulk<QueueLog>(queueLogs.Select(ql => ql.Id), QueueLog.GenerateIndex());
            Assert.IsTrue(result.IsSuccessful);
            Assert.AreEqual(2, result.CountAffected);
        }

        [TestMethod]
        public void DeleteTests()
        {
            var queueLog = Mock.QueueLog();
            var result = Client.AddUpdate(queueLog, QueueLog.GenerateIndex());
            Assert.IsTrue(result.IsSuccessful);
            result = Client.Delete<QueueLog>(queueLog.Id, QueueLog.GenerateIndex());
            Assert.IsTrue(result.IsSuccessful);

            //Clean Up
            var deleteQueueLog = Client.Delete<QueueLog>(queueLog.Id, QueueLog.GenerateIndex()).CountAffected;
            Assert.AreEqual(1, deleteQueueLog);
        }

        [TestMethod]
        public void TempDeleteTest()
        {
            // Same thing from DataApi?  Null ref exception.  ??????
            Client.DeleteByQuery<License>(new SearchRequest { }, License.GenerateIndex());
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DeleteByQueryNoFilterTest()
        {
            var indexName = "throwaway";
            var request = new SearchRequest { }; // crux of the test, will it break with no search stuff?
            Client.AddUpdate(new ThrowawayEntity { Id = "hi" }, indexName);
            Task.Delay(2000).Wait(); // Will fail unless we give elastic time to process
            var booboo = false;
            var msg = "";
            IElasticClientResponse<ThrowawayEntity> result = null;
            try { result = Client.DeleteByQuery<ThrowawayEntity>(request, indexName); }
            catch (Exception ex) { booboo = true; msg = ex.Message; }
            Assert.IsFalse(booboo, "DeleteByQuery had a boo-boo with no searchy stuff. Msg: {msg}", msg);
            Assert.AreEqual(1, result.CountAffected);
            Client.DeleteIndex(indexName);
        }

        [TestMethod]
        public void DeleteByQueryTest()
        {
            var sourceType = "ElasticClient";
            var issue = Mock.Issue(sourceType);
            var issueCount = 2;
            var issueIndex = Issue.GenerateIndex(issue.Saltminer.Asset.AssetType, issue.Saltminer.Asset.SourceType, issue.Saltminer.Asset.Instance);
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
            var result = Client.DeleteByQuery<Issue>(request, Issue.GenerateIndex(issue.Saltminer.Asset.AssetType, issue.Saltminer.Asset.SourceType, issue.Saltminer.Asset.Instance));
            Assert.AreEqual(2, result.CountAffected);
        }

        [TestMethod]
        public void UpdateWithLocking()
        {
            var queueLog = Mock.QueueLog();
            var result = Client.AddUpdate(queueLog, QueueLog.GenerateIndex());

            var queueDescription = "A different description";
            queueLog.QueueDescription = queueDescription;

            result = Client.UpdateWithLocking(queueLog, QueueLog.GenerateIndex(), result.Result.Primary, result.Result.Sequence);
            Assert.IsTrue(result.IsSuccessful);

            result = Client.Get<QueueLog>(queueLog.Id, QueueLog.GenerateIndex());
            Assert.AreEqual(queueDescription, result.Result.Document.QueueDescription);

            //Clean Up
            var delete = Client.Delete<QueueLog>(result.Result.Document.Id, QueueLog.GenerateIndex()).CountAffected;
            Assert.AreEqual(1, delete);
        }

        [TestMethod]
        public void UpdateWithLocking_Error()
        {
            var queueLog = Mock.QueueLog();
            var result = Client.AddUpdate(queueLog, QueueLog.GenerateIndex());

            Assert.IsTrue(result.IsSuccessful);

            var get = Client.Get<QueueLog>(result.Result.Document.Id, QueueLog.GenerateIndex());
            Assert.IsTrue(get.IsSuccessful);

            get.Result.Document.Message = "test";
            var update = Client.Update(get.Result.Document, QueueLog.GenerateIndex());
            Assert.IsTrue(update.IsSuccessful);

            update.Result.Document.Message = "test2";
            var update2 = Client.UpdateWithLocking(update.Result.Document, QueueLog.GenerateIndex(), get.Result.Primary, get.Result.Sequence);
            Assert.IsFalse(update2.IsSuccessful);

            //Clean Up
            var delete = Client.Delete<QueueLog>(result.Result.Document.Id, QueueLog.GenerateIndex()).CountAffected;
            Assert.AreEqual(1, delete);
        }

        [TestMethod]
        public void QueueLog_AddUpdate()
        {
            var queueLog = Mock.QueueLog();
            var result = Client.AddUpdate(queueLog, QueueLog.GenerateIndex());
            Assert.IsTrue(result.IsSuccessful);

            queueLog.Read = true;

            result = Client.AddUpdate(queueLog, QueueLog.GenerateIndex());
            Assert.IsTrue(result.IsSuccessful);

            queueLog = Client.Get<QueueLog>(queueLog.Id, QueueLog.GenerateIndex()).Result.Document;
            Assert.IsTrue(queueLog.Read);

            //Clean Up
            var delete = Client.Delete<QueueLog>(result.Result.Document.Id, QueueLog.GenerateIndex()).CountAffected;
            Assert.AreEqual(1, delete);
        }

        [TestMethod]
        public void AssetIssue_AddUpdate()
        {
            var sourceType = "ElasticClient";
            var issue = Mock.Issue(sourceType);
            var isssueIndex = Issue.GenerateIndex(issue.Saltminer.Asset.AssetType, issue.Saltminer.Asset.SourceType, issue.Saltminer.Asset.Instance);
            var result = Client.AddUpdate(issue, isssueIndex);
            Assert.IsTrue(result.IsSuccessful);

            issue.Message = "Updated Message";
            issue.Labels["customer_specific_key1"] = "newly updated value";

            result = Client.AddUpdate(issue, isssueIndex);
            Assert.IsTrue(result.IsSuccessful);

            //Clean Up
            var delete = Client.Delete<Issue>(result.Result.Document.Id, isssueIndex).CountAffected;
            Assert.AreEqual(1, delete);
        }

        [TestMethod]
        public void AssetQueueIssue_AddUpdate()
        {
            var queueIssue = Mock.QueueIssue();
            var result = Client.AddUpdate(queueIssue, QueueIssue.GenerateIndex());
            Assert.IsTrue(result.IsSuccessful);

            queueIssue.Saltminer.Attributes = null;
            queueIssue.Labels = new Dictionary<string, string>() { };
            queueIssue.Tags = new string[] { "newtag1", "newtag2" };

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
            var sourceType = "ElasticClient";
            var scan = Mock.Scan(sourceType);
            var scanIndex = Scan.GenerateIndex(scan.Saltminer.Asset.AssetType, scan.Saltminer.Asset.SourceType, scan.Saltminer.Asset.Instance);
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

        public void Snapshot_AddUpdate()
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

            var indexDelete = Client.DeleteIndex(assetSnapshotIndex).IsSuccessful;
            Assert.IsTrue(indexDelete);
        }

        [TestMethod]
        public void Asset_AddUpdate()
        {
            var sourceType = "ElasticClient";
            var asset = Mock.Asset(sourceType);
            var assetIndex = Asset.GenerateIndex(asset.Saltminer.Asset.AssetType, asset.Saltminer.Asset.SourceType, asset.Saltminer.Asset.Instance);
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
            var results = Client.Search<QueueScan>(new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = kvps
                },
                PitPagingInfo = new PitPagingInfo(300)
            }, QueueScan.GenerateIndex());

            Assert.IsTrue(results != null);
        }
    }
}