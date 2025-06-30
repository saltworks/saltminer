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
using Saltworks.SaltMiner.SourceAdapters.Oligo;
using Saltworks.Utility.ApiHelper;
using System;
using System.Threading.Tasks;
namespace Saltworks.SaltMiner.SourceAdapters.IntegrationTests
{
    [TestClass]
    public class OligoTests
    {
        private Config Config;
        private IServiceProvider LocalServiceProvider;
        private ILogger<OligoAdapter> Logger; 

        [TestInitialize]
        public void SetUp()
        {
            // Arrange
            Config = Helpers.GetConfig();
            LocalServiceProvider = Helpers.GetLocalDataServiceProvider(Config);
            Logger = LocalServiceProvider.GetRequiredService<ILogger<OligoAdapter>>();
        }
        
        [TestMethod]
        public async Task TestGetAsync()
        {

            var adapter = new OligoAdapter(LocalServiceProvider, Logger);
            var client = new OligoClient(LocalServiceProvider.GetRequiredService<ApiClientFactory<OligoAdapter>>().CreateApiClient(), Config.OligoConfig, NullLogger.Instance);
            int count = 0;
            await foreach (var dto in adapter.GetAsync(client, Config.OligoConfig))
            {
                count++;
                Assert.IsNotNull(dto);
            }
        }

        [TestMethod]
        public void TestVulnerabilitiesAsync()
        {
            // Arrange
            var client = new OligoClient(LocalServiceProvider.GetRequiredService<ApiClientFactory<OligoAdapter>>().CreateApiClient(), Config.OligoConfig, NullLogger.Instance);
            // Act
            var rsp = client.GetVulnerabilitiesAsync(10, 1).Result;
            //Assert
            Assert.IsNotNull(rsp);
        }

        [TestMethod]
        public void TestImagesAsync()
        {
            // Arrange
            var client = new OligoClient(LocalServiceProvider.GetRequiredService<ApiClientFactory<OligoAdapter>>().CreateApiClient(), Config.OligoConfig, NullLogger.Instance);
            // Act
            var rsp = client.GetImages();
            //Assert
            Assert.IsNotNull(rsp);
        }
    }
}
