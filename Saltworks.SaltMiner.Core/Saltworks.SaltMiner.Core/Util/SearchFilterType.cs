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
    public enum SearchFilterType
    {
        [Description("Engagement Search Filters")]
        EngagementSearchFilters = 0,
        [Description("Engagement Sort Filters")]
        EngagementSortFilters,
        [Description("Issue Search Filters")]
        IssueSearchFilters,
        [Description("Issue Sort Filters")]
        IssueSortFilters,
        [Description("Asset Sort Filters")]
        AssetSortFilters,
        [Description("Comment Sort Filters")]
        CommentSortFilters,
        [Description("Reporting Queue Sort Filters")]
        ReportingQueueSortFilters,
        [Description("Inventory Asset Search Filters")]
        InventoryAssetSearchFilters,
        [Description("Inventory Asset Sort Filters")]
        InventoryAssetSortFilters,
        [Description("Service Job Search Filters")]
        ServiceJobSearchFilters,
        [Description("Service Job Sort Filters")]
        ServiceJobSortFilters,
        [Description("Role Search Filters")]
        RoleSearchFilters,
        [Description("Role Sort Filters")]
        RoleSortFilters
    }
}