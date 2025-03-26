using System;
using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{
    [Serializable]
    public enum EngagementLogType
    {
        [Description("Engagement")]
        Engagement = 0,
        [Description("Asset")]
        Asset,
        [Description("Issue")]
        Issue
    }
}