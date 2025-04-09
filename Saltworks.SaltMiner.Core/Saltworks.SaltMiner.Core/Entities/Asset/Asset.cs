/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-04-09
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿namespace Saltworks.SaltMiner.Core.Entities
{
    /// <summary>
    /// Represents a single asset. To be recognized by SaltMiner, and asset must have at least
    /// one scan from any source completed. Not all scanners will version assets. In those cases, only the asset name
    /// will be present, the version id and version name will not be.
    /// </summary>
    public class Asset : SaltMinerEntity
    {
        private static string _indexEntity = "assets";

        public static string GenerateIndex(string assetType = null, string sourceType = null, string instance = null)
        {
            return GenerateHydratedIndex(_indexEntity, assetType, sourceType, instance);
        }

        /// <summary>
        /// Gets or sets Saltminer for this asset.  See the object for more details.
        /// </summary>
        /// <seealso cref="SaltMinerAssetInfo"/>
        /// <remarks>Spelling is intentional, do not "fix"</remarks>
        public SaltMinerAssetInfo Saltminer { get; set; } = new();
    }
    public class SaltMinerAssetInfo
    {
        /// <summary>
        /// Gets or sets CompositeKey for this asset.  Composite key used internally by SaltMiner
        /// </summary>
        public string CompositeKey => Engagement?.Id != null ? $"{Asset.SourceType}_{Asset.SourceId}_{Asset.AssetType}_{Engagement.Id}" : $"{Asset.SourceType}_{Asset.SourceId}_{Asset.AssetType}";

        /// <summary>
        /// Gets or sets InventoryAssetInfo (just key really) for this asset.  See the object for more details.
        /// </summary>
        /// <seealso cref="InventoryAssetKeyInfo"/>
        public InventoryAssetKeyInfo InventoryAsset { get; set; } = new();

        /// <summary>
        /// Gets or sets Asset for this asset.  See the object for more details.
        /// </summary>
        /// <seealso cref="AssetInfoPolicy"/>
        public AssetInfoPolicy Asset { get; set; } = new();

        /// <summary>
        /// Gets or sets Engagement for this asset.  See the object for more details.
        /// </summary>
        /// <seealso cref="EngagementInfo"/>
        public EngagementInfo Engagement { get; set; } = new();
    }
}