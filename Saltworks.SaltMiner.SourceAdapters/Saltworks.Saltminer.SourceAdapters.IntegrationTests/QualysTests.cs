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

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.Utility.ApiHelper;
using Saltworks.SaltMiner.SourceAdapters.Qualys;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.SourceAdapters.Core;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Saltworks.SaltMiner.SourceAdapters.IntegrationTests
{
    [TestClass]
    public class QualysTests
    {
        private Config _config;
        private static IServiceProvider _localDataProvider = null;

        [TestInitialize]
        public void SetUp()
        {
            // Arrange
            _config = Helpers.GetConfig();
            _localDataProvider = Helpers.GetLocalDataServiceProvider(_config);
        }

        private QualysClient GetClient() => new(_localDataProvider.GetRequiredService<ApiClientFactory<QualysAdapter>>().CreateApiClient(), _config.QualysConfig, new TestLogger());

        [TestMethod]
        public async Task RunAsync()
        {
            var adapter = new QualysAdapter(_localDataProvider, new TestLogger<QualysAdapter>());
            Assert.IsNotNull(adapter);
            await adapter.RunAsync(_config.QualysConfig, CancellationToken.None);
        }

        #region Client Tests

        [TestMethod]
        public async Task Client_ScanList()
        {
            // Act
            var r = await GetClient().ScanListAsync(DateTime.UtcNow.AddMonths(-2));

            // Assert
            Assert.IsTrue(r.Any(), "Should return non-zero count enumerable");
        }

        [TestMethod]
        public async Task Client_AssetList()
        {
            // All
            await foreach (var host in GetClient().HostListAsync([]))
            {
                var count = 0;
                Assert.IsNotNull(host);
                count++;
                if (count > 1)
                    break;
            }
        }

        [TestMethod]
        public async Task Client_IssueList()
        {
            // Act
            var r = GetClient().HostDetectionsAsync(["10.0.0.9"], null);
            var e = r.GetAsyncEnumerator();
            await e.MoveNextAsync();

            // Assert
            Assert.IsNotNull(e.Current, "Should contain at least one element");
        }

        [TestMethod]
        public async Task Client_KbList()
        {
            // Act
            var r = await GetClient().KnowledgeBaseAsync(["650035"]);

            // Assert
            Assert.IsTrue(r.Any(), "Should return non-zero count enumerable");
        }

        #endregion
    }
}
