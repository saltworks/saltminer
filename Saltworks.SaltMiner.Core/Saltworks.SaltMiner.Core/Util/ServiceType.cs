using System;
using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{
    [Serializable]
    public enum ServiceType
    {
        [Description("Manager")]
        Manager = 0,
        [Description("SyncAgent")]
        SyncAgent,
        [Description("DataAPI")]
        DataApi,
        [Description("Reporting")]
        Reporting,
        [Description("JobManager")]
        JobManager,
        [Description("ServiceManager")]
        ServiceManager,
        [Description("UiApi")]
        UiApi
    }
}