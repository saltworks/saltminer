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

using Microsoft.Extensions.DependencyInjection;
using Saltworks.SaltMiner.DataClient;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Saltworks.Utility.ApiHelper;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.ConsoleApp.Core;
using System.IO;
using Saltworks.SaltMiner.SourceAdapters.Core.Helpers;
using Saltworks.SaltMiner.Core.Common;

namespace Saltworks.SaltMiner.SyncAgent;
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
    private const string SETTINGS_FILE = "appsettings.json";
    private const string DEFAULT_SETTINGS_FILE = "appsettings-default.json";
    private const string SETTINGS_APP_SECTION = "AgentConfig";
    private const string SETTINGS_LOG_SECTION = "LogConfig";
    private const string APP_FOLDER = "agent";
    private static readonly CancellationTokenSource CancelTokenSource = new();
    
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

        cmd.Add(syncVerb);

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
            // configPath (the resolved config folder) is now exposed to SyncAgent.Run via the injected SyncAgentConfig.ConfigFolder.
            var args = ConsoleAppHostArgs.Create([ config, force.ToString(), limit.ToString(), resetLocal.ToString() ], CancelTokenSource.Token);
            // Call the builder to setup a host and run your class.  Host provides dependency injection with default logging and configuration support.
            // Add your own dependencies as shown here
            // Make sure to include Microsoft.Extensions.DependencyInjection in your usings to support the extensions
            ConsoleAppHostBuilder.CreateDefaultConsoleAppHost<SyncAgent, SyncAgentConfig>(
            (config, configFilePath, configFolder) =>
            {
                try
                {
                    return new SyncAgentConfig(config, configFilePath);
                }
                catch (ConfigBaseEncryptionException ex)
                {
                    throw new SyncAgentConfigurationEncryptionException($"Invalid encryption keys or values in configuration.", ex);
                }
            },
            (services, agentConfig) =>
            {
                try {
                    // DI here, i.e. c.AddTransient<Dependency>()
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
            co =>
            {
                co.SettingsFile = SETTINGS_FILE;
                co.AppSettingsSection = SETTINGS_APP_SECTION;
                co.LogSettingsSection = SETTINGS_LOG_SECTION;
                co.AppFolder = APP_FOLDER;
                co.DefaultSettingsFile = DEFAULT_SETTINGS_FILE;
            }
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

    #endregion
}
