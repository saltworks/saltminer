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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class IndexTests
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
        public void Refresh()
        {
            var response = Client.RefreshIndex(QueueAsset.GenerateIndex());
            Assert.IsTrue(response.Success);
        }
    }
}
