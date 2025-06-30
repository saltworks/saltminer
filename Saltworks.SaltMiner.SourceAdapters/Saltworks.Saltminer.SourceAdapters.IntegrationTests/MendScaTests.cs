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
