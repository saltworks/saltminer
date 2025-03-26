using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{

    public enum SourceType
    {
        [Description("Saltworks.CheckmarxSast")]
        CheckmarxSast = 1,
        [Description("Saltworks.Sonatype")]
        Sonatype,
        [Description("Saltworks.MendSca")]
        MendSca,
        [Description("Saltworks.WhiteSource")]
        WhiteSource,
        [Description("Saltworks.SSC")]
        SSC,
        [Description("Saltworks.BlackDuck")]
        BlackDuck,
        [Description("Saltworks.Qualys")]
        Qualys,
        [Description("Saltworks.WebInspect")]
        WebInspect,
        [Description("Saltworks.Burp")]
        Burp,
        [Description("Saltworks.SonarQube")]
        SonarQube,
        [Description("Saltworks.Twistlock")]
        Twistlock,
        [Description("Saltworks.FOD")]
        FOD,
        [Description("Saltworks.Contrast")]
        Contrast,
        [Description("Saltworks.PenTest")]
        Pentest,
        [Description("Saltworks.Snyk")]
        Snyk,
        [Description("Saltworks.Debricked")]
        Debricked,
        [Description("Saltworks.Wiz")]
        Wiz,
        [Description("Saltworks.CheckmarxOne")]
        CheckmarxOne,
        [Description("Saltworks.Polaris")]
        Polaris,
        [Description("Saltworks.GHAS")]
        GHAS,
        [Description("Saltworks.GitLab")]
        GitLab,
        [Description("Saltworks.Oligo")]
        Oligo,
        [Description("Saltworks.Dynatrace")]
        Dynatrace,
        [Description("Saltworks.NowSecure")]
        NowSecure,
        [Description("Saltworks.Mobb")]
        Mobb,
        [Description("Saltworks.Traceable")]
        Traceable
    }
}
