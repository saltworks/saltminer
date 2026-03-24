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
using Saltworks.SaltMiner.SourceAdapters.CheckmarxOne;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.Utility.ApiHelper;
using System;

namespace Saltworks.SaltMiner.SourceAdapters.IntegrationTests
{
    [TestClass]
    public class CheckmarxOneTests
    {
        private Config Config;
        private ApiClientFactory<SourceAdapter> ClientFactory;
        private DataClientFactory<DataClient.DataClient> DataClientFactory;
        private IServiceProvider LocalServiceProvider;
        private ILogger<CheckmarxOneAdapter> Logger; 

        [TestInitialize]
        public void SetUp()
        {
            // Arrange
            Config = Helpers.GetConfig();
            ClientFactory = Helpers.CreateApiClientFactory<SourceAdapter>(Helpers.GetApiClientOptions(Config));
            DataClientFactory = Helpers.CreateDataClientFactory<DataClient.DataClient>(Helpers.GetDataClientOptions(Config));
            LocalServiceProvider = Helpers.GetLocalDataServiceProvider(Config);
            Logger = LocalServiceProvider.GetService<ILogger<CheckmarxOneAdapter>>();
        }

        [TestMethod]
        public void TestProjects()
        {
            // Arrange
            var client = new CheckmarxOneClient(ClientFactory.CreateApiClient(), Config.CheckmarxOneConfig, NullLogger.Instance);
            // Act
            var rsp = client.GetProjectsAsync(10, 0).Result;
            //Assert
            Assert.IsNotNull(rsp);
        }

        [TestMethod]
        public void TestResultsOverviewAsync()
        {
            // Arrange
            var client = new CheckmarxOneClient(ClientFactory.CreateApiClient(), Config.CheckmarxOneConfig, NullLogger.Instance);
            // Act
            var rsp = client.GetResultsOverviewAsync("a00f5bd6-9130-42c4-9cc1-1eee0e445d6d");
            //Assert
            Assert.IsNotNull(rsp);
        }

        [TestMethod]
        public void TestScansAsync()
        {
            // Arrange
            var client = new CheckmarxOneClient(ClientFactory.CreateApiClient(), Config.CheckmarxOneConfig, NullLogger.Instance);
            // Act
            var rsp = client.GetScansAsync("a00f5bd6-9130-42c4-9cc1-1eee0e445d6d");
            //Assert
            Assert.IsNotNull(rsp);
        }

        [TestMethod]
        public void TestScanResultsAsync()
        {
            // Arrange
            var client = new CheckmarxOneClient(ClientFactory.CreateApiClient(), Config.CheckmarxOneConfig, NullLogger.Instance);
            // Act
            var rsp = client.GetScanResultsAsync("a00f5bd6-9130-42c4-9cc1-1eee0e445d6d", 10, 0);
            //Assert
            Assert.IsNotNull(rsp);
        }

        [TestMethod]
        public void TestScanDetailsAsync()
        {
            // Arrange
            var client = new CheckmarxOneClient(ClientFactory.CreateApiClient(), Config.CheckmarxOneConfig, NullLogger.Instance);
            // Act
            var rsp = client.GetScanDetailsAsync("f6a8d861-6fd2-4a63-bcb7-74ed52d9e109");
            //Assert
            Assert.IsNotNull(rsp);
        }

        [TestMethod]
        public void TestScanSummaryAsync()
        {
            // Arrange
            var client = new CheckmarxOneClient(ClientFactory.CreateApiClient(), Config.CheckmarxOneConfig, NullLogger.Instance);
            // Act
            var rsp = client.GetScanSummaryAsync("f6a8d861-6fd2-4a63-bcb7-74ed52d9e109");
            //Assert
            Assert.IsNotNull(rsp);
        }

        [TestMethod]
        public void TestApplicationsAsync()
        {
            // Arrange
            var client = new CheckmarxOneClient(ClientFactory.CreateApiClient(), Config.CheckmarxOneConfig, NullLogger.Instance);
            // Act
            var rsp = client.GetApplicationsAsync(1, 0);
            //Assert
            Assert.IsNotNull(rsp);

        }

        [TestMethod]
        public void TestApplicationDetailsAsync()
        {
            // Arrange
            var client = new CheckmarxOneClient(ClientFactory.CreateApiClient(), Config.CheckmarxOneConfig, NullLogger.Instance);
            // Act
            var rsp = client.GetApplicationDetailsAsync("91a55065-4acb-4e0a-a8b2-65e1d41250a2");
            //Assert
            Assert.IsNotNull(rsp);

        }

    }
}
