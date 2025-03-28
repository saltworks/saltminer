using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.ViewModels;
using System.IO.Compression;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.UiApiClient.Import
{
    public class EngagementImporter(DataClient.DataClient dataClient, ILogger logger) : BaseImporter(dataClient, logger)
    {
        private readonly string FullEngagementFile = "FullEngagement.json";

        public async Task<string> ImportEngagementFromFileAsync(EngagementImport importRequest)
        {
            Logger.LogInformation("Import initiated");
            var fileStream = importRequest.File.OpenReadStream();
            var fullEngagement = new EngagementFull();
            var tempDirectory = Guid.NewGuid().ToString();

            if (importRequest.File.Length > importRequest.MaxImportFileSize && !importRequest.FromQueue)
            {
                // upload the file and put it in a job queue to be processed by outside process
                var filePath = await FileHelper.CreateFileAsync(importRequest.File, importRequest.UserName, importRequest.UserFullName, importRequest.FileRepo);
                var response = QueueEngagementImportFile(filePath, importRequest);
                Logger.LogInformation("Engagement file sent to job queue id {JobId}", response.Id);

                // delete temp files created when reading zip
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, true);

                // send back invalid engagement id to signal it was put in queue
                return "0";
            }

            Logger.LogInformation($"Reading zip file");
            try
            {
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
                Directory.CreateDirectory(tempDirectory);

                foreach (var entry in archive.Entries)
                {
                    if (entry.Name == FullEngagementFile)
                    {
                        fullEngagement = UpgradeTool.UpgradeEngagementImport(ReadZipEntry(entry), importRequest).ToEngagementFull();
                    }
                    else
                    {
                        try
                        {
                            var filePath = Path.Combine(tempDirectory, Path.GetFileName(entry.Name));
                            entry.ExtractToFile(filePath, true);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("is denied"))
                            {
                                continue;
                            }
                            throw;
                        }
                    }
                }

                if (string.IsNullOrEmpty(fullEngagement.Id))
                {
                    throw new UiApiClientValidationMissingValueException($"File {FullEngagementFile} not found in submitted zip file.");
                }
            }
            catch (Exception ex)
            {
                var message = $"Error Processing Zip File '{importRequest.File.FileName}' Error: ";

                message += ex.Message;

                if (ex?.InnerException?.Message != null)
                {
                    message += ex.InnerException.Message;
                }

                Logger.LogError(ex, "{Msg}", message);

                throw new UiApiClientValidationException(message);
            }

            Logger.LogInformation($"Updating engagement data");
            if (fullEngagement != null)
            {
                try
                {
                    // cannot import engagements that are published
                    if (fullEngagement.Status == EngagementStatus.Published.ToString("g"))
                    {
                        var msg = "Cannot import a published engagement. It must be a draft to import.";
                        Logger.LogError("{Msg}", msg);
                        throw new UiApiClientValidationException(msg);
                    }

                    try
                    {
                        var queueScan = DataClient.QueueScanGet(fullEngagement.Scan.ScanId);
                        if (!queueScan.Success || queueScan.Data?.Id == null || queueScan.Data.Saltminer.Internal.QueueStatus != QueueScanStatus.Loading.ToString("g"))
                        {
                            importRequest.CreateNew = true;
                        }
                    }
                    catch(Exception ex)
                    {
                        Logger.LogInformation(ex, "No queue scan found for {ScanId}", fullEngagement.Scan.ScanId);
                    }

                    if (importRequest.CreateNew)
                    {
                        Logger.LogDebug("Creating New Engagement and Scan");
                        ImportNewEngagement(fullEngagement, importRequest);
                        Logger.LogDebug("Creating New Assets");
                        ImportNewQueueAssets(fullEngagement, importRequest);
                        Logger.LogDebug("Created ({Count}) New Assets", fullEngagement.Assets.Count);
                        Logger.LogDebug("Creating New Issues");
                        ImportNewQueueIssues(fullEngagement, importRequest);
                        Logger.LogDebug("Created ({Count}) New Issues", fullEngagement.Issues.Count);
                    }
                    else
                    {
                        Logger.LogDebug("Importing Engagement");
                        ImportEngagement(fullEngagement);
                        Logger.LogDebug("Importing Scan");
                        ImportQueueScans(fullEngagement, importRequest);
                        Logger.LogDebug("Importing Batched Assets");
                        ImportQueueAssets(fullEngagement, importRequest);
                        Logger.LogDebug("Imported ({Count}) New Assets", fullEngagement.Assets.Count);
                        Logger.LogDebug("Importing Batched Issues");
                        ImportQueueIssues(fullEngagement, importRequest);
                        Logger.LogDebug("Imported ({Count}) New Assets", fullEngagement.Issues.Count);
                    }

                    Logger.LogDebug("Adding Attachments");
                    await AttachmentHelper.CloneEngagementAttachmentsAsync(fullEngagement.Id, fullEngagement.Attachments?.Where(x => x.IssueId == null).ToList(), fullEngagement.Attributes, importRequest.UserName, importRequest.UserFullName, importRequest.FileRepo, importRequest.ApiBaseUrl, tempDirectory);
                    await CloneIssuesAttachmentsAsync(fullEngagement.Id, fullEngagement.Issues, fullEngagement.Attachments?.Where(x => x.IssueId != null).ToList(), importRequest.UserName, importRequest.UserFullName, importRequest.FileRepo, importRequest.ApiBaseUrl, tempDirectory);
                    Logger.LogDebug("Added ({Count}) Attachments", fullEngagement.Attachments.Count);

                    Logger.LogDebug("Adding Comments");
                    ImportComments(fullEngagement, importRequest.CreateNew);
                    Logger.LogDebug("Added ({Count}) Comments", fullEngagement.Comments.Count);
                }
                catch(UiApiClientValidationReferentialIntegrityException ex)
                {
                    Logger.LogCritical(ex, "Child Ids presented do not reference current engagement {Id}", fullEngagement.Id);
                }
                catch (UiApiClientValidationMissingValueException ex)
                {
                    Logger.LogCritical(ex, $"Child Ids must be present");
                }
            }
            else
            {
                throw new UiApiNotFoundException("Engagement not found in data");
            }

            Logger.LogInformation("Import complete");

            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }

            return fullEngagement.Id;
        }

        private Job QueueEngagementImportFile(string filePath, EngagementImport importRequest)
        {
            Logger.LogInformation("Queue engagement import file initiated");

            try
            {
                var attributes = new Dictionary<string, string>
                {
                    {"ApiBaseUrl", importRequest.ApiBaseUrl},
                    {"UiBaseUrl", importRequest.UiBaseUrl},
                    {"FileRepo", importRequest.FileRepo },
                };

                //add to job queue
                var job = new Job
                {
                    Status = Job.JobStatus.Pending.ToString("g"),
                    Type = Job.JobType.EngagementImport.ToString("g"),
                    FileName = filePath,
                    Overwrite = !importRequest.CreateNew,
                    Message = string.Empty,
                    Attributes = attributes,
                    User = importRequest.UserName,
                    UserFullName = importRequest.UserFullName
                };

                return DataClient.JobAddUpdate(job).Data;
            }
            catch (Exception ex)
            {
                throw new UiApiClientValidationException("Error sending engagement file to queue", ex);
            }
        }

        private void ImportEngagement(EngagementFull fullEngagement)
        {
            if (string.IsNullOrEmpty(fullEngagement.Id))
            {
                throw new UiApiClientValidationMissingValueException("Engagement must have a ID");
            }
            var engagement = fullEngagement.ImportEngagement();
            engagement.Saltminer.Engagement.Attributes = EngagementHelper.FilterInternalAndMergeAttributes(engagement.Saltminer.Engagement.Attributes, engagement.Id);
            try
            {
                DataClient.EngagementDelete(engagement.Id);
            }
            catch (Exception ex)
            {
                Logger.LogInformation(ex, "The engagement '{Id}' does not exist will create new", engagement.Id);
            }
            DataClient.EngagementAddUpdate(engagement);
            DataClient.RefreshIndex(Engagement.GenerateIndex());
        }

        private void ImportQueueScans(EngagementFull fullEngagement, EngagementImport importRequest)
        {
            if (string.IsNullOrEmpty(fullEngagement.Scan.ScanId))
            {
                throw new UiApiClientValidationMissingValueException("Engagement must have a ID");
            }

            var queueScans = new List<QueueScan>() { fullEngagement.ImportQueueScan(importRequest.SourceType, importRequest.AssetType, importRequest.Instance) }.ToList();
            if (queueScans != null && queueScans.Count > 0)
            {
                try
                {
                    var currentQueueScan = DataClient.QueueScanGet(queueScans[0].Id);
                    if (currentQueueScan.Success && currentQueueScan.Data != null && currentQueueScan.Data.Saltminer.Engagement.Id != fullEngagement.Id)
                    {
                        throw new UiApiClientValidationReferentialIntegrityException($"Queue Scan '{queueScans[0].Id}' does not belong to Engagement '{fullEngagement.Id}'");
                    }
                } 
                catch(Exception ex)
                {
                    if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.LogInformation(ex, "The queue scan '{Id}' does not exist will create new", queueScans[0].Id);
                    }
                    else
                    {
                        throw new UiApiClientImportException($"Failure when looking up queue scan for ID '{queueScans[0].Id}'", ex);
                    }
                }

                DataClient.QueueScanAddUpdateBulk(queueScans);
            }

            DataClient.RefreshIndex(QueueScan.GenerateIndex());
        }

        private void ImportQueueAssets(EngagementFull fullEngagement, EngagementImport importRequest)
        {
            var queueAssets = fullEngagement.ImportQueueAssets(importRequest.SourceType, importRequest.AssetType, importRequest.Instance, importRequest.InventoryAssetKeyAttribute);
            if (queueAssets != null && queueAssets.Count > 0)
            {
                BulkAddAssets(queueAssets, fullEngagement.Id, importRequest.ImportBatchSize);
            }

            DataClient.RefreshIndex(QueueAsset.GenerateIndex());
        }

        private void ImportQueueIssues(EngagementFull fullEngagement, EngagementImport importRequest)
        {
            foreach (var status in fullEngagement.Issues.Select(i => i.TestStatus))
            {
                status.Value = EngagementHelper.ValidateTestStatus(status.Value, importRequest.TestStatusLookups);
            }
            var queueIssues = fullEngagement.ImportQueueIssues(importRequest.UiBaseUrl);
            if (queueIssues != null && queueIssues.Count > 0)
            {
                BulkAddIssues(queueIssues, fullEngagement.Id, importRequest.ImportBatchSize);
            }

            DataClient.RefreshIndex(QueueIssue.GenerateIndex());
        }

        private void ImportComments(EngagementFull fullEngagement, bool createNew = false)
        {
            var comments = fullEngagement.ImportComments(createNew);
            if (comments != null && comments.Count > 0)
            {
                var batch = new List<Comment>();
                foreach (var comment in comments)
                {

                    if (!createNew)
                    {

                        if (string.IsNullOrEmpty(comment.Id))
                        {
                            throw new UiApiClientValidationMissingValueException("Comment must have a ID");
                        }

                        try
                        {
                            var currentQueue = DataClient.CommentGet(comment.Id);
                            if (currentQueue.Success && currentQueue.Data != null && currentQueue.Data.Saltminer.Engagement.Id != fullEngagement.Id)
                            {
                                throw new UiApiClientValidationReferentialIntegrityException($"Comment '{comment.Id}' does not belong to Engagement '{fullEngagement.Id}'");
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                            {
                                Logger.LogInformation(ex, "The comment '{Id}' does not exist will create new", comment.Id);
                            }
                            else
                            {
                                throw new UiApiClientImportException($"Failure retrieving comment with ID '{comment.Id}'", ex);
                            }
                        }
                    }

                    batch.Add(comment);

                    if (batch.Count != 30)
                    {
                        continue;
                    }

                    Logger.LogInformation("Sending ({Count}) batched comments", batch.Count);
                    DataClient.CommentAddUpdateBulk(batch);

                    batch = [];
                }

                if (batch.Count > 0)
                {
                    Logger.LogInformation("Sending ({Count}) batched comments", batch.Count);
                    DataClient.CommentAddUpdateBulk(batch);
                }
            }

            DataClient.RefreshIndex(Comment.GenerateIndex());
        }

        private void ImportNewEngagement(EngagementFull fullEngagement, EngagementImport importRequest)
        {
            if (string.IsNullOrEmpty(fullEngagement.Id))
            {
                throw new UiApiClientValidationMissingValueException("Engagement must have a ID");
            }
            if (string.IsNullOrEmpty(fullEngagement.Scan.ScanId))
            {
                throw new UiApiClientValidationMissingValueException("Scan must have a ID");
            }

            var engagementName = EngagementHelper.CreateUniqueEngagementName(fullEngagement.Name, importRequest.AssetType);
            var engagement = DataClient.EngagementAddUpdate(fullEngagement.ImportNewEngagement(engagementName, fullEngagement.CreateDate)).Data;
            var queueScan = EngagementHelper.CreateEngagementQueueScan(engagement.Saltminer.Engagement.Name, engagement.Id, importRequest.SourceType, importRequest.AssetType, importRequest.Instance, engagement.Saltminer.Engagement.Subtype, engagement.Saltminer.Engagement.Customer);

            DataClient.RefreshIndex(Engagement.GenerateIndex());

            foreach (var comment in fullEngagement.Comments.Where(x => x.ScanId == fullEngagement.Scan.ScanId))
            {
                comment.ScanId = queueScan.Id;
            }
            foreach (var comment in fullEngagement.Comments.Where(x => x.EngagementId == fullEngagement.Id))
            {
                comment.EngagementId = engagement.Id;
            }

            fullEngagement.Id = engagement.Id;
            fullEngagement.Name = engagement.Saltminer.Engagement.Name;
            fullEngagement.Scan = new ScanFull(queueScan, fullEngagement.AppVersion);
            fullEngagement.PublishDate = engagement.Saltminer.Engagement.PublishDate;
            fullEngagement.GroupId = engagement.Saltminer.Engagement.GroupId;
        }

        private void ImportNewQueueAssets(EngagementFull fullEngagement, EngagementImport importRequest)
        {
            var result = new List<AssetFull>();

            foreach (var asset in fullEngagement.Assets)
            {
                if (string.IsNullOrEmpty(asset.AssetId))
                {
                    throw new UiApiClientValidationMissingValueException("Asset must have a ID");
                }

                var newAsset = fullEngagement.ImportNewQueueAsset(asset, importRequest.SourceType, importRequest.AssetType, importRequest.Instance, importRequest.InventoryAssetKeyAttribute);
                var newAssetResult = DataClient.QueueAssetAddUpdate(newAsset);

                foreach (var issue in fullEngagement.Issues.Where(x => x.AssetId == asset.AssetId))
                {
                    issue.AssetId = newAssetResult.Data.Id;
                }

                result.Add(new AssetFull(newAssetResult.Data, fullEngagement.AppVersion));

                foreach (var comment in fullEngagement.Comments.Where(x => x.AssetId == asset.AssetId))
                {
                    comment.AssetId = newAssetResult.Data.Id;
                }
            }

            DataClient.RefreshIndex(QueueAsset.GenerateIndex());

            fullEngagement.Assets = result;
        }

        private void ImportNewQueueIssues(EngagementFull fullEngagement, EngagementImport importRequest)
        {
            var result = new List<IssueFull>();

            foreach (var issue in fullEngagement.Issues)
            {
                if (string.IsNullOrEmpty(issue.Id))
                {
                    throw new UiApiClientValidationMissingValueException("Issue must have a ID");
                }
                if (string.IsNullOrEmpty(issue.AssetId))
                {
                    throw new UiApiClientValidationMissingValueException("Issue must have a Asset ID");
                }

                issue.TestStatus.Value = EngagementHelper.ValidateTestStatus(issue.TestStatus.Value, importRequest.TestStatusLookups);
                var newIssue = fullEngagement.ImportNewQueueIssue(issue, importRequest.UiBaseUrl);
                var newIssueResult = DataClient.QueueIssueAddUpdate(newIssue);

                foreach (var attachment in fullEngagement.Attachments.Where(x => x.IssueId == issue.Id))
                {
                    attachment.IssueId = newIssueResult.Data.Id;
                }

                result.Add(new IssueFull(newIssueResult.Data, fullEngagement.AppVersion));

                foreach (var comment in fullEngagement.Comments.Where(x => x.IssueId == issue.Id))
                {
                    comment.IssueId = newIssueResult.Data.Id;
                }
            }

            DataClient.RefreshIndex(QueueIssue.GenerateIndex());

            fullEngagement.Issues = result;
        }

        private static string ReadZipEntry(ZipArchiveEntry entry)
        {
            using var sr = new StreamReader(entry.Open());
            string content = sr.ReadToEnd();
            return content;
        }

        private void BulkAddIssues(IEnumerable<QueueIssue> queueIssues, string engagementId, int checkoutBatchSize)
        {
            var batch = new List<QueueIssue>();
            foreach (var queue in queueIssues)
            {
                try
                {
                    var currentQueue = DataClient.QueueIssueGet(queue.Id);
                    if (currentQueue.Success && currentQueue.Data != null && currentQueue.Data.Saltminer.Engagement.Id != engagementId)
                    {
                        throw new UiApiClientValidationReferentialIntegrityException($"Queue Issue '{queue.Id}' does not belong to Engagement '{engagementId}'");
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("not found"))
                    {
                        Logger.LogInformation(ex, "The queue issue '{Id}' does not exist will create new", queue.Id);
                    }
                    else
                    {
                        throw new UiApiClientImportException($"Failure when retrieving queue issue id '{queue.Id}'", ex);
                    }
                }

                batch.Add(queue);

                if (batch.Count != checkoutBatchSize)
                {
                    continue;
                }

                Logger.LogInformation("Sending ({Count}) batched issues", batch.Count);
                DataClient.QueueIssuesAddUpdateBulk(batch);

                batch = [];
            }

            // Return the last bucket with all remaining elements
            if (batch.Count > 0)
            {
                Logger.LogInformation("Sending ({Count}) batched issues", batch.Count);
                DataClient.QueueIssuesAddUpdateBulk(batch);
            }
        }

        private void BulkAddAssets(IEnumerable<QueueAsset> queueAssets, string engagementId, int checkoutBatchSize)
        {
            var batch = new List<QueueAsset>();
            foreach (var queue in queueAssets)
            {
                try
                {
                    var currentQueue = DataClient.QueueAssetGet(queue.Id);
                    if (currentQueue.Success && currentQueue.Data != null && currentQueue.Data.Saltminer.Engagement.Id != engagementId)
                    {
                        throw new UiApiClientValidationReferentialIntegrityException($"Queue Asset '{queue.Id}' does not belong to Engagement '{engagementId}'");
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("not found"))
                    {
                        Logger.LogInformation(ex, "The queue asset '{Id}' does not exist will create new", queue.Id);
                    }
                    else
                    {
                        throw new UiApiClientImportException($"Failure when retrieving queue asset id '{queue.Id}'", ex);
                    }
                }

                batch.Add(queue);

                if (batch.Count != checkoutBatchSize)
                {
                    continue;
                }

                Logger.LogInformation("Sending ({Count}) batched assets", batch.Count);
                DataClient.QueueAssetAddUpdateBulk(batch);

                batch = [];
            }

            // Return the last bucket with all remaining elements
            if (batch.Count > 0)
            {
                Logger.LogInformation("Sending ({Count}) batched assets", batch.Count);
                DataClient.QueueAssetAddUpdateBulk(batch);
            }
        }

        private async Task<NoDataResponse> CloneIssuesAttachmentsAsync(string engagementId, List<IssueFull> issues, List<UiAttachment> attachments, string user, string userFullName, string fileRepo, string apiBaseUrl, string directory = null)
        {
            var count = 0;

            if (attachments != null && attachments.Count > 0)
            {
                foreach (var id in issues.Select(i => i.Id))
                {
                    var issueAttachments = attachments.Where(x => x.IssueId == id);
                    count = +(int)(await AttachmentHelper.CloneIssueAttachmentsAsync(engagementId, id, issueAttachments.Where(x => x.IsMarkdown).Select(x => x.Attachment).Where(q => q.FileId != "Image Not Found").ToList(), user, userFullName, fileRepo, apiBaseUrl, directory ?? fileRepo, true)).Affected;
                    count = +(int)(await AttachmentHelper.CloneIssueAttachmentsAsync(engagementId, id, issueAttachments.Where(x => !x.IsMarkdown).Select(x => x.Attachment).ToList(), user, userFullName, fileRepo, apiBaseUrl, directory ?? fileRepo)).Affected;
                }
            }

            return new NoDataResponse(count);
        }
    }
}
