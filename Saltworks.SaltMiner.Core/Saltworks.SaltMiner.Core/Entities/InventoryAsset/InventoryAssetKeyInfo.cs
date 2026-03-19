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