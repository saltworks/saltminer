using System;
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