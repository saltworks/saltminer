using System;
using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{
    [Serializable]
    public enum EngagementIssueStatus
    {
        [Description("Not Tested")]
        NotTested = 1,
        [Description("Found")]
        Found = 2,
        [Description("Not Found")]
        NotFound = 3,
        [Description("Out of Scope")]
        OutOfScope = 4,
        [Description("Tested")]
        Tested = 5
    }
}
