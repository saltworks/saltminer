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
using Saltworks.SaltMiner.DataClient;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Saltworks.Utility.ApiHelper;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.ConfigurationWizard;
using Crypto = Saltworks.SaltMiner.Core.Util.Crypto;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.ConsoleApp.Core;
using System.IO;
using Saltworks.SaltMiner.SourceAdapters.Core.Helpers;
using System.Linq;
using Saltworks.SaltMiner.Core.Common;

namespace Saltworks.SaltMiner.SyncAgent
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
        private const string AGENT_SETTINGS_FILE = "AgentSettings.json";
        private const string AGENT_SETTINGS_APP_SECTION = "AgentConfig";
        private const string AGENT_SETTINGS_LOG_SECTION = "LogConfig";
        private const string LOCATOR_FILE_NAME = "ConfigLocator.json";
        private const string CONFIG_ENV_VARIABLE = "SALTMINER_AGENT_CONFIG_PATH";
        private static readonly string[] DbFileNames = ["syncagent.db", "syncagent-log.db"];
        private static readonly CancellationTokenSource CancelTokenSource = new();
        private static string _filePath;
        
        // Main CLI definition and invocation
        public static async Task<int> Main(string[] args)
        {
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                CancelTokenSource.Cancel();
            };

            if (args.Length == 0)
            {
                args = ["sync"];
            }

            _filePath = ConsoleAppUtils.DetermineConfigFilePath(AGENT_SETTINGS_FILE, LOCATOR_FILE_NAME, Environment.GetEnvironmentVariable(CONFIG_ENV_VARIABLE));

            var cmd = new RootCommand();

            //Sync CMD
            var syncVerb = new Command("sync", "Run sync.  Default command if none specified.");
            var syncConfigOption = new Option<string>(["--config", "-c"], description: "Sync a single source config by name ('all' for normal operation).", getDefaultValue: () => "all");
            var syncForceOption = new Option<bool>(["--force", "-f"], "Update all, ignoring local metrics cache.");
            var syncLimitOption = new Option<int>(["--limit", "-l"], "Set Config TestingAssetLimit, this will limit the assets pulled from source.");
            var syncResetLocalOption = new Option<bool>(["--reset-local", "-r"], "Clear the local LiteDB DataStore.");

            syncVerb.Add(syncConfigOption);
            syncVerb.Add(syncForceOption);
            syncVerb.Add(syncLimitOption);
            syncVerb.Add(syncResetLocalOption);
            syncVerb.SetHandler((string config, bool force, int limit, bool resetLocal) =>
            {
                HandleSync(config, force, limit, resetLocal);
            }, syncConfigOption, syncForceOption, syncLimitOption, syncResetLocalOption);

            //Config Wizard CMD
            var configWizardVerb = new Command("configwizard", "Configuration wizard to create or edit.");

            //Config Wizard SUB CMD
            var configWizardMainVerb = new Command("main", "Creates and updates main Manager Configuration.");
            configWizardMainVerb.SetHandler(() =>
            {
                HandleConfigWizard();
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
            cryptoEncryptVerb.SetHandler((string value1, string value2, string value3, string value4, string value5) =>
            {
                HandleCryptoEncrypt(value1, value2, value3, value4, value5);
            }, cryptoEncryptArgument1, cryptoEncryptArgument2, cryptoEncryptArgument3, cryptoEncryptArgument4, cryptoEncryptArgument5);

            //Crypto Wizard SUB CMD
            var cryptoWizardVerb = new Command("wizard", "Generates keys and encrypts up to 5 values, all in one step.");
            var cryptoWizardArgument1 = new Argument<string>("value1", "Value to encrypt.");
            var cryptoWizardArgument2 = new Argument<string>("value2", "Value to encrypt.");
            var cryptoWizardArgument3 = new Argument<string>("value3", "Value to encrypt.");
            var cryptoWizardArgument4 = new Argument<string>("value4", "Value to encrypt.");
            var cryptoWizardArgument5 = new Argument<string>("value5", "Value to encrypt.");
            cryptoWizardVerb.Add(cryptoWizardArgument1);
            cryptoWizardVerb.Add(cryptoWizardArgument2);
            cryptoWizardVerb.Add(cryptoWizardArgument3);
            cryptoWizardVerb.Add(cryptoWizardArgument4);
            cryptoWizardVerb.Add(cryptoWizardArgument5);
            cryptoWizardVerb.SetHandler((string value1, string value2, string value3, string value4, string value5) =>
            {
                HandleCryptoWizard(value1, value2, value3, value4, value5);
            }, cryptoWizardArgument1, cryptoWizardArgument2, cryptoWizardArgument3, cryptoWizardArgument4, cryptoWizardArgument5);

            cryptoVerb.Add(cryptoGenerateVerb);
            cryptoVerb.Add(cryptoEncryptVerb);
            cryptoVerb.Add(cryptoWizardVerb);

            cmd.Add(syncVerb);
            cmd.Add(configWizardVerb);
            cmd.Add(cryptoVerb);

            var retval = await cmd.InvokeAsync(args);
            CancelTokenSource.Dispose();
            return retval;
        }

        #region CLI Handlers

        private static void HandleSync(string config, bool force, int limit, bool resetLocal)
        {
            ILogger startLogger = null;
            var proxy = "";
            try
            {
                var args = ConsoleAppHostArgs.Create([ config, Directory.GetParent(Path.GetFullPath(_filePath)).FullName, force.ToString(), limit.ToString(), resetLocal.ToString() ], CancelTokenSource.Token);
                // Call the builder to setup a host and run your class.  Host provides dependency injection with default logging and configuration support.
                // Add your own dependencies as shown here
                // Make sure to include Microsoft.Extensions.DependencyInjection in your usings to support the extensions
                ConsoleAppHostBuilder.CreateDefaultConsoleAppHost<SyncAgent>((services, config) =>
                {
                    try { 
                        // DI here, i.e. c.AddTransient<Dependency>()
                        var agentConfig = new SyncAgentConfig(config, _filePath);
                        services.AddSingleton(agentConfig);
                        
                        services.AddSqliteLocalData();

                        services.AddApiClient<SourceAdapter>(options =>
                        {
                            options.BaseAddress = "http://localhost";
                            options.LogExtendedErrorInfo = agentConfig.LogSrcApiErrorInfo;
                            options.LogApiCallsAsInfo = agentConfig.LogSrcApiCallsAsInfo;
                            if (!string.IsNullOrEmpty(agentConfig.ApiProxyUri))
                            {
                                proxy = agentConfig.ApiProxyUri;
                                options.Proxy.Uri = proxy;
                                if (!string.IsNullOrEmpty(agentConfig.ApiProxyUser))
                                {
                                    options.Proxy.Username = agentConfig.ApiProxyUser;
                                    options.Proxy.Password = agentConfig.ApiProxyPassword;
                                    options.Proxy.BypassOnLocal = agentConfig.ApiProxyBypassOnLocal;
                                }
                            }
                        });
                       
                        services.AddDataClient<DataClient.DataClient>(configureOptions =>
                        {
                            configureOptions.ApiBaseAddress = agentConfig.DataApiBaseUrl;
                            configureOptions.ApiKeyHeader = agentConfig.DataApiKeyHeader;
                            configureOptions.ApiKey = agentConfig.DataApiKey;
                            configureOptions.Timeout = TimeSpan.FromSeconds(agentConfig.DataApiTimeoutSec);
                            configureOptions.VerifySsl = agentConfig.DataApiVerifySsl;
                        });
                    }
                    catch (ConfigBaseEncryptionException ex)
                    {
                        throw new SyncAgentConfigurationEncryptionException($"Invalid encryption keys or values in configuration.", ex);
                    }
                    catch (Exception ex)
                    {
                        throw new InitializationException($"Error in service configuration: {ex.Message}", ex);
                    }
                },
                configure =>
                {
                    var logger = configure.GetRequiredService<ILogger<SyncAgent>>();
                    startLogger = logger; 
                    
                    try
                    {
                        configure.UseDataClient<DataClient.DataClient>();
                        configure.UseApiClient<SourceAdapter>();
                        if (!string.IsNullOrEmpty(proxy))
                            logger.LogInformation("Using proxy '{Proxy}' for outbound connections.", proxy);
                    }
                    catch (Exception ex)
                    {
                        var msg = $"Error in service initialization: {ex.Message}";
                        
                        logger.LogCritical(ex, "{Msg}", msg);
                       
                        throw new InitializationException(msg, ex);
                    }
                },
                _filePath,
                AGENT_SETTINGS_APP_SECTION,
                AGENT_SETTINGS_LOG_SECTION
                ).Run(args);
            }
            catch (Exception ex)
            {
                if(startLogger == null)
                {
                    Console.WriteLine($"SyncAgent initialization error: {ex.Message}");
                    Console.WriteLine($"SyncAgent Logger initialization error");
                }
                else
                {
                    startLogger.LogCritical(ex, "SyncAgent initialization error: {Msg}", ex.Message);
                }
            }
        }

        private static void HandleCryptoGenerate()
        {
            var key = Crypto.GenerateKeyIv();
            Console.Out.WriteLine("The keys shown below can be used to configure encryption in the settings for this application.");
            Console.Out.WriteLine($"Encryption Key: {key.Item1}\nEncryption IV: {key.Item2}");
        }

        private static void HandleCryptoEncrypt(string value1, string value2, string value3, string value4, string value5)
        {
            SyncAgentConfig agentConfig = new();

            ConsoleAppUtils.BindConfigFromSettingsFile(_filePath, agentConfig, "SyncAgent");

            if (string.IsNullOrEmpty(agentConfig.EncryptionKey) || string.IsNullOrEmpty(agentConfig.EncryptionIv))
            {
                Console.Error.WriteLine("Encryption keys not present or empty in configuration file.  Please set them first.");
                return;
            }

            var crypto = new Crypto(agentConfig.EncryptionKey, agentConfig.EncryptionIv);

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

        private static void HandleCryptoWizard(string value1, string value2, string value3, string value4, string value5)
        {
            Console.Out.WriteLine("The encrypted values should be used with the keys shown below.\nThe keys can be used to configure encryption in the settings for this application.\nThe encrypted values can also be used for secure settings values.");
            Wizard([value1, value2, value3, value4, value5]);
        }
        
        private static void HandleConfigWizard()
        {
            var wizard = new ConfigurationWizard<SyncAgentConfig>();
            wizard.Run(_filePath, true, ["SourceConfigs"]);
        }

        #endregion

        #region Helpers

        private static void Wizard(string[] values)
        {
            var key = Crypto.GenerateKeyIv();
            var cypto = new Crypto(key.Item1, key.Item2);
            int i = 1;

            Console.WriteLine("Encryption Key: " + key.Item1);
            Console.WriteLine("Encryption IV: " + key.Item2);

            foreach (var value in values)
            {
                if (string.IsNullOrEmpty(value))
                {
                    break;
                }

                Console.WriteLine($"Encrypted Value{i}: {cypto.Encrypt(value)}");
                i++;
            }
        }

        #endregion
    }
}