using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.UiApiClient.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    // Formerly multiple
    public class IssueEdit : UiModelBase
    {
        public string EngagementId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        [SeverityValidation]
        public string Severity { get; set; }
        [Required]
        public string AssetId { get; set; }
        [Required]
        [DateValidation]
        public DateTime? FoundDate { get; set; }
        [TestStatusValidation]
        public string TestStatus { get; set; }
        public string Status { get; set; }
        public bool IsSuppressed { get; set; }
        public string Id { get; set; }
        public string SourceSeverity { get; set; }
        public DateTime? RemovedDate { get; set; }
        public string Location { get; set; }
        public string LocationFull { get; set; }
        public string Classification { get; set; }
        public string Description { get; set; }
        public string Enumeration { get; set; }
        [Markdown]
        public string Proof { get; set; }
        [Markdown]
        public string Details { get; set; }
        [Markdown]
        public string Implication { get; set; }
        [Markdown]
        public string TestingInstructions { get; set; }
        [Markdown]
        public string Recommendation { get; set; }
        [Markdown]
        public string References { get; set; }
        public string Reference { get; set; }
        public string Vendor { get; set; }
        public string Product { get; set; }
        [AttributesValidation]
        public Dictionary<string, string> Attributes { get; set; }

        public override void IsModelValid(string regex, string splatReplace = null, bool replaceMarkdown = false, List<LookupValue> subtypeOptions = null, List<AttributeDefinitionValue> attributeOptions = null, List<LookupValue> testedOptions = null, bool leaveOffFieldinError = false)
        {
            base.IsModelValid(regex, splatReplace, replaceMarkdown, subtypeOptions, attributeOptions, testedOptions, leaveOffFieldinError);
        }
        public QueueIssue UpdateQueueIssue(QueueIssue queueIssue)
        {
            queueIssue.Saltminer.Attributes = Attributes;
            queueIssue.Saltminer.QueueAssetId = AssetId;
            queueIssue.Vulnerability.FoundDate = FoundDate;
            queueIssue.Vulnerability.Severity = Severity;
            queueIssue.Vulnerability.Name = Name;
            queueIssue.Vulnerability.TestStatus = TestStatus;
            queueIssue.Vulnerability.IsSuppressed = IsSuppressed;
            queueIssue.Vulnerability.RemovedDate = RemovedDate;
            queueIssue.Vulnerability.Classification = Classification;
            queueIssue.Vulnerability.Description = Description;
            queueIssue.Vulnerability.Enumeration = Enumeration;
            queueIssue.Vulnerability.Location = string.IsNullOrEmpty(Location) ? " " : Location;
            queueIssue.Vulnerability.LocationFull = string.IsNullOrEmpty(LocationFull) ? " " : LocationFull;
            queueIssue.Vulnerability.Reference = Reference;
            queueIssue.Vulnerability.References = References;
            queueIssue.Vulnerability.Details = Details;
            queueIssue.Vulnerability.Implication = Implication;
            queueIssue.Vulnerability.TestingInstructions = TestingInstructions;
            queueIssue.Vulnerability.Recommendation = Recommendation;
            queueIssue.Vulnerability.Proof = Proof;
            queueIssue.Vulnerability.SourceSeverity = SourceSeverity;
            queueIssue.Vulnerability.Scanner.Product = string.IsNullOrEmpty(Product) ? " " : Product;
            queueIssue.Vulnerability.Scanner.Vendor = string.IsNullOrEmpty(Vendor) ? " " : Vendor;
            queueIssue.Vulnerability.Scanner.AssessmentType = AssessmentType.Pen.ToString();
            queueIssue.Vulnerability.ReportId = queueIssue.Saltminer.Engagement.Id ?? queueIssue.Vulnerability.ReportId;
            queueIssue.Saltminer.Engagement.Id = string.IsNullOrEmpty(EngagementId) ? null : EngagementId;

            return queueIssue;
        }

        public QueueIssue CreateNewIssue(SaltMinerEngagementInfo engagement, string scanId, string uiBaseUrl)
        {
            var scannerId = Guid.NewGuid().ToString();

            return new QueueIssue
            {
                Id = Guid.NewGuid().ToString(),
                Saltminer = new()
                {
                    QueueScanId = scanId,
                    QueueAssetId = AssetId,
                    Engagement = new EngagementInfo
                    {
                        Id = EngagementId,
                        Name = engagement.Name,
                        Attributes = engagement.Attributes,
                        PublishDate = engagement.PublishDate,
                        Customer = engagement.Customer,
                        Subtype = engagement.Subtype
                    },
                    Attributes = Attributes,
                },
                Vulnerability = new()
                {
                    FoundDate = FoundDate,
                    Severity = Severity,
                    Name = Name,
                    TestStatus = TestStatus,
                    Category = [ "Application" ],
                    Audit = null,
                    Classification = Classification,
                    Description = Description,
                    Enumeration = Enumeration,
                    IsFiltered = false,
                    IsSuppressed = false,
                    Location = string.IsNullOrEmpty(Location) ? " " : Location,
                    LocationFull = string.IsNullOrEmpty(LocationFull) ? " " : LocationFull,
                    Reference = Reference,
                    References = References,
                    RemovedDate = RemovedDate,
                    Details = Details,
                    Implication = Implication,
                    TestingInstructions = TestingInstructions,
                    Recommendation = Recommendation,
                    Proof = Proof,
                    SourceSeverity = SourceSeverity,
                    Scanner = new()
                    {
                        Id = scannerId,
                        GuiUrl = $"{uiBaseUrl}/engagements/{EngagementId}/scanner/{scannerId}",
                        Product = string.IsNullOrEmpty(Product) ? " " : Product,
                        Vendor = string.IsNullOrEmpty(Vendor) ? " " : Vendor,
                        AssessmentType = AssessmentType.Pen.ToString()
                    },
                    ReportId = EngagementId,
                    Score = new()
                    {
                        Version = null,
                        Base = 0,
                        Environmental = 0,
                        Temporal = 0
                    }
                },
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
