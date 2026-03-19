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
using System.Threading;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class AssetTests
    {
        private static DataClient Client = null;
        
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }
            Client = Helpers.GetDataClient<AssetTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(false, true)));
        }

        [TestMethod]
        public void Crud()
        {
            // Arrange
            var sourceType = "DataClient";
            var instance = "UnitTest";
            var sourceId = "F1231";
            var asset = Mock.Asset(sourceType);
            asset.Id = string.Empty;
            // Ensure instance matches the index we will query against
            asset.Saltminer.Asset.Instance = instance;
            asset.Saltminer.Asset.SourceType = sourceType;
            asset.Saltminer.Asset.SourceId = sourceId;
            var assetIndex = Asset.GenerateIndex(asset.Saltminer.Asset.AssetType, sourceType, instance);
            Helpers.RegisterDeleteIndex(assetIndex);

            // Act
            var asset1 = Client.AssetAddUpdate(asset).Data;
            Client.RefreshIndex(Asset.GenerateIndex(asset.Saltminer.Asset.AssetType, sourceType, instance));
            Task.Delay(2000).Wait(); // wait for "save" to complete
            var asset2 = Client.AssetGet(asset1.Id, asset.Saltminer.Asset.AssetType, sourceType, instance);
            var response = Client.AssetSearch(Helpers.SearchRequest("Saltminer.Asset.SourceId", sourceId, asset.Saltminer.Asset.AssetType, sourceType, instance));

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(asset1.Id), "Asset ID shouldn't be empty after adding it");
            Assert.IsNotNull(asset2, "Asset should exist and be GETable");
            Assert.IsTrue(response.Data.Any(), "Search should return at least 1 result");

            //Clean up
            var delete = Client.AssetDelete(asset1.Id, asset.Saltminer.Asset.AssetType, sourceType, instance);
            Assert.IsTrue(delete.Success, "Delete should succeed");
        }
    }
}
