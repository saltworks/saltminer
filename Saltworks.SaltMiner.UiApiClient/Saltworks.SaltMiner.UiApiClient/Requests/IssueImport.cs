using Microsoft.AspNetCore.Http;
using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class IssueImport
    {
        public IFormFile File { get; set; }
        public int ImportBatchSize { get; set; }
        public List<string> RequiredCSVAssetHeaders { get; set; }
        public List<string> RequiredCSVIssueHeaders { get; set; }
        public string FileRepo { get; set; }
        public string UiBaseUrl { get; set; }
        public string Regex { get; set; }
        public string FailedRegexSplat { get; set; }
        public string TemplatePath { get; set; }
        public int MaxImportFileSize { get; set; }
        public string Instance { get; set; }
        public string SourceType { get; set; }
        public string AssetType { get; set; }
        public string LastScanDaysPolicy { get; set; }
        public string EngagementId { get; set; }
        public string UserName { get; set; }
        public string UserFullName { get; set; }
        public string DefaultQueueAssetId { get; set; } = null;
        public bool IsTemplate { get; set; } = false;
        public bool FromQueue { get; set; } = false;
        public string InventoryAssetKeyAttribute { get; set; } = string.Empty;
        public List<LookupValue> TestStatusLookups { get; set; } = new();
    }
}
