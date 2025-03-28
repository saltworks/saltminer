using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class ScanNew : UiModelBase
    {
        [Required]
        public string EngagementId { get; set; }

        [Required]
        public string ReportId { get; set; }

        public string ProductType { get; set; }

        [Required]
        public DateTime ScanDate { get; set; }

        public string Status { get; set; }

        [Required]
        public string Product { get; set; }

        [Required]
        public string Vendor { get; set; }

        public QueueScan CreateNewQueueScan(string sourceType, string assetType, string instance, string engaegmentName, string engagementSubtype, string engagementCustomer)
        {
            return new QueueScan
            {
                Timestamp = DateTime.UtcNow,
                Saltminer = new SaltMinerQueueScanInfo
                {
                    Engagement = new EngagementInfo
                    {
                        Id = EngagementId,
                        Name = engaegmentName,
                        Subtype = engagementSubtype,
                        Attributes = null,
                        Customer = engagementCustomer,
                        PublishDate = null
                    },
                    Internal = new QueueScanInternal
                    {
                        IssueCount = -1,
                        QueueStatus = Status
                    },
                    Scan = new QueueScanInfo
                    {
                        ReportId = ReportId,
                        AssessmentType = AssessmentType.Pen.ToString(),
                        ProductType = ProductType,
                        ScanDate = ScanDate,
                        AssetType = assetType,
                        SourceType = sourceType,
                        Product = Product,
                        Vendor = Vendor,
                        Instance = instance,
                        IsSaltminerSource = true
                    }
                }
            };
        }
    }
}
