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
using Saltworks.SaltMiner.UiApiClient.Helpers;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    // formerly FullEngagementDto
    public class EngagementFull : UiModelBase
    {
        public string AppVersion { get; set; }

        public string Id { get; set; }

        public bool IsPublished { get; set; }

        public List<string> OptionalFields { get; set; }

        [DateValidation]
        public DateTime? CreateDate { get; set; }

        [DateValidation]
        public DateTime? PublishDate { get; set; }

        public string Name { get; set; }

        public string Customer { get; set; }

        public string Subtype { get; set; }

        public string Summary { get; set; }

        public string Status { get; set; }

        public string GroupId { get; set; }

        public List<UiAttachment> Attachments { get; set; }

        [AttributesValidation]
        public List<TextField> Attributes { get; set; }

        public ScanFull Scan { get; set; }

        public List<AssetFull> Assets { get; set; }

        public List<IssueFull> Issues { get; set; }

        public IssueCount IssueCount { get; set; }

        public List<UiComment> Comments { get; set; }

        public EngagementFull()
        {
        }

        public EngagementFull(EngagementSummary engagement, string appVersion)
        {
            AppVersion = appVersion;
            Name = engagement.Name;
            Id = engagement.Id;
            Subtype = engagement.Subtype;
            CreateDate = engagement.Timestamp;
            PublishDate = engagement.PublishDate;
            Status = engagement.Status;
            IsPublished = engagement.Status == EngagementStatus.Published.ToString("g");
            Summary = engagement.Summary;
            Customer = engagement.Customer;
            Attachments = engagement.Attachments;
            Attributes = engagement.Attributes;
            GroupId = engagement.GroupId;
        }

        public EngagementFull(Engagement engagement, QueueScan queueScan, List<QueueAsset> queueAssets, List<QueueIssue> queueIssues, List<Comment> logs, List<Attachment> attachments, string appVersion, FieldInfo fieldInfo, FieldInfo assetFieldInfo, FieldInfo issueFieldInfo)
        {
            AppVersion = appVersion;
            Name = engagement.Saltminer.Engagement.Name;
            Id = engagement.Id;
            Subtype = engagement.Saltminer.Engagement.Subtype;
            CreateDate = engagement.Timestamp;
            PublishDate = engagement.Saltminer.Engagement.PublishDate;
            Status = engagement.Saltminer.Engagement.Status;
            IsPublished = engagement.Saltminer.Engagement.PublishDate.HasValue;
            Summary = engagement.Saltminer.Engagement.Summary;
            Customer = engagement.Saltminer.Engagement.Customer;
            Attributes = engagement.Saltminer.Engagement.Attributes.ToAttributeFields(fieldInfo);
            Scan = new ScanFull(queueScan, appVersion);
            Assets = queueAssets.Select(qa => new AssetFull(qa, appVersion, assetFieldInfo)).ToList();
            Issues = queueIssues.Select(qi => new IssueFull(qi, appVersion, issueFieldInfo)).ToList();
            Comments = logs.Select(c => new UiComment(c, appVersion)).ToList();
            Attachments = attachments.Select(a => new UiAttachment(a, appVersion)).ToList();
            GroupId = engagement.Saltminer.Engagement.GroupId;
        }

        public EngagementFull(Engagement engagement, Scan scan, List<Asset> assets, List<Issue> issues, List<Comment> logs, List<Attachment> attachments, string appVersion, FieldInfo fieldInfo, FieldInfo assetFieldInfo, FieldInfo issueFieldInfo)
        {
            AppVersion = appVersion;
            Name = engagement.Saltminer.Engagement.Name;
            Id = engagement.Id;
            Subtype = engagement.Saltminer.Engagement.Subtype;
            CreateDate = engagement.Timestamp;
            PublishDate = engagement.Saltminer.Engagement.PublishDate;
            IsPublished = engagement.Saltminer.Engagement.PublishDate.HasValue;
            Status = engagement.Saltminer.Engagement.Status;
            Summary = engagement.Saltminer.Engagement.Summary;
            Customer = engagement.Saltminer.Engagement.Customer;
            Attributes = engagement.Saltminer.Engagement.Attributes.ToAttributeFields(fieldInfo);
            GroupId = engagement.Saltminer.Engagement.GroupId;
            Scan = new ScanFull(scan, appVersion);
            Assets = assets.Select(a => new AssetFull(a, appVersion, assetFieldInfo)).ToList();
            Issues = issues.Select(i => new IssueFull(i, appVersion, issueFieldInfo)).ToList();
            Comments = logs.Select(c => new UiComment(c, appVersion)).ToList();
            Attachments = attachments.Select(a => new UiAttachment(a, appVersion)).ToList();
        }

        public Engagement ToEngagement()
        {
            return new Engagement
            {
                Id = Id,
                Timestamp = (CreateDate ?? DateTime.UtcNow),
                Saltminer = new SaltMinerEngagementWrapper
                {
                    Engagement = new SaltMinerEngagementInfo
                    {
                        Name = Name,
                        Customer = Customer,
                        Summary = Summary,
                        Subtype = Subtype,
                        PublishDate = PublishDate,
                        Attributes = Attributes.ToDictionary(),
                        GroupId = GroupId,
                        Status = Status
                    }
                }
            };
        }

        public Engagement ImportEngagement()
        {
            return new Engagement
            {
                Id = Id,
                Timestamp = (CreateDate ?? DateTime.UtcNow),
                Saltminer = new SaltMinerEngagementWrapper
                {
                    Engagement = new SaltMinerEngagementInfo
                    {
                        Name = Name,
                        Customer = Customer,
                        Summary = Summary,
                        Subtype = Subtype,
                        PublishDate = PublishDate,
                        Attributes = Attributes.ToDictionary(),
                        GroupId = GroupId,
                        Status = Status
                    }
                }
            };
        }

        public QueueScan ImportQueueScan(string sourceType, string assetType, string instance)
        {
            return new QueueScan
            {
                Id = Scan.ScanId,
                Timestamp = Scan.Timestamp,
                Saltminer = new SaltMinerQueueScanInfo
                {
                    Engagement = new EngagementInfo
                    {
                        Id = Id,
                        Name = Name,
                        Subtype = Subtype,
                        Customer = Customer,
                        PublishDate = PublishDate,
                        Attributes = Attributes.ToDictionary()
                    },
                    Internal = new QueueScanInternal
                    {
                        IssueCount = -1,
                        QueueStatus = Scan.Status
                    },
                    Scan = new QueueScanInfo
                    {
                        ReportId = Scan.ReportId,
                        AssessmentType = AssessmentType.Pen.ToString(),
                        ProductType = Scan.ProductType,
                        ScanDate = Scan.ScanDate,
                        AssetType = assetType,
                        SourceType = sourceType,
                        Product = Scan.Product,
                        Vendor = Scan.Vendor,
                        Instance = instance
                    }
                }
            };
        }

        public List<QueueAsset> ImportQueueAssets(string sourceType, string assetType, string instance, string inventoryAssetKeyAttribute)
        {
            List<QueueAsset> list = [];
            foreach (var asset in Assets)
            {
                if (string.IsNullOrEmpty(asset.AssetId))
                {
                    throw new UiApiClientValidationMissingValueException("Asset must have a ID");
                }

                list.Add(new QueueAsset
                {
                    Saltminer = new SaltMinerQueueAssetInfo
                    {
                        InventoryAsset = EngagementHelper.GetInventoryAssetKeyValue(inventoryAssetKeyAttribute, asset.Attributes.ToDictionary()),
                        Asset = new AssetInfoPolicy
                        {
                            Attributes = asset.Attributes.ToDictionary(),
                            Description = asset.Description.Value,
                            IsProduction = asset.IsProduction,
                            Name = asset.Name.Value,
                            Instance = instance,
                            SourceType = sourceType,
                            IsSaltminerSource = asset.IsSaltminerSource,
                            SourceId = asset.SourceId,
                            Version = asset.Version.Value,
                            VersionId = asset.VersionId.Value,
                            AssetType = assetType,
                            IsRetired = asset.IsRetired,
                            LastScanDaysPolicy = asset.LastScanDaysPolicy
                        },
                        Internal = new QueueAssetInternal
                        {
                            QueueScanId = Scan.ScanId
                        },
                        Engagement = new EngagementInfo
                        {
                            Id = Id,
                            Name = Name,
                            Subtype = Subtype
                        }
                    },
                    Timestamp = asset.Timestamp,
                    Id = asset.AssetId
                });
            }

            return list;
        }

        public List<QueueIssue> ImportQueueIssues(string uiBaseUrl)
        {
            List<QueueIssue> list = [];
            foreach (IssueFull issue in Issues)
            {
                if (string.IsNullOrEmpty(issue.Id))
                {
                    throw new UiApiClientValidationMissingValueException("Issue must have a ID");
                }

                if (string.IsNullOrEmpty(issue.AssetId))
                {
                    throw new UiApiClientValidationMissingValueException("Issue must have a Asset ID");
                }

                string text = issue.ScannerId ?? Guid.NewGuid().ToString();
                list.Add(new QueueIssue
                {
                    Saltminer = new SaltMinerQueueIssueInfo
                    {
                        QueueScanId = Scan.ScanId,
                        QueueAssetId = issue.AssetId,
                        Engagement = new EngagementInfo
                        {
                            Id = Id,
                            Name = Name,
                            Subtype = Subtype,
                            Customer = Customer,
                            PublishDate = PublishDate,
                            Attributes = Attributes.ToDictionary()
                        },
                        Attributes = issue.Attributes.ToDictionary()
                    },
                    Vulnerability = new VulnerabilityInfo
                    {
                        FoundDate = issue.FoundDate,
                        Severity = issue.Severity.Value,
                        Name = issue.Name.Value,
                        TestStatus = issue.TestStatus.Value,
                        Category = issue.Category,
                        Audit = new AuditInfo
                        {
                            Audited = issue.Audited,
                            Auditor = issue.Auditor,
                            LastAudit = issue.LastAudit
                        },
                        Classification = issue.Classification,
                        Description = issue.Description.Value,
                        Enumeration = issue.Enumeration,
                        IsFiltered = issue.IsFiltered,
                        IsSuppressed = issue.IsSuppressed.Value ?? false,
                        Location = issue.Location.Value,
                        LocationFull = issue.LocationFull.Value,
                        Reference = issue.Reference,
                        References = issue.References.Value,
                        RemovedDate = issue.RemovedDate.Value,
                        Details = issue.Details.Value,
                        Implication = issue.Implication.Value,
                        TestingInstructions = issue.TestingInstructions.Value,
                        Recommendation = issue.Recommendation.Value,
                        Proof = issue.Proof.Value,
                        SourceSeverity = issue.Severity.Value,
                        Scanner = new ScannerInfo
                        {
                            Id = text,
                            Product = issue.Product.Value,
                            Vendor = issue.Vendor.Value,
                            AssessmentType = AssessmentType.Pen.ToString(),
                            GuiUrl = $"{uiBaseUrl}/engagements/{Id}/scanner/{text}"
                        },
                        ReportId = issue.ReportId,
                        Score = new ScoreInfo
                        {
                            Version = issue.Version,
                            Base = issue.Base,
                            Environmental = issue.Environmental,
                            Temporal = issue.Temporal
                        },
                        Id = issue.VulnerabilityId
                    },
                    Timestamp = issue.Timestamp,
                    Id = issue.Id
                });
            }

            return list;
        }

        public List<Comment> ImportComments(bool createNew)
        {
            List<Comment> list = [];
            if ((Comments?.Count ?? 0) > 0)
            {
                foreach (UiComment comment in Comments)
                {
                    IdInfo asset = (string.IsNullOrEmpty(comment.AssetId) ? null : new IdInfo
                    {
                        Id = comment.AssetId
                    });
                    IdInfo engagement = (string.IsNullOrEmpty(comment.EngagementId) ? new IdInfo
                    {
                        Id = Id
                    } : new IdInfo
                    {
                        Id = comment.EngagementId
                    });
                    IdInfo issue = (string.IsNullOrEmpty(comment.IssueId) ? null : new IdInfo
                    {
                        Id = comment.IssueId
                    });
                    IdInfo scan = (string.IsNullOrEmpty(comment.ScanId) ? new IdInfo
                    {
                        Id = Scan.ScanId
                    } : new IdInfo
                    {
                        Id = comment.ScanId
                    });
                    list.Add(new Comment
                    {
                        Id = (createNew ? null : comment.Id),
                        Saltminer = new SaltMinerCommentInfo
                        {
                            Asset = asset,
                            Scan = scan,
                            Engagement = engagement,
                            Issue = issue,
                            Comment = new CommentInfo
                            {
                                Message = comment.Message,
                                User = comment.User,
                                UserFullName = comment.UserFullName,
                                ParentId = comment.ParentId,
                                Type = comment.Type,
                                Added = comment.Added
                            }
                        },
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            return list;
        }

        public Engagement ImportNewEngagement(string name, DateTime? createDate)
        {
            return new Engagement
            {
                Id = null,
                Timestamp = (createDate ?? DateTime.UtcNow),
                Saltminer = new SaltMinerEngagementWrapper
                {
                    Engagement = new SaltMinerEngagementInfo
                    {
                        Name = name,
                        Customer = Customer,
                        Summary = Summary,
                        Subtype = Subtype,
                        PublishDate = null,
                        Attributes = Attributes.ToDictionary(),
                        GroupId = Guid.NewGuid().ToString(),
                        Status = EngagementStatus.Draft.ToString("g")
                    }
                }
            };
        }

        public QueueAsset ImportNewQueueAsset(AssetFull asset, string sourceType, string assetType, string instance, string inventoryAssetKeyAttribute)
        {
            return new QueueAsset
            {
                Saltminer = new SaltMinerQueueAssetInfo
                {
                    InventoryAsset = new() { Key = asset.Attributes.FirstOrDefault(a => a.Name == inventoryAssetKeyAttribute)?.Value },
                    Asset = new AssetInfoPolicy
                    {
                        Attributes = asset.Attributes.ToDictionary(),
                        Description = asset.Description.Value,
                        IsProduction = asset.IsProduction,
                        Name = asset.Name.Value,
                        Instance = instance,
                        SourceType = sourceType,
                        IsSaltminerSource = asset.IsSaltminerSource,
                        SourceId = asset.SourceId,
                        Version = asset.Version.Value,
                        VersionId = asset.VersionId.Value,
                        AssetType = assetType,
                        IsRetired = asset.IsRetired,
                        LastScanDaysPolicy = asset.LastScanDaysPolicy
                    },
                    Internal = new QueueAssetInternal
                    {
                        QueueScanId = Scan.ScanId
                    },
                    Engagement = new EngagementInfo
                    {
                        Id = Id,
                        Name = Name,
                        Subtype = Subtype
                    }
                },
                Timestamp = DateTime.UtcNow,
                Id = null
            };
        }

        public QueueIssue ImportNewQueueIssue(IssueFull issue, string uiBaseUrl)
        {
            string value = issue.ScannerId ?? Guid.NewGuid().ToString();
            return new QueueIssue
            {
                Saltminer = new SaltMinerQueueIssueInfo
                {
                    QueueScanId = Scan.ScanId,
                    QueueAssetId = issue.AssetId,
                    Engagement = new EngagementInfo
                    {
                        Id = Id,
                        Name = Name,
                        Subtype = Subtype,
                        Customer = Customer,
                        PublishDate = PublishDate,
                        Attributes = Attributes.ToDictionary()
                    },
                    Attributes = issue.Attributes.ToDictionary()
                },
                Vulnerability = new VulnerabilityInfo
                {
                    FoundDate = issue.FoundDate,
                    Severity = issue.Severity.Value,
                    Name = issue.Name.Value,
                    TestStatus = issue.TestStatus.Value,
                    Category = issue.Category,
                    Audit = new AuditInfo
                    {
                        Audited = issue.Audited,
                        Auditor = issue.Auditor,
                        LastAudit = issue.LastAudit
                    },
                    Classification = issue.Classification,
                    Description = issue.Description.Value,
                    Enumeration = issue.Enumeration,
                    IsFiltered = issue.IsFiltered,
                    IsSuppressed = issue.IsSuppressed.Value ?? false,
                    Location = issue.Location.Value,
                    LocationFull = issue.LocationFull.Value,
                    Reference = issue.Reference,
                    References = issue.References.Value,
                    RemovedDate = issue.RemovedDate.Value,
                    Details = issue.Details.Value,
                    Implication = issue.Implication.Value,
                    TestingInstructions = issue.TestingInstructions.Value,
                    Recommendation = issue.Recommendation.Value,
                    Proof = issue.Proof.Value,
                    SourceSeverity = issue.Severity.Value,
                    Scanner = new ScannerInfo
                    {
                        Id = (issue.ScannerId ?? Guid.NewGuid().ToString()),
                        Product = issue.Product.Value,
                        Vendor = issue.Vendor.Value,
                        AssessmentType = AssessmentType.Pen.ToString(),
                        GuiUrl = $"{uiBaseUrl}/engagements/{Id}/scanner/{value}"
                    },
                    ReportId = issue.ReportId,
                    Score = new ScoreInfo
                    {
                        Version = issue.Version,
                        Base = issue.Base,
                        Environmental = issue.Environmental,
                        Temporal = issue.Temporal
                    },
                    Id = issue.VulnerabilityId
                },
                Timestamp = DateTime.UtcNow,
                Id = null
            };
        }
    }
}
