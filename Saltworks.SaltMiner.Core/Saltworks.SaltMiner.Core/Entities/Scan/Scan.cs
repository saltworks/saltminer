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