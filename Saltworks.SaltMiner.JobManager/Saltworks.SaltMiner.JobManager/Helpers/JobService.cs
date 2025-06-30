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
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.ConsoleApp.Core;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.JobManager.Processor.CleanUp;
using Saltworks.SaltMiner.JobManager.Processor.Engagement;
using Saltworks.SaltMiner.UiApiClient;

namespace Saltworks.SaltMiner.JobManager.Helpers
{
    public class JobService
    {
        private readonly ILogger Logger;
        private readonly IServiceProvider ServiceProvider;
        private readonly UiApiClient.UiApiClient UiApiClient;
        private bool processing = false;

        public JobService
        (
            ILogger<JobService> logger,
            IServiceProvider serviceProvider,
            UiApiClientFactory<JobManager> UiApiClientFactory
        )
        {
            Logger = logger;
            ServiceProvider = serviceProvider;
            UiApiClient = UiApiClientFactory.GetClient();
        }

        public void ProcessJobs(IConsoleAppHostArgs args)
        {
            try
            {
                Logger.LogInformation("Reading job queue");

                if (processing) return;

                var impendingResult = UiApiClient.PollPendingJob();
                var pendingResult = new UiApiClient.Responses.UiDataItemResponse<Job>(impendingResult.Data, impendingResult);

                var rptTemplateProcessor = ServiceProvider.GetRequiredService<ReportTemplateProcessor>();
                var rptTemplateArgs = ReportTemplateRuntimeConfig.GetArgs(false, args.CancelToken);
                rptTemplateProcessor.Run(ReportTemplateRuntimeConfig.FromArgs(rptTemplateArgs), pendingResult);

                var cleanUpProcessor = ServiceProvider.GetRequiredService<CleanUpProcessor>();
                var cleanUpArgs = CleanUpRuntimeConfig.GetArgs(false, args.CancelToken);
                cleanUpProcessor.Run(CleanUpRuntimeConfig.FromArgs(cleanUpArgs));


                if (pendingResult?.Data != null)
                {
                    processing = true;
                    Logger.LogInformation("Processing job type {JobType} started", pendingResult.Data.Type);

                    if (pendingResult.Data.Type == Job.JobType.EngagementReport.ToString("g"))
                    {
                        var processor = ServiceProvider.GetRequiredService<ReportProcessor>();
                        var consoleAppArgs = EngagementReportRuntimeConfig.GetArgs(false, args.CancelToken);
                        processor.Run(EngagementReportRuntimeConfig.FromArgs(consoleAppArgs), pendingResult);
                    }
                    if (pendingResult.Data.Type == Job.JobType.PenIssuesImport.ToString("g"))
                    {
                        var processor = ServiceProvider.GetRequiredService<Processor.Issue.ImportProcessor>();
                        processor.Run(PenIssueImportRuntimeConfig.FromArgs(args), pendingResult);
                    }
                    if (pendingResult.Data.Type == Job.JobType.PenTemplateIssuesImport.ToString("g"))
                    {
                        var processor = ServiceProvider.GetRequiredService<Processor.Issue.TemplateImportProcessor>();
                        processor.Run(PenTemplateIssueImportRuntimeConfig.FromArgs(args), pendingResult);
                    }
                    if (pendingResult.Data.Type == Job.JobType.EngagementImport.ToString("g"))
                    {
                        var processor = ServiceProvider.GetRequiredService<ImportProcessor>();
                        processor.Run(EngagementImportRuntimeConfig.FromArgs(args), pendingResult);
                    }

                    processing = false;
                    Logger.LogInformation("Processing job type {JobType} completed", pendingResult.Data.Type);
                }
            }
            catch (DataClientException ex)
            {
                processing = false;
                Logger.LogError(ex, "DataClientException: {Msg}", ex.Message);
            }
            catch (Exception ex)
            {
                processing = false;
                Logger.LogError(ex, "Error in read job queue: {Msg}", ex.Message);
                throw new JobManagerException($"Error in read job queue: [{ex.GetType().Name}] {ex.Message}");
            }
        }
    }
}