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

﻿using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.UiApiClient.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class IssueImport : UiModelBase
    {
        [Required]
        public string AppVersion { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [SeverityValidation]
        public string Severity { get; set; }

        [Required]
        [DateValidation]
        public DateTime? FoundDate { get; set; }

        [Required]
        [TestStatusValidation]
        public string TestStatus { get; set; }

        public bool IsSuppressed { get; set; }

        public bool IsFiltered { get; set; }

        [DateValidation]
        public DateTime? RemovedDate { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public string LocationFull { get; set; }

        [Required]
        public string ReportId { get; set; }

        public string[] Category { get; set; }

        public string Classification { get; set; }

        public string Description { get; set; }

        public bool Audited { get; set; }

        public string Auditor { get; set; }

        [DateValidation]
        public DateTime? LastAudit { get; set; }

        public string Enumeration { get; set; }

        [Markdown]
        public string Proof { get; set; }

        [Markdown]
        public string TestingInstructions { get; set; }

        [Markdown]
        public string Details { get; set; }

        [Markdown]
        public string Implication { get; set; }

        [Markdown]
        public string Recommendation { get; set; }

        [Markdown]
        public string References { get; set; }

        public string Reference { get; set; }

        [Required]
        public string Vendor { get; set; }

        [Required]
        public string Product { get; set; }

        public float Base { get; set; }

        public float Environmental { get; set; }

        public float Temporal { get; set; }

        public string ScoreVersion { get; set; }

        [AttributesValidation]
        public Dictionary<string, string> Attributes { get; set; }

        public IssueImport()
        {
        }

        public IssueImport(IssueFull issue)
        {
            AppVersion = issue.AppVersion;
            Name = issue.Name.Value;
            Severity = issue.Severity.Value;
            FoundDate = issue.FoundDate;
            TestStatus = issue.TestStatus.Value;
            IsSuppressed = issue.IsSuppressed.Value ?? false;
            IsFiltered = issue.IsFiltered;
            RemovedDate = issue.RemovedDate.Value;
            Location = issue.Location.Value;
            LocationFull = issue.LocationFull.Value;
            ReportId = issue.ReportId;
            Category = issue.Category;
            Classification = issue.Classification;
            Description = issue.Description.Value;
            Audited = issue.Audited;
            Auditor = issue.Auditor;
            LastAudit = issue.LastAudit;
            Enumeration = issue.Enumeration;
            Proof = issue.Proof.Value;
            TestingInstructions = issue.TestingInstructions.Value;
            Details = issue.Details.Value;
            Implication = issue.Implication.Value;
            Recommendation = issue.Recommendation.Value;
            References = issue.References.Value;
            Reference = issue.Reference;
            Vendor = issue.Vendor.Value;
            Product = issue.Product.Value;
            Base = issue.Base;
            Environmental = issue.Environmental;
            Temporal = issue.Temporal;
            ScoreVersion = issue.Version;
            Attributes = issue.Attributes.ToDictionary();
        }

        public QueueIssue ParseQueueIssue(Engagement engagement, string scanId, string assetId, string uiBaseUrl)
        {
            string text = Guid.NewGuid().ToString();
            return new QueueIssue
            {
                Id = Guid.NewGuid().ToString(),
                Vulnerability = new VulnerabilityInfo
                {
                    ReportId = ReportId,
                    FoundDate = FoundDate,
                    Severity = Severity,
                    Name = Name,
                    TestStatus = TestStatus,
                    Category = Category ?? [ "Application" ],
                    Audit = new AuditInfo
                    {
                        Audited = Audited,
                        Auditor = Auditor,
                        LastAudit = LastAudit
                    },
                    Classification = Classification,
                    Description = Description,
                    Enumeration = Enumeration,
                    IsFiltered = IsFiltered,
                    IsSuppressed = IsSuppressed,
                    Location = (string.IsNullOrEmpty(Location) ? " " : Location),
                    LocationFull = (string.IsNullOrEmpty(LocationFull) ? " " : LocationFull),
                    Reference = Reference,
                    References = References,
                    RemovedDate = RemovedDate,
                    Details = Details,
                    Implication = Implication,
                    TestingInstructions = TestingInstructions,
                    Recommendation = Recommendation,
                    Proof = Proof,
                    SourceSeverity = Severity,
                    Scanner = new ScannerInfo
                    {
                        Id = text,
                        GuiUrl = $"{uiBaseUrl}/engagements/{engagement.Id}/scanner/{text}",
                        Product = (string.IsNullOrEmpty(Product) ? " " : Product),
                        Vendor = (string.IsNullOrEmpty(Vendor) ? " " : Vendor),
                        AssessmentType = AssessmentType.Pen.ToString(),
                        ApiUrl = null
                    },
                    Score = new ScoreInfo
                    {
                        Base = Base,
                        Environmental = Environmental,
                        Temporal = Temporal,
                        Version = ScoreVersion
                    }
                },
                Saltminer = new SaltMinerQueueIssueInfo
                {
                    QueueScanId = scanId,
                    QueueAssetId = assetId,
                    IssueType = IssueType.Pen.ToString("g"),
                    Engagement = new EngagementInfo
                    {
                        Id = engagement.Id,
                        Name = engagement.Saltminer.Engagement.Name,
                        Subtype = engagement.Saltminer.Engagement.Subtype,
                        Customer = engagement.Saltminer.Engagement.Customer,
                        Attributes = engagement.Saltminer.Engagement.Attributes,
                        PublishDate = engagement.Saltminer.Engagement.PublishDate
                    },
                    Attributes = Attributes,
                    Source = new SourceInfo
                    {
                        Analyzer = null,
                        Confidence = null,
                        Impact = null,
                        IssueStatus = null,
                        Kingdom = null,
                        Likelihood = null
                    }
                },
                Message = null,
                Tags = null,
                Labels = [],
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public class TemplateIssueImport : UiModelBase
    {
        public TemplateIssueImport() { }
        public IssueImport Issue { get; set; }
    }
}
