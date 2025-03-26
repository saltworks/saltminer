using Saltworks.SaltMiner.Core.Common;

namespace Saltworks.SaltMiner.Core.UnitTests.Helpers
{
    public class Config : ConfigBase
    {

        public string ThisIsSecret { get; set; }
        public string OkToRead { get; set; } 
        public int IntPassword { get; set; }

        public void Decrypt() => DecryptProperties(this);
        public void CheckEncryption() => CheckEncryption(this, "settings.json", "SomeConfig");
        public new string RewriteConfigNode(string fileContents, string node, string json) => base.RewriteConfigNode(fileContents, node, json);
    }
}
