using System;
using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{
    [Serializable]
    public enum EventReference
    {
        [Description("Engagement")]
        Engagement = 0,
        [Description("queue_scan")]
        QueueScan,
        [Description("queue_issue")]
        QueueIssue,
        [Description("queue_asset")]
        QueueAsset,
        [Description("scan")]
        Scan,
        [Description("issue")]
        Issue,
        [Description("asset")]
        Asset,

    }
}