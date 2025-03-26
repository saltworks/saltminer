namespace Saltworks.SaltMiner.Core.Entities
{
    /// <summary>
    /// Represent a scan performed against a single application/version.
    /// </summary>
    public class Scan : SaltMinerEntity
    {
        private static string _indexEntity = "scans";

        public static string GenerateIndex(string assetType = null, string sourceType = null, string instance = null)
        {
            return GenerateHydratedIndex(_indexEntity, assetType, sourceType, instance);
        }

        /// <summary>
        /// Gets or sets Saltminer for this scan.  See the object for more details.
        /// </summary>
        /// <seealso cref="SaltMinerScanInfo"/>
        /// <remarks>Spelling is intentional, do not "fix"</remarks>
        public SaltMinerScanInfo Saltminer { get; set; } = new();
    }

    public class SaltMinerScanInfo
    {
        /// <summary>
        /// Gets or sets Scan for this scan.  See the object for more details.
        /// </summary>
        /// <seealso cref="ScanInfo"/>
        public ScanInfo Scan { get; set; } = new();

        /// <summary>
        /// Gets or sets Asset for this scan.  See the object for more details.
        /// </summary>
        /// <seealso cref="AssetIdInfo"/>
        public AssetIdInfo Asset { get; set; } = new();

        /// <summary>
        /// Gets or sets Engagement for this scan.  See the object for more details.
        /// </summary>
        /// <seealso cref="EngagementInfo"/>
        public EngagementInfo Engagement { get; set; } = new();

        /// <summary>
        /// Gets or sets InventoryAssetInfo (just key really) for this asset.  See the object for more details.
        /// </summary>
        /// <seealso cref="InventoryAssetKeyInfo"/>
        public InventoryAssetKeyInfo InventoryAsset { get; set; } = new();
    }
}