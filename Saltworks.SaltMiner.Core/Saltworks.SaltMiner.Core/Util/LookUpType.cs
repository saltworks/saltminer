using System;
using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{
    [Serializable]
    public enum LookupType
    {
        [Description("Add Item Dropdown")]
        AddItemDropdown = 0,
        [Description("Severity Dropdown")]
        SeverityDropdown,
        [Description("Tested Dropdown")]
        TestedDropdown,
        [Description("Engagement Type Dropdown")]
        EngagementTypeDropdown,
        [Description("Asset Type Dropdown")]
        AssetTypeDropdown,
        [Description("Engagement Issue Optional Fields")]
        EngagementIssueOptionalFields,
        [Description("Engagement SubType Dropdown")]
        EngagementSubTypeDropdown,
        [Description("Report Template Dropdown")]
        ReportTemplateDropdown
    }
}