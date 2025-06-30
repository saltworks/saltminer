/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
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