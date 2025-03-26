namespace Saltworks.SaltMiner.Core.Entities
{
    /// <summary>
    /// Represents a scan queued to be processed by the SaltMiner "Manager"
    /// </summary>
    public class QueueAsset : SaltMinerEntity
    {
        private static string _indexEntity = "queue_assets";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        /// <summary>
        /// Gets or sets Saltminer for this queue issue.  See the object for more details.
        /// </summary>
        /// <seealso cref="SaltMinerQueueAssetInfo"/>
        /// <remarks>Spelling is intentional, do not "fix"</remarks>
        public SaltMinerQueueAssetInfo Saltminer { get; set; } = new();
    }

    public class SaltMinerQueueAssetInfo
    {
        /// <summary>
        /// Gets CompositeKey. This is a unqiue identifer to each asset record. EngagementId, SourceType, SoruceId, and AssetType.
        /// </summary>
        public string CompositeKey => Engagement?.Id != null ? $"{Asset.SourceType}_{Asset.SourceId}_{Asset.AssetType}_{Engagement.Id}" : $"{Asset.SourceType}_{Asset.SourceId}_{Asset.AssetType}";

        /// <summary>
        /// Gets or sets Asset.
        /// </summary>
        /// <seealso cref="AssetInfoPolicy"/>
        public AssetInfoPolicy Asset { get; set; } = new();

        /// <summary>
        /// Gets or sets Internal.
        /// </summary>
        /// <seealso cref="QueueAssetInternal"/>
        public QueueAssetInternal Internal { get; set; } = new();

        /// <summary>
        /// Gets or sets AssetInv.
        /// </summary>
        /// <seealso cref="InventoryAssetKeyInfo"/>
        public InventoryAssetKeyInfo InventoryAsset { get; set; } = new();

        /// <summary>
        /// Gets or sets Engagement.
        /// </summary>
        /// <seealso cref="EngagementInfo"/>
        public EngagementInfo Engagement { get; set; } = new();
    }
}
