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
