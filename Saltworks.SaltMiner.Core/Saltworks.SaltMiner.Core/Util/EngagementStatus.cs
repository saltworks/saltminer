using System;
using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{
    [Serializable]
    public enum EngagementStatus
    {
        [Description("Draft")]
        Draft = 1,
        [Description("Queued")]
        Queued,
        [Description("Processing")]
        Processing,
        [Description("Published")]
        Published,
        [Description("Historical")]
        Historical,
        [Description("Error")]
        Error
    }
}
