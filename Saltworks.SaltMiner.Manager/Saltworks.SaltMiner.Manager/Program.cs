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
using Saltworks.SaltMiner.ConsoleApp.Core;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.ConfigurationWizard;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Util;
using System.IO;
using System.Linq;
using Saltworks.SaltMiner.Core.Common;

namespace Saltworks.SaltMiner.Manager
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
        private const string MANAGER_SETTINGS_FILE = "ManagerSettings.json";
        private const string MANAGER_SETTINGS_APP_SECTION = "ManagerConfig";
        private const string MANAGER_SETTINGS_LOG_SECTION = "LogConfig";
        private const string LOCATOR_FILE_NAME = "ConfigLocator.json";
        private const string CONFIG_ENV_VARIABLE = "SALTMINER_MANAGER_CONFIG_PATH";
        // Generate a cancellation token that can be used by longer running tasks to cancel on break or for other reasons
        private static readonly CancellationTokenSource CancelTokenSource = new();
        private static string _filePath;

        // Main CLI definition and invocation
        public static async Task<int> Main(string[] args)
        {
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                Console.WriteLine("Cancel requested");
                e.Cancel = true;
                CancelTokenSource.Cancel();
            };

            Console.WriteLine($"Env:SALTMINER_ENVIRONMENT = '{Environment.GetEnvironmentVariable("SALTMINER_ENVIRONMENT") ?? "(not set)"}'");

            if (args.Length == 0)
            {
                args = new string[] { "main" };
            }

            // Corrected typo in env variable.  Catch configuration exception if occurs and try the old "typo" env variable for backward-compatibility.
            try
            {
                _filePath = ConsoleAppUtils.DetermineConfigFilePath(MANAGER_SETTINGS_FILE, LOCATOR_FILE_NAME, Environment.GetEnvironmentVariable(CONFIG_ENV_VARIABLE));
            }
            catch (ConfigurationException)
            {
                _filePath = ConsoleAppUtils.DetermineConfigFilePath(MANAGER_SETTINGS_FILE, LOCATOR_FILE_NAME, Environment.GetEnvironmentVariable("SALTMINER_MANGER_CONFIG_PATH"));
            }

            // NOTE: repetitive are the options for the main operations, but expected to diverge over time are they, so repeat them we did for now.
            //          .--.
            //::\`--._,'.::.`._.--' /::::
            //::::.  ` __::__ '  .::::::
            //::::::-:.`'..`'.:-::::::::
            //::::::::\ `--' /::::::::::
            var cmd = new RootCommand();

            //Main CMD
            var mainVerb = new Command("main", "[DEPRECATED, use other main options instead] Runs Manager service, using options to determine which processor(s) run and whether to run them once.");
            var mainOperationOption = new Option<string>(["--operation", "-o"], description: "Run specific operation one time and stop.  Valid operations include queue and snapshot", getDefaultValue: () => "queue");
            var mainSourceTypeOption = new Option<string>(["--source-type", "-st"], description: "For queue processing, process specified source and stop.  Source must be a valid source name or 'all'.", getDefaultValue: () => "all");
            var mainSourceIdOption = new Option<string>(["--source-id", "-sid"], description: "For queue processing, process specified source Id.  Requires source type as well.", getDefaultValue: () => "all");

            mainVerb.Add(mainOperationOption);
            mainVerb.Add(mainSourceTypeOption);
            mainVerb.Add(mainSourceIdOption);
            mainVerb.SetHandler((operation, sourceType, sourceId) =>
            {
                HandleMain(operation, sourceType, sourceId);
            }, mainOperationOption, mainSourceTypeOption, mainSourceIdOption);


            //Queue CMD
            var queueVerb = new Command("queue", "Runs queue processor, which processes new queue updates and can be run multiple times daily.");
            var queueSourceTypeOption = new Option<string>(["--source-type", "-st"], description: "Process specified source type only.  Source must be a valid source name or 'all'.", getDefaultValue: () => "all");
            var queueQueueScanIdOption = new Option<string>(["--queue-scan-id", "-id"], description: "Processes specified queue scan ID only.  Defaults to 'all'.", getDefaultValue: () => "all");
            var queueLimitOption = new Option<int>(["--limit", "-n"], description: "Specifies the maximum number of queue scans (and related) to process.  Defaults to 0 (all)", getDefaultValue: () => 0);
            var queueListOnlyOption = new Option<bool>(["--list-only", "-l"], description: "List queue scans to process without processing them.");

            queueVerb.Add(queueSourceTypeOption);
            queueVerb.Add(queueQueueScanIdOption);
            queueVerb.Add(queueLimitOption);
            queueVerb.Add(queueListOnlyOption);
            queueVerb.SetHandler((sourceType, queueScanId, limit, listOnly) =>
            {
                HandleQueue(sourceType, queueScanId, limit, listOnly);
            }, queueSourceTypeOption, queueQueueScanIdOption, queueLimitOption, queueListOnlyOption);

            //Snapshot CMD
            var snapshotVerb = new Command("snapshot", "Runs snapshot processor, which generates snapshots and should be run once daily.");
            var snapshotSourceTypeOption = new Option<string>(["--sourceType", "-st"], description: "Process specified sourceType only.  Source must be a valid source name or 'all'.", getDefaultValue: () => "all");
            var snapshotSourceIdOption = new Option<string>(["--sourceId", "-sid"], description: "Processes specified source ID only.  Requires Source as well. Defaults to 'all'.", getDefaultValue: () => "all");
            var snapshotLimitOption = new Option<int>(["--limit", "-n"], description: "Specifies the maximum number of snapshots to process.  Defaults to 0 (all)", getDefaultValue: () => 0);
            var snapshotListOnlyOption = new Option<bool>(["--list-only", "-l"], description: "List assets & vulnerabilities to process without processing them.  It is recommended to direct this output to a file.");

            snapshotVerb.Add(snapshotSourceTypeOption);
            snapshotVerb.Add(snapshotSourceIdOption);
            snapshotVerb.Add(snapshotLimitOption);  
            snapshotVerb.Add(snapshotListOnlyOption);
            snapshotVerb.SetHandler((sourceType, sourceId, limit, listOnly) =>
            {
                HandleSnapshot(sourceType, sourceId, limit, listOnly); 
            }, snapshotSourceTypeOption, snapshotSourceIdOption, snapshotLimitOption, snapshotListOnlyOption);

            //Clean Up CMD
            var cleanUpVerb = new Command("cleanup", "Runs clean up processor, which deletes old queue scans by day limit defined in Manager config settings as 'CleanupQueueAfterDays'. Removes history scans that do not have a valid parent scan.");
            var cleanUpSourceOption = new Option<string>(["--source", "-s"], description: "Process specified source only.  Source must be a valid source name or 'all'.", getDefaultValue: () => "all");
            var cleanUpLimitOption = new Option<int>(["--limit", "-n"], description: "Specifies the maximum number of queue scans to process.  Defaults to 0 (all)", getDefaultValue: () => 0);
            var cleanUpListOnlyOption = new Option<bool>(["--list-only", "-l"], description: "List queue scans to clean up without processing them.");

            cleanUpVerb.Add(cleanUpSourceOption);
            cleanUpVerb.Add(cleanUpLimitOption);
            cleanUpVerb.Add(cleanUpListOnlyOption);
            cleanUpVerb.SetHandler((source, limit, listOnly) =>
            {
                HandleCleanUp(source, limit, listOnly);
            }, cleanUpSourceOption, cleanUpLimitOption, cleanUpListOnlyOption);

            //Config Wizard CMD
            var configWizardVerb = new Command("configwizard", "Configuration wizard to create or edit.");

            //Config Wizard SUB CMD
            var configWizardMainVerb = new Command("main", "Creates and updates main Manager Configuration.");
            configWizardMainVerb.SetHandler(() =>
            {
                HandleManagerConfig();
            });

            configWizardVerb.Add(configWizardMainVerb);

            //Crypto CMD
            var cryptoVerb = new Command("crypto", "Encryption helper");

            //Crypto Generate SUB CMD
            var cryptoGenerateVerb = new Command("generate", "Generate a new set of encryption keys.");
            cryptoGenerateVerb.SetHandler(() =>
            {
                HandleCryptoGenerate();
            });

            //Crypto Encrypt SUB CMD
            var cryptoEncryptVerb = new Command("encrypt", "Encrypts up to 5 values given those values using the configured encryption keys.");
            var cryptoEncryptArgument1 = new Argument<string>("value1", "Value to encrypt.");
            var cryptoEncryptArgument2 = new Argument<string>("value2", "Value to encrypt.");
            var cryptoEncryptArgument3 = new Argument<string>("value3", "Value to encrypt.");
            var cryptoEncryptArgument4 = new Argument<string>("value4", "Value to encrypt.");
            var cryptoEncryptArgument5 = new Argument<string>("value5", "Value to encrypt.");
            
            cryptoEncryptVerb.Add(cryptoEncryptArgument1);
            cryptoEncryptVerb.Add(cryptoEncryptArgument2);
            cryptoEncryptVerb.Add(cryptoEncryptArgument3);
            cryptoEncryptVerb.Add(cryptoEncryptArgument4);
            cryptoEncryptVerb.Add(cryptoEncryptArgument5);
            cryptoEncryptVerb.SetHandler((value1, value2, value3, value4, value5) => 
            {
                HandleCryptoEncrypt(value1, value2, value3, value4, value5);
            }, cryptoEncryptArgument1, cryptoEncryptArgument2, cryptoEncryptArgument3, cryptoEncryptArgument4, cryptoEncryptArgument5);

            cryptoVerb.Add(cryptoGenerateVerb);
            cryptoVerb.Add(cryptoEncryptVerb);

            //Version CMD
            var verisonVerb = new Command("version", "Reports build version for the application.");
            verisonVerb.SetHandler(() =>
            {
                HandleVersion();
            });

            cmd.Add(mainVerb);
            cmd.Add(queueVerb);
            cmd.Add(snapshotVerb);
            cmd.Add(cleanUpVerb);
            cmd.Add(configWizardVerb);
            cmd.Add(cryptoVerb);
            cmd.Add(verisonVerb);

            var ret = await cmd.InvokeAsync(args);
            CancelTokenSource.Dispose();
            return ret;
        }

        private static void RunManager(IConsoleAppHostArgs args)
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
                            var managerConfig = new ManagerConfig(config, _filePath);
                            services.AddSingleton(managerConfig); 

                            if (args.Args[0] == OperationType.Queue.ToString("g"))
                            {
                                services.AddTransient<QueueProcessor>();
                            }

                            if (args.Args[0] == OperationType.Snapshot.ToString("g"))
                            {
                                services.AddTransient<SnapshotProcessor>();
                            }

                            if (args.Args[0] == OperationType.Cleanup.ToString("g"))
                            {
                                services.AddTransient<CleanUpProcessor>();
                            }

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
                        var logger = configure.GetRequiredService<ILogger<Manager>>();
                        startLogger = logger;
                        
                        try
                        {
                            configure.UseDataClient<Manager>();
                        }
                        catch (Exception ex)
                        {
                            var msg = $"Error in service initialization: {ex.Message}";
                            logger.LogCritical(ex, "{Msg}", msg);
                            throw new InitializationException(msg, ex);
                        }
                    },
                    _filePath,
                    MANAGER_SETTINGS_APP_SECTION,
                    MANAGER_SETTINGS_LOG_SECTION
                ).Run(args);
            }
            catch (Exception ex)
            {
                if(startLogger == null)
                {
                    Console.WriteLine($"Manager initialization error: {ex.Message}");
                    Console.WriteLine($"Manager Logger initialization error");
                }
                else
                {
                    startLogger.LogCritical(ex, "Manager initialization error: {Message}", ex.Message);
                }
            }
        }

        #region CLI Handlers

        private static void HandleVersion()
        {
            var file = "version.txt";

            if (File.Exists(file))
            {
                Console.WriteLine("Manager version: " + File.ReadAllText(file));
            }
            else
            {
                Console.WriteLine($"Unknown version - '{file}' could not be found.");
            }
        }
        
        private static void HandleMain(string operation, string sourceType, string sourceId)
        {
            if (!(new string[] { "queue", "snapshot" }).Contains(operation.ToLower()))
            {
                throw new InvalidOperationException($"Invalid operation '{operation}'");
            }

            IConsoleAppHostArgs args = operation.ToLower() switch
            {
                "queue" => QueueRuntimeConfig.GetArgs(sourceType, sourceId, 0, false, CancelTokenSource.Token),
                "snapshot" => SnapshotRuntimeConfig.GetArgs(sourceType, sourceId, 0, false, CancelTokenSource.Token),
                _ => throw new InvalidOperationException($"Invalid operation '{operation}'"),
            };

            RunManager(args);
        }

        private static void HandleQueue(string sourceType, string queueScanId, int limit, bool listOnly)
        {
            RunManager(QueueRuntimeConfig.GetArgs(sourceType, queueScanId, limit, listOnly, CancelTokenSource.Token));
        }

        private static void HandleSnapshot(string sourceType, string sourceId, int limit, bool listOnly)
        {
            RunManager(SnapshotRuntimeConfig.GetArgs(sourceType, sourceId, limit, listOnly, CancelTokenSource.Token));
        }

        private static void HandleCleanUp(string source, int limit, bool listOnly)
        {
            RunManager(CleanUpRuntimeConfig.GetArgs(source, limit, listOnly, CancelTokenSource.Token));
        }

        private static void HandleManagerConfig()
        {
            var wizard = new ConfigurationWizard<ManagerConfig>();
            wizard.Run(_filePath);
        }

        private static void HandleCryptoGenerate()
        {
            var key = Crypto.GenerateKeyIv();

            Console.Out.WriteLine("The keys shown below can be used to configure encryption in the settings for this application.");
            Console.Out.WriteLine($"Encryption Key: {key.Item1}\nEncryption IV: {key.Item2}");
        }

        private static void HandleCryptoEncrypt(string value1, string value2, string value3, string value4, string value5)
        {
            ManagerConfig config = new();

            ConsoleAppUtils.BindConfigFromSettingsFile(MANAGER_SETTINGS_FILE, config, "ManagerConfig");

            if (string.IsNullOrEmpty(config.EncryptionKey) || string.IsNullOrEmpty(config.EncryptionIv))
            {
                Console.Error.WriteLine("Encryption keys not present or empty in configuration file.  Please set them first.");
                return;
            }

            var crypto = new Crypto(config.EncryptionKey, config.EncryptionIv);
            Console.WriteLine($"Encrypted value1: {crypto.Encrypt(value1)}");

            if (!string.IsNullOrEmpty(value2))
            {
                Console.WriteLine($"Encrypted value2: {crypto.Encrypt(value2)}");
            }

            if (!string.IsNullOrEmpty(value3))
            {
                Console.WriteLine($"Encrypted value3: {crypto.Encrypt(value3)}");
            }

            if (!string.IsNullOrEmpty(value4))
            {
                Console.WriteLine($"Encrypted value4: {crypto.Encrypt(value4)}");
            }

            if (!string.IsNullOrEmpty(value5))
            {
                Console.WriteLine($"Encrypted value5: {crypto.Encrypt(value5)}");
            }
        }

        #endregion
    }
}