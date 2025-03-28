using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.UiApiClient.Import;

namespace Saltworks.SaltMiner.JobManager.Processor.Issue;

public class BaseIssueImportProcessor
{
    public void Process(JobManagerConfig jobConfig, IssueImporter issueImporter, Job jobQueue, UiApiClient.UiApiClient uiClient, DataClient.DataClient dataClient, ILogger logger)
    {
        IFormFile file = null;
        IssueImporterRequest issueImport = null;

        var isTemplate = bool.Parse(jobQueue.Attributes["IsTemplate"]);

        //download file
        var fileUrl = jobQueue.FileName;

        var tempFilePath = jobConfig.FileRepo;
        Directory.CreateDirectory(tempFilePath);

        var fileBytes = uiClient.DownloadFile($"file/{Path.GetFileName(fileUrl)}");
        logger.LogInformation("Downloaded file {Url}. Received {Len} total bytes", fileUrl, fileBytes.Length);
        string filePath = Path.Combine(tempFilePath, Path.GetFileName(fileUrl));
        File.WriteAllBytes(filePath, fileBytes);

        // get current tested status lookup values and pass to importer process
        var Lookups = dataClient.LookupSearch(new SearchRequest()).Data.ToList();
        var testedDropdowns = Lookups?.FirstOrDefault(x => x.Type == LookupType.TestedDropdown.ToString())?.Values ?? [];

        using (var stream = new FileStream(filePath, FileMode.Open))
        {
            // Create an IFormFile object
            file = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(stream.Name));

            UpdateJobStatus(dataClient, jobQueue, $"Issue import started {DateTime.UtcNow:yyyy-MM-dd HH:mm}", Job.JobStatus.Processing);

            if (isTemplate)
            {
                logger.LogInformation("Importing Pentest template issue {IssueFileName} from job queue", jobQueue.FileName);
            }
            else
            {
                logger.LogInformation("Importing Pentest issue {IssueFileName} from job queue", jobQueue.FileName);
            }

            issueImport = new IssueImporterRequest()
            {
                FromQueue = true,
                AssetType = BaseImporter.AssetType,
                SourceType = BaseImporter.SourceType,
                Instance = BaseImporter.Instance,
                ImportBatchSize = jobConfig.IssueImportCSVBatchSize,
                RequiredCsvAssetHeaders = jobConfig.IssueImportRequiredCsvAssetHeaders,
                RequiredCsvIssueHeaders = jobConfig.IssueImportRequiredCsvIssueHeaders,
                Regex = jobQueue.Attributes["Regex"],
                DefaultQueueAssetId = jobQueue.Attributes["DefaultQueueAssetId"],
                File = file,
                EngagementId = jobQueue.TargetId,
                UserName = jobQueue.User,
                UserFullName = jobQueue.UserFullName,
                IsTemplate = isTemplate,
                FileRepo = jobQueue.Attributes["FileRepo"],
                LastScanDaysPolicy = jobQueue.Attributes["LastScanDaysPolicy"],
                MaxImportFileSize = 0,
                TemplatePath = jobConfig.TemplateRepository,
                UiBaseUrl = jobQueue.Attributes["UiBaseUrl"],
                FailedRegexSplat = jobQueue.Attributes["FailedRegexSplat"],
                TestStatusLookups = testedDropdowns
            };

            issueImporter.ProcessImport(issueImport);

            UpdateJobStatus(dataClient, jobQueue, $"Issue import completed {DateTime.UtcNow:yyyy-MM-dd HH:mm}", Job.JobStatus.Complete);
        }

        logger.LogInformation("Deleting queue file and downloaded file");
        File.Delete(filePath);
        uiClient.DeleteFile(Path.GetFileName(fileUrl));
    }

    private static void UpdateJobStatus(DataClient.DataClient dataClient, Job jobQueue, string message, Job.JobStatus status)
    {
        if (jobQueue != null)
        {
            jobQueue.Status = status.ToString();
            jobQueue.Message = message;
            dataClient.JobUpdateStatus(jobQueue);
        }
    }
}
