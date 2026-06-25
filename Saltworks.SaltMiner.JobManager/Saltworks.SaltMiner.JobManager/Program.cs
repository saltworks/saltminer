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
using Saltworks.SaltMiner.ConsoleApp.Core;
using System.CommandLine;
using Saltworks.SaltMiner.DataClient;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Common;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.JobManager.Processor.Engagement;
using Saltworks.SaltMiner.JobManager.Processor.CleanUp;
using Saltworks.SaltMiner.JobManager.Helpers;

namespace Saltworks.SaltMiner.JobManager;
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
    private const string APP_FOLDER = "jobmanager";
    private const string SETTINGS_FILE = "appsettings.json";
    private const string DEFAULT_SETTINGS_FILE = "appsettings-default.json";
    private const string DEFAULT_TEMPLATE_FOLDER = "TemplateDefaults";
    private const string TEMPLATE_FOLDER = "report-templates";
    private const string SETTINGS_APP_SECTION = "JobManagerConfig";
    private const string SETTINGS_LOG_SECTION = "LogConfig";
    // Generate a cancellation token that can be used by longer running tasks to cancel on break or for other reasons
    private static readonly CancellationTokenSource CancelTokenSource = new();

    // Main CLI definition and invocation
    [STAThread]
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
            args = [ "service" ];

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
        }

        Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Cancel requested");
            e.Cancel = true;
            CancelTokenSource.Cancel();
        };

        var cmd = ConfigureCliCommands();
        var ret = await cmd.InvokeAsync(args);
        CancelTokenSource.Dispose();
        return ret;
    }

    private static RootCommand ConfigureCliCommands()
    {
        // NOTE: repetitive are the options for the main operations, but expected to diverge over time are they, so repeat them we did for now.
        //          .--.
        //::\`--._,'.::.`._.--'/::::
        //::::.  ` __::__ '  .::::::
        //::::::-:.`'..`'.:-::::::::
        //::::::::\ `--' /::::::::::

        RootCommand cmd = [];

        // Service CMD
        var serviceVerb = new Command("service", "Run job manager as a service.");
        serviceVerb.SetHandler(() => HandleService());

        var importVerb = new Command("import", "File import helper");

        // Pentest Issue Import CMD
        var penIssueImportVerb = new Command("issue", "Runs pentest issue import processor, which imports the engagement issues of uploaded files.");
        penIssueImportVerb.SetHandler(() => HandlePenIssueImport());

        // Pentest Template Issue Import CMD
        var penTemplateIssueImportVerb = new Command("templateissue", "Runs pentest issue import processor, which imports the engagement issues of uploaded files.");
        penTemplateIssueImportVerb.SetHandler(() => HandlePenTemplateIssueImport());

        // Engagement Import CMD
        var engagementImportVerb = new Command("engagement", "Runs engagement import processor, which imports engagements of uploaded files.");
        engagementImportVerb.SetHandler(() => HandleEngagementImport());

        importVerb.Add(penIssueImportVerb);
        importVerb.Add(penTemplateIssueImportVerb);
        importVerb.Add(engagementImportVerb);

        // Engagement Report CMD
        var engagementReportVerb = new Command("engagementreport", "Runs engagement report processor, which processes a queue of engagement reports to be created.");
        var engagementReportListOnly = new Option<bool>(["--list-only", "-l"], description: "List queued reports without processing them.");
        engagementReportVerb.Add(engagementReportListOnly);
        engagementReportVerb.SetHandler((listOnly) => HandleEngagementReport(listOnly), engagementReportListOnly);

        // Report Template Upload CMD
        var reportTemplateVerb = new Command("reporttemplate", "Runs template processor, which processes templates in folder to elastic.");
        var templateListOnly = new Option<bool>(["--list-only", "-l"], description: "List the report templates without processing them.");
        reportTemplateVerb.Add(templateListOnly);
        reportTemplateVerb.SetHandler((listOnly) => HandleTemplate(listOnly), templateListOnly);

        // Clean Up CMD
        var cleanUpVerb = new Command("cleanup", "Runs clean up processor, which deletes old job queues by day limit defined in Job Manager config settings as 'CleanupQueueAfterDays'.");
        var cleanUpListOnlyOption = new Option<bool>(["--list-only", "-l"], description: "List job queues to clean up without processing them.");

        cleanUpVerb.Add(cleanUpListOnlyOption);
        cleanUpVerb.SetHandler((listOnly) => HandleCleanUp(listOnly), cleanUpListOnlyOption);

        // Version CMD
        var verisonVerb = new Command("version", "Reports build version for the application.");
        verisonVerb.SetHandler(() => HandleVersion());

        cmd.Add(serviceVerb);
        cmd.Add(importVerb);
        cmd.Add(engagementReportVerb);
        cmd.Add(reportTemplateVerb);
        cmd.Add(cleanUpVerb);
        cmd.Add(verisonVerb);

        return cmd;
    }

    private static void CopyDirectoryRecursive(string sourceDir, string destDir)
    {
        // Copy all files in current directory
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Join(destDir, fileName);
            if (!File.Exists(destFile))
                File.Copy(file, destFile);
        }

        // Copy all subdirectories recursively
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(subDir);
            var destSubDir = Path.Join(destDir, dirName);
            if (!Directory.Exists(destSubDir))
                Directory.CreateDirectory(destSubDir);
            
            CopyDirectoryRecursive(subDir, destSubDir);
        }
    }

    // Copies the default report-template folder into the (builder-resolved) config folder if needed.
    private static void EnsureReportTemplates(string configFolder)
    {
        var defaultTemplateFolderPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, DEFAULT_TEMPLATE_FOLDER);
        var templateFolderPath = Path.Join(configFolder, TEMPLATE_FOLDER);

        try
        {
            if (Directory.Exists(defaultTemplateFolderPath))
            {
                if (!Directory.Exists(templateFolderPath))
                    Directory.CreateDirectory(templateFolderPath);

                CopyDirectoryRecursive(defaultTemplateFolderPath, templateFolderPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to copy default template folder to '{templateFolderPath}'. {ex.Message}");
        }
    }

    private static void RunJobManager(IConsoleAppHostArgs args)
    {
        ILogger startLogger = null;
        try
        {
            ConsoleAppHostBuilder.CreateDefaultConsoleAppHost<JobManager, JobManagerConfig>
            (
                (config, configFilePath, configFolder) =>
                {
                    try
                    {
                        EnsureReportTemplates(configFolder);
                        return new JobManagerConfig(config, configFilePath, configFolder);
                    }
                    catch (ConfigBaseEncryptionException ex)
                    {
                        throw new ConfigurationEncryptionException($"Invalid encryption keys or values in configuration.", ex);
                    }
                },
                (services, jobManagerConfig) =>
                {
                    try
                    {
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

                        services.AddDataClientAsSingleton<DataClient.DataClient>(options =>
                        {
                            options.ApiBaseAddress = jobManagerConfig.DataApiBaseUrl;
                            options.ApiKeyHeader = jobManagerConfig.DataApiKeyHeader;
                            options.ApiKey = jobManagerConfig.DataApiKey;
                            options.Timeout = TimeSpan.FromSeconds(jobManagerConfig.DataApiTimeoutSec);
                            options.VerifySsl = jobManagerConfig.DataApiVerifySsl;
                        });
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
                Console.WriteLine($"JobManager initialization error: {ex.Message}");
                Console.WriteLine($"JobManager Logger initialization error");
            }
            else
            {
                startLogger.LogCritical(ex, "JobManager initialization error: {Message}", ex.Message);
            }
        }
    }

    #region CLI Handlers

    private static void HandleVersion()
    {
        var file = "version.txt";

        if (File.Exists(file))
            Console.WriteLine("JobManager version: " + File.ReadAllText(file));
        else
            Console.WriteLine($"Unknown version - '{file}' could not be found.");
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

    #endregion
}