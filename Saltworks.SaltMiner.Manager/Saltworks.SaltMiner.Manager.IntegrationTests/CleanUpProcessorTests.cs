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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Saltworks.SaltMiner.Manager.IntegrationTests
{
    [TestClass]
    public class CleanUpProcessorTests
    {
        private static readonly CancellationTokenSource CancelTokenSource = new();
        private Config _config;
        private DataClientFactory<DataClient.DataClient> _dataClientFactory;
        private DataClient.DataClient Client;

        [TestInitialize]
        public void SetUp()
        {
            //Arrange
            _config = Helpers.GetConfig();
            _dataClientFactory = Helpers.CreateDataClientFactory<DataClient.DataClient>(Helpers.GetDataClientOptions(_config));
            Client = _dataClientFactory.GetClient();
        }

        [TestMethod]
        public void RunCleanUpProcessorTest_WithoutProcess()
        {
            Helpers.RunManager(CleanUpRuntimeConfig.GetArgs("Qualys", 1000, true, CancelTokenSource.Token));
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void RunCleanUpProcessorTest()
        {
            //Arrange
            var queueScan = Mock.QueueScan();
            queueScan.Saltminer.Internal.QueueStatus = "Complete";
            queueScan.Id = string.Empty;
            queueScan.Saltminer.Scan.ScanDate = DateTime.Now.AddDays(-8);
            queueScan = Client.QueueScanAddUpdate(queueScan).Data;

            var queueAsset = Mock.QueueAsset();
            queueAsset.Id = string.Empty;
            queueAsset.Saltminer.Internal.QueueScanId = queueScan.Id;
            queueAsset = Client.QueueAssetAddUpdate(queueAsset).Data;

            var list = new List<QueueIssue>();
            var queueIssue = Mock.QueueIssue();
            queueIssue.Id = string.Empty;
            queueIssue.Saltminer.QueueAssetId = queueAsset.Id;
            queueIssue.Saltminer.QueueScanId = queueScan.Id;
            list.Add(queueIssue);

            queueIssue = Mock.QueueIssue();
            queueIssue.Id = string.Empty;
            queueIssue.Saltminer.QueueAssetId = queueAsset.Id;
            queueIssue.Saltminer.QueueScanId = queueScan.Id;
            list.Add(queueIssue);


            queueIssue = Mock.QueueIssue();
            queueIssue.Id = string.Empty;
            queueIssue.Saltminer.QueueAssetId = queueAsset.Id;
            queueIssue.Saltminer.QueueScanId = queueScan.Id;
            list.Add(queueIssue);

            queueIssue = Mock.QueueIssue();
            queueIssue.Id = string.Empty;
            queueIssue.Saltminer.QueueAssetId = queueAsset.Id;
            queueIssue.Saltminer.QueueScanId = queueScan.Id;
            list.Add(queueIssue);

            Client.QueueIssuesAddUpdateBulk(list);

            //Act
            Helpers.RunManager(CleanUpRuntimeConfig.GetArgs("Mocked", 1000, false, CancelTokenSource.Token));
            // Rest call takes a long time;
            Thread.Sleep(5000);

            //Assert
            var queueScanReponse = Client.QueueScanGet(queueScan.Id);
            Assert.IsTrue(queueScanReponse.Data != null);

            var seachReqest = new SearchRequest() { 
                PitPagingInfo = new PitPagingInfo(1, false),
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        {
                            "Saltminer.Internal.QueueScanid", queueScan.Id
                        }
                    }
                }
            };
            var queueAssetReponse = Client.QueueAssetSearch(seachReqest);
            Assert.IsTrue(queueAssetReponse.Data.Any());

            seachReqest = new SearchRequest() { 
                PitPagingInfo = new PitPagingInfo(1, false),
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        {
                            "Saltminer.QueueScanId", queueScan.Id
                        }
                    }
                }
            };
            var queueIssueReponse = Client.QueueIssueSearch(seachReqest);
            Assert.IsTrue(queueIssueReponse.Data.Any());
        }
    }
}
