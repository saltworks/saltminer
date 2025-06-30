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
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.SourceAdapters.Core;
using System;
using System.IO;

namespace Saltworks.Saltminer.SourceAdapters.Core.IntegrationTests
{
    [TestClass]
    public class SendTests
    {
        private static Config Config;
        private static IServiceProvider LocalDataProvider;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            Config = Helpers.GetConfig();
            var dbFile = $"sw-{Config.SourceConfig.SourceType.ToLower().Replace("Saltworks.", "")}-{Config.SourceConfig.Instance.ToLower()}.db";
            if (File.Exists(dbFile))
                File.Delete(dbFile);
            LocalDataProvider = Helpers.GetLocalDataServiceProvider(Config);
        }

        [TestMethod]
        public void Send()
        {
            // Arrange
            var a = new TestAdapter(LocalDataProvider, new TestLogger());

            // Act
            a.RunAsync(Config.SourceConfig, System.Threading.CancellationToken.None).Wait();

            // Assert
            Assert.IsTrue(true);
        }
    }
}
