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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using System;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests;

[TestClass]
public class EngagementTests
{
    private static DataClient Client = null;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        if (context == null)
        {
            return;
        }

        Client = Helpers.GetDataClient<EngagementTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(true)));
    }

    [TestMethod]
    public void TempCounts()
    {
        var srcType = SourceType.Pentest.ToString("g");
        var aType = AssetType.Pen.ToString("g");
        var instance = "Pentest";
        var summaryNoIssues = Client.EngagementIssueCounts("d6943cef-b0ba-41b5-b8c1-6fb3d7bb5ea3", aType, srcType, instance);
        Assert.IsNotNull(summaryNoIssues.Data);
    }

    [TestMethod]
    public void SummaryCounts()
    {
        var eid = "";
        try {

            // Arrange
            var srcType = SourceType.Pentest.ToString("g");
            var aType = AssetType.Pen.ToString("g");
            var instance = "Pentest";
            var engagement = Mock.Engagement();
            engagement.Saltminer.Engagement.Subtype = "Pen";
            engagement = Client.EngagementAddUpdate(engagement).Data;
            eid = engagement.Id;
            var qscan = Mock.QueueScan();
            qscan.Saltminer.Scan.SourceType = srcType;
            qscan.Saltminer.Scan.AssetType = aType;
            qscan.Saltminer.Scan.Instance = instance;
            qscan.Saltminer.Scan.AssessmentType = AssessmentType.Pen.ToString("g");
            qscan.Saltminer.Scan.ScanDate = DateTime.UtcNow;
            qscan.Saltminer.Engagement = engagement.Saltminer.Engagement;
            qscan = Client.QueueScanAddUpdate(qscan).Data;
            var qasset = Mock.QueueAsset();
            qasset.Saltminer.Engagement = engagement.Saltminer.Engagement;
            qasset.Saltminer.Internal.QueueScanId = qscan.Id;
            qasset.Saltminer.Asset.SourceType = srcType;
            qasset.Saltminer.Asset.AssetType = aType;
            qasset.Saltminer.Asset.Instance = instance;
            qasset = Client.QueueAssetAddUpdate(qasset).Data;

            // Act
            var summaryNoIssues = Client.EngagementIssueCounts(engagement.Id, aType, srcType, instance);
            foreach (var severity in new[] { "Critical", "High", "Medium", "Low", "Info" })
            {
                var qissue = Mock.QueueIssue();
                qissue.Saltminer.Engagement = engagement.Saltminer.Engagement;
                qissue.Saltminer.Engagement.Id = engagement.Id; // Sync nested engagement ID
                qissue.Saltminer.QueueScanId = qscan.Id;
                qissue.Saltminer.QueueAssetId = qasset.Id;
                qissue.Vulnerability.Severity = severity;
                Client.QueueIssueAddUpdate(qissue);
            }
            Client.RefreshIndex(QueueIssue.GenerateIndex());

            var summaryWithIssues = Client.EngagementIssueCounts(engagement.Id, aType, srcType, instance);

            // Assert
            Assert.IsNotNull(summaryNoIssues.Data);
            Assert.AreEqual(0, summaryNoIssues.Data.Count, "Summary should be empty when there are no issues");

            Assert.IsNotNull(summaryWithIssues.Data);
            Assert.AreEqual(5, summaryWithIssues.Data.Count, "Response should contain counts for all 5 severity levels");
            foreach (var severity in new[] { "Critical", "High", "Medium", "Low", "Info" })
            {
                Assert.IsTrue(summaryWithIssues.Data.ContainsKey(severity), $"Summary is missing severity: {severity}");
                Assert.AreEqual(1, summaryWithIssues.Data[severity], $"Expected 1 issue for severity {severity} but found {summaryWithIssues.Data[severity]}");
            }
        
        }
        catch (AssertFailedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Assert.Fail($"Test failed with exception: {ex.Message}");
        }
        finally
        {
            // Cleanup - delete the engagement and all associated data
            Client.EngagementDelete(eid);
        }
    }


    [TestMethod]
    public void Crud()
    {
        var engagement = Mock.Engagement();

        engagement = Client.EngagementAddUpdate(engagement).Data;
        Task.Delay(2000).Wait();
        var search = Client.EngagementSearch(new SearchRequest
        {
            Filter = new()
            {
                FilterMatches = []
            },
            PagingInfo = new PagingInfo(10)
        });

        Assert.IsNotNull(search.Data);

        var get = Client.EngagementGet(engagement.Id);

        Assert.IsNotNull(get.Data);

        Client.EngagementDelete(engagement.Id);

        try
        {
            Client.EngagementGet(engagement.Id);
        }
        catch (Exception ex)
        {
            Assert.IsTrue(ex.Message.ToLower().Contains("not found", StringComparison.OrdinalIgnoreCase));
        }
    }
}
