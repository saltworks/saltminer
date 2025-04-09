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
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Sonatype;

namespace Saltworks.SaltMiner.SourceAdapters.IntegrationTests
{
    [TestClass]
    public class SonatypeTests
    {
        private Config Config;
        private SonatypeClient Client;


        [TestInitialize]
        public void SetUp()
        {
            // Arrange
            Config = Helpers.GetConfig();
            var ClientFactory = Helpers.CreateApiClientFactory<SourceAdapter>(Helpers.GetApiClientOptions(Config));
            Client = new SonatypeClient(ClientFactory.CreateApiClient(), Config.SonatypeConfig, new TestLogger());
        }
    }
}