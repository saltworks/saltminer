/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
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
