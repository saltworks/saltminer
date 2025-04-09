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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Saltworks.SaltMiner.SourceAdapters.SonarQube;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.Utility.ApiHelper;
using System;

namespace Saltworks.SaltMiner.SourceAdapters.IntegrationTests
{
    [TestClass]
    public class SonarQubeTests
    {
        private Config Config;
        private ApiClientFactory<SourceAdapter> ClientFactory;
        private DataClientFactory<DataClient.DataClient> DataClientFactory;
        private IServiceProvider LocalServiceProvider;
        private ILogger<SonarQubeAdapter> Logger;


        [TestInitialize]
        public void SetUp()
        {
            // Arrange
            Config = Helpers.GetConfig();
            ClientFactory = Helpers.CreateApiClientFactory<SourceAdapter>(Helpers.GetApiClientOptions(Config));
            DataClientFactory = Helpers.CreateDataClientFactory<DataClient.DataClient>(Helpers.GetDataClientOptions(Config));
            LocalServiceProvider = Helpers.GetLocalDataServiceProvider(Config);

        }
        [TestMethod]
        public void TestComponents()
        {
            var client = new SonarQubeClient(ClientFactory.CreateApiClient(), Config.SonarQubeConfig, NullLogger.Instance);
            var rsp = client.GetAllComponentsAsync();
            Assert.IsNotNull(rsp);
        }
        [TestMethod]
        public void TestIssues()
        {
            DateTime currentUtcDateTime = DateTime.UtcNow;
            var client = new SonarQubeClient(ClientFactory.CreateApiClient(), Config.SonarQubeConfig, NullLogger.Instance);
            var rsp = client.GetIssuesByComponentAsync("akaunting", currentUtcDateTime);
            Assert.IsNotNull(rsp);
        }
    }
}
