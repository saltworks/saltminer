/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-04-09
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
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
