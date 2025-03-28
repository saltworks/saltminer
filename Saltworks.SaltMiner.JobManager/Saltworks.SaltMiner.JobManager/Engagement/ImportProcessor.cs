using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Import;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Responses;

namespace Saltworks.SaltMiner.JobManager.Processor.Engagement
{
    public class ImportProcessor
    {
        private readonly JobManagerConfig Config;
        private readonly ILogger Logger;
        private readonly DataClient.DataClient DataClient;
        private readonly UiApiClient.UiApiClient UiApiClient;
        private readonly EngagementImporter EngagementImporter;
        private EngagementImportRuntimeConfig RunConfig = null;
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
            EngagementImporter = new EngagementImporter(DataClient, Logger);
        }

        /// <summary>
        /// Runs engagement import processing for job queue
        /// </summary>
        public void Run(RuntimeConfig config, UiDataItemResponse<Job> job = null)
        {
            if (config is not EngagementImportRuntimeConfig)
            {
                throw new ArgumentException($"Expected type '{typeof(EngagementImportRuntimeConfig).Name}', but passed value is '{config.GetType().Name}'", nameof(config));
            }

            RunConfig = config.Validate() as EngagementImportRuntimeConfig;

            try
            {
                // todo: add option for Config.ListOnly

                var pendingResult = job ?? UiApiClient.PollPendingJob(Job.JobType.EngagementImport.ToString());
                
                while (pendingResult?.Data != null)
                {
                    JobQueue = pendingResult.Data;

                    IFormFile file = null;
                    EngagementImport engagementImport = null;
                    //download file
                    var fileUrl = JobQueue.FileName;

                    var tempFilePath = Config.FileRepo;
                    Directory.CreateDirectory(tempFilePath);

                    var fileBytes = UiApiClient.DownloadFile($"file/{Path.GetFileName(fileUrl)}");
                    string filePath = Path.Combine(tempFilePath, Path.GetFileName(fileUrl));
                    File.WriteAllBytes(filePath, fileBytes);

                    using (var stream = new FileStream(filePath, FileMode.Open))
                    {
                        // Create an IFormFile object
                        file = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(stream.Name));

                        UpdateJobStatus($"Engagement import started {DateTime.UtcNow:yyyy-MM-dd HH:mm}", Job.JobStatus.Processing);

                        Logger.LogInformation("Importing engagement {EngagementFileName} from job queue", JobQueue.FileName);

                        // get current tested status lookup values and pass to importer process
                        var Lookups = DataClient.LookupSearch(new SearchRequest { }).Data.ToList();
                        var testedDropdowns = Lookups?.FirstOrDefault(x => x.Type == LookupType.TestedDropdown.ToString())?.Values ?? new List<LookupValue>();

                        engagementImport = new EngagementImport()
                        {
                            FromQueue = true,
                            ApiBaseUrl = JobQueue.Attributes["ApiBaseUrl"],
                            AssetType = EngagementImporter.AssetType,
                            ImportBatchSize = Config.EngagementImportCheckoutBatchSize,
                            CreateNew = !JobQueue.Overwrite,
                            File = file,
                            FileRepo = JobQueue.Attributes["FileRepo"],
                            Instance = EngagementImporter.Instance,
                            MaxImportFileSize = 0,
                            SourceType = EngagementImporter.SourceType,
                            UiBaseUrl = JobQueue.Attributes["UiBaseUrl"],
                            TestStatusLookups = testedDropdowns
                        };

                        var newEngagementId = EngagementImporter.ImportEngagementFromFileAsync(engagementImport).GetAwaiter().GetResult();

                        UpdateJobStatus($"Engagement import completed {DateTime.UtcNow:yyyy-MM-dd HH:MM}", Job.JobStatus.Complete, newEngagementId);
                    }

                    Logger.LogInformation("Deleting queue file and downloaded file");
                    File.Delete(filePath);
                    UiApiClient.DeleteFile(Path.GetFileName(fileUrl));

                    if (job != null)
                    {
                        break;
                    }
                    pendingResult = UiApiClient.PollPendingJob(Job.JobType.EngagementImport.ToString());
                }
            }
            catch (CancelTokenException)
            {
                // Already logged, so just do nothing but quit silently
                UpdateJobStatus("Import cancelled.", Job.JobStatus.Error);
            }
            catch (Exception ex)
            {
                UpdateJobStatus(ex.Message, Job.JobStatus.Error);
                Logger.LogError(ex, "Error in engagement import processor: {Msg}", ex.Message);
                throw new JobManagerException($"Error in engagement import processor: {ex.Message}", ex);
            }
        }

        private void UpdateJobStatus(string message, Job.JobStatus status, string engagementId = "")
        {
            if (JobQueue != null)
            {
                JobQueue.Status = status.ToString();
                JobQueue.Message = message;
                JobQueue.TargetId = engagementId;
                DataClient.JobUpdateStatus(JobQueue);
            }
        }
    }
}
