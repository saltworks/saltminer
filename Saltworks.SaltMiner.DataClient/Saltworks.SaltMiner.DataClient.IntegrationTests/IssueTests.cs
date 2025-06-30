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
    public class IssueTests
    {
        private static DataClient Client = null;
        private PitPagingInfo PagingInfo = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }
            Client = Helpers.GetDataClient<IssueTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(false, true)));
        }

        [ClassCleanup]
        public static void CleanUp()
        {
            Helpers.CleanIndex(Client, "issue");
            Helpers.CleanIndex(Client, "scan");
        }

        [TestMethod]
        public void Issue_Validation_Error()
        {
            // Arrange
            var source = "Fortify";
            var sourceType = "DataClient";
            var sourceId = "12345678g";

            var issue1 = Mock.Issue( sourceType);
            issue1.Id = string.Empty;
            issue1.Saltminer.Asset.Instance = source;
            issue1.Saltminer.Asset.SourceType = sourceType;
            issue1.Saltminer.Asset.SourceId = sourceId;

            var issue2 = Mock.Issue( sourceType);
            issue2.Id = string.Empty;
            issue2.Saltminer.Asset.Instance = source;
            issue2.Saltminer.Asset.SourceType = sourceType;
            issue2.Saltminer.Asset.SourceId = sourceId;

            // Act
            var ok = false;
            try { 
                Client.IssuesAddUpdateBulk(new Issue[] { issue1, issue2 });
            }
            catch (DataClientResponseException) {
                ok = true;
            }
            catch (Exception) {
                /* Ignore */
            }

            // Assert
            Assert.IsTrue(ok, "Should be throwing a DataClientResponseException but didn't");
        }

        [TestMethod]
        public void Crud()
        {
            // Arrange
            var source = "Fortify";
            var sourceType = "DataClient";
            var sourceId = "12345678g";

            var scan = Mock.Scan(sourceType);
            scan.Id = string.Empty;

            var issue = Mock.Issue(sourceType);
            issue.Id = string.Empty;
            scan.Saltminer.Asset.Instance = source;
            scan.Saltminer.Asset.SourceType = sourceType;
            scan.Saltminer.Asset.SourceId = sourceId;
            issue.Saltminer.Asset.Instance = source;
            issue.Saltminer.Asset.SourceType = sourceType;
            issue.Saltminer.Asset.SourceId = sourceId;
            issue.Saltminer.Critical = 1;
            issue.Saltminer.High = 0;
            issue.Saltminer.Medium = 0;
            issue.Saltminer.Low = 0;
            
            // Act
            scan = Client.ScanAddUpdate(scan).Data;
            issue.Saltminer.Scan.Id = scan.Id;

            var issues = Client.IssuesAddUpdateBulk(new Issue[] { issue, issue });
            Thread.Sleep(2000); // wait for the new item to finish saving

            var issueSearch = Client.IssueSearch(Helpers.SearchRequest("Saltminer.Scan.Id", scan.Id, scan.Saltminer.Asset.AssetType, sourceType));
            var request = Helpers.SearchRequest("Saltminer.Scan.Id", scan.Id);
            var issuesByScan = Client.IssueSearch(request);

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(scan.Id), "Scan ID shouldn't be empty after adding it");
            Assert.AreEqual(2, issues.Affected, "Issue add should return 2");
            Assert.AreEqual(2, issueSearch.Data.Count(), "Issue search should return 2");
            Assert.AreEqual(2, issuesByScan.Data.Count(), "Issue get by scan ID - should have returned 2");

            //Clean up
            var issuesDeleted = Client.IssuesDeleteByScan(scan.Id, scan.Saltminer.Asset.AssetType, scan.Saltminer.Asset.SourceType, scan.Saltminer.Asset.Instance);
            var scanDeleted = Client.ScanDelete(scan.Id, scan.Saltminer.Asset.AssetType, scan.Saltminer.Asset.SourceType, scan.Saltminer.Asset.Instance);
            Assert.IsTrue(issuesDeleted.Affected == issuesByScan.Data.Count(), "Delete issue(s) should succeed");
            Assert.IsTrue(scanDeleted.Success, "Delete scan should succeed");
        }


        public void Make_Stuff_To_Test()
        {
            // Arrange
            var sourceType = "DataClient";

            var s = Mock.Scan(sourceType);
            s.Id = string.Empty;
            s = Client.ScanAddUpdate(s).Data;
            var list = new List<Issue>();
            
            var i = Mock.Issue(sourceType);
            i.Id = string.Empty;
            i.Saltminer.Scan.Id = s.Id;
            list.Add(i);

            i = Mock.Issue(sourceType);
            i.Id = string.Empty;
            i.Saltminer.Scan.Id = s.Id;
            i.Vulnerability.FoundDate = DateTime.Parse("01/15/2021").ToUniversalTime();
            list.Add(i);


            i = Mock.Issue(sourceType);
            i.Id = string.Empty;
            i.Saltminer.Scan.Id = s.Id;
            list.Add(i);

            i = Mock.Issue(sourceType);
            i.Id = string.Empty;
            i.Saltminer.Scan.Id = s.Id;
            i.Vulnerability.IsFiltered = true;
            list.Add(i);

            //Act
            var issues = Client.IssuesAddUpdateBulk(list);

            //Assert
            Assert.IsTrue(true);

            //Clean up
            var issuesDeleted = Client.IssuesDeleteByScan(s.Id, s.Saltminer.Asset.AssetType, s.Saltminer.Asset.SourceType, s.Saltminer.Asset.Instance);
            Assert.IsTrue(issuesDeleted.Affected == issues.Affected, "Delete issue(s) should succeed");
        }
    }
}
