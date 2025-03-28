using Saltworks.Utility.ApiHelper;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Saltworks.SaltMiner.DataClient
{
    public class DataClientFactory<T>(ApiClientFactory<T> factory, ILogger<DataClient> logger, DataClientConfig config) where T : class
    {
        private readonly ApiClientFactory<T> Factory = factory ?? throw new DataClientInitializationException("Error instantiating data client - underlying ApiClient factory is null.  Check startup.");
        private readonly ILogger Logger = logger;
        private readonly DataClientConfig RunConfig = config;

        public DataClient GetClient() => new(Factory.CreateApiClient(), Logger, RunConfig);
        public static DataClient GetClient(IServiceProvider services) => services.GetService<DataClientFactory<T>>().GetClient();
    }
}
