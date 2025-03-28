using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataClient;

namespace Saltworks.SaltMiner.JobManager.Processor.CleanUp
{
    public class CleanUpProcessor
    {
        private readonly JobManagerConfig Config;
        private readonly ILogger Logger;
        private readonly DataClient.DataClient DataClient;
        private CleanUpRuntimeConfig RunConfig;

        public CleanUpProcessor
        (
            JobManagerConfig config,
            ILogger<CleanUpProcessor> logger,
            DataClientFactory<DataClient.DataClient> dataClientFactory
        )
        {
            Config = config;
            Logger = logger;
            DataClient = dataClientFactory.GetClient();
        }

        /// <summary>
        /// Runs clean up processing for job queues that have exceeded the maximum days as assigned
        /// to the setting variable CleanupQueueAfterDays in config settings;
        /// </summary>
        public void Run(RuntimeConfig config)
        {

            if (!(config is CleanUpRuntimeConfig))
            {
                throw new ArgumentException($"Expected type '{nameof(CleanUpRuntimeConfig)}', but passed value is '{config.GetType().Name}'", nameof(config));
            }

            RunConfig = config.Validate() as CleanUpRuntimeConfig;

            Logger.LogInformation("Looking for job queue(s) to clean up, configured for listOnly {listOnly}", RunConfig?.ListOnly);

            try
            {
                CleanUpScansByStatus(Job.JobStatus.Complete);
                CleanUpScansByStatus(Job.JobStatus.Error);
                CleanUpScansByStatus(Job.JobStatus.Processing);
            }
            catch (CancelTokenException)
            {
                // Already logged, so just do nothing but quit silently
            }
            catch (JobManagerException ex)
            {
                Logger.LogError(ex, "Error in CleanUp processor");
                throw;
            }
        }

        private void CleanUpScansByStatus(Job.JobStatus jobStatus)
        {
            var jobQueueRequest = new SearchRequest()
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string>()
                    {
                        { "Status", jobStatus.ToString("g")},
                        { "Timestamp", SaltMiner.DataClient.Helpers.BuildLessThanFilterValue($"{DateTime.Now.AddDays(-1 * Config.CleanupQueueAfterDays).ToString("yyyy-MM-dd")}") }
                    },
                    AnyMatch = false
                },
                PitPagingInfo = new PitPagingInfo(Config.CleanupProcessorBatchSize, false)
            };

            if (RunConfig.ListOnly)
            {
                // for listing only, just query all and display the count. Job queue should never be too big
                jobQueueRequest.PitPagingInfo = null;
                var outdatedJobQueuesList = DataClient.JobSearch(jobQueueRequest).Data.ToList();
                Logger.LogInformation("{count} job queue(s) with status '{status}' will be removed", outdatedJobQueuesList.Count, jobStatus.ToString("g"));
            }
            else
            {
                var count = 0;
                IEnumerable<Job> outdatedJobQueues = DataClient.JobSearch(jobQueueRequest).Data;
                do
                {
                    if (!outdatedJobQueues.Any())
                    {
                        break;
                    }

                    foreach (var jobQueue in outdatedJobQueues)
                    {
                        CheckCancel();
                        if (!RunConfig.ListOnly)
                        {
                            DataClient.JobDelete(jobQueue.Id);
                        }
                    }

                    count += Config.CleanupProcessorBatchSize;

                    Logger.LogInformation("{count} job queue(s) with status '{status}' removed", outdatedJobQueues.Count(), jobStatus.ToString("g"));
                    DataClient.RefreshIndex(QueueScan.GenerateIndex());

                    if (Config.CleanupProcessorBatchDelayMs > 0)
                    {
                        Thread.Sleep(Config.CleanupProcessorBatchDelayMs);
                    }

                    outdatedJobQueues = DataClient.JobSearch(jobQueueRequest).Data;

                } while (outdatedJobQueues.Any());
            }
        }

        private void CheckCancel(bool readyToAbort = true)
        {
            if (RunConfig.CancelToken.IsCancellationRequested)
            {
                if (readyToAbort)
                {
                    Logger.LogInformation("Cancellation requested, aborting clean up process.");
                    throw new CancelTokenException();
                }
                if (!RunConfig.CancelRequestedReported)
                {
                    Logger.LogInformation("Cancellation requested, finishing current clean up process");
                    RunConfig.CancelRequestedReported = true;
                }
            }
        }
    }

}
