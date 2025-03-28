using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Import;
using Saltworks.SaltMiner.UiApiClient.Responses;

namespace Saltworks.SaltMiner.JobManager.Processor.Issue
{
    public class TemplateImportProcessor : BaseIssueImportProcessor
    {
        private readonly JobManagerConfig Config;
        private readonly ILogger Logger;
        private readonly DataClient.DataClient DataClient;
        private readonly UiApiClient.UiApiClient UiApiClient;
        private readonly IssueImporter IssueImporter;
        private Job JobQueue;

        public TemplateImportProcessor
        (
            JobManagerConfig config,
            ILogger<TemplateImportProcessor> logger,
            DataClientFactory<DataClient.DataClient> dataClientFactory,
            UiApiClientFactory<JobManager> UiApiClientFactory
        )
        {
            Logger = logger;
            DataClient = dataClientFactory.GetClient();
            UiApiClient = UiApiClientFactory.GetClient();
            Config = config;
            IssueImporter = new IssueImporter(DataClient, Logger);
        }

        /// <summary>
        /// Runs Pentest template issue import processing for job queue.
        /// </summary>
        public void Run(RuntimeConfig config, UiDataItemResponse<Job> job = null)
        {
            if (config is not PenTemplateIssueImportRuntimeConfig)
            {
                throw new ArgumentException($"Expected type '{typeof(PenTemplateIssueImportRuntimeConfig).Name}', but passed value is '{config.GetType().Name}'", nameof(config));
            }

            try
            {
                // todo: add option for Config.ListOnly

                var pendingResult = job ?? UiApiClient.PollPendingJob(Job.JobType.PenTemplateIssuesImport.ToString());

                while (pendingResult?.Data != null)
                {
                    JobQueue = pendingResult.Data;

                    Process(Config, IssueImporter, JobQueue, UiApiClient, DataClient, Logger);

                    if (job != null)
                    {
                        break;
                    }
                    pendingResult = UiApiClient.PollPendingJob(Job.JobType.PenTemplateIssuesImport.ToString());
                }
            }
            catch (CancelTokenException)
            {
                UpdateJobStatus("Import cancelled.", Job.JobStatus.Error);
            }
            catch (Exception ex)
            {
                UpdateJobStatus(ex.Message, Job.JobStatus.Error);
                Logger.LogError(ex, "Error in Template issue import processor : [{Type}] {Msg}", ex.GetType().Name, ex.Message);
                throw new JobManagerException($"Template issue import processor error: [{ex.GetType().Name}] {ex.Message}", ex);
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
