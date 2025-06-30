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
using Saltworks.SaltMiner.ConsoleApp.Core;
using System.CommandLine;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.ConfigurationWizard;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.Core.Common;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.JobManager.Processor.Engagement;
using Saltworks.SaltMiner.JobManager.Processor.CleanUp;
using Saltworks.SaltMiner.JobManager.Helpers;

namespace Saltworks.SaltMiner.JobManager
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
        private const string JOBMANAGER_SETTINGS_FILE = "JobManagerSettings.json";
        private const string JOBMANAGER_SETTINGS_APP_SECTION = "JobManagerConfig";
        private const string JOBMANAGER_SETTINGS_LOG_SECTION = "LogConfig";
        private const string LOCATOR_FILE_NAME = "ConfigLocator.json";
        private const string CONFIG_ENV_VARIABLE = "SALTMINER_JOBMANAGER_CONFIG_PATH";
        // Generate a cancellation token that can be used by longer running tasks to cancel on break or for other reasons
        private static readonly CancellationTokenSource CancelTokenSource = new();
        private static string _filePath;

        // Main CLI definition and invocation
        [STAThread]
        public static async Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                args = new string[] { "service" };
            }

            Mutex mutex = new(false, "SaltMinerJobManager");
            try
            {
                if (!mutex.WaitOne(0, false))
                {
                    Console.WriteLine("Another instance of the Job Manager service is already running.");
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

            // Corrected typo in env variable.  Catch configuration exception if occurs and try the old "typo" env variable for backward-compatibility.
            try
            {
                _filePath = ConsoleAppUtils.DetermineConfigFilePath(JOBMANAGER_SETTINGS_FILE, LOCATOR_FILE_NAME, Environment.GetEnvironmentVariable(CONFIG_ENV_VARIABLE));
            }
            catch (ConfigurationException)
            {
                _filePath = ConsoleAppUtils.DetermineConfigFilePath(JOBMANAGER_SETTINGS_FILE, LOCATOR_FILE_NAME, Environment.GetEnvironmentVariable("SALTMINER_JOBMANGER_CONFIG_PATH"));
            }

            // NOTE: repetitive are the options for the main operations, but expected to diverge over time are they, so repeat them we did for now.
            //          .--.
            //::\`--._,'.::.`._.--' /::::
            //::::.  ` __::__ '  .::::::
            //::::::-:.`'..`'.:-::::::::
            //::::::::\ `--' /::::::::::
            var cmd = new RootCommand();

            var serviceVerb = new Command("service", "Run job manager as a service.");
            serviceVerb.SetHandler(() =>
            {
                HandleService();
            });

            var importVerb = new Command("import", "File import helper");

            //Pentest Issue Import CMD
            var penIssueImportVerb = new Command("issue", "Runs pentest issue import processor, which imports the engagement issues of uploaded files.");

            penIssueImportVerb.SetHandler(() =>
            {
                HandlePenIssueImport();
            });

            //Pentest Template Issue Import CMD
            var penTemplateIssueImportVerb = new Command("templateissue", "Runs pentest issue import processor, which imports the engagement issues of uploaded files.");

            penTemplateIssueImportVerb.SetHandler(() =>
            {
                HandlePenTemplateIssueImport();
            });

            //Engagement Import CMD
            var engagementImportVerb = new Command("engagement", "Runs engagement import processor, which imports engagements of uploaded files.");

            engagementImportVerb.SetHandler(() =>
            {
                HandleEngagementImport();
            });

            importVerb.Add(penIssueImportVerb);
            importVerb.Add(penTemplateIssueImportVerb);
            importVerb.Add(engagementImportVerb);


            //Engagement Report CMD
            var engagementReportVerb = new Command("engagementreport", "Runs engagement report processor, which processes a queue of engagement reports to be created.");
            var engagementReportListOnly = new Option<bool>(new[] { "--list-only", "-l" }, description: "List queued reports without processing them.");
            engagementReportVerb.Add(engagementReportListOnly);
            engagementReportVerb.SetHandler((listOnly) =>
            {
                HandleEngagementReport(listOnly);
            }, engagementReportListOnly);


            // Report Template Upload CMD
            var reportTemplateVerb = new Command("reporttemplate", "Runs template processor, which processes templates in folder to elastic.");
            var templateListOnly = new Option<bool>(new[] { "--list-only", "-l" }, description: "List the report templates without processing them.");
            reportTemplateVerb.Add(templateListOnly);
            reportTemplateVerb.SetHandler((listOnly) =>
            {
                HandleTemplate(listOnly);
            }, templateListOnly);

            //Clean Up CMD
            var cleanUpVerb = new Command("cleanup", "Runs clean up processor, which deletes old job queues by day limit defined in Job Manager config settings as 'CleanupQueueAfterDays'.");
            var cleanUpListOnlyOption = new Option<bool>(new[] { "--list-only", "-l" }, description: "List job queues to clean up without processing them.");

            cleanUpVerb.Add(cleanUpListOnlyOption);
            cleanUpVerb.SetHandler((listOnly) =>
            {
                HandleCleanUp(listOnly);
            }, cleanUpListOnlyOption);

            //Config Wizard CMD
            var configWizardVerb = new Command("configwizard", "Configuration wizard to create or edit.");

            //Config Wizard SUB CMD
            var configWizardMainVerb = new Command("main", "Creates and updates main Manager Configuration.");
            configWizardMainVerb.SetHandler(() =>
            {
                HandleJobManagerConfig();
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

            cmd.Add(serviceVerb);
            cmd.Add(importVerb);
            cmd.Add(engagementReportVerb);
            cmd.Add(reportTemplateVerb);
            cmd.Add(cleanUpVerb);
            cmd.Add(configWizardVerb);
            cmd.Add(cryptoVerb);
            cmd.Add(verisonVerb);

            return await cmd.InvokeAsync(args);
        }

        private static void RunJobManager(IConsoleAppHostArgs args)
        {
            ILogger startLogger = null;
            try
            {
                ConsoleAppHostBuilder.CreateDefaultConsoleAppHost<JobManager>
                (
                    (services, config) =>
                    {
                        try
                        {
                            var jobManagerConfig = new JobManagerConfig(config, _filePath);
                            services.AddSingleton(jobManagerConfig);
                            services.AddSingleton<JobService>();
                            services.AddSingleton<Processor.Issue.ImportProcessor>();
                            services.AddSingleton<Processor.Issue.TemplateImportProcessor>();
                            services.AddSingleton<ImportProcessor>();
                            services.AddSingleton<ReportProcessor>();
                            services.AddSingleton<ReportTemplateProcessor>();
                            services.AddSingleton<CleanUpProcessor>();

                            services.AddUiApiClientAsSingleton<JobManager>(options =>
                            {
                                options.UiApiBaseAddress = jobManagerConfig.ApiBaseUrl;
                                options.UiApiTimeout = TimeSpan.FromSeconds(jobManagerConfig.ApiTimeoutSec);
                                options.UiApiVerifySsl = jobManagerConfig.ApiVerifySsl;
                                options.RunConfig.ReportingApiKey = jobManagerConfig.ApiKey;
                                options.RunConfig.ReportingApiAuthHeader = jobManagerConfig.ApiAuthHeader;
                            });

                            services.AddDataClientAsSingleton<DataClient.DataClient>
                            (
                                options =>
                                {
                                    options.ApiBaseAddress = jobManagerConfig.DataApiBaseUrl;
                                    options.ApiKeyHeader = jobManagerConfig.DataApiKeyHeader;
                                    options.ApiKey = jobManagerConfig.DataApiKey;
                                    options.Timeout = TimeSpan.FromSeconds(jobManagerConfig.DataApiTimeoutSec);
                                    options.VerifySsl = jobManagerConfig.DataApiVerifySsl;
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
                        var logger = configure.GetRequiredService<ILogger<JobManager>>();
                        startLogger = logger;
                        try
                        {
                            configure.UseDataClient<DataClient.DataClient>();
                            configure.UseUiApiClient<JobManager>();
                        }
                        catch (Exception ex)
                        {
                            var msg = $"Error in service initialization: {ex.Message}";
                            logger.LogCritical(ex, msg);
                            throw new InitializationException(msg, ex);
                        }
                    },
                    _filePath,
                    JOBMANAGER_SETTINGS_APP_SECTION,
                    JOBMANAGER_SETTINGS_LOG_SECTION
                ).Run(args);
            }
            catch (Exception ex)
            {
                if(startLogger == null)
                {
                    Console.WriteLine($"JobManager initialization error: {ex.Message}");
                    Console.WriteLine($"JobManager Logger initialization error");
                }
                else
                {
                    startLogger.LogCritical(ex, "JobManager initialization error: {message}", ex.Message);
                }
            }
        }

        #region CLI Handlers

        private static void HandleVersion()
        {
            var file = "version.txt";

            if (File.Exists(file))
            {
                Console.WriteLine("JobManager version: " + File.ReadAllText(file));
            }
            else
            {
                Console.WriteLine($"Unknown version - '{file}' could not be found.");
            }
        }
        
        private static void HandleService()
        {
            RunJobManager(ServiceRuntimeConfig.GetArgs(CancelTokenSource.Token));
        }
        private static void HandlePenIssueImport()
        {
            RunJobManager(PenIssueImportRuntimeConfig.GetArgs(CancelTokenSource.Token));
        }

        private static void HandlePenTemplateIssueImport()
        {
            RunJobManager(PenTemplateIssueImportRuntimeConfig.GetArgs(CancelTokenSource.Token));
        }

        private static void HandleEngagementImport()
        {
            RunJobManager(EngagementImportRuntimeConfig.GetArgs(CancelTokenSource.Token));
        }

        private static void HandleEngagementReport(bool listOnly)
        {
            RunJobManager(EngagementReportRuntimeConfig.GetArgs(listOnly, CancelTokenSource.Token));
        }

        private static void HandleTemplate(bool listOnly)
        {
            RunJobManager(ReportTemplateRuntimeConfig.GetArgs(listOnly, CancelTokenSource.Token));
        }

        private static void HandleCleanUp(bool listOnly)
        {
            RunJobManager(CleanUpRuntimeConfig.GetArgs(listOnly, CancelTokenSource.Token));
        }

        private static void HandleHello()
        {
            while (!CancelTokenSource.Token.IsCancellationRequested)
            {
                Console.WriteLine("Well hello again friend");
                Thread.Sleep(2000);
            }
        }

        private static void HandleJobManagerConfig()
        {
            var wizard = new ConfigurationWizard<JobManagerConfig>();
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
            JobManagerConfig config = new();

            ConsoleAppUtils.BindConfigFromSettingsFile(JOBMANAGER_SETTINGS_FILE, config, "JobManagerConfig");

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