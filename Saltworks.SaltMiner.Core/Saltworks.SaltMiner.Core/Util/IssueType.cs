using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{
    public enum IssueType
    {
        [Description("SAST")]
        SAST = 1,
        [Description("DAST")]
        DAST,
        [Description("Open")]
        Open,
        [Description("Secrets")]
        Secrets,
        [Description("IAC")]
        IAC,
        [Description("KICS")]
        KICS,
        [Description("Net")]
        Net,
        [Description("Container")]
        Container,
        [Description("Pen")]
        Pen
    }
}
