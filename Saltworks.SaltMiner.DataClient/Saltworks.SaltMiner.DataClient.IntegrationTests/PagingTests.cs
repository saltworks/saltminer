using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class PagingTests
    {
        private static DataClient Client = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }
            Client = Helpers.GetDataClient<PagingTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(false, true)));
        }

        [ClassCleanup]
        public static void CleanUp()
        {
            Helpers.CleanIndex(Client, "issue");
            Helpers.CleanIndex(Client, "scan");
        }

        [TestMethod]
        public void PitPaging()
        {
            var count = 10;
            var batchSize = 5;
            var sourceType = "DataClient";
            var batch = new List<Issue>();
            var processed = 0;

            var scan = Mock.Scan(sourceType);
            var asset = Mock.Asset(sourceType);

            var scanResponse = Client.ScanAddUpdate(scan);
            var assetResponse = Client.AssetAddUpdate(asset);

            string scanId = scanResponse.Data.Id;
            string assetId = assetResponse.Data.Id;

            for (int i = 1; i <= count; i++)
            {
                var issue = Mock.Issue(sourceType);
                issue.Vulnerability.ReportId = $"100{i}";
                issue.Saltminer.Scan.Id = scanId;
                issue.Saltminer.Asset.Id = assetId;
                issue.Id = $"{issue.Id}{i}";

                batch.Add(issue);

                if (i % batchSize == 0)
                {
                    Client.IssuesAddUpdateBulk(batch);
                    batch = new List<Issue>();
                }
            }

            Thread.Sleep(2000); // wait for "save" to complete

            var request = new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string> { { "Saltminer.Scan.Id", scan.Id } }
                },
                AssetType = asset.Saltminer.Asset.AssetType,
                SourceType = sourceType,
                PitPagingInfo = new PitPagingInfo(20, true)
            };

            var response = Client.IssueSearch(request);

            while (response.Data.Any())
            {
                foreach (var issue in response.Data)
                {
                    processed++;
                }
                request.AfterKeys = response.AfterKeys;
                request.PitPagingInfo = response.PitPagingInfo;
                response = Client.IssueSearch(request);
            }
            
            Assert.AreEqual(count, processed);

            Client.IssuesDeleteByScan(scanResponse.Data.Id, scan.Saltminer.Asset.AssetType, scan.Saltminer.Asset.SourceType, assetResponse.Data.Saltminer.Asset.Instance);
            Client.AssetDelete(assetResponse.Data.Id, assetResponse.Data.Saltminer.Asset.AssetType, assetResponse.Data.Saltminer.Asset.SourceType, assetResponse.Data.Saltminer.Asset.Instance);
            Client.ScanDelete(scanResponse.Data.Id, scanResponse.Data.Saltminer.Asset.AssetType, scanResponse.Data.Saltminer.Asset.SourceType, assetResponse.Data.Saltminer.Asset.Instance);
        }

        [TestMethod]
        public void UIPaging()
        {
            var count = 500;
            var batchSize = 50;
            var sourceType = "DataClient";
            var batch = new List<Issue>();
            var processed = 0;

            var scan = Mock.Scan(sourceType);
            var asset = Mock.Asset(sourceType);

            var scanResponse = Client.ScanAddUpdate(scan);
            var assetResponse = Client.AssetAddUpdate(asset);

            for (int i = 1; i <= count; i++)
            {
                var issue = Mock.Issue(sourceType);
                issue.Vulnerability.ReportId = $"100{i}";
                issue.Saltminer.Scan.Id = scanResponse.Data.Id;
                issue.Saltminer.Asset.Id = assetResponse.Data.Id;
                issue.Id = $"{issue.Id}{i}";

                batch.Add(issue);

                if (i % batchSize == 0)
                {
                    Client.IssuesAddUpdateBulk(batch);
                    batch = new List<Issue>();
                }
            }

            Thread.Sleep(2000); // wait for "save" to complete

            var searchRequest = new SearchRequest
            {
                AssetType = scan.Saltminer.Asset.AssetType,
                SourceType = sourceType,
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Scan.Id", scanResponse.Data.Id }
                    }
                },
                UIPagingInfo = new UIPagingInfo(20)
            };

            var response = Client.IssueSearch(searchRequest);

            while (response?.Data != null && response.Data.Any())
            {
                foreach (var issue in response.Data)
                {
                    processed++;
                }

                searchRequest.UIPagingInfo = response.UIPagingInfo;
                searchRequest.UIPagingInfo.Page++;

                response = Client.IssueSearch(searchRequest); // continue previous via scrolling
            }

            Assert.AreEqual(count, processed);

            Client.IssuesDeleteByScan(scanResponse.Data.Id, scan.Saltminer.Asset.AssetType, scan.Saltminer.Asset.SourceType, assetResponse.Data.Saltminer.Asset.Instance);
            Client.AssetDelete(assetResponse.Data.Id, assetResponse.Data.Saltminer.Asset.AssetType, assetResponse.Data.Saltminer.Asset.SourceType, assetResponse.Data.Saltminer.Asset.Instance);
            Client.ScanDelete(scanResponse.Data.Id, scanResponse.Data.Saltminer.Asset.AssetType, scanResponse.Data.Saltminer.Asset.SourceType, assetResponse.Data.Saltminer.Asset.Instance);
        }
    }
}
