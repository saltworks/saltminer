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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.ConsoleApp.Core;
using Saltworks.SaltMiner.JobManager.Helpers;
using Saltworks.SaltMiner.JobManager.Processor.CleanUp;
using Saltworks.SaltMiner.JobManager.Processor.Engagement;

namespace Saltworks.SaltMiner.JobManager
{
    public enum OperationType { None, IssueImport, TemplateIssueImport, EngagementImport, EngagementReport, ReportTemplate, Cleanup, Service }

    public class JobManager : BackgroundService, IConsoleAppHost
    {
        private readonly ILogger Logger;
        private readonly JobManagerConfig Config;
        private readonly JobService JobService;
        private readonly IServiceProvider ServiceProvider;
        private static IConsoleAppHostArgs HostArgs;

        // Dependencies are injected via dependency injection, default logging and configuration available, and any customs specified in the builder
        public JobManager(ILogger<JobManager> logger, JobManagerConfig config, JobService jobService, IServiceProvider serviceProvider)
        {
            Logger = logger;
            Config = config;
            JobService = jobService;
            ServiceProvider = serviceProvider;
            Logger.LogInformation("Initialized...");
        }

        // This class must implement IConsoleAppHost.Run so it can be run by the builder
        // With the addition of the CLI, args can be assembled to pass into this run (see Program.cs)
        public void Run(IConsoleAppHostArgs args)
        {
            HostArgs = args;
            try
            {
                Logger.LogInformation("Data client is using base url '{url}'", Config.DataApiBaseUrl);

                if (args.Args[0] == OperationType.Service.ToString("g"))
                {
                    Logger.LogInformation("Service starting.  Data API client is using base url '{dataApiBaseUrl}'", Config.DataApiBaseUrl);
                    var runConfig = ServiceRuntimeConfig.FromArgs(args);
                    ExecuteAsync(runConfig.CancelToken).Wait();
                }

                if (args.Args[0] == OperationType.IssueImport.ToString("g"))
                {
                    var processor = ServiceProvider.GetRequiredService<Processor.Issue.ImportProcessor>();
                    processor.Run(PenIssueImportRuntimeConfig.FromArgs(args));
                }

                if (args.Args[0] == OperationType.TemplateIssueImport.ToString("g"))
                {
                    var processor = ServiceProvider.GetRequiredService<Processor.Issue.TemplateImportProcessor>();
                    processor.Run(PenTemplateIssueImportRuntimeConfig.FromArgs(args));
                }

                if (args.Args[0] == OperationType.EngagementImport.ToString("g"))
                {
                    var processor = ServiceProvider.GetRequiredService<ImportProcessor>();
                    processor.Run(EngagementImportRuntimeConfig.FromArgs(args));
                }

                if (args.Args[0] == OperationType.EngagementReport.ToString("g"))
                {
                    var processor = ServiceProvider.GetRequiredService<ReportProcessor>();
                    processor.Run(EngagementReportRuntimeConfig.FromArgs(args));
                }

                if (args.Args[0] == OperationType.ReportTemplate.ToString("g"))
                {
                    var processor = ServiceProvider.GetRequiredService<ReportTemplateProcessor>();
                    processor.Run(ReportTemplateRuntimeConfig.FromArgs(args));
                }

                if (args.Args[0] == OperationType.Cleanup.ToString("g"))
                {
                    var processor = ServiceProvider.GetRequiredService<CleanUpProcessor>();
                    processor.Run(CleanUpRuntimeConfig.FromArgs(args));
                }

                if (args.Args[0] == OperationType.None.ToString("g"))
                {
                    Logger.LogInformation("As requested, doing nothing...");
                }

                Logger.LogInformation("{processor} processor complete.", args.Args[0]);

                // Until we get to a scheduler, we'll run once only
                // ProcEm(args, Config);
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogWarning(ex, "Process cancellation requested, stopping.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unhandled exception caught in Job Manager: [{name}] {message}", ex.GetType().Name, ex.Message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    JobService.ProcessJobs(HostArgs);
                    
                    await Task.Delay(Config.ServiceProcessorIntervals["ImportJobs"] * 1000, stoppingToken);
                }
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogInformation(ex, "Service stop requested, stopping.");
            }
        }
    }
}