namespace Saltworks.SaltMiner.DataClient
{
    public class DataClientConfig
    {
        /// <summary>
        /// How many times to retry a failed API call (if failure is a server error)
        /// </summary>
        public int ApiClientRetryCount { get; set; } = 3;
        /// <summary>
        /// How long (in seconds) to wait between retries in a retry situation
        /// </summary>
        public int ApiClientRetryDelaySec { get; set; } = 10;
        /// <summary>
        /// Disables automatic initial connection attempt by the DataClient. Defaults to false.
        /// </summary>
        /// <remarks>Useful for debug/tests, as you can take actions on objects before a DataClient connection is attempted.</remarks>
        public bool DisableInitialConnection { get; set; } = false;
    }
}
