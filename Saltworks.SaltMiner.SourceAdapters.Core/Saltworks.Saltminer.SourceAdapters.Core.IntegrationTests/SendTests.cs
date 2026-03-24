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
