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

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.Manager.IntegrationTests
{
    /// <summary>
    /// PBI-115: needs Elasticsearch, a running Data API and settings.json (TestCategory Live).
    /// </summary>
    [TestClass]
    public class SnapshotProcessorRunTests
    {
        private const int BatchSize = 100;



        [TestMethod]
        [TestCategory("Live")]
        public void Run_MoreGroupsThanOneBatch_PagesThroughAllAndEnds()
        {
            const int groups = 250;
            var config = Helpers.GetConfig();
            var options = Helpers.GetDataClientOptions(config);
            var factory = Helpers.CreateDataClientFactory<Manager>(options);
            var client = factory.GetClient();

            var assetType = EnumExtensions.GetDescription(AssetType.App);
            // Source type accepts letters and dots only, so the run suffix is letters, not hex.
            var run = new string(Guid.NewGuid().ToString("N")[..8].Select(c => (char)('a' + Convert.ToInt32(c.ToString(), 16))).ToArray());
            var sourceType = "snapshotpaging" + run;
            var sourceId = "pbi115-" + run;
            var instance = sourceType;
            var assetIndex = Asset.GenerateIndex(assetType, sourceType, instance);
            var issueIndex = Issue.GenerateIndex(assetType, sourceType, instance);
            var scanIndex = Scan.GenerateIndex(assetType, sourceType, instance);

            try
            {
                var asset = Mock.Asset(sourceType);
                asset.Id = string.Empty;
                asset.Saltminer.Engagement = null;
                asset.Saltminer.Asset.AssetType = assetType;
                asset.Saltminer.Asset.Instance = instance;
                asset.Saltminer.Asset.SourceId = sourceId;
                asset = client.AssetAddUpdate(asset).Data;

                // The Data API rejects issues whose scan does not exist, so seed a scan for the same asset first.
                var scan = Mock.Scan(sourceType);
                scan.Id = string.Empty;
                scan.Saltminer.Engagement = null;
                scan.Saltminer.Asset.Id = asset.Id;
                scan.Saltminer.Asset.AssetType = assetType;
                scan.Saltminer.Asset.Instance = instance;
                scan.Saltminer.Asset.SourceId = sourceId;
                scan = client.ScanAddUpdate(scan).Data;

                var issues = Enumerable.Range(0, groups).Select(i =>
                {
                    var issue = Mock.Issue(sourceType);
                    issue.Id = string.Empty;
                    issue.Saltminer.Asset.Id = asset.Id;
                    issue.Saltminer.Asset.AssetType = assetType;
                    issue.Saltminer.Asset.Instance = instance;
                    issue.Saltminer.Asset.SourceId = sourceId;
                    issue.Saltminer.Scan.Id = scan.Id;
                    issue.Saltminer.Engagement = null;
                    issue.Saltminer.IsHistorical = false;
                    issue.Vulnerability.Name = $"pbi115-vuln-{i:D4}";
                    return issue;
                }).ToList();
                client.IssuesAddUpdateBulk(issues);
                client.RefreshIndex(assetIndex);
                client.RefreshIndex(issueIndex);
                Task.Delay(2000).Wait();

                var logger = new CapturingLogger();
                var managerConfig = new ManagerConfig { SnapshotProcessorApiBatchSize = BatchSize };
                var processor = new SnapshotProcessor(logger, factory, managerConfig);
                using var cts = new CancellationTokenSource();
                var limit = TimeSpan.FromMinutes(5);
                cts.CancelAfter(limit);

                var runTask = Task.Run(() => processor.Run(new SnapshotRuntimeConfig(sourceType, sourceId, 0, false, cts.Token)));
                var finished = runTask.Wait(limit + TimeSpan.FromSeconds(30));

                string[] lines;
                lock (logger.Lines) lines = logger.Lines.Select(l => l.Message).ToArray();
                Assert.IsTrue(finished, "The run did not end within its time limit");
                Assert.IsTrue(lines.Any(l => l.Contains("Snapshot processing complete")), "The run did not reach its normal end");

                var sent = lines.Where(l => l.Contains("sending batch")).ToList();
                // The Data API aggregates every active issue in the datastore and ignores the SourceId filter, so a shared
                // datastore adds groups of its own: require at least the 3 batches our 250 groups need, each batch number once,
                // and a normal end. A paging loop would repeat batches until the time limit and fail the checks above.
                Assert.IsTrue(sent.Count >= 3, "Expected at least batches 1 to 3: " + string.Join(" | ", sent));
                for (var n = 1; n <= sent.Count; n++)
                    Assert.AreEqual(1, sent.Count(l => l.Contains($"sending batch {n} to datastore")), $"batch {n}");

                // Every seeded group was read exactly once: one bucket log line per group. (The snapshots go to the daily
                // index, which the search endpoint does not read, so the log is the evidence of what was paged.)
                Assert.AreEqual(groups, lines.Count(l => l.Contains("Bucket values") && l.Contains(sourceId)), "One bucket line per seeded group");
            }
            finally
            {
                try
                {
                    client.SnapshotDelete(new SearchRequest
                    {
                        AssetType = assetType,
                        Filter = new Filter { FilterMatches = new Dictionary<string, string> { { "Saltminer.Asset.SourceId", sourceId } } }
                    });
                }
                catch (Exception) { /* best effort */ }
                try { client.DeleteIndex(assetIndex); } catch (Exception) { /* best effort */ }
                try { client.DeleteIndex(issueIndex); } catch (Exception) { /* best effort */ }
                try { client.DeleteIndex(scanIndex); } catch (Exception) { /* best effort */ }
            }
        }
    }
}
