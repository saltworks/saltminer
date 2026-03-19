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
