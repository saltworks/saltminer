using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Entities;
using System.Linq;
using System.Threading;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class InventoryAssetTests
    {
        private static DataClient Client = null;
        
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }
            Client = Helpers.GetDataClient<InventoryAssetTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(false, true)));
        }

        [TestMethod]
        public void Crud()
        {
            // Arrange
            var a = Mock.InventoryAsset();
            var name = "Juiceshop";
            var version = "v1.0";
            a.Id = string.Empty;
            a.Name = name;
            a.Version = version;

            // Act
            var a1 = Client.InventoryAssetAddUpdate(a).Data;
            Thread.Sleep(2000); // wait for "save" to complete
            var a2 = Client.InventoryAssetGet(a1.Id);
            var r = Client.InventoryAssetSearch(Helpers.SearchRequest("Saltminer.AssetInv.Name", name));

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(a1.Id), "AssetInventory ID shouldn't be empty after adding it");
            Assert.IsNotNull(a2, "AssetInventory should exist and be GETable");
            Assert.IsTrue(r.Data.Any(), "Search should return at least 1 result");

            //Clean Up
            var d = Client.InventoryAssetDelete(a1.Id);
            Assert.IsTrue(d.Success, "Delete should succeed");
        }
    }
}
