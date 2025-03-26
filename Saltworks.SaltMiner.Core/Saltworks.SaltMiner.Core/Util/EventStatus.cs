using System;
using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{
    [Serializable]
    public enum EventStatus
    {
        [Description("In Progress")]
        InProgress = 1,
        [Description("Complete")]
        Complete,
        [Description("Error")]
        Error
    }
}
