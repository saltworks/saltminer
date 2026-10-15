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

﻿using System;
using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{
    [Serializable]
    public enum AttributeDefinitionType
    {
        [Description("Engagement Attribute")]
        Engagement = 0,
        [Description("Issue Attribute")]
        Issue,
        [Description("Asset Attribute")]
        Asset,
        [Description("Scan Attribute")]
        Scan,
        [Description("Inventory Asset Attribute")]
        InventoryAsset,
        [Description("Snapshot Attribute")]
        Snapshot
    }
}