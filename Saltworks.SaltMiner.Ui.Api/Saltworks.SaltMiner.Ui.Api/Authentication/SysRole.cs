using System.ComponentModel;

namespace Saltworks.SaltMiner.Ui.Api.Authentication
{
    [DefaultValue(None)]
    public enum SysRole
    {
        [Description("None")]
        None,
        [Description("pentester-read-only")]
        ReadOnly,
        [Description("superuser")]
        SuperUser, 
        [Description("pentester")]
        Pentester,
        [Description("assetmanager")]
        AssetManager,
        [Description("pentest-admin")]
        PentestAdmin
    }
}