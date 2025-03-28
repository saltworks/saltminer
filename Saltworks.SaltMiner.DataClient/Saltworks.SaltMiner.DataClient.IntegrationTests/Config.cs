namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    public class Config
    {
        public string ApiBaseAddress { get; set; }
        public string ApiKey { get; set; }
        public string AgentApiKey { get; set; }
        public string ManagerApiKey { get; set; }
        public string AdminApiKey { get; set; }
        public string ApiKeyHeader { get; set; }
        public int TimeoutSec { get; set; }
        public bool VerifySsl { get; set; }
    }
}
