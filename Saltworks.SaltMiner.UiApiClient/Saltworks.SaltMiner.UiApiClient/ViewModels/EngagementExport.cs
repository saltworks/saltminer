using Saltworks.SaltMiner.UiApiClient.Attributes;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    /// <summary>
    /// Class used to serialize / deserialize a full engagement
    /// </summary>
    public class EngagementExport : UiModelBase
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

        public List<UiAttachmentExport> Attachments { get; set; }

        [AttributesValidation]
        public Dictionary<string, string> Attributes { get; set; }

        public ScanExport Scan { get; set; }

        public List<AssetExport> Assets { get; set; }

        public List<IssueExport> Issues { get; set; }

        public IssueCount IssueCount { get; set; }

        public List<UiComment> Comments { get; set; }

        public EngagementExport()
        {
        }

        public EngagementExport(EngagementFull fullEngagement)
        {
            AppVersion = fullEngagement.AppVersion;
            Id = fullEngagement.Id;
            IsPublished = fullEngagement.IsPublished;
            OptionalFields = fullEngagement.OptionalFields;
            CreateDate = fullEngagement.CreateDate;
            PublishDate = fullEngagement.PublishDate;
            Name = fullEngagement.Name;
            Customer = fullEngagement.Customer;
            Subtype = fullEngagement.Subtype;
            Summary = fullEngagement.Summary;
            Status = fullEngagement.Status;
            GroupId = fullEngagement.GroupId;
            Attachments = TransformAttachments(fullEngagement.Attachments);
            Attributes = fullEngagement.Attributes.ToDictionary();
            Scan = new ScanExport(fullEngagement.Scan);
            Assets = TransformAssets(fullEngagement.Assets);
            Issues = TransformIssues(fullEngagement.Issues);
            IssueCount = fullEngagement.IssueCount;
            Comments = fullEngagement.Comments;
            Status = fullEngagement.Status;
        }

        public EngagementFull ToEngagementFull()
        {
            return new EngagementFull
            {
                AppVersion = AppVersion,
                Id = Id,
                IsPublished = IsPublished,
                OptionalFields = OptionalFields,
                CreateDate = CreateDate,
                PublishDate = PublishDate,
                Name = Name,
                Customer = Customer,
                Subtype = Subtype,
                Summary = Summary,
                Status = Status,
                GroupId = GroupId,
                Attachments = Attachments.Select(attachment => new UiAttachment
                {
                    Id = attachment.Id,
                    Timestamp = attachment.Timestamp,
                    IssueId = attachment.IssueId,
                    Attachment = attachment.Attachment,
                    IsMarkdown = attachment.IsMarkdown,
                    User = attachment.User,
                    UserFullName = attachment.UserFullName
                }).ToList(),
                Attributes = Attributes.ToAttributeFields(),
                Scan = Scan.ToScanFull(),
                Assets = Assets.Select(asset => new AssetFull
                {
                    Name = new() { Value = asset.Name, Name = "Name" },
                    Description = new() { Value = asset.Description, Name = "Description" },
                    AssetId = asset.AssetId,
                    ScanId = asset.ScanId,
                    Timestamp = asset.Timestamp,
                    VersionId = new() { Value = asset.VersionId, Name = "VersionId" },
                    Version = new() { Value = asset.Version, Name = "Version" },
                    Host = asset.Host,
                    Ip = asset.Ip,
                    Scheme = asset.Scheme,
                    Port = asset.Port,
                    IsSaltminerSource = asset.IsSaltminerSource,
                    SourceId = asset.SourceId,
                    IsProduction = asset.IsProduction,
                    IsRetired = asset.IsRetired,
                    LastScanDaysPolicy = asset.LastScanDaysPolicy,
                    InventoryAssetKey = asset.InventoryAssetKey,
                    Attributes = asset.Attributes.ToAttributeFields()
                }).ToList(),
                Issues = Issues.Select(issue => new IssueFull
                {
                    Name = new() { Value = issue.Name, Name = "Name" },
                    Severity = new() { Value = issue.Severity, Name = "Severity" },
                    SeverityLevel = issue.SeverityLevel,
                    AssetName = new() { Value = issue.AssetName, Name = "AssetName" },
                    AssetId = issue.AssetId,
                    FoundDate = issue.FoundDate,
                    TestStatus = new() { Value = issue.TestStatus, Name = "TestStatus" },
                    IsRemoved = issue.IsRemoved,
                    IsSuppressed = new() { Value = issue.IsSuppressed, Name = "IsSuppressed" },
                    IsActive = issue.IsActive,
                    Id = issue.Id,
                    VulnerabilityId = [issue.VulnerabilityId],
                    ScanId = issue.ScanId,
                    RemovedDate = new() { Value = issue.RemovedDate, Name = "RemovedDate" },
                    Location = new() { Value = issue.Location, Name = "Location" },
                    LocationFull = new() { Value = issue.LocationFull, Name = "LocationFull" },
                    ReportId = issue.ReportId,
                    Classification = issue.Classification,
                    Description = new() { Value = issue.Description, Name = "Description" },
                    Enumeration = issue.Enumeration,
                    Proof = new() { Value = issue.Proof, Name = "Proof" },
                    Details = new() { Value = issue.Details, Name = "Details" },
                    TestingInstructions = new() { Value = issue.TestingInstructions, Name = "TestingInstructions" },
                    Implication = new() { Value = issue.Implication, Name = "Implication" },
                    Recommendation = new() { Value = issue.Recommendation, Name = "Recommendation" },
                    References = new() { Value = issue.References, Name = "References" },
                    Reference = issue.Reference,
                    Vendor = new() { Value = issue.Vendor, Name = "Vendor" },
                    Product = new() { Value = issue.Product, Name = "Product" },
                    ScannerId = issue.ScannerId,
                    Audited = issue.Audited,
                    Auditor = issue.Auditor,
                    LastAudit = issue.LastAudit,
                    Category = issue.Category,
                    Attributes = issue.Attributes.ToAttributeFields(),
                    Base = issue.Base,
                    Version = issue.Version,
                    Environmental = issue.Environmental,
                    Temporal = issue.Temporal,
                    LockInfo = null,
                    IsHistorical = issue.IsHistorical,
                    Timestamp = issue.Timestamp
                }).ToList(),
                IssueCount = IssueCount,
                Comments = Comments
            };
        }

        private static List<UiAttachmentExport> TransformAttachments(List<UiAttachment> attachments)
        {
            List<UiAttachmentExport> list = [];
            foreach (var attachment in attachments)
            {
                list.Add(new()
                {
                    Id = attachment.Id,
                    Timestamp = attachment.Timestamp,
                    IssueId = attachment.IssueId,
                    Attachment = attachment.Attachment,
                    IsMarkdown = attachment.IsMarkdown,
                    User = attachment.User,
                    UserFullName = attachment.UserFullName
                });
            }
            return list;
        }

        private static List<AssetExport> TransformAssets(List<AssetFull> assets)
        {
            List<AssetExport> list = [];
            foreach (var asset in assets)
            {
                list.Add(new AssetExport
                {
                    Name = asset.Name.Value,
                    Description = asset.Description.Value,
                    AssetId = asset.AssetId,
                    ScanId = asset.ScanId,
                    Timestamp = asset.Timestamp,
                    VersionId = asset.VersionId.Value,
                    Version = asset.Version.Value,
                    Host = asset.Host,
                    Ip = asset.Ip,
                    Scheme = asset.Scheme,
                    Port = asset.Port,
                    IsSaltminerSource = asset.IsSaltminerSource,
                    SourceId = asset.SourceId,
                    IsProduction = asset.IsProduction,
                    IsRetired = asset.IsRetired,
                    LastScanDaysPolicy = asset.LastScanDaysPolicy,
                    InventoryAssetKey = asset.InventoryAssetKey,
                    Attributes = asset.Attributes.ToDictionary()
                });
            }
            return list;
        }

        private static List<IssueExport> TransformIssues(List<IssueFull> issues)
        {
            List<IssueExport> list = [];
            foreach (IssueFull issue in issues)
            {
                list.Add(new IssueExport
                {
                    Name = issue.Name.Value,
                    Severity = issue.Severity.Value,
                    SeverityLevel = issue.SeverityLevel,
                    AssetName = issue.AssetName?.Value,
                    AssetId = issue.AssetId,
                    FoundDate = issue.FoundDate,
                    TestStatus = issue.TestStatus.Value,
                    IsRemoved = issue.IsRemoved,
                    IsSuppressed = issue.IsSuppressed.Value ?? false,
                    IsActive = issue.IsActive,
                    Id = issue.Id,
                    VulnerabilityId = issue.VulnerabilityId?.FirstOrDefault(),
                    ScanId = issue.ScanId,
                    RemovedDate = issue.RemovedDate.Value,
                    Location = issue.Location.Value,
                    LocationFull = issue.LocationFull.Value,
                    ReportId = issue.ReportId,
                    Classification = issue.Classification,
                    Description = issue.Description.Value,
                    Enumeration = issue.Enumeration,
                    Proof = issue.Proof.Value,
                    Details = issue.Details.Value,
                    TestingInstructions = issue.TestingInstructions.Value,
                    Implication = issue.Implication.Value,
                    Recommendation = issue.Recommendation.Value,
                    References = issue.References.Value,
                    Reference = issue.Reference,
                    Vendor = issue.Vendor.Value,
                    Product = issue.Product.Value,
                    ScannerId = issue.ScannerId,
                    Audited = issue.Audited,
                    Auditor = issue.Auditor,
                    LastAudit = issue.LastAudit,
                    Category = issue.Category,
                    Attributes = issue.Attributes.ToDictionary(),
                    Base = issue.Base,
                    Version = issue.Version,
                    Environmental = issue.Environmental,
                    Temporal = issue.Temporal,
                    LockInfo = null,
                    IsHistorical = issue.IsHistorical,
                    Timestamp = issue.Timestamp
                });
            }
            return list;
        }
    }
}
