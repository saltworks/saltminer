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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Threading;
using System.Linq;
using System.IO;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Util;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests
{
    [TestClass]
    public class SnapshotTests
    {
        private static IElasticClient Client = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            var c = Helpers.SettingsConfig();
            Client = Helpers.GetElasticClient(c);
        }

        [TestMethod]
        public void TestAggregateBucketIssuesLoading()
        {
            var sourceFields = new List<string>
            {
                "Saltminer.Asset.SourceId",
                "Saltminer.Asset.AssetType",
                "Saltminer.Asset.SourceType",
                "Saltminer.Asset.Instance",
                "Vulnerability.Scanner.AssessmentType",
                "Vulnerability.Name",
                "Vulnerability.Severity"
            };
            var sumAggFields = new List<string>
            {
                "saltminer.critical",
                "saltminer.high",
                "saltminer.medium",
                "saltminer.low",
                "saltminer.info"
            };

            var aggList = sumAggFields.Select(x => Client.BuildRequestAggregate(x, x, ElasticAggregateType.Sum)).ToList();

            // Act/Assert
            using StreamWriter file = new("TestAggregateBucketIssuesLoading.txt", false);
            file.AutoFlush = true;

            var request = new SearchRequest
            {
                AssetType = "App",
                PitPagingInfo = new PitPagingInfo()
            };

            var response = Client.GetCompositeAggregate<Snapshot>(request, sourceFields, aggList, Issue.GenerateIndex());
            foreach (var b in response.Results)
            {
                var searchVals = b.Document.BucketKey.Split("|").Select(v => v.Replace("{P}", "|")).ToArray();
                var count = 0;
                var srsp = Client.Search<Issue>(new SearchRequest
                {
                    Filter = new()
                    {
                        FilterMatches = new Dictionary<string, string> {
                            { "Saltminer.Asset.AssetType", searchVals[0] },
                            { "Saltminer.Asset.SourceType", searchVals[1] },
                            { "Saltminer.Asset.SourceId", searchVals[2] },
                            { "Vulnerability.Name", searchVals[3] }
                        }
                    },
                    PitPagingInfo = new PitPagingInfo()
                }, Issue.GenerateIndex());
                if (srsp?.Results?.Any() ?? false)
                {
                    count++;
                    file.WriteLine($"Bucket {count} FOUND with values '{b.Document.BucketKey}'");
                }
                else
                {
                    file.WriteLine($"Bucket {count} NOT FOUND with values '{b.Document.BucketKey}'");
                }

                // Assert
                Assert.IsTrue(srsp?.Results?.Any() ?? false);
            }
        }

        [TestMethod]
        public void GetAggregateBucketList()
        {
            var aggFields = new List<string>
            {
                "Saltminer.Asset.AssetType",
                "Saltminer.Asset.SourceType",
                "Saltminer.Asset.SourceId",
                "Vulnerability.Name"
            };

            var sumAggFields = new List<string>
            {
                "saltminer.critical",
                "saltminer.high",
                "saltminer.medium",
                "saltminer.low",
                "saltminer.info"
            };

            var aggList = sumAggFields.Select(x => Client.BuildRequestAggregate(x, x, ElasticAggregateType.Sum)).ToList();

            var request = new SearchRequest
            {
                AssetType = "App",
                PitPagingInfo = new PitPagingInfo(5)
            };

            // Act
            var response = Client.GetCompositeAggregate<Snapshot>(request, aggFields, aggList, Issue.GenerateIndex("App"));
            request.PitPagingInfo.Size = 6;
            var response2 = Client.GetCompositeAggregate<Snapshot>(request, aggFields, aggList, Issue.GenerateIndex());

            // Assert
            Assert.IsNotNull(response);
            Assert.IsNotNull(response2);
            Assert.AreEqual(response.Results?.Count(), 5);
            Assert.AreEqual(response2.Results?.Count(), 6);
            Assert.AreNotEqual(response.Results?.First().Document.BucketKey, response2.Results?.First().Document.BucketKey);
        }

        [TestMethod]
        public void TestSearch()
        {
            var searchRequest = new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string> {
                        {"Saltminer.Asset.SourceType", "Sonatype" },
                        {"Saltminer.Asset.SourceId", "04bdd5108b1c4b1491b7ec3e9a653636|release" }
                    }
                }
            };

            var kvps = searchRequest.ToSearchRequest();

            var response = Client.Search<Snapshot>(new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = kvps
                },
                PitPagingInfo = new PitPagingInfo()
            }, Snapshot.GenerateIndex(AssetType.Mocked.ToString()));

            // Assert
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Results);
        }

        [TestMethod]
        public void TestAttributes() {
            var instance = "Fortify321";
            var assetType = "ElasticClient";
            var srcId = "F321";
            var daily = false;
            var sdate = DateTime.Parse((DateTime.UtcNow.Month == 1 ? 12 : DateTime.UtcNow.Month).ToString() + "/15/" + (DateTime.UtcNow.Month == 1 ? DateTime.UtcNow.Year - 1 : DateTime.UtcNow.Year).ToString());
            var s = Mock.Snapshot();
            s.Id = string.Empty;
            s.Saltminer.SnapshotDate = sdate;
            // Act
            var result = Client.AddUpdate(s, Snapshot.GenerateIndex(AssetType.Mocked.ToString(), daily)).Result.Document;
            Thread.Sleep(2000); // wait for "save" to complete

            var attr = new Dictionary<string, string>
            {
                { "Test", "123" },
                { "123", "Test" }
            };

            var newResponse = Client.BulkPartialUpdate(new List<Snapshot>() { result }, (doc) => Snapshot.GenerateIndex(AssetType.Mocked.ToString(), daily), "ctx._source.saltminer.asset.attributes = params.object", attr);

            Assert.IsNotNull(newResponse);

            //Clean Up
            var delete = Client.Delete<Snapshot>(result.Id, Snapshot.GenerateIndex(AssetType.Mocked.ToString(), daily)).CountAffected;
            Assert.AreEqual(1, delete);

            var indexDelete = Client.DeleteIndex(Snapshot.GenerateIndex(AssetType.Mocked.ToString(), daily)).IsSuccessful;
            Assert.IsTrue(indexDelete);
        }
    }
}
