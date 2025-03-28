namespace Saltworks.SaltMiner.UiApiClient
{
    public class UiApiClientOptions
    {
        public string UiApiBaseAddress { get; set; }
        public bool UiApiVerifySsl { get; set; } = true;
        public TimeSpan UiApiTimeout { get; set; }
        public UiApiClientConfig RunConfig { get; set; } = new();
    }
}
