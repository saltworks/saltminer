using System;
using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{
    [Serializable]
    public enum AdminType
    {
        [Description("Configuration Services")]
        Configuration= 0,
        [Description("Look Ups")]
        Lookups,
        [Description("Custom Attribute Definitions")]
        Attributes,
        [Description("Custom Issue Definitions")]
        CustomIssue,
        [Description("Search Filter Definitions")]
        SearchFilters,
        [Description("Service Job Scheduler")]
        ServiceJobs
    }
}