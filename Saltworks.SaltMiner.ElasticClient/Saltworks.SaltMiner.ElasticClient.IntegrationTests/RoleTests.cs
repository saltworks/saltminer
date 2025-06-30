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

﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests
{
    [TestClass]
    public class RoleTests
    {
        private static IElasticClient Client = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            var c = Helpers.SettingsConfig();
            Client = Helpers.GetElasticClient(c);
        }

        [TestMethod]
        public void Crud()
        {
            var role = "TestRole";
            var rsp1 = Client.UpsertRole(role, "{}");
            var rsp2 = Client.RoleExists(role);
            var rsp3 = Client.DeleteRole(role);

            Assert.IsTrue(rsp1.IsSuccessful);
            Assert.IsTrue(rsp2.IsSuccessful);
            Assert.IsTrue(rsp3.IsSuccessful);
        }
    }
}
