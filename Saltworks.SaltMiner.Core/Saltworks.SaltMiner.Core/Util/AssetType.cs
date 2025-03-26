using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{
    public enum AssetType
    {
        [Description("net")]
        Net = 1,
        [Description("app")]
        App,
        [Description("ctr")]
        Ctr,
        [Description("mocked")]
        Mocked,
        [Description("pen")]
        Pen
    }
}
