/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
*
* ----
*/

﻿namespace Saltworks.SaltMiner.Core.Entities
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
