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

﻿using Microsoft.Extensions.DependencyInjection;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.Ui.Api.Models;
using System;

namespace Saltworks.SaltMiner.Ui.IntegrationTests
{
    internal static class Helpers
    {
        #region DataClient

        internal static IServiceProvider GetServicesWithDataClient<T>() where T: class
        {
            var services = new ServiceCollection();
            var config = GetConfig();
            services.AddSingleton(config);
            services.AddSingleton(new FieldInfoCache());
            var options = GetDataClientOptions(config);
            return AddDataClientFactory<T>(services, options);
        }

        internal static DataClient.DataClient GetDataClient(IServiceProvider services)
        {
            return services.GetRequiredService<DataClientFactory<DataClient.DataClient>>().GetClient();
        }

        internal static IServiceProvider AddDataClientFactory<T>(IServiceCollection services, DataClientOptions options) where T : class
        {
            services.AddDataClient<T>(c =>
            {
                c.ApiBaseAddress = options.ApiBaseAddress;
                c.ApiKey = options.ApiKey;
                c.ApiKeyHeader = options.ApiKeyHeader;
                c.Timeout = options.Timeout;
                c.VerifySsl = options.VerifySsl;
                c.RunConfig = new() { DisableInitialConnection = true };
            });
            var sp = services.BuildServiceProvider();
            sp.UseDataClient<T>();
            return sp;
        }

        internal static DataClientOptions GetDataClientOptions(UiApiConfig config)
        {
            return new DataClientOptions
            {
                ApiBaseAddress = config.DataApiBaseUrl,
                ApiKey = config.DataApiKey,
                ApiKeyHeader = config.DataApiKeyHeader,
                Timeout = TimeSpan.FromSeconds(config.DataApiTimeoutSec),
                VerifySsl = config.DataApiVerifySsl
            };
        }

        #endregion

        internal static UiApiConfig GetConfig()
        {
            var configuration = System.Text.Json.JsonSerializer.Deserialize<UiApiConfig>(System.IO.File.ReadAllText("settings.json"));
            return configuration!;
        }
    }
}
