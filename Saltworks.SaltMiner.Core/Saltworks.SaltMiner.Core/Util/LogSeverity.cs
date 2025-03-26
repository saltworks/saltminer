using System;
using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{
    [Serializable]
    public enum LogSeverity
    {
        [Description("Debug")]
        Debug = 0,
        [Description("Information")]
        Information,
        [Description("Warning")]
        Warning,
        [Description("Error")]
        Error,
        [Description("Critical")]
        Critical
    }
}
