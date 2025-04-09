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

﻿using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.UiApiClient.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class IssueFull : UiModelBase
    {
        public string AppVersion { get; set; }

        [Required]
        public EngagementInfo Engagement { get; set; }

        public TextField Name { get; set; }

        public string Id { get; set; }

        [SeverityValidation]
        public SelectField Severity { get; set; }

        public int SeverityLevel { get; set; }

        public TextField AssetName { get; set; }

        public string AssetId { get; set; }

        [Required, DateValidation]
        public DateTime? FoundDate { get; set; }

        public int? DaysToClose { get; private set; }

        [TestStatusValidation]
        public SelectField TestStatus { get; set; }

        public BooleanField IsSuppressed { get; set; }

        public bool IsRemoved { get; set; }

        public bool IsActive { get; set; }

        public bool IsFiltered { get; set; }

        public string[] VulnerabilityId { get; set; }

        [Required]
        public string ScanId { get; set; }

        [DateValidation]
        public DateField RemovedDate { get; set; }

        public TextField Location { get; set; }

        public TextField LocationFull { get; set; }

        public bool IsHistorical { get; set; }

        [Required]
        public string ReportId { get; set; }

        public string ScannerId { get; set; }

        [Required]
        public string[] Category { get; set; }

        public string Classification { get; set; }

        public TextField Description { get; set; }

        public bool Audited { get; set; }

        public string Auditor { get; set; }

        [DateValidation]
        public DateTime? LastAudit { get; set; }

        public string Enumeration { get; set; }

        [Markdown]
        public MarkdownField Proof { get; set; }

        [Markdown]
        public MarkdownField TestingInstructions { get; set; }

        [Markdown]
        public MarkdownField Details { get; set; }

        [Markdown]
        public MarkdownField Implication { get; set; }

        [Markdown]
        public MarkdownField Recommendation { get; set; }

        [Markdown]
        public MarkdownField References { get; set; }

        public string Reference { get; set; }

        public TextField Vendor { get; set; }

        public TextField Product { get; set; }

        public float Base { get; set; }

        public float Environmental { get; set; }

        public float Temporal { get; set; }

        [DateValidation]
        public DateTime Timestamp { get; set; }

        public string Version { get; set; }

        [AttributesValidation]
        public List<TextField> Attributes { get; set; }

        public string GuiUrl { get; set; }

        public LockInfo LockInfo { get; set; }

        public IssueFull()
        {
        }

        public IssueFull(Engagement engagement, FieldInfo fieldInfo)
        {
            Name = new(default, nameof(Name), fieldInfo, true);
            AssetName = new(default, nameof(AssetName), fieldInfo, true);
            Description = new(default, nameof(Description), fieldInfo, true);
            Severity = new(default, nameof(Severity), fieldInfo, true);
            TestStatus = new(default, nameof(TestStatus), fieldInfo, true);
            IsSuppressed = new(default, nameof(IsSuppressed), fieldInfo, true);
            RemovedDate = new(default, nameof(RemovedDate), fieldInfo, true);
            Location = new(default, nameof(Location), fieldInfo, true);
            LocationFull = new(default, nameof(LocationFull), fieldInfo, true);
            TestingInstructions = new(default, nameof(TestingInstructions), fieldInfo, true);
            Proof = new(default, nameof(Proof), fieldInfo, true);
            Details = new(default, nameof(Details), fieldInfo, true);
            Implication = new(default, nameof(Implication), fieldInfo, true);
            Recommendation = new(default, nameof(Recommendation), fieldInfo, true);
            References = new(default, nameof(References), fieldInfo, true);
            Vendor = new(default, nameof(Vendor), fieldInfo, true);
            Product = new(default, nameof(Product), fieldInfo, true);
            Attributes = fieldInfo.AttributeDefinitions.Select(ad => new TextField(default, ad.Name, fieldInfo, true, true)).ToList();
            Engagement = new()
            {
                Id = engagement.Id,
                Name = engagement.Saltminer.Engagement.Name,
                Attributes = engagement.Saltminer.Engagement.Attributes,
                Customer = engagement.Saltminer.Engagement.Customer,
                PublishDate = engagement.Saltminer.Engagement.PublishDate,
                Subtype = engagement.Saltminer.Engagement.Subtype
            };
        }

        /// <summary>
        /// Only for use with import
        /// </summary>
        public IssueFull(QueueIssue queueIssue, string appVersion)
        {
            AppVersion = appVersion;
            SetIssueNonFieldValues(queueIssue.Vulnerability, queueIssue.Saltminer.Engagement);
            SetIssueFieldValues(queueIssue.Vulnerability, queueIssue.Saltminer.Attributes, null);
            AssetName = null;
            AssetId = queueIssue.Saltminer.QueueAssetId;
            Id = queueIssue.Id;
            ScanId = queueIssue.Saltminer.QueueScanId;
            LockInfo = null;
            IsHistorical = queueIssue.Saltminer.IsHistorical;
            Timestamp = queueIssue.Timestamp;
        }

        /// <summary>
        /// Only for use with import
        /// </summary>
        public IssueFull(Issue issue, string appVersion)
        {
            AppVersion = appVersion;
            SetIssueNonFieldValues(issue.Vulnerability, issue.Saltminer.Engagement);
            SetIssueFieldValues(issue.Vulnerability, issue.Saltminer.Attributes, null);
            AssetName = new(issue.Saltminer.Asset.Name, nameof(AssetName), null);
            AssetId = issue.Saltminer.Asset.Id;
            Id = issue.Id;
            ScanId = issue.Saltminer.Scan.Id;
            LockInfo = null;
            IsHistorical = issue.Saltminer.IsHistorical;
            Timestamp = issue.Timestamp;
        }

        public IssueFull(QueueIssue queueIssue, string appVersion, FieldInfo fieldInfo)
        {
            AppVersion = appVersion;
            SetIssueNonFieldValues(queueIssue.Vulnerability, queueIssue.Saltminer.Engagement);
            SetIssueFieldValues(queueIssue.Vulnerability, queueIssue.Saltminer.Attributes, fieldInfo);
            AssetName = null;
            AssetId = queueIssue.Saltminer.QueueAssetId;
            Id = queueIssue.Id;
            ScanId = queueIssue.Saltminer.QueueScanId;
            LockInfo = null;
            IsHistorical = queueIssue.Saltminer.IsHistorical;
            Timestamp = queueIssue.Timestamp;
        }

        public IssueFull(Issue issue, string appVersion, FieldInfo fieldInfo)
        {
            AppVersion = appVersion;
            SetIssueNonFieldValues(issue.Vulnerability, issue.Saltminer.Engagement);
            SetIssueFieldValues(issue.Vulnerability, issue.Saltminer.Attributes, fieldInfo);
            AssetName = new(issue.Saltminer.Asset.Name, nameof(AssetName), fieldInfo);
            AssetId = issue.Saltminer.Asset.Id;
            Id = issue.Id;
            ScanId = issue.Saltminer.Scan.Id;
            LockInfo = null;
            IsHistorical = issue.Saltminer.IsHistorical;
            Timestamp = issue.Timestamp;
        }

        private void SetIssueFieldValues(VulnerabilityInfo vulnerability, Dictionary<string, string> attributes, FieldInfo fieldInfo)
        {
            // Import use case only
            if (fieldInfo == null)
            {
                Attributes = attributes.ToAttributeFields();
                Severity = new() { Value = vulnerability.Severity, Name = nameof(Severity) };
                TestStatus = new() { Value = vulnerability.TestStatus, Name = nameof(TestStatus) };
                IsSuppressed = new() { Value = vulnerability.IsSuppressed, Name = nameof(IsSuppressed) };
                RemovedDate = new() { Value = vulnerability.RemovedDate, Name = nameof(RemovedDate) };
                Location = new() { Value = vulnerability.Location.Trim(), Name = nameof(Location) };
                LocationFull = new() { Value = vulnerability.LocationFull.Trim(), Name = nameof(LocationFull) };
                Description = new() { Value = vulnerability.Description, Name = nameof(Description) };
                Proof = new() { Value = vulnerability.Proof, Name = nameof(Proof) };
                Details = new() { Value = vulnerability.Details, Name = nameof(Details) };
                TestingInstructions = new() { Value = vulnerability.TestingInstructions, Name = nameof(TestingInstructions) };
                Implication = new() { Value = vulnerability.Implication, Name = nameof(Implication) };
                Recommendation = new() { Value = vulnerability.Recommendation, Name = nameof(Recommendation) };
                References = new() { Value = vulnerability.References, Name = nameof(References) };
                Vendor = new() { Value = vulnerability.Scanner.Vendor.Trim(), Name = nameof(Vendor) };
                Product = new() { Value = vulnerability.Scanner.Product.Trim(), Name = nameof(Product) };
                Name = new() { Value = vulnerability.Name, Name = nameof(Name) };
            }
            // Normal
            else
            {
                Attributes = attributes.ToAttributeFields(fieldInfo);
                Severity = new(vulnerability.Severity, nameof(Severity), fieldInfo);
                TestStatus = new(vulnerability.TestStatus, nameof(TestStatus), fieldInfo);
                IsSuppressed = new(vulnerability.IsSuppressed, nameof(IsSuppressed), fieldInfo);
                RemovedDate = new(vulnerability.RemovedDate, nameof(RemovedDate), fieldInfo);
                Location = new(vulnerability.Location.Trim(), nameof(Location), fieldInfo);
                LocationFull = new(vulnerability.LocationFull.Trim(), nameof(LocationFull), fieldInfo);
                Description = new(vulnerability.Description, nameof(Description), fieldInfo);
                Proof = new(vulnerability.Proof, nameof(Proof), fieldInfo);
                Details = new(vulnerability.Details, nameof(Details), fieldInfo);
                TestingInstructions = new(vulnerability.TestingInstructions, nameof(TestingInstructions), fieldInfo);
                Implication = new(vulnerability.Implication, nameof(Implication), fieldInfo);
                Recommendation = new(vulnerability.Recommendation, nameof(Recommendation), fieldInfo);
                References = new(vulnerability.References, nameof(References), fieldInfo);
                Vendor = new(vulnerability.Scanner.Vendor.Trim(), nameof(Vendor), fieldInfo);
                Product = new(vulnerability.Scanner.Product.Trim(), nameof(Product), fieldInfo);
                Name = new(vulnerability.Name, nameof(Name), fieldInfo);
            }
        }

        private void SetIssueNonFieldValues(VulnerabilityInfo vulnerability, EngagementInfo engagement)
        {
            Engagement = new()
            {
                Id = engagement.Id,
                Name = engagement.Name,
                Attributes = engagement.Attributes,
                Customer = engagement.Customer,
                Subtype = engagement.Subtype,
                PublishDate = engagement.PublishDate
            };
            VulnerabilityId = vulnerability.Id;
            FoundDate = vulnerability.FoundDate;
            ReportId = vulnerability.ReportId;
            Classification = vulnerability.Classification;
            Enumeration = vulnerability.Enumeration;
            Reference = vulnerability.Reference;
            Category = vulnerability.Category;
            ScannerId = vulnerability.Scanner.Id;
            Audited = vulnerability.Audit?.Audited ?? false;
            Auditor = vulnerability.Audit?.Auditor;
            LastAudit = vulnerability.Audit?.LastAudit;
            Base = vulnerability.Score?.Base ?? 0f;
            Version = vulnerability.Score?.Version;
            Environmental = vulnerability.Score?.Environmental ?? 0f;
            Temporal = vulnerability.Score?.Temporal ?? 0f;
            DaysToClose = vulnerability.DaysToClose;
            SeverityLevel = vulnerability.SeverityLevel;
            IsRemoved = vulnerability.IsRemoved;
            IsActive = vulnerability.IsActive;
        }

        public QueueIssue CloneRequest(Engagement engagement, string scanId, string assetId, string uiBaseUrl)
        {
            string id = Guid.NewGuid().ToString();
            var qIssue = GetQueueIssue(uiBaseUrl);
            qIssue.Id = id;
            qIssue.Saltminer.QueueScanId = scanId;
            qIssue.Saltminer.QueueAssetId = assetId;
            qIssue.Saltminer.Engagement = new EngagementInfo
            {
                Id = engagement.Id,
                Name = engagement.Saltminer.Engagement.Name,
                Customer = engagement.Saltminer.Engagement.Customer,
                Subtype = engagement.Saltminer.Engagement.Subtype,
                Attributes = engagement.Saltminer.Engagement.Attributes,
                PublishDate = engagement.Saltminer.Engagement.PublishDate
            };
            return qIssue;
        }

        public QueueIssue CreateNewIssue(SaltMinerEngagementInfo engagement, string scanId, string uiBaseUrl)
        {
            var qIssue = GetQueueIssue(uiBaseUrl);
            qIssue.Id = Guid.NewGuid().ToString();
            qIssue.Vulnerability.Scanner.Id = Guid.NewGuid().ToString();
            qIssue.Saltminer.QueueScanId= scanId;
            qIssue.Saltminer.Engagement = new EngagementInfo
            {
                Id = Engagement.Id,
                Name = engagement.Name,
                Customer = engagement.Customer,
                Subtype = engagement.Subtype,
                Attributes = engagement.Attributes,
                PublishDate = engagement.PublishDate
            };
            return qIssue;
        }

        public QueueIssue GetQueueIssue(string uiBaseUrl)
        {
            QueueIssue queueIssue = new()
            {
                Id = Id,
                Saltminer = new SaltMinerQueueIssueInfo
                {
                    QueueScanId = ScanId,
                    QueueAssetId = AssetId,
                    Engagement = new EngagementInfo
                    {
                        Id = Engagement.Id,
                        Name = Engagement.Name,
                        Customer = Engagement.Customer,
                        Subtype = Engagement.Subtype,
                        Attributes = Engagement.Attributes,
                        PublishDate = Engagement.PublishDate
                    },
                    Attributes = Attributes.ToDictionary(),
                    IsHistorical = IsHistorical
                },
                Vulnerability = new VulnerabilityInfo
                {
                    FoundDate = FoundDate,
                    Severity = Severity.Value,
                    Name = Name.Value,
                    TestStatus = TestStatus.Value,
                    Category = [ "Application" ],
                    Audit = new AuditInfo
                    {
                        Auditor = Auditor,
                        Audited = Audited,
                        LastAudit = LastAudit
                    },
                    Classification = Classification,
                    Description = Description.Value,
                    Enumeration = Enumeration,
                    Id = VulnerabilityId,
                    IsFiltered = IsFiltered,
                    IsSuppressed = IsSuppressed.Value ?? false,
                    Location = (string.IsNullOrEmpty(Location.Value) ? " " : Location.Value),
                    LocationFull = (string.IsNullOrEmpty(LocationFull.Value) ? " " : LocationFull.Value),
                    Reference = Reference,
                    References = References.Value,
                    RemovedDate = RemovedDate.Value,
                    Details = Details.Value,
                    Implication = Implication.Value,
                    TestingInstructions = TestingInstructions.Value,
                    Recommendation = Recommendation.Value,
                    Proof = Proof.Value,
                    SourceSeverity = Severity.Value,
                    Scanner = new ScannerInfo
                    {
                        Id = ScannerId,
                        Product = (string.IsNullOrEmpty(Product.Value) ? " " : Product.Value),
                        Vendor = (string.IsNullOrEmpty(Vendor.Value) ? " " : Vendor.Value),
                        AssessmentType = AssessmentType.Pen.ToString(),
                        GuiUrl = $"{uiBaseUrl}/engagements/{Engagement.Id}/scanner/{ScannerId}"
                    },
                    ReportId = Engagement.Id,
                    Score = new ScoreInfo
                    {
                        Base = Base,
                        Environmental = Environmental,
                        Temporal = Temporal,
                        Version = Version
                    }
                },
                Timestamp = DateTime.UtcNow
            };
            return queueIssue;
        }
    }
}
