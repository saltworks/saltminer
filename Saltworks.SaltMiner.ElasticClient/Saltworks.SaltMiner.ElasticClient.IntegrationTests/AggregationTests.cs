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
using System.Linq;
using Saltworks.SaltMiner.Core.Entities;
using System.Threading;
using Saltworks.SaltMiner.Core.Data;
using System;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests
{
    [TestClass]
    public class AggregationTests
    {
        private const string SOURCE_TYPE = "ElasticClient";
        private static IElasticClient Client = null;
        private static readonly List<string> _indicesToDelete = [];

        private static void RegisterDeleteIndex(string index)
        {
            if (!_indicesToDelete.Contains(index))
                _indicesToDelete.Add(index);
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


        [ClassInitialize]
        public static void Initialize(TestContext _)
        {
            var c = Helpers.SettingsConfig();
            Client = Helpers.GetElasticClient(c);
        }
        
        [TestMethod]
        public async Task Aggregate_No_Query()
        {
            // Arrange
            var agg = Client.BuildRequestAggregation("counts", "Saltminer.Asset.SourceType", [
                Client.BuildRequestAggregate("Saltminer.critical", "Saltminer.Critical", ElasticAggregateType.Sum),
                Client.BuildRequestAggregate("Saltminer.high", "Saltminer.High", ElasticAggregateType.Sum),
                Client.BuildRequestAggregate("Saltminer.medium", "Saltminer.Medium", ElasticAggregateType.Sum),
                Client.BuildRequestAggregate("Saltminer.low", "Saltminer.Low", ElasticAggregateType.Sum)
            ]);

            // Act
            var sourceType = SOURCE_TYPE;
            var issue = Mock.Issue(sourceType);
            var issueIndex = Issue.GenerateIndex(issue.Saltminer.Asset.AssetType, issue.Saltminer.Asset.SourceType, issue.Saltminer.Asset.Instance);
            RegisterDeleteIndex(issueIndex);
            var issueResult = Client.AddUpdate(issue, issueIndex);
            await Task.Delay(2000); // give time to digest
            var request = new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = null
                }
            };

            var response = Client.SearchWithCompositeAgg(agg, request, issueIndex);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsTrue(response.Results.Any());

            //Clean up
            var ok = Client.Delete<Issue>(issueResult.Result.Document.Id, issueIndex).IsSuccessful;
            Assert.IsTrue(ok);

        }

        [TestMethod]
        public async Task Aggregate_With_Query()
        {
            // Arrange
            var agg = Client.BuildRequestAggregation("counts", "Saltminer.Asset.SourceType", 
            [
                Client.BuildRequestAggregate("Saltminer.critical", "Saltminer.Critical", ElasticAggregateType.Sum),
                Client.BuildRequestAggregate("Saltminer.high", "Saltminer.High", ElasticAggregateType.Sum),
                Client.BuildRequestAggregate("Saltminer.medium", "Saltminer.Medium", ElasticAggregateType.Sum),
                Client.BuildRequestAggregate("Saltminer.low", "Saltminer.Low", ElasticAggregateType.Sum)
            ]);
            var qry = new Dictionary<string, string> { { "Saltminer.Asset.IsProduction", "true" } };

            // Act
            var sourceType = SOURCE_TYPE;
            var issue = Mock.Issue(sourceType);
            var issueIndex = Issue.GenerateIndex(issue.Saltminer.Asset.AssetType, issue.Saltminer.Asset.SourceType, issue.Saltminer.Asset.Instance);
            RegisterDeleteIndex(issueIndex);
            issue = Client.AddUpdate(issue, issueIndex).Result.Document;
            await Task.Delay(2000); // give time to digest
            var request = new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = qry
                }
            };

            var response = Client.SearchWithCompositeAgg(agg, request, issueIndex);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsTrue(response.Results.Any());

            //Clean Up
            var ok = Client.Delete<Issue>(issue.Id, issueIndex).IsSuccessful;
            Assert.IsTrue(ok);
        }

        [TestMethod]
        public async Task Aggregate_Source_Counts()
        {
            // Arrange
            var instance = "ElasticClient";
            var sourceType = SOURCE_TYPE;
            var sourceId = "ElasticClientTest001";
            var scan = Mock.Scan(sourceType);
            var scanIndex = Scan.GenerateIndex(scan.Saltminer.Asset.AssetType, scan.Saltminer.Asset.SourceType, scan.Saltminer.Asset.Instance);
            RegisterDeleteIndex(scanIndex);
            scan.Saltminer.Asset.SourceId = sourceId;
            scan.Id = "";
            scan = Client.AddUpdate(scan, scanIndex).Result.Document;

            var list = new List<Issue>();
            for (var x = 1; x < 6; x++)
            {
                var issue = Mock.Issue(sourceType);
                issue.Id = "";
                issue.Saltminer.Scan = new IssueScanInfo
                {
                    Id = scan.Id
                };
                issue.Saltminer.Critical = 1;
                issue.Saltminer.High = 0;
                issue.Saltminer.Medium = 0;
                issue.Saltminer.Low = 0;
                issue.Saltminer.Asset.SourceId = sourceId;
                list.Add(issue);
            }
            var issueIndex = Issue.GenerateIndex(scan.Saltminer.Asset.AssetType, sourceType, instance);

            Client.AddUpdateBulk(list, issueIndex);
            await Task.Delay(2000); // wait for save to complete

            var agg = Client.BuildRequestAggregation("Saltminer.Asset.Instance", "Saltminer.Asset.SourceType",
            [
                Client.BuildRequestAggregate("Saltminer.Critical", "Saltminer.Critical", ElasticAggregateType.Sum),
                Client.BuildRequestAggregate("Saltminer.High", "Saltminer.High", ElasticAggregateType.Sum),
                Client.BuildRequestAggregate("Saltminer.Medium", "Saltminer.Medium", ElasticAggregateType.Sum),
                Client.BuildRequestAggregate("Saltminer.Low", "Saltminer.Low", ElasticAggregateType.Sum)
            ]);
            var qry = new Dictionary<string, string>
            {
                { "Saltminer.Asset.Instance", sourceType },
                { "Saltminer.Asset.SourceId", sourceId }
            };
            var request = new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = qry
                }
            };

            // Act
            var response = Client.SearchWithCompositeAgg(agg, request, issueIndex);
            var result = response.Results.First().Document;

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(instance, result.BucketKey);
            Assert.IsTrue(result.Aggregates.Count > 0);

            //Clean Up
            var scanDelete = Client.Delete<Scan>(scan.Id, scanIndex);
            var deleteRequest = new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = qry
                }
            };
            var issueDelete = Client.DeleteByQuery<Issue>(deleteRequest, issueIndex);
            Assert.AreEqual(1, scanDelete.CountAffected);
            Assert.AreEqual(list.Count, issueDelete.CountAffected);
        }
    }
}
