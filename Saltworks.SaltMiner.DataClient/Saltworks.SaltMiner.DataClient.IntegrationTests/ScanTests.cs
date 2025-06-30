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
using Saltworks.SaltMiner.Core.Entities;
using System.Linq;
using System.Threading;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class ScanTests
    {
        private static DataClient Client = null;
        
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }
            Client = Helpers.GetDataClient<ScanTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(false, true)));
        }

        [ClassCleanup]
        public static void CleanUp()
        {
            Helpers.CleanIndex(Client, "scan");
        }

        [TestMethod]
        public void Crud()
        {
            // Arrange
            var sourceType = "DataClient";
            var sourceId = "F1231";
            var scan = Mock.Scan(sourceType);
            scan.Saltminer.Asset.SourceId = sourceId;
            scan.Id = string.Empty;

            // Act
            scan = Client.ScanAddUpdate(scan).Data;
            Thread.Sleep(2000); // wait for "save" to complete
            var scanGet = Client.ScanGet(scan.Id, scan.Saltminer.Asset.AssetType, scan.Saltminer.Asset.SourceType, scan.Saltminer.Asset.Instance);
            var scanSearch = Client.ScanSearch(Helpers.SearchRequest("Saltminer.Asset.SourceId", sourceId, scan.Saltminer.Asset.AssetType, sourceType));

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(scan.Id), "Scan ID shouldn't be empty after adding it");
            Assert.IsNotNull(scanGet, "Scan should exist and be GETable");
            Assert.IsTrue(scanSearch.Data.Any(), "Search should return at least 1 result");

            //Clean Up
            var scanDelete = Client.ScanDelete(scan.Id, scan.Saltminer.Asset.AssetType, scan.Saltminer.Asset.SourceType, scan.Saltminer.Asset.Instance);
            Assert.IsTrue(scanDelete.Success, "Delete should succeed");
        }
    }
}
