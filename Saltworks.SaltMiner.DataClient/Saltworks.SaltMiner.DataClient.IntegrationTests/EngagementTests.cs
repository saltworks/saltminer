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

﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class EngagementTests
    {
        private static DataClient Client = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }

            Client = Helpers.GetDataClient<EngagementTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(true)));
        }

        [TestMethod]
        public void CRUDTest()
        {
            var engagement = Mock.Engagement();

            engagement = Client.EngagementAddUpdate(engagement).Data;
            Thread.Sleep(2000);
            var search = Client.EngagementSearch(new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string>()
                },
                UIPagingInfo = new UIPagingInfo(10)
            });

            Assert.IsNotNull(search.Data);

            var get = Client.EngagementGet(engagement.Id);

            Assert.IsNotNull(get.Data);

            Client.EngagementDelete(engagement.Id);

            try
            {
                Client.EngagementGet(engagement.Id);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("not found"));
            }
        }
    }
}
