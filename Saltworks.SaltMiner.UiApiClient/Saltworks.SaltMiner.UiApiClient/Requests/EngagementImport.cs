using Microsoft.AspNetCore.Http;
using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    // formerly EngagementImportRequest
    public class EngagementImport : UiModelBase
    {
        public IFormFile File { get; set; }
        public int MaxImportFileSize { get; set; }
        public string FileRepo { get; set; }
        public string ApiBaseUrl { get; set; }
        public string AssetType { get; set; }
        public string SourceType { get; set; }
        public string Instance { get; set; }
        public string UiBaseUrl { get; set; }
        public int ImportBatchSize { get; set; }
        public string UserName { get; set; }
        public string UserFullName { get; set; }
        public bool CreateNew { get; set; } = false;
        public bool FromQueue { get; set; } = false;
        public string InventoryAssetKeyAttribute { get; set; } = string.Empty;
        public List<LookupValue> TestStatusLookups { get; set; } = [];
    }
}
