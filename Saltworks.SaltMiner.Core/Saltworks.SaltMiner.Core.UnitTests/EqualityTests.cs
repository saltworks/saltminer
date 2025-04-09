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
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.Core.UnitTests
{
    [TestClass]
    public class EqualityTests
    {
        [TestMethod]
        public void QueueIssue_To_Issue()
        {
            // Arrange
            var i = Mock.Issue("saltminer");
            var assetAttr = i.Saltminer.Asset.Attributes;
            var attr = new Dictionary<string, string>();
            
            foreach (var e in i.Saltminer.Attributes)
            {
                attr.Add(e.Key, e.Value);
            }

            var q = new QueueIssue
            {
                Saltminer = new SaltMinerQueueIssueInfo()
                {
                    Source = new SourceInfo()
                    {
                        Analyzer = i.Saltminer.Source.Analyzer,
                        Confidence = i.Saltminer.Source.Confidence,
                        Impact = i.Saltminer.Source.Impact,
                        IssueStatus = i.Saltminer.Source.IssueStatus,
                        Kingdom = i.Saltminer.Source.Kingdom,
                        Likelihood = i.Saltminer.Source.Likelihood,
                    },
                    QueueScanId = "SM-QScan-ID-prolly-guid-00002",
                    QueueAssetId = "SM-QAsset-ID-prolly-guid-00002",
                    CustomData = null,
                    Engagement = new EngagementInfo()
                    {
                        Id = i.Saltminer.Engagement.Id,
                        Name = i.Saltminer.Engagement.Name,
                        Customer = i.Saltminer.Engagement.Customer,
                        Subtype = i.Saltminer.Engagement.Subtype,
                        PublishDate = i.Saltminer.Engagement.PublishDate,
                        Attributes = i.Saltminer.Engagement.Attributes,
                    },
                    Attributes = attr,
                },
                Vulnerability = new VulnerabilityInfo()
                {
                    SourceSeverity = i.Vulnerability.SourceSeverity,
                    TestStatus = i.Vulnerability.TestStatus,
                    Audit = new AuditInfo()
                    {
                        Audited = i.Vulnerability.Audit.Audited,
                        Auditor = i.Vulnerability.Audit.Auditor,
                        LastAudit = i.Vulnerability.Audit.LastAudit,
                    },
                    FoundDate = i.Vulnerability.FoundDate,
                    Id = i.Vulnerability.Id,
                    IsFiltered = i.Vulnerability.IsFiltered,
                    IsSuppressed = i.Vulnerability.IsSuppressed,
                    Location = i.Vulnerability.Location,
                    LocationFull = i.Vulnerability.LocationFull,
                    RemovedDate = i.Vulnerability.RemovedDate,
                    ReportId = i.Vulnerability.ReportId,
                    Category = i.Vulnerability.Category,
                    Classification = i.Vulnerability.Classification,
                    Description = i.Vulnerability.Description,
                    Enumeration = i.Vulnerability.Enumeration,
                    Name = i.Vulnerability.Name,
                    Details = i.Vulnerability.Details,
                    Implication = i.Vulnerability.Implication,
                    Proof = i.Vulnerability.Proof,
                    Recommendation = i.Vulnerability.Recommendation,
                    References = i.Vulnerability.References,
                    TestingInstructions = i.Vulnerability.TestingInstructions,
                    Reference = i.Vulnerability.Reference,
                    Scanner = i.Vulnerability.Scanner,
                    Score = i.Vulnerability.Score,
                    Severity = i.Vulnerability.Severity
                },
                Id = i.Id,
                Labels = i.Labels,
                Message = i.Message,
                Tags = i.Tags,
                Timestamp = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            // Act
            var e1 = q.Equals(i, assetAttr);

            // Assert
            Assert.IsTrue(e1.IsEqual, "Should be equal");
        }


        [TestMethod]
        public void QueueIssue_To_Issue_NULLS()
        {
            // Arrange
            var i = Mock.Issue("saltminer");
            var q = new QueueIssue
            {
                Saltminer = new SaltMinerQueueIssueInfo()
                {
                    Source = new SourceInfo()
                    {
                        Analyzer = null,
                        Confidence = i.Saltminer.Source.Confidence,
                        Impact = i.Saltminer.Source.Impact,
                        IssueStatus = i.Saltminer.Source.IssueStatus,
                        Kingdom = i.Saltminer.Source.Kingdom,
                        Likelihood = i.Saltminer.Source.Likelihood,
                    },
                    QueueScanId = "SM-QScan-ID-prolly-guid-00002",
                    QueueAssetId = "SM-QAsset-ID-prolly-guid-00002",
                    Attributes = null,
                    Engagement = new EngagementInfo()
                    {
                        Customer = "Engagement Customer",
                        Id = Guid.NewGuid().ToString(),
                        Name = "Engagement Test",
                        Subtype = "PenTest",
                        PublishDate = DateTime.UtcNow,
                        Attributes = null
                    },
                    CustomData = null
                },
                Vulnerability = null,
                Id = i.Id,
                Labels = null,
                Message = i.Message,
                Tags = i.Tags,
                Timestamp = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            // Act
            var e1 = q.Equals(i, null);

            // Assert
            Assert.IsFalse(e1.IsEqual, "Should not be equal");
            Assert.IsTrue(e1.Messages.Any(m => m.Contains("Saltminer.Attributes")), "Should have shown attributes to be unequal");
            Assert.IsTrue(e1.Messages.Any(m => m.Contains("Vulnerability")), "Should have shown vulnerability to be unequal");
        }
    }
}
