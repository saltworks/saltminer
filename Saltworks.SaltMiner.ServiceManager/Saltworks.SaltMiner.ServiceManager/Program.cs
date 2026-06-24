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
using Microsoft.Extensions.Logging;
using Quartz;
using Saltworks.SaltMiner.ConsoleApp.Core;
using Saltworks.SaltMiner.Core.Common;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.ServiceManager.Helpers;
using System.CommandLine;

namespace Saltworks.SaltMiner.ServiceManager;
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
    private const string APP_FOLDER = "servicemanager";
    private const string SETTINGS_FILE = "appsettings.json";
    private const string DEFAULT_SETTINGS_FILE = "appsettings-default.json";
    private const string SETTINGS_APP_SECTION = "ServiceManagerConfig";
    private const string SETTINGS_LOG_SECTION = "LogConfig";
    // Generate a cancellation token that can be used by longer running tasks to cancel on break or for other reasons
    private static readonly CancellationTokenSource CancelTokenSource = new();

    // Main CLI definition and invocation
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
            args = [ "service" ];

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
        }

        Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Cancel requested");
            e.Cancel = true;
            CancelTokenSource.Cancel();
        };

        // Setup commandline commands, options, handlers
        var cmd = new RootCommand();

        var serviceVerb = new Command("service", "Runs service, which runs commands and/or processes by configured schedule");
        serviceVerb.SetHandler(() => HandleService());

        var versionVerb = new Command("version", "Build version for the application.");
        versionVerb.SetHandler(() => HandleVersion());

        cmd.Add(serviceVerb);
        cmd.Add(versionVerb);

        try
        {
            return await cmd.InvokeAsync(args);
        }
        finally
        {
            CancelTokenSource.Dispose();
        }
    }

    private static void RunAppBuilder(IConsoleAppHostArgs args)
    {
        var configFilePath = ConsoleAppUtils.DetermineConfigFilePath(SETTINGS_FILE, DEFAULT_SETTINGS_FILE, APP_FOLDER);
        ILogger startLogger = null;
        try
        {
            ConsoleAppHostBuilder.CreateDefaultConsoleAppHost<ServiceManager>
            (
                (services, config) =>
                {
                    try
                    {
                        var serviceManagerConfig = new ServiceManagerConfig(config, configFilePath);
                        services.AddSingleton(serviceManagerConfig);
                        services.AddTransient<ScheduleData>();
                        services.AddTransient<EventLogger>();
                        services.AddSingleton<IJobStatusService, JobStatusService>();
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
                            configureOptions.RunConfig.DisableInitialConnection = true;

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
                        logger.LogCritical(ex, "{Msg}", msg);
                        throw new InitializationException(msg, ex);
                    }
                },
                SETTINGS_FILE,
                SETTINGS_APP_SECTION,
                SETTINGS_LOG_SECTION
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
                startLogger.LogCritical(ex, "Service Manager initialization error: {Msg}", ex.Message);
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

    private static void HandleService()
    {
        RunAppBuilder(ServiceRuntimeConfig.GetArgs(CancelTokenSource.Token));
    }

    #endregion
}
