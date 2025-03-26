namespace Saltworks.SaltMiner.Core.Entities
{
    /// <summary>
    /// Key wrapper used to maintain correct index mapping position for the inventory asset key in the Asset and QueueAsset entities.  Yes we meant to only have one property here.
    /// </summary>
    /// <seealso cref="InventoryAsset"/>
    public class InventoryAssetKeyInfo
    {
        /// <summary>
        /// Gets or sets Key for this Inventory Asset. Universal Asset identifier (i.e. from CMDB or other official app DB)
        /// </summary>
        public string Key { get; set; }
    }
}