using System;
using System.ComponentModel;


namespace Saltworks.SaltMiner.Core.Util
{
    [Serializable]
    public enum ServiceJobType
    {
        [Description("Command")]
        Command = 0,
        [Description("API")]
        API
    }
}
