using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    // formerly EngagementScanDto
    public class ScanFull : UiModelBase
    {
        public string AppVersion { get; set; }

        public string EngagementId { get; set; }

        public string EngagementName { get; set; }

        public string EngagementSubtype { get; set; }

        public Dictionary<string, string> EngagementAttributes { get; set; }

        public string EngagementCustomer { get; set; }

        public DateTime? EngagementPublishDate { get; set; }

        public string ReportId { get; set; }

        public string ProductType { get; set; }

        public string ScanId { get; set; }

        public DateTime ScanDate { get; set; }

        public string Status { get; set; }

        public string Product { get; set; }

        public string Vendor { get; set; }

        public DateTime Timestamp { get; set; }

        public ScanFull()
        {
        }

        public ScanFull(QueueScan queueScan, string appVersion)
        {
            AppVersion = appVersion;
            ReportId = queueScan.Saltminer.Scan.ReportId;
            ProductType = queueScan.Saltminer.Scan.ProductType;
            ScanId = queueScan.Id;
            ScanDate = queueScan.Saltminer.Scan.ScanDate;
            Status = queueScan.Saltminer.Internal.QueueStatus;
            EngagementId = queueScan.Saltminer.Engagement.Id;
            EngagementName = queueScan.Saltminer.Engagement.Name;
            EngagementSubtype = queueScan.Saltminer.Engagement.Subtype;
            EngagementSubtype = queueScan.Saltminer.Engagement.Subtype;
            EngagementAttributes = queueScan.Saltminer.Engagement.Attributes;
            EngagementCustomer = queueScan.Saltminer.Engagement.Customer;
            EngagementPublishDate = queueScan.Saltminer.Engagement.PublishDate;
            Product = queueScan.Saltminer.Scan.Product;
            Vendor = queueScan.Saltminer.Scan.Vendor;
            Timestamp = queueScan.Timestamp;
        }

        public ScanFull(Scan scan, string appVersion)
        {
            AppVersion = appVersion;
            ReportId = scan.Saltminer.Scan.ReportId;
            ProductType = scan.Saltminer.Scan.ProductType;
            ScanId = scan.Id;
            ScanDate = scan.Saltminer.Scan.ScanDate;
            EngagementId = scan.Saltminer.Engagement.Id;
            EngagementName = scan.Saltminer.Engagement.Name;
            EngagementSubtype = scan.Saltminer.Engagement.Subtype;
            EngagementAttributes = scan.Saltminer.Engagement.Attributes;
            EngagementCustomer = scan.Saltminer.Engagement.Customer;
            EngagementPublishDate = scan.Saltminer.Engagement.PublishDate;
            Product = scan.Saltminer.Scan.Product;
            Vendor = scan.Saltminer.Scan.Vendor;
            Timestamp = scan.Timestamp;
        }
    }
}
