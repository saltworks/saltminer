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
