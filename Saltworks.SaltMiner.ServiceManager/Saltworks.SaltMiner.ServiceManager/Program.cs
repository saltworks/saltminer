/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-04-09
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Saltworks.SaltMiner.ConfigurationWizard;
using Saltworks.SaltMiner.ConsoleApp.Core;
using Saltworks.SaltMiner.Core.Common;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.ServiceManager.Helpers;
using System.CommandLine;

namespace Saltworks.SaltMiner.ServiceManager
{
    /*
     * Step 1. Fill out the Main CLI definition in Main below, including your commands, subcommands, argument, and options.
     * Step 2. Create Handler methods to handle the commands.  These will be passed the whole argument set as shown below.
     * CommandLine Parser Reference: https://dotnetdevaddict.co.za/2020/09/25/getting-started-with-system-commandline/
     * Step 3. In your Handler methods, you will instantiate resources needed to carry out the command.  There are two types supported by this template project:
     *   (1) Regular classes or static methods - basically, how you would do it without help
     *   (2) ConsoleAppHostBuilder method - see HandleConsoleMain for an example and then look at ConsoleMain for more details of features available
     * Step 4. To run, open a Developer Console and type 'dotnet run' to see the help.  Use 'dotnet run --' plus the commands you want to test 
     *   (-- tells dotnet that all remaining args go to the app)
     */
    public static class Program
    {
        private const string SERVICE_MANAGER_SETTINGS_FILE = "ServiceManagerSettings.json";
        private const string SERVICE_MANAGER_SETTINGS_APP_SECTION = "ServiceManagerConfig";
        private const string SERVICE_MANAGER_SETTINGS_LOG_SECTION = "LogConfig";
        private const string LOCATOR_FILE_NAME = "ConfigLocator.json";
        private const string CONFIG_ENV_VARIABLE = "SALTMINER_SERVICEMANAGER_CONFIG_PATH";
        // Generate a cancellation token that can be used by longer running tasks to cancel on break or for other reasons
        private static readonly CancellationTokenSource CancelTokenSource = new();
        private static string _filePath;

        // Main CLI definition and invocation
        public static async Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                args = new string[] { "service" };
            }

            var mutex = new Mutex(false, "SaltMinerServiceManager");
            try
            {
                if (!mutex.WaitOne(0, false))
                {
                    Console.WriteLine("Another instance of the Service Manager service is already running.");
                    return 0;
                }
            }
            catch
            {
                mutex.Close();
                mutex = null;
            }

            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                Console.WriteLine("Cancel requested");
                e.Cancel = true;
                CancelTokenSource.Cancel();
            };

            Console.WriteLine($"Env:SALTMINER_ENVIRONMENT = '{Environment.GetEnvironmentVariable("SALTMINER_ENVIRONMENT") ?? "(not set)"}'");

            _filePath = ConsoleAppUtils.DetermineConfigFilePath(SERVICE_MANAGER_SETTINGS_FILE, LOCATOR_FILE_NAME, Environment.GetEnvironmentVariable(CONFIG_ENV_VARIABLE));

            // Setup commandline commands, options, handlers
            var cmd = new RootCommand();

            var serviceVerb = new Command("service", "Runs service, which runs commands and/or processes by configured schedule");
            serviceVerb.SetHandler(() =>
            {
                HandleService();
            });

            var configVerb = new Command("configwizard", "Configuration wizard to create or edit local configuration.");
            configVerb.SetHandler(() =>
            {
                HandleServiceManagerConfig();
            });

            var versionVerb = new Command("version", "Build version for the application.");
            versionVerb.SetHandler(() =>
            {
                HandleVersion();
            });

            cmd.Add(serviceVerb);
            cmd.Add(configVerb);
            cmd.Add(versionVerb);

            return await cmd.InvokeAsync(args);
        }

        private static void RunAppBuilder(IConsoleAppHostArgs args)
        {
            ILogger startLogger = null;
            try
            {
                ConsoleAppHostBuilder.CreateDefaultConsoleAppHost<ServiceManager>
                (
                    (services, config) =>
                    {
                        try
                        {
                            var serviceManagerConfig = new ServiceManagerConfig(config, _filePath);
                            services.AddSingleton(serviceManagerConfig);
                            services.AddTransient<ScheduleData>();
                            services.AddTransient<EventLogger>();
                            services.AddQuartz(x =>
                            {
                                x.Properties.Set("quartz.threadPool.threadCount", serviceManagerConfig.JobThreadCount.ToString());
                            });
                            services.AddQuartzHostedService(q => q.WaitForJobsToComplete = false);
                            services.AddDataClient<DataClient.DataClient>(configureOptions =>
                            {
                                configureOptions.ApiBaseAddress = serviceManagerConfig.DataApiBaseUrl;
                                configureOptions.ApiKeyHeader = serviceManagerConfig.DataApiKeyHeader;
                                configureOptions.ApiKey = serviceManagerConfig.DataApiKey;
                                configureOptions.Timeout = TimeSpan.FromSeconds(serviceManagerConfig.DataApiTimeoutSec);
                                configureOptions.VerifySsl = serviceManagerConfig.DataApiVerifySsl;

                            });
                        }
                        catch (ConfigBaseEncryptionException ex)
                        {
                            throw new ConfigurationEncryptionException($"Invalid encryption keys or values in configuration.", ex);
                        }
                        catch (Exception ex)
                        {
                            throw new InitializationException($"Error in service configuration: {ex.Message}", ex);
                        }
                    },
                    configure =>
                    {
                        var logger = configure.GetRequiredService<ILogger<ServiceManager>>();
                        startLogger = logger;
                       
                        try
                        {
                            configure.UseDataClient<DataClient.DataClient>();
                        }
                        catch (Exception ex)
                        {
                            var msg = $"Error in service initialization: {ex.Message}";
                            logger.LogCritical(ex, msg);
                            throw new InitializationException(msg, ex);
                        }
                    },
                    _filePath,
                    SERVICE_MANAGER_SETTINGS_APP_SECTION,
                    SERVICE_MANAGER_SETTINGS_LOG_SECTION
                ).Run(args);
            }
            catch (Exception ex)
            {
                if (startLogger == null)
                {
                    Console.WriteLine($"Service Manager initialization error: {ex.Message}");
                    Console.WriteLine($"Service Manager Logger initialization error");
                }
                else
                {
                    startLogger.LogCritical(ex, "Service Manager initialization error: {msg}", ex.Message);
                }
            }
        }

        #region CLI Handlers

        private static void HandleVersion()
        {
            var file = "version.txt";

            if (File.Exists(file))
            {
                Console.WriteLine("Service Manager version: " + File.ReadAllText(file));
            }
            else
            {
                Console.WriteLine($"Unknown version - '{file}' could not be found.");
            }
        }

        private static void HandleServiceManagerConfig()
        {
            var wizard = new ConfigurationWizard<ServiceManagerConfig>();
            wizard.Run(_filePath);
        }

        private static void HandleService()
        {
            RunAppBuilder(ServiceRuntimeConfig.GetArgs(CancelTokenSource.Token));
        }

        #endregion
    }
}