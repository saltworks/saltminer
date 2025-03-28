using System;

namespace Saltworks.SaltMiner.DataClient
{
    public class DataClientOptions
    {
        // ApiClient pass-through settings
        public string ApiKey { get; set; }
        public string ApiKeyHeader { get; set; } = "Authorization";
        public string ApiBaseAddress { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
        public bool VerifySsl { get; set; } = true;
        public bool LogExtendedErrorInfo { get; set; } = false;
        public bool LogApiCallsAsInfo { get; set; } = false;
        /// <summary>
        /// DataClient specific settings
        /// </summary>
        public DataClientConfig RunConfig { get; set; } = new();
    }
}
