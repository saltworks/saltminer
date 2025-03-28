using Microsoft.AspNetCore.Http;

namespace Saltworks.SaltMiner.UiApiClient.Import.CustomImporter
{
    public class CustomIssueImportRequest
    {
        public IFormFile File { get; set; }
        public List<string> RequiredCSVAssetHeaders { get; set; }
        public List<string> RequiredCSVIssueHeaders { get; set; }
        public string FileRepo { get; set; }
        public string UiBaseUrl { get; set; }
        public string Regex { get; set; }
        public string TemplatePath { get; set; }
        public string Instance { get; set; }
        public string SourceType { get; set; }
        public string AssetType { get; set; }
        public string LastScanDaysPolicy { get; set; }
        public string EngagementId { get; set; }
        public string UserName { get; set; }
        public string UserFullName { get; set; }
        public string DefaultQueueAssetId { get; set; } = null;
        public string ImporterId { get; set; }
        public string JSON { get; set; }
        public int ImportBatchSize { get; set; }
    }
}
