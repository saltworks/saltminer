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
            var rsp = client.GetApplicationsAsync();
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
