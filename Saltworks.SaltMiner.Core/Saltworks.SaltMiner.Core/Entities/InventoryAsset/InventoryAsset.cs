using System.Collections.Generic;

namespace Saltworks.SaltMiner.Core.Entities
{
    /// <summary>
    /// This object represents an inventory asset. An inventory asset can have issues reported from a variety
    /// of different scan sources. Each source with its own findings. Inventory assets may have
    /// one or more custom attribute(s) that are not source-specific.
    /// The Saltminer.InventoryAsset.Key property is the Asset Tag, CMDB ID, UAID, etc. of a specific asset.
    /// All instances of asset with the same InventoryKey value are grouped together and the
    /// last scan of each source is referenced by this object. 
    /// </summary>
    public class InventoryAsset : SaltMinerEntity
    {
        private static string _indexEntity = "inventory_assets";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        /// <summary>
        /// Gets or sets Key for this Inventory Asset.  Used to match assets when applying inventory asset attributes to other entities.
        /// </summary>
        /// <remarks>Enrichment updates other indices with this inv asset's attributes.</remarks>
        public string Key { get; set; }
        /// <summary>
        /// Gets or sets IsProduction for this Inventory Asset. Production flag used to determine if an app/ver is prod or non-prod
        /// </summary>
        public bool IsProduction { get; set; }

        /// <summary>
        /// Gets or sets Description for this Inventory Asset. Asset description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets Version for this Inventory Asset. Application version name
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets Attributes for this Inventory Asset. Custom attributes that apply to APP/VERSION used in visualizations and so on
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> Attributes { get; set; } = new();

        /// <summary>
        /// Gets or sets Name for this Inventory Asset. Asset (or application) name
        /// </summary>
        public string Name { get; set; }
    }
}