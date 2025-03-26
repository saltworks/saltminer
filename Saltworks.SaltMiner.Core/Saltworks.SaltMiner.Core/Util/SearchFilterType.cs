using System;
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