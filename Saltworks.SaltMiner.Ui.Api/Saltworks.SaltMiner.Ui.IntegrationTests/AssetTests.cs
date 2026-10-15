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

﻿using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Ui.Api.Contexts;

namespace Saltworks.SaltMiner.Ui.IntegrationTests
{
    [TestClass]
    public class AssetTests
    {
        private AssetContext AssetContext;

        [TestInitialize]
        public void SetUp()
        {
            //Arrange
            var services = Helpers.GetServicesWithDataClient<DataClient.DataClient>();
            AssetContext = new AssetContext(services, NullLogger<AssetContext>.Instance);
        }


        [TestMethod]
        public void Asset_Primer()
        {
            AssetContext.DebugUserRoles = ["superuser"];
            var response = AssetContext.NewPrimer("8a605e59-9172-442b-8a9a-3c97237146d9");
            Assert.IsNotNull(response);
        }
    }
}
