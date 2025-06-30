/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.SaltMiner.SourceAdapters.Core.Helpers;
using Saltworks.Utility.ApiHelper;

namespace Saltworks.SaltMiner.SourceAdapters.IntegrationTests
{
    public static class Helpers
    {
        #region ApiClient

        public static ApiClient GetApiClient<T>(ApiClientOptions options) where T : class =>
            CreateApiClientFactory<T>(options).CreateApiClient();

        public static ApiClientFactory<T> CreateApiClientFactory<T>(ApiClientOptions options) where T : class
        {
            var services = new ServiceCollection();
            services.AddApiClient<T>(c =>
            {
                c.BaseAddress = options.BaseAddress;
                c.Timeout = options.Timeout;
                c.VerifySsl = options.VerifySsl;
                c.DefaultHeaders.Add(options.DefaultHeaders);
            });

            var sp = services.BuildServiceProvider();
            sp.UseApiClient<T>();

            return sp.GetRequiredService<ApiClientFactory<T>>();
        }

        public static ApiClientOptions GetApiClientOptions(Config config, string username="", string password="")
        {
            var options = new ApiClientOptions
            {
                BaseAddress = config.ApiBaseAddress,
                Timeout = TimeSpan.FromSeconds(config.ApiTimeoutSec),
                VerifySsl = config.ApiVerifySsl
            };

            if (!string.IsNullOrEmpty(username))
            {
                var hdrs = ApiClientHeaders.AuthorizationBasicHeader(username, password);

                foreach (var header in config.DefaultHeaders)
                {
                    hdrs.Add(header.Key, header.Value);
                }

                options.DefaultHeaders.Add(hdrs);
            }

            return options;
        }

        #endregion

        #region DataClient

        public static DataClient.DataClient GetDataClient<T>(DataClientOptions options) where T : class
        {
            return CreateDataClientFactory<T>(options).GetClient();
        }

        public static DataClientFactory<T> CreateDataClientFactory<T>(DataClientOptions options) where T : class
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
                Timeout = TimeSpan.FromSeconds(config.ApiTimeoutSec),
                VerifySsl = config.ApiVerifySsl
            };
        }

        #endregion

        public static IServiceProvider GetLocalDataServiceProvider(Config config)
        {
            var clientFactory = CreateApiClientFactory<SourceAdapter>(GetApiClientOptions(config));
            var dataClientFactory = CreateDataClientFactory<DataClient.DataClient>(GetDataClientOptions(config));
            return new ServiceCollection()
                .AddSqliteLocalData()
                .AddTransient<ILogger<SqliteLocalDataRepository>, TestLogger<SqliteLocalDataRepository>>()
                .AddSingleton(clientFactory)
                .AddSingleton(dataClientFactory)
                .AddLogging()
                .BuildServiceProvider();
        }

        public static Config GetConfig()
        {
            var configuration = System.Text.Json.JsonSerializer.Deserialize<Config>(System.IO.File.ReadAllText("settings.json"));

            return configuration;
        }
    }
}
