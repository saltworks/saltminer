using Saltworks.SaltMiner.UiApiClient.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class AssetExport : UiModelBase
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public string AssetId { get; set; }

        [Required]
        public string ScanId { get; set; }

        [DateValidation]
        public DateTime Timestamp { get; set; }

        public string VersionId { get; set; }

        public string Version { get; set; }

        public string Host { get; set; }

        public string Ip { get; set; }

        public string Scheme { get; set; }

        public int Port { get; set; }

        public bool IsSaltminerSource { get; set; }

        [Required]
        public string SourceId { get; set; }

        public bool IsProduction { get; set; }

        public bool IsRetired { get; set; }

        [Required]
        public string LastScanDaysPolicy { get; set; }

        public string InventoryAssetKey { get; set; }

        [AttributesValidation]
        public Dictionary<string, string> Attributes { get; set; }
    }
}
