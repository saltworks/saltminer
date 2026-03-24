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
using Saltworks.SaltMiner.Core.Entities;
using System.Linq;
using System.Threading.Tasks;

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

        [TestMethod]
        public void Crud()
        {
            // Arrange
            var sourceType = "DataClient";
            var sourceId = "F1231";
            var scan = Mock.Scan(sourceType);
            scan.Saltminer.Asset.SourceId = sourceId;
            scan.Id = string.Empty;
            var scanIndex = Scan.GenerateIndex(scan.Saltminer.Asset.AssetType, scan.Saltminer.Asset.SourceType, scan.Saltminer.Asset.Instance);
            Helpers.RegisterDeleteIndex(scanIndex);

            // Act
            scan = Client.ScanAddUpdate(scan).Data;
            Client.RefreshIndex(Scan.GenerateIndex(scan.Saltminer.Asset.AssetType, scan.Saltminer.Asset.SourceType, scan.Saltminer.Asset.Instance));
            Task.Delay(2000).Wait(); // wait for "save" to complete
            var scanGet = Client.ScanGet(scan.Id, scan.Saltminer.Asset.AssetType, scan.Saltminer.Asset.SourceType, scan.Saltminer.Asset.Instance);
            var scanSearch1 = Client.ScanSearch(Helpers.SearchRequest("Saltminer.Asset.SourceId", sourceId, scan.Saltminer.Asset.AssetType, scan.Saltminer.Asset.SourceType, scan.Saltminer.Asset.Instance));
            var scanSearch2 = Client.ScanSearch(Helpers.SearchRequest("Saltminer.Asset.SourceId", sourceId, scan.Saltminer.Asset.AssetType, scan.Saltminer.Asset.SourceType));

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(scan.Id), "Scan ID shouldn't be empty after adding it");
            Assert.IsNotNull(scanGet, "Scan should exist and be GETable");
            Assert.IsTrue(scanSearch1.Data.Any(), "Search should return at least 1 result with instance specified");
            Assert.IsTrue(scanSearch2.Data.Any(), "Search should return at least 1 result with no instance specified");

            //Clean Up
            var scanDelete = Client.ScanDelete(scan.Id, scan.Saltminer.Asset.AssetType, scan.Saltminer.Asset.SourceType, scan.Saltminer.Asset.Instance);
            Assert.IsTrue(scanDelete.Success, "Delete should succeed");
        }
    }
}
