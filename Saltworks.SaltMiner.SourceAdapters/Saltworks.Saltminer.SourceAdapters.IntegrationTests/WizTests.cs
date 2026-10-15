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

﻿using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Wiz;
using Saltworks.Utility.ApiHelper;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.SourceAdapters.IntegrationTests
{
    [TestClass]
    public class WizTests
    {
        private Config Config;
        private ApiClientFactory<SourceAdapter> ClientFactory;
        private DataClientFactory<DataClient.DataClient> DataClientFactory;
        private IServiceProvider LocalServiceProvider;
        private ILogger<WizAdapter> Logger;
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
            LocalServiceProvider = Helpers.GetLocalDataServiceProvider(Config);
            Logger = LocalServiceProvider.GetService<ILogger<WizAdapter>>();
        }

        [TestMethod]
        public void TestIssues()
        {
            // All
            var client = new WizClient(ClientFactory.CreateApiClient(), Config.WizConfig, NullLogger.Instance);
            var rsp = client.IssuesGetAsync("01b5c4d5-a863-5b71-bd32-1fad38baf7b7", null, null).Result;
            var dto = rsp.Content;
            Assert.IsNotNull(dto);
        }

        [TestMethod]
        public void TestVulns()
        {
            // All
            var client = new WizClient(ClientFactory.CreateApiClient(), Config.WizConfig, NullLogger.Instance);
            var rsp = client.VulnsGetAsync("01b5c4d5-a863-5b71-bd32-1fad38baf7b7", null, null).Result;
            var dto = rsp.Content;
            Assert.IsNotNull(dto);
        }

        [TestMethod]
        public void TestAssets()
        {
            // All
            var client = new WizClient(ClientFactory.CreateApiClient(), Config.WizConfig, NullLogger.Instance);
            var rsp = client.VulnsGetUpdatedAssetsAsync(DateTime.Today, null, null).Result;
            var dto = rsp.Content;
            Assert.IsNotNull(dto);
        }

        [TestMethod]
        public async Task TestGetAsync()
        {
            // Arrange
            var client = new WizClient(ClientFactory.CreateApiClient(), Config.WizConfig, NullLogger.Instance);
            var adapter = new WizAdapter(LocalServiceProvider, Logger);
            
            // Act/Assert
            var c = 0;
            await foreach (var asset in adapter.GetAsync(client, new() { Data = new DateTime(2001, 1, 1).ToString("o") + "|" }))
            {
                c++;
                if (c == 10000)
                    break;
            }
            Assert.IsTrue(c > 1);
        }

        [TestMethod]
        public void TestCsv()
        {
            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.Replace(" ", "").ToLower()
            };
            using var csv = new CsvReader(new StreamReader("TempData\\issuereport.csv"), cfg);
            foreach (var item in csv.GetRecords<Issue>())
            {
                Console.WriteLine(item.Control.Id + ": " + item.Control.Name);
            }
            Assert.IsTrue(true);
        }
    }
}
