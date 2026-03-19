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
            Client = Helpers.GetDataClient<InventoryAssetTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(true, false)));
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
            Client.RefreshIndex(InventoryAsset.GenerateIndex());
            Task.Delay(2000).Wait(); // wait for "save" to complete
            var a2 = Client.InventoryAssetGet(a1.Id);
            var r = Client.InventoryAssetSearch(Helpers.SearchRequest("Name", name));

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(a1.Id), "InventoryAsset ID shouldn't be empty after adding it");
            Assert.IsNotNull(a2, "InventoryAsset should exist and be GETable");
            Assert.IsTrue(r.Data.Any(), "Search should return at least 1 result");

            //Clean Up
            var d = Client.InventoryAssetDelete(a1.Id);
            Assert.IsTrue(d.Success, "Delete should succeed");
        }
    }
}
