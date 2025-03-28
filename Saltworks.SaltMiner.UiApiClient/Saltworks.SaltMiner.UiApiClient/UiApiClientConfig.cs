namespace Saltworks.SaltMiner.UiApiClient
{
    public class UiApiClientConfig
    {
        /// <summary>
        /// How many times to retry a failed API call (if failure is a server error)
        /// </summary>
        public int UiApiApiRetryCount { get; set; } = 3;
        /// <summary>
        /// How long (in seconds) to wait between retries in a retry situation
        /// </summary>
        public int UiApiApiDelaySec { get; set; } = 10;
        /// <summary>
        /// Reporting service API key (if applicable)
        /// </summary>
        public string ReportingApiKey { get; set; }
        /// <summary>
        /// Header in which to put the reporting service API key
        /// </summary>
        public string ReportingApiAuthHeader { get; set; }
    }
}
