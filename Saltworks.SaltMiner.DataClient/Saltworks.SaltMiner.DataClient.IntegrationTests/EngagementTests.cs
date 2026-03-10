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

            // Act
            var summary = Client.EngagementIssueCounts(engagement.Id, aType, srcType, instance);

            // Assert
            Assert.IsNotNull(summary.Data);
            Assert.AreEqual(5, summary.Data.Count, "Response should contain counts for all 5 severity levels");
            foreach (var severity in new[] { "Critical", "High", "Medium", "Low", "Info" })
            {
                Assert.IsTrue(summary.Data.ContainsKey(severity), $"Summary is missing severity: {severity}");
                Assert.AreEqual(1, summary.Data[severity], $"Expected 1 issue for severity {severity} but found {summary.Data[severity]}");
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
