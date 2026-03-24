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
using Saltworks.SaltMiner.SourceAdapters.MendSca;
using Saltworks.Utility.ApiHelper;
using System;
using System.IO;

namespace Saltworks.SaltMiner.SourceAdapters.IntegrationTests
{
    [TestClass]
    public class MendScaTests
    {
        private Config Config;
        private ApiClientFactory<SourceAdapter> ClientFactory;
        private DataClientFactory<DataClient.DataClient> DataClientFactory;
        private IServiceProvider LocalDataProvider;
        private const string DBPATH = "mydata.db";

        [TestInitialize]
        public void SetUp()
        {
            // Arrange
            Config = Helpers.GetConfig();
            ClientFactory = Helpers.CreateApiClientFactory<SourceAdapter>(Helpers.GetApiClientOptions(Config));
            DataClientFactory = Helpers.CreateDataClientFactory<DataClient.DataClient>(Helpers.GetDataClientOptions(Config));
            if (File.Exists(DBPATH))
            {
                var dir = new DirectoryInfo(".");
                foreach (var f in dir.EnumerateFiles(DBPATH.Replace(".db", "*.*")))
                    f.Delete();
            }
            LocalDataProvider = Helpers.GetLocalDataServiceProvider(Config);
        }

        [TestMethod]
        public void Sync()
        {
            var adapter = new MendScaAdapter(LocalDataProvider, new TestLogger<MendScaAdapter>());
            Config.MendScaConfig.TestingAssetLimit = 10;
            adapter.SyncTest(new MendScaClient(ClientFactory.CreateApiClient(), Config.MendScaConfig, new TestLogger<MendScaClient>()), Config.MendScaConfig);
            adapter.SendTest();
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Get()
        {
            var adapter = new MendScaAdapter(LocalDataProvider, new TestLogger<MendScaAdapter>());
            Config.MendScaConfig.TestingAssetLimit = 10;
            adapter.GetTest(new MendScaClient(ClientFactory.CreateApiClient(), Config.MendScaConfig, new TestLogger<MendScaClient>()));
            Assert.IsTrue(true);
        }
    }
}
