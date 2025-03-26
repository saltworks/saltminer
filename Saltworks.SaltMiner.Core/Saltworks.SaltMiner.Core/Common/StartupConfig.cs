namespace Saltworks.SaltMiner.Core.Common
{
    public abstract class StartupConfig
    {
        public string Id { get; set;}
        public string Type  { get; set; }
        public string Name { get; set; }
        public string DataApiBaseUrl { get; set; }
        public bool DataApiVerifySsl { get; set; } = true;
        public int DataApiTimeoutSec { get; set; } = 10;
    }
}
