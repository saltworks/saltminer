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
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class QueueIssueTests
    {
        private static DataClient Client = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }
            
            Client = Helpers.GetDataClient<QueueIssueTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(false, true)));
        }

        [TestMethod]
        public void Crud()
        {
            // Arrange
            var queueScan = Mock.QueueScan();
            queueScan.Id = string.Empty;
            queueScan.Saltminer.Internal.QueueStatus = QueueScan.QueueScanStatus.Pending.ToString("g");

            // Act
            queueScan = Client.QueueScanAddUpdate(queueScan).Data;
            var queueAsset = Mock.QueueAsset();
            queueAsset.Id = string.Empty;
            queueAsset.Saltminer.Internal.QueueScanId = queueScan.Id;
            queueAsset = Client.QueueAssetAddUpdate(queueAsset).Data;
            var queueIssues = new List<QueueIssue>();
            var issueCount = 5;
            var scrollInfo = new PitPagingInfo(5);

            for (int i = 0; i < issueCount; i++)
            {
                var queueIssue = Mock.QueueIssue();
                queueIssue.Id = string.Empty;
                queueIssue.Saltminer.QueueScanId = queueScan.Id;
                queueIssue.Saltminer.QueueAssetId = queueAsset.Id;
                queueIssues.Add(queueIssue);
            }
            var queueIssueEdit = Client.QueueIssuesAddUpdateBulk(queueIssues);
            Thread.Sleep(2000); // hold on a moment there youngster, let them thangs simmer a bit
            var queueIssueCountByScan = Client.QueueIssueCountByScan(queueScan.Id);
            var queueIssueByQueueScan = Client.QueueIssueSearch(new SearchRequest { 
                Filter = new Filter {
                    FilterMatches = new Dictionary<string, string>
                    {
                        {
                            "Saltminer.QueueScanId", queueScan.Id
                        }
                    }
                },
                PitPagingInfo = scrollInfo
            });
            //var qil2 = Client.QueueIssuesGetByQueueScan("7efcd3d6-fb84-4fc1-ba75-4d34e050565d");

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(queueScan.Id), "QueueScan ID shouldn't be empty after adding it");
            Assert.AreEqual(issueCount, queueIssueEdit.Affected, "QueueIssue add should return the right number of issues added");
            Assert.AreEqual(issueCount, queueIssueByQueueScan.Data.Count(), "QueueIssue search by scan ID should return the right number of issues");
            Assert.AreEqual(issueCount, queueIssueCountByScan.Affected, "Issue count should match expected");

            //Clean Up
            var queueScanDeleteAll = Client.QueueScanDeleteAll(queueScan.Id);
            Assert.IsTrue(queueScanDeleteAll.Success, "Delete queue should succeed");
        }

        [TestMethod]
        public void NewQueueIssueWithId()
        {
            var queueIssue = Mock.QueueIssue();
            var id = Guid.NewGuid().ToString();
            queueIssue.Id = id;
            var queueIssueEdit = Client.QueueIssueAddUpdate(queueIssue);
            Assert.IsTrue(queueIssueEdit.Success);
            Thread.Sleep(2000);
            var queueIssueSearch = Client.QueueIssueSearch(new SearchRequest { 
                Filter = new() { 
                    FilterMatches = new Dictionary<string, string> { 
                        { 
                            "id", id 
                        } 
                    }
                }
            });
            Assert.IsTrue(queueIssueSearch.Data.Count() == 1);
        }
    }
}
