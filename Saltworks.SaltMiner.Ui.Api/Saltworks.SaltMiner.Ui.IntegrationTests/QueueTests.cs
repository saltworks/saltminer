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

﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.ViewModels;
using System;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.Ui.IntegrationTests
{
    [TestClass]
    public class QueueTests
    {
        private DataClient.DataClient Client;
        private EngagementContext EngagementContext;
        private ScanContext ScanContext;
        private AssetContext AssetContext;
        private IssueContext IssueContext;
        private IServiceProvider ServiceProvider;

        private FieldInfo FieldInfo(FieldInfoEntityType entityType, string[] currUserRoles) => ServiceProvider.GetRequiredService<FieldInfoCache>().GetFieldInfo(entityType, currUserRoles);
        
        [TestInitialize]
        public void SetUp()
        {
            //Arrange
            var services = Helpers.GetServicesWithDataClient<DataClient.DataClient>();
            ServiceProvider = services;
            EngagementContext = new EngagementContext(services, NullLogger<EngagementContext>.Instance);
            ScanContext = new ScanContext(services, NullLogger<ScanContext>.Instance);
            AssetContext = new AssetContext(services, NullLogger<AssetContext>.Instance);
            IssueContext = new IssueContext(services, NullLogger<IssueContext>.Instance);
            Client = Helpers.GetDataClient(services);
        }

        [TestMethod]
        public void Scan_Crud()
        {
            // CRUD operations for queue scan

            // Arrange
            var queueScan = Mock.QueueScan();
            queueScan.Id = String.Empty;
            
            // Act
            var results1 = Client.QueueScanAddUpdate(queueScan);
            Task.Delay(2000).Wait();
            var results2 = ScanContext.Get(results1.Data.Id);
            Task.Delay(2000).Wait();

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(results1.Data.Id), "QueueScan Id should not be empty after adding new");
            Assert.IsTrue(results2.Success, "EngagementScan should exist after fetching");
        }

        [TestMethod]
        public void Asset_Crud()
        {
            // CRUD operations for queue asset

            // Arrange
            var asset = Mock.Asset("SourceType");
            var fieldInfo = FieldInfo(FieldInfoEntityType.Asset, ["superuser"]);
            var engagement = Mock.Engagement();
            var engagementAsset = new AssetFull(asset, UiApiConfig.AppVersion, fieldInfo);
            var newRequest = new AssetNew
            {
                Attributes = engagementAsset.Attributes.ToDictionary(),
                Description = engagementAsset.Description.Value,
                Name = engagementAsset.Name.Value,
                Version = engagementAsset.Version.Value,
                VersionId = engagementAsset.VersionId.Value
            };

            // Act
            var engagementResult = EngagementContext.Create(new EngagementNew
            {
                Customer = engagement.Saltminer.Engagement.Customer,
                Name = engagement.Saltminer.Engagement.Name,
                Summary = engagement.Saltminer.Engagement.Summary,
                Subtype = "PenTest"
            });

            newRequest.EngagementId = engagementResult.Data.Id;

            newRequest.Attributes.Clear();
            newRequest.Attributes.Add("business_unit", "business unit");
            newRequest.Attributes.Add("owner", "Grace Hopper");

            var results1 = AssetContext.New(newRequest);
            var results2 = AssetContext.Get(results1.Data.AssetId);
            Task.Delay(2000).Wait();
            var request = new AssetEdit
            {
                AssetId = results2.Data.AssetId,
                Attributes = engagementAsset.Attributes.ToDictionary(),
                Description = engagementAsset.Description.Value,
                Name = "123",
                Version = engagementAsset.Version.Value,
                VersionId = engagementAsset.VersionId.Value
            };
            var results3 = AssetContext.Edit(request);
            Task.Delay(2000).Wait();

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(results1.Data.AssetId), "QueueAsset Id should not be empty after adding new");
            Assert.IsTrue(results2.Success, "AssetFull should exist after fetching");
            Assert.IsTrue(results3.Data.Name.Value == "123", "Name changed when editing");

            //Clean up
            AssetContext.Delete(results3.Data.AssetId);
            EngagementContext.Delete(engagementResult.Data.Id);
        }

        [TestMethod]
        public void Issue_Crud()
        {
            // CRUD operations for queue asset

            // Arrange
            var engagementRequest = new EngagementNew()
            {
                Name = "Engagement1",
                Summary = "Summary",
                Subtype = "PenTest",
                Customer = "Customer"
            };

            var issue = Mock.Issue("SourceType");
            var status = issue.Vulnerability.IsSuppressed ? "Suppressed" : "Open";
            status = issue.Vulnerability.IsRemoved ? "Removed" : status;
            var newIssueRequest = new IssueEdit
            {
                EngagementId = issue.Saltminer.Engagement.Id,
                Name = issue.Vulnerability.Name,
                Severity = issue.Vulnerability.Severity,
                AssetId = issue.Saltminer.Asset.Id,
                FoundDate = issue.Vulnerability.FoundDate,
                TestStatus = issue.Vulnerability.TestStatus,
                Status = status,
                SourceSeverity = issue.Vulnerability.SourceSeverity,
                RemovedDate = issue.Vulnerability.RemovedDate,
                Location = issue.Vulnerability.Location,
                LocationFull = issue.Vulnerability.LocationFull,
                Classification = issue.Vulnerability.Classification,
                Description = issue.Vulnerability.Description,
                Enumeration = issue.Vulnerability.Enumeration,
                Proof = issue.Vulnerability.Proof,
                Details = issue.Vulnerability.Details,
                Implication = issue.Vulnerability.TestingInstructions,
                Recommendation = issue.Vulnerability.Recommendation,
                References = issue.Vulnerability.References,
                Reference = issue.Vulnerability.Reference,
                Vendor = issue.Vulnerability.Scanner.Vendor,
                Product = issue.Vulnerability.Scanner.Product,
            };

            var engagementIssueRequest = new IssueSearch()
            {
                EngagementId = issue.Saltminer.Engagement.Id,
                SearchFilters = [],
                Pager = new()
                {
                    Size = 50,
                    Page = 1
                }
            };

            // Act
            var engagementResult = EngagementContext.Create(engagementRequest);
            Task.Delay(2000).Wait();

            newIssueRequest.EngagementId = engagementResult.Data.Id;
            engagementIssueRequest.EngagementId = engagementResult.Data.Id;
            var results1 = IssueContext.New(newIssueRequest, new KibanaUser("Testing", "Testing User"));
            //engagementIssueRequest.
            var results2 = IssueContext.Search(engagementIssueRequest);
            Task.Delay(2000).Wait();
            var request = new IssueEdit
            {
                Classification = issue.Vulnerability.Classification,
                Description = issue.Vulnerability.Description,
                Details = issue.Vulnerability.Details,
                Enumeration = issue.Vulnerability.Enumeration, 
                FoundDate = issue.Vulnerability.FoundDate,
                Implication = issue.Vulnerability.Implication,
                Id = results1.Data.Id,  
                Location = issue.Vulnerability.Location,    
                LocationFull = issue.Vulnerability.LocationFull,    
                Name = "123",    
                Proof = issue.Vulnerability.Proof,  
                Recommendation = issue.Vulnerability.Recommendation,
                Reference = issue.Vulnerability.Reference,
                References = issue.Vulnerability.References,
                RemovedDate = issue.Vulnerability.RemovedDate,
                Severity = issue.Vulnerability.Severity,
                SourceSeverity = issue.Vulnerability.SourceSeverity,
                TestStatus  = issue.Vulnerability.TestStatus,
                Vendor = issue.Vulnerability.Scanner.Vendor,
                Product = issue.Vulnerability.Scanner.Product,
                AssetId = issue.Saltminer.Asset.Id
            };
            var results3 = IssueContext.Edit(request, new KibanaUser("Testing", "Testing User"));
            Task.Delay(2000).Wait();

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(results1.Data.Id), "QueueIssue Id should not be empty after adding new");
            Assert.IsTrue(results2.Success, "EngagementIssue should exist after fetching");
            Assert.IsTrue(results3.Data.Name.Value == "123", "Name changed when editing");
        }
    }
}
