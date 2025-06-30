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
using Saltworks.SaltMiner.ConsoleApp.Core;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataClient;

namespace Saltworks.SaltMiner.Manager.IntegrationTests
{
    public static class Helpers
    {
        private const string ManagerSettingsFile = "ManagerSettings.json";
        private const string MANAGER_SETTINGS_APP_SECTION = "ManagerConfig";
        private const string MANAGER_SETTINGS_LOG_SECTION = "LogConfig";

        public static void RunManager(IConsoleAppHostArgs args)
        {
            ILogger startLogger = null;
            try
            {
                ConsoleAppHostBuilder.CreateDefaultConsoleAppHost<Manager>
                (
                    (services, config) =>
                    {
                        try
                        {
                            // Configuration available here if needed when adding DI services
                            var managerConfig = new ManagerConfig(config, ManagerSettingsFile);

                            services.AddSingleton(managerConfig);
                            services.AddTransient<QueueProcessor>();
                            services.AddTransient<SnapshotProcessor>();
                            services.AddTransient<CleanUpProcessor>();
                            services.AddDataClient<Manager>
                            (
                                options =>
                                {
                                    options.ApiBaseAddress = managerConfig.DataApiBaseUrl;
                                    options.ApiKeyHeader = managerConfig.DataApiKeyHeader;
                                    options.ApiKey = managerConfig.DataApiKey;
                                    options.Timeout = TimeSpan.FromSeconds(managerConfig.DataApiTimeoutSec);
                                    options.VerifySsl = managerConfig.DataApiVerifySsl;
                                }
                            );
                        }
                        catch (Exception ex)
                        {
                            throw new InitializationException($"Error in service configuration: {ex.Message}", ex);
                        }
                    },
                    configure =>
                    {
                        var logger = configure.GetRequiredService<ILogger<Manager>>();
                        startLogger = logger;
                        
                        try
                        {
                            configure.UseDataClient<Manager>();
                        }
                        catch (Exception ex)
                        {
                            var msg = $"Error in service initialization: {ex.Message}";
                            logger.LogCritical(ex, msg);
                            throw new InitializationException(msg, ex);
                        }
                    },
                    ManagerSettingsFile,
                    MANAGER_SETTINGS_APP_SECTION,
                    MANAGER_SETTINGS_LOG_SECTION
                ).Run(args);
            }
            catch (Exception ex)
            {
                startLogger.LogCritical(ex, "Manager initialization error: {message}", ex.Message);
            }
        }

        public static void CleanIndex(DataClient.DataClient Client, string indexType)
        {
            var assetType = "Test";
            var sourceType = "Manager";
            var configName = "UnitTest";
            
            switch (indexType)
            {
                case "asset":
                    Client.DeleteIndex(Asset.GenerateIndex(assetType, sourceType, configName));
                    break;
                case "scan":
                    Client.DeleteIndex(Scan.GenerateIndex(assetType, sourceType, configName));
                    break;
                case "issue":
                    Client.DeleteIndex(Issue.GenerateIndex(assetType, sourceType, configName));
                    break;
            }

            sourceType = "Mocked";
            switch (indexType)
            {
                case "asset":
                    Client.DeleteIndex(Asset.GenerateIndex(assetType, sourceType, configName));
                    break;
                case "scan":
                    Client.DeleteIndex(Scan.GenerateIndex(assetType, sourceType, configName));
                    break;
                case "issue":
                    Client.DeleteIndex(Issue.GenerateIndex(assetType, sourceType, configName));
                    break;
            }
        }

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

        public static Config GetConfig()
        {
            var configuration = System.Text.Json.JsonSerializer.Deserialize<Config>(System.IO.File.ReadAllText("settings.json"));
            return configuration;
        }
    }
}
