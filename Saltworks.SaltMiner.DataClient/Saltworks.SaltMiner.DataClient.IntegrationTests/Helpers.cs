using Microsoft.Extensions.DependencyInjection;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    public static class Helpers
    {

        public static DataClient GetDataClient<T>(DataClientOptions options) where T: class
        {
            return CreateDataClientFactory<T>(options).GetClient();
        }

        public static DataClientFactory<T> CreateDataClientFactory<T>(DataClientOptions options) where T: class
        {
            var services = new ServiceCollection();

            services.AddDataClient<T>(c =>
            {
                c.ApiBaseAddress = options.ApiBaseAddress;
                c.ApiKey = options.ApiKey;
                c.ApiKeyHeader = options.ApiKeyHeader;
                c.Timeout = options.Timeout;
                c.VerifySsl = options.VerifySsl;
            });
            var sp = services.BuildServiceProvider();

            sp.UseDataClient<T>();

            return sp.GetRequiredService<DataClientFactory<T>>();
        }

        public static DataClientOptions GetDataClientOptions(Config config)
        {
            return new DataClientOptions
            {
                ApiBaseAddress = config.ApiBaseAddress,
                ApiKey = config.ApiKey,
                ApiKeyHeader = config.ApiKeyHeader,
                Timeout = TimeSpan.FromSeconds(config.TimeoutSec),
                VerifySsl = config.VerifySsl
            };
        }

        public static Config GetConfig(bool admin = false, bool manager = false)
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<Config>(System.IO.File.ReadAllText("settings.json"));
            
            if (manager)
            {
                config.ApiKey = config.ManagerApiKey;
            }

            if (admin)
            {
                config.ApiKey = config.AdminApiKey;
            }

            return config;
        }

        public static SearchRequest SearchRequest(string field, string value, string assetType = null, string sourceType = null, string instance = null)
        {
            return new SearchRequest()
            {
                Filter = new()
                {
                    FilterMatches = new() { { field, value } }
                },
                AssetType = assetType,
                SourceType = sourceType,
                Instance = instance
            };
        }

        public static SearchRequest SearchRequest(Dictionary<string, string> filters, string assetType = null, string sourceType = null, string instance = null)
        {
            return new SearchRequest()
            {
                Filter = new()
                {
                    FilterMatches = filters
                },
                AssetType = assetType,
                SourceType = sourceType,
                Instance = instance
            };
        }

        public static void CleanIndex(DataClient Client, string indexType)
        {
            var assetType = AssetType.Mocked.ToString();
            var sourceType = "DataClient";
            var instance = "UnitTest";

            switch (indexType)
            {
                case "asset":
                    Client.DeleteIndex(Asset.GenerateIndex(assetType, sourceType));
                    break;
                case "scan":
                    Client.DeleteIndex(Scan.GenerateIndex(assetType, sourceType));
                    break;
                case "issue":
                    Client.DeleteIndex(Issue.GenerateIndex(assetType, sourceType));
                    break;
            }

            sourceType = "Mocked";
            switch (indexType)
            {
                case "asset":
                    Client.DeleteIndex(Asset.GenerateIndex(assetType, sourceType));
                    break;
                case "scan":
                    Client.DeleteIndex(Scan.GenerateIndex(assetType, sourceType));
                    break;
                case "issue":
                    Client.DeleteIndex(Issue.GenerateIndex(assetType, sourceType));
                    break;
            }
        }
    }
}
