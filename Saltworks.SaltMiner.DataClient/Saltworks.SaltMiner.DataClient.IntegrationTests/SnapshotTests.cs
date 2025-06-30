/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class SnapshotTests
    {
        private static DataClient Client = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }

            Client = Helpers.GetDataClient<SnapshotTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(true)));
        }

        [TestMethod]
        public void Crud()
        {
            // Arrange
            var sourceId = "F1231";
            var daily = false;
            var sdate = DateTime.Parse((DateTime.UtcNow.Month == 1 ? 12 : DateTime.UtcNow.Month).ToString() + "/15/" + (DateTime.UtcNow.Month == 1 ? DateTime.UtcNow.Year - 1 : DateTime.UtcNow.Year).ToString());
            var snapshot = Mock.Snapshot();
            snapshot.Id = string.Empty;
            snapshot.Saltminer.SnapshotDate = sdate;
            snapshot.Saltminer.Asset.SourceId = sourceId;
            // Act
            var snapshot1 = Client.SnapshotAddUpdate(snapshot).Data;
            Thread.Sleep(2000); // wait for "save" to complete
            var search = Helpers.SearchRequest("SnapshotDate", sdate.ToString("O"), snapshot.Saltminer.Asset.AssetType, null);
            Assert.AreEqual(2, search.Filter.FilterMatches.Count);
            var search2 = Client.SnapshotSearch(search);
            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(snapshot1.Id), "AssetSnapshot ID shouldn't be empty after adding it");
            Assert.AreEqual(1, search2.Data.Count(), "AssetSnapshot should be found by search");

            //Clean Up
            var delete = Client.SnapshotDelete(Helpers.SearchRequest(new Dictionary<string, string>() { { "Saltminer.SourceId", sourceId } }, snapshot.Saltminer.Asset.AssetType));
            Assert.AreEqual(1, delete.Affected, "Delete should succeed for 1 AssetSnapshot");

            var indexDelete = Client.DeleteIndex(Snapshot.GenerateIndex(snapshot.Saltminer.Asset.AssetType, daily)).Success;
            Assert.IsTrue(indexDelete);
        }

        [TestMethod]
        public void CrudBatch()
        {
            // Arrange
            var assetId1 = "F1231";

            var assetId2 = "F1232";

            var assetId3 = "F1233";

            var assetId4 = "F1234";

            var daily = false;
            var sdate = DateTime.Parse((DateTime.UtcNow.Month == 1 ? 12 : DateTime.UtcNow.Month).ToString() + "/15/" + (DateTime.UtcNow.Month == 1 ? DateTime.UtcNow.Year - 1 : DateTime.UtcNow.Year).ToString());

            var snapshot1 = Mock.Snapshot();
            var snapshot2 = Mock.Snapshot();
            var snapshot3 = Mock.Snapshot();
            var snapshot4 = Mock.Snapshot();

            snapshot1.Id = string.Empty;
            snapshot1.Saltminer.Asset.AssetType = AssetType.Mocked.ToString();
            snapshot1.Saltminer.Asset.Id = assetId1;
            snapshot1.Saltminer.SnapshotDate = sdate;

            snapshot2.Id = string.Empty;
            snapshot2.Saltminer.Asset.AssetType = AssetType.Mocked.ToString();
            snapshot2.Saltminer.Asset.Id = assetId2;
            snapshot2.Saltminer.SnapshotDate = sdate;

            snapshot3.Id = string.Empty;
            snapshot3.Saltminer.Asset.AssetType = AssetType.Mocked.ToString();
            snapshot3.Saltminer.Asset.Id = assetId3;
            snapshot3.Saltminer.SnapshotDate = sdate;

            snapshot4.Id = string.Empty;
            snapshot4.Saltminer.Asset.AssetType = AssetType.Mocked.ToString();
            snapshot4.Saltminer.Asset.Id = assetId4;
            snapshot4.Saltminer.SnapshotDate = sdate;

            // Act
            var response = Client.SnapshotAddUpdateBatch(new List<Snapshot> { snapshot1, snapshot2, snapshot3, snapshot4 });
            Thread.Sleep(2000); // wait for "save" to complete

            // Assert
            Assert.AreEqual(4, response.Affected);

            //Clean up
            var delete = Client.SnapshotDelete(new SearchRequest
            {
                AssetType = AssetType.Mocked.ToString()
            });

            Assert.AreEqual(1, delete.Affected, "Delete should succeed for 4 AssetSnapshot");

            var indexDelete = Client.DeleteIndex(Snapshot.GenerateIndex(snapshot1.Saltminer.Asset.AssetType, daily)).Success;
            Assert.IsTrue(indexDelete);
        }

        [TestMethod]
        public void SnapshotCounts()
        {
            //Arrange
            var assetType = "Test";

            //Act
            var response = Client.SnapshotCounts(new SearchRequest
            {
                AssetType = assetType
            });

            //Asset
            Assert.IsNotNull(response);
            Assert.IsTrue(response.Results.Count > 0);
        }
    }
}
