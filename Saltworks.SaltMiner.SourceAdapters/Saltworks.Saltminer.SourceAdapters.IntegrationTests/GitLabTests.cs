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
using Saltworks.SaltMiner.SourceAdapters.GitLab;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.Utility.ApiHelper;
using System;
using System.IO;


namespace Saltworks.SaltMiner.SourceAdapters.IntegrationTests
{
    [TestClass]
    public class GitLabTests
    {
        private Config Config;
        private ApiClientFactory<SourceAdapter> ClientFactory;
        private DataClientFactory<DataClient.DataClient> DataClientFactory;
        private IServiceProvider LocalServiceProvider;
        private ILogger<GitLabAdapter> Logger;
        private const string DBPATH = "mydata.db";
        private static string assetFullPath = string.Empty;


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
            Logger = LocalServiceProvider.GetService<ILogger<GitLabAdapter>>();
        }

        [TestMethod]
        public void TestGroupsAsync()
        {
            // Arrange
            var client = new GitLabClient(ClientFactory.CreateApiClient(), Config.GitLabConfig, NullLogger.Instance);
            // Act
            var assets = client.GetNamespaceGroupsAsync(Config.GitLabConfig.GroupNamespace, string.Empty, 1).Result;
            //Assert
            Assert.IsTrue(assets.Data.Groups.PageInfo.HasPreviousPage == false);
        }

        [TestMethod]
        public void TestAssetsAsync()
        {
            // Arrange
            var client = new GitLabClient(ClientFactory.CreateApiClient(), Config.GitLabConfig, NullLogger.Instance);
            // Act
            var assets = client.GetProjectsByGroupAsync(Config.GitLabConfig.GroupNamespace, string.Empty, 1).Result;
            //Assert
            Assert.IsTrue(assets.Data.Group.Projects.PageInfo.HasPreviousPage == false);
        }

        [TestMethod]
        public void TestLatestProjectScanAsync()
        {
            // Arrange
            var client = new GitLabClient(ClientFactory.CreateApiClient(), Config.GitLabConfig, NullLogger.Instance);
            // Act
            var rsp =  client.GetLatestProjectScanAsync(@"onetrust/appsec/ot-orbit").Result;
            //Assert
            Assert.IsTrue(rsp.Data.Project.Pipelines.PageInfo.HasPreviousPage == false);
        }

        [TestMethod]
        public void TestVulnerabilitiesAsync()
        {
            // Arrange
            var client = new GitLabClient(ClientFactory.CreateApiClient(), Config.GitLabConfig, NullLogger.Instance);
            // Act
            var rsp = client.GetProjectVulnerabilitiesAsync(@"onetrust/appsec/ot-orbit").Result;
            //Assert
            Assert.IsTrue(rsp.Data.Project.Vulnerabilities.PageInfo.HasPreviousPage == false);
        }
    }
}
