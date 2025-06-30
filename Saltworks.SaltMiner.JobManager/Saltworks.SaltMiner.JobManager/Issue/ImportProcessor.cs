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

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Import;
using Saltworks.SaltMiner.UiApiClient.Responses;

namespace Saltworks.SaltMiner.JobManager.Processor.Issue
{
    public class ImportProcessor : BaseIssueImportProcessor
    {
        private readonly JobManagerConfig Config;
        private readonly ILogger Logger;
        private readonly DataClient.DataClient DataClient;
        private readonly UiApiClient.UiApiClient UiApiClient;
        private readonly IssueImporter IssueImporter;
        private Job JobQueue;

        public ImportProcessor
        (
            JobManagerConfig config,
            ILogger<ImportProcessor> logger,
            DataClientFactory<DataClient.DataClient> dataClientFactory,
            UiApiClientFactory<JobManager> UiApiClientFactory
        )
        {
            Config = config;
            Logger = logger;
            DataClient = dataClientFactory.GetClient();
            UiApiClient = UiApiClientFactory.GetClient();
            IssueImporter = new IssueImporter(DataClient, Logger);
        }

        /// <summary>
        /// Runs Pentest issue import processing for job queue.
        /// </summary>
        public void Run(RuntimeConfig config, UiDataItemResponse<Job> job = null)
        {
            if (config is not PenIssueImportRuntimeConfig)
            {
                throw new ArgumentException($"Expected type '{typeof(PenIssueImportRuntimeConfig).Name}', but passed value is '{config.GetType().Name}'", nameof(config));
            }

            try
            {
                // todo: add option for Config.ListOnly

                var pendingResult = job ?? UiApiClient.PollPendingJob(Job.JobType.PenIssuesImport.ToString());

                while (pendingResult?.Data != null)
                {
                    JobQueue = pendingResult.Data;

                    Process(Config, IssueImporter, JobQueue, UiApiClient, DataClient, Logger);

                    if (job != null)
                    {
                        break;
                    }
                    pendingResult = UiApiClient.PollPendingJob(Job.JobType.PenIssuesImport.ToString());
                }
            }
            catch (CancelTokenException)
            {
                UpdateJobStatus("Import cancelled.", Job.JobStatus.Error);
            }
            catch (Exception ex)
            {
                UpdateJobStatus(ex.Message, Job.JobStatus.Error);
                Logger.LogError(ex, "Error in Issue import processor : [{Type}] {Msg}", ex.GetType().Name, ex.Message);
                throw new JobManagerException($"Issue import processor error: [{ex.GetType().Name}] {ex.Message}", ex);
            }
        }

        private void UpdateJobStatus(string message, Job.JobStatus status)
        {
            if (JobQueue != null)
            {
                JobQueue.Status = status.ToString();
                JobQueue.Message = message;
                DataClient.JobUpdateStatus(JobQueue);
            }
        }
    }
}
