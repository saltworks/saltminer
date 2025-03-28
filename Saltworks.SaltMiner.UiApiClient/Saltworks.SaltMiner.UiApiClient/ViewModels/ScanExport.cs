using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class ScanExport : UiModelBase
    {
        public string ReportId { get; set; }

        public string ProductType { get; set; }

        public string ScanId { get; set; }

        public DateTime ScanDate { get; set; }

        public string Status { get; set; }

        public string Product { get; set; }

        public string Vendor { get; set; }

        public DateTime Timestamp { get; set; }

        public ScanExport()
        {
        }

        public ScanExport(ScanFull scan)
        {
            ReportId = scan.ReportId;
            ProductType = scan.ProductType;
            ScanId = scan.ScanId;
            ScanDate = scan.ScanDate;
            Product = scan.Product;
            Vendor = scan.Vendor;
            Timestamp = scan.Timestamp;
            Status = scan.Status;
        }

        public ScanFull ToScanFull() => new()
        {
            ReportId = ReportId,
            ProductType = ProductType,
            ScanId = ScanId,
            ScanDate = ScanDate,
            Product = Product,
            Vendor = Vendor,
            Timestamp = Timestamp,
            Status = Status
        };
    }
}
