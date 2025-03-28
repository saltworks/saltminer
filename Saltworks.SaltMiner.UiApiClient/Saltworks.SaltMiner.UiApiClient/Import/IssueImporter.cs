using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using System.Globalization;
using System.Text.Json;
using System.Text;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.ViewModels;

namespace Saltworks.SaltMiner.UiApiClient.Import
{
    public class IssueImporter(DataClient.DataClient dataClient, ILogger logger) : BaseImporter(dataClient, logger)
    {
        public List<QueueAsset> QueueAssetsBatch { get; set; } = [];
        public List<QueueIssue> QueueIssuesBatch { get; set; } = [];

        public IssueImporterResponse ProcessImport(IssueImporterRequest importRequest)
        {
            if (importRequest.File != null && importRequest.File.Length > 0)
            {
                if (importRequest.File.FileName.EndsWith(".csv"))
                {
                    if (ValidateCSVHeaders(importRequest.File, importRequest.RequiredCsvAssetHeaders, importRequest.RequiredCsvIssueHeaders))
                    {
                        return ProcessCsv(importRequest);
                    }
                    else
                    {
                        throw new UiApiClientImportException("Required Headers are missing");
                    }
                }
                else if (importRequest.File.FileName.EndsWith(".json"))
                {
                    if (importRequest.DefaultQueueAssetId == null)
                    {
                        throw new UiApiClientImportException("If File is type JSON then you must supply default queue asset");
                    }
                    if (IsValidJson(importRequest.File))
                    {
                        var json = GetJson(importRequest);
                        return ProcessJson(json, importRequest);
                    }
                    else
                    {
                        throw new UiApiClientImportException("Json is not valid");
                    }
                }
                else
                {
                    throw new UiApiClientImportException("File is not a CSV or JSON file type");
                }
            }
            else
            {
                throw new UiApiClientImportException("No File Attached");
            }
        }

        private IssueImporterResponse ProcessCsv(IssueImporterRequest importRequest)
        {
            General.ValidateIdAndInput(importRequest.EngagementId, importRequest.Regex, "Id");

            var timer = new ProgressTimer("timer");
            Logger.LogInformation("Process CSV File '{File}' for Engagement '{Id}' initiated", importRequest.File.FileName, importRequest.EngagementId);
            var map = new Dictionary<string, int>();

            Logger.LogInformation("Read CSV Template");

            // open the file "data.csv" which is a CSV file with headers
            using (var reader = new StreamReader(importRequest.File.OpenReadStream()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                Logger.LogInformation("Check Issue Count for import");
                csv.Read();

                var issueCounter = 0;

                while (csv.Read())
                {
                    issueCounter++;
                }

                if (importRequest.File.Length > importRequest.MaxImportFileSize && !importRequest.FromQueue)
                {
                    // upload the file and put it in a job queue to be processed by outside process
                    var response = QueueIssueImportFile(importRequest).GetAwaiter().GetResult();
                    Logger.LogInformation("Issue import file sent to job queue id {JobId}", response.Data.Id);
                    return new IssueImporterResponse(true, true);
                }
            }

            var rowCount = 0;
            using (var reader = new StreamReader(importRequest.File.OpenReadStream()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                try
                {
                    csv.Read();
                    rowCount++;
                    csv.ReadHeader();
                    var headers = csv.HeaderRecord;

                    Logger.LogInformation("Map CSV Headers");
                    for (int i = 0; i < headers.Length; i++)
                    {
                        map[headers[i].Trim().ToLower().Replace("*", "")] = i;
                    }
                    Logger.LogInformation("Map CSV Headers finished");

                    var queueAssets = new List<QueueAsset>();
                    var queueIssues = new List<QueueIssue>();

                    Logger.LogInformation("Get Engagement '{Id}'", importRequest.EngagementId);
                    var engagementResponse = DataClient.EngagementGet(importRequest.EngagementId);

                    if (!engagementResponse.Success || engagementResponse.Data == null)
                    {
                        throw new UiApiClientImportException($"Engagement {importRequest.EngagementId} does not exist.");
                    }
                    var engagement = engagementResponse.Data;

                    Logger.LogInformation("Get Scan for engagement '{Id}'", importRequest.EngagementId);
                    var scanResponse = DataClient.QueueScanGetByEngagement(importRequest.EngagementId);

                    if (!scanResponse.Success || scanResponse.Data == null)
                    {
                        throw new UiApiClientImportException($"Scan for Engagement {importRequest.EngagementId} does not exist.");
                    }
                    var queueScan = scanResponse.Data;
                    QueueAsset defaultQueueAsset = null;
                    if (importRequest.DefaultQueueAssetId != null)
                    {
                        var defaultQueueAssetResponse = DataClient.QueueAssetGet(importRequest.DefaultQueueAssetId);
                        if (defaultQueueAssetResponse.Success && defaultQueueAssetResponse.Data != null)
                        {
                            defaultQueueAsset = defaultQueueAssetResponse.Data;
                        }
                    }

                    Logger.LogInformation("Read and Validate");
                    while (csv.Read())
                    {
                        rowCount++;
                        // Map and Validate
                        var queueAsset = MapAndValidateAsset(csv, map, importRequest, queueScan.Id, queueAssets, defaultQueueAsset, engagement);
                        queueIssues.Add(MapAndValidateIssue(csv, map, importRequest, queueScan.Id, queueAsset.Id, queueAsset.Saltminer.Asset.Name, engagement));
                    }
                    Logger.LogInformation("Read and Validate finished");
                    rowCount = -1;
                    #region Send

                    Logger.LogInformation("Send");

                    foreach (var queueAsset in queueAssets)
                    {
                        SendBatchAddQueueAssets(queueAsset, importRequest.ImportBatchSize);
                        SendBatchAddQueueIssues(queueIssues.Where(x => x.Saltminer.QueueScanId == queueScan.Id && x.Saltminer.QueueAssetId == queueAsset.Id), importRequest.ImportBatchSize);
                    }

                    SendBatchAddQueueAssets(null, importRequest.ImportBatchSize, true);
                    SendBatchAddQueueIssues(null, importRequest.ImportBatchSize, true);

                    #endregion

                    Logger.LogInformation("Send finished: {QueueAssetsCount} Queue Assets and {QueueIssuesCount} Queue Issues Created.", queueAssets.Count, queueIssues.Count);

                    Logger.LogInformation("Edit QueueScan '{Id}' with new Issue Counts", queueScan.Id);
                    DataClient.QueueScanAddUpdate(queueScan);

                    Logger.LogInformation("Edit Engagement '{Id}' with new Issue Counts", engagement.Id);
                    DataClient.EngagementAddUpdate(engagement);

                    timer.Stop = DateTime.UtcNow;
                    Logger.LogInformation("Process CSV File '{File}' for Engagement '{Id}' finished took {Time} sec/s", importRequest.File.FileName, importRequest.EngagementId, timer.GetSeconds());

                    return new IssueImporterResponse(true);
                }
                catch (Exception ex)
                {

                    var message = rowCount > -1 ? $"CSV Row {rowCount}: " : "";
                    message += ex.InnerException?.Message != null ? ex.InnerException.Message : ex.Message;
                    var error = "Process CSV File '" + importRequest.File.FileName + "' for Engagement '" + importRequest.EngagementId + "' Error: " + message;
                    Logger.LogError(ex, "{Msg}", error);
                    throw new UiApiClientImportException(message);
                }
            }
        }

        public IssueImporterResponse ProcessJson(string json, IssueImporterRequest importRequest)
        {
            General.ValidateIdAndInput(importRequest.EngagementId, importRequest.Regex, "Id");

            var timer = new ProgressTimer("timer");
            Logger.LogInformation("Process JSON File '{File}' for Engagement '{Id}' initiated", importRequest.File.FileName, importRequest.EngagementId);

            var queueIssues = new List<QueueIssue>();
            var queueAssets = new List<QueueAsset>();

            Logger.LogInformation("Get Engagement '{Id}'", importRequest.EngagementId);
            var engagementResponse = DataClient.EngagementGet(importRequest.EngagementId);

            if (!engagementResponse.Success || engagementResponse.Data == null)
            {
                throw new UiApiClientImportException($"Engagement {importRequest.EngagementId} does not exist.");
            }
            var engagement = engagementResponse.Data;

            Logger.LogInformation("Get QueueScan for engagement '{Id}'", engagement.Id);
            var queueScanResponse = DataClient.QueueScanGetByEngagement(engagement.Id);

            if (!queueScanResponse.Success || queueScanResponse.Data == null)
            {
                throw new UiApiClientImportException($"QueueScan for engagement {engagement.Id} does not exist.");
            }
            var queueScan = queueScanResponse.Data;
            QueueAsset defaultQueueAsset = null;
            if (importRequest.DefaultQueueAssetId != null)
            {
                var defaultQueueAssetResponse = DataClient.QueueAssetGet(importRequest.DefaultQueueAssetId);
                defaultQueueAsset = defaultQueueAssetResponse.Data;
                queueAssets.Add(defaultQueueAsset);
            }
            else
            {
                throw new UiApiNotFoundException($"Default Queue Asset Not Provided");
            }

            var maxSizeExceeded = importRequest.File.Length > importRequest.MaxImportFileSize;

            try
            {
                Logger.LogInformation("Read and Validate");

                //Template Issues
                if (importRequest.IsTemplate)
                {
                    if (maxSizeExceeded && !importRequest.FromQueue)
                    {
                        // upload the file and put it in a job queue to be processed by outside process
                        var response = QueueIssueImportFile(importRequest).GetAwaiter().GetResult();
                        Logger.LogInformation("Issue import file sent to job queue id {JobId}", response.Data.Id);
                        return new IssueImporterResponse(true, true);
                    }

                    var jsonIssues = !importRequest.IsTemplate ? null : UpgradeTool.UpgradeIssueTemplatesImport(json);

                    foreach (var jsonIssue in jsonIssues.Select(i => i.Issue))
                    {
                        jsonIssue.IsModelValid(importRequest.Regex, importRequest.FailedRegexSplat, true, null, IssueAttributeDefinitions, TestedDropdowns);

                        AddQueueIssue(jsonIssue, queueIssues, engagement, queueScan.Id, defaultQueueAsset.Id, importRequest.UiBaseUrl, importRequest.TestStatusLookups);
                    }
                }
                //Engagement Issues
                else
                {
                    if (maxSizeExceeded && !importRequest.FromQueue)
                    {
                        // upload the file and put it in a job queue to be processed by outside process
                        var response = QueueIssueImportFile(importRequest).GetAwaiter().GetResult();
                        Logger.LogInformation("Issue import file sent to job queue id {JobId}", response.Data.Id);
                        return new IssueImporterResponse(true, true);
                    }

                    var engagementIssueImports = UpgradeTool.UpgradeEngagementIssuesImport(json);

                    foreach (var engagementIssueImport in engagementIssueImports)
                    {
                        Logger.LogInformation("Validate Issue");
                        engagementIssueImport.Issue.IsModelValid(importRequest.Regex, importRequest.FailedRegexSplat, true, null, IssueAttributeDefinitions, TestedDropdowns);

                        Logger.LogInformation("Validate Asset");
                        engagementIssueImport.Asset.IsModelValid(importRequest.Regex, importRequest.FailedRegexSplat, true, null, AssetAttributeDefinitions, TestedDropdowns);

                        Logger.LogInformation("Assigning Asset");
                        var queueAsset = ChooseQueueAsset(engagementIssueImport.Asset, engagement, queueScan.Id, queueAssets, defaultQueueAsset, importRequest);

                        Logger.LogInformation("Adding Issue to batch");
                        AddQueueIssue(engagementIssueImport.Issue, queueIssues, engagement, queueScan.Id, queueAsset?.Id, importRequest.UiBaseUrl, importRequest.TestStatusLookups);
                    }
                }

                Logger.LogInformation("Read and Validate finished");

                #region Send

                Logger.LogInformation("Sending batches {Batches}", Math.Ceiling((decimal)queueAssets.Count / importRequest.ImportBatchSize));
                foreach (var queueAsset in queueAssets)
                {
                    SendBatchAddQueueAssets(queueAsset, importRequest.ImportBatchSize);
                    SendBatchAddQueueIssues(queueIssues.Where(x => x.Saltminer.QueueAssetId == queueAsset.Id), importRequest.ImportBatchSize);
                }

                SendBatchAddQueueAssets(null, importRequest.ImportBatchSize, true);
                SendBatchAddQueueIssues(null, importRequest.ImportBatchSize, true);
                DataClient.RefreshIndex(QueueIssue.GenerateIndex());

                #endregion

                Logger.LogInformation("Send finished: {QueueAssetsCount} Queue Assets and {QueueIssuesCount} Queue Issues Created.", 1, queueIssues.Count);

                Logger.LogInformation("Edit QueueScan '{Id}' with new Issue Counts", queueScan.Id);
                DataClient.QueueScanAddUpdate(queueScan);

                Logger.LogInformation("Edit Engagement '{Id}' with new Issue Counts", engagement.Id);
                DataClient.EngagementAddUpdate(engagement);

                timer.Stop = DateTime.UtcNow;
                Logger.LogInformation("Process JSON File '{File}' for Engagement '{Id}' finished took {Time} sec/s", importRequest.File.FileName, importRequest.EngagementId, timer.GetSeconds());

                return new IssueImporterResponse(true);
            }
            catch (Exception ex)
            {
                var message = $"Process JSON File '{importRequest.File.FileName}' for Engagement '{importRequest.EngagementId}' Error: ";

                message += ex.Message; 
                
                if (ex?.InnerException?.Message != null)
                {
                    message += ex.InnerException.Message;
                }

                Logger.LogError(ex, "{Msg}", message);
                throw new UiApiClientValidationException(message);
            }
        }

        public static string GetJson(IssueImporterRequest importRequest)
        {
            string json = null;
            using (var reader = new StreamReader(importRequest.File.OpenReadStream()))
            {
                json = reader.ReadToEnd();
            }

            return json;
        }

        private void AddQueueIssue(IssueImport issue, List<QueueIssue> queueIssues, Engagement engagement, string queueScanId, string queueAssetId, string baseUrl, List<LookupValue> TestStatusLookups)
        {
            issue.TestStatus = EngagementHelper.ValidateTestStatus(issue.TestStatus, TestStatusLookups);
            var queueIssue = issue.ParseQueueIssue(engagement, queueScanId, queueAssetId, baseUrl);
            AttachmentHelper.ReplaceImages(queueIssue, "", "Image Not Found");
            queueIssues.Add(queueIssue);
        }

        private QueueAsset ChooseQueueAsset(AssetImport assetImport, Engagement engagement, string queueScanId, List<QueueAsset> queueAssets, QueueAsset defaultQueueAsset, IssueImporterRequest importRequest)
        {
            var queueAsset = assetImport.ParseQueueAsset(engagement, queueScanId, importRequest.Instance, importRequest.SourceType, importRequest.AssetType, importRequest.LastScanDaysPolicy, importRequest.InventoryAssetKeyAttribute);

            if (queueAsset != null)
            {
                var processedQueueAsset = queueAssets.FirstOrDefault(x => x.Saltminer.Asset.Name == queueAsset.Saltminer.Asset.Name && x.Saltminer.Asset.Version == queueAsset.Saltminer.Asset.Version && x.Saltminer.Asset.VersionId == queueAsset.Saltminer.Asset.VersionId);
                if (processedQueueAsset == null)
                {
                    var foundQueueAssets = DataClient.QueueAssetSearch(new SearchRequest
                    {
                        Filter = new Filter
                        {
                            AnyMatch = false,
                            FilterMatches = new Dictionary<string, string>
                                        {
                                            { "Saltminer.Engagement.Id", importRequest.EngagementId },
                                            { "Saltminer.Asset.Name", queueAsset.Saltminer.Asset.Name },
                                            { "Saltminer.Asset.Version", queueAsset.Saltminer.Asset.Version },
                                            { "Saltminer.Asset.VersionId", queueAsset.Saltminer.Asset.VersionId }
                                        }
                        },
                    });

                    if (foundQueueAssets.Success && foundQueueAssets.Data != null && foundQueueAssets.Data.Any())
                    {
                        queueAsset = foundQueueAssets.Data.FirstOrDefault();
                    }

                    queueAssets.Add(queueAsset);
                }
                else
                {
                    queueAsset = processedQueueAsset;
                }
            }
            else
            {
                queueAsset = defaultQueueAsset;
            }

            return queueAsset;
        }

        private async Task<DataItemResponse<Job>> QueueIssueImportFile(IssueImporterRequest importRequest)
        {
            Logger.LogInformation("Queue issue import file initiated");

            try
            {
                var filePath = await FileHelper.CreateFileAsync(importRequest.File, importRequest.UserName, importRequest.UserFullName, importRequest.FileRepo);

                var jobType = Job.JobType.PenIssuesImport.ToString("g");
                if (importRequest.IsTemplate)
                {
                    jobType = Job.JobType.PenTemplateIssuesImport.ToString("g");
                }

                var attributes = new Dictionary<string, string>
                {
                    { "DefaultQueueAssetId", importRequest.DefaultQueueAssetId },
                    { "UiBaseUrl", importRequest.UiBaseUrl },
                    { "FileRepo", importRequest.FileRepo },
                    { "Regex", importRequest.Regex },
                    { "LastScanDaysPolicy", importRequest.LastScanDaysPolicy },
                    { "TemplatePath", importRequest.TemplatePath },
                    { "IsTemplate", importRequest.IsTemplate.ToString() },
                    { "FailedRegexSplat", importRequest.FailedRegexSplat }
                };

                //add to job queue
                var job = new Job
                {
                    Status = Job.JobStatus.Pending.ToString("g"),
                    Type = jobType,
                    FileName = filePath,
                    TargetId = importRequest.EngagementId,
                    Message = string.Empty,
                    Attributes = attributes,
                    User = importRequest.UserName,
                    UserFullName = importRequest.UserFullName
                };

                return DataClient.JobAddUpdate(job);
            }
            catch (Exception ex)
            {
                throw new UiApiClientValidationException("Error sending issue import file to queue", ex);
            }
        }

        private static bool IsValidJson(IFormFile file)
        {
            string json = null;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                json = reader.ReadToEnd();
            }

            if (json == null)
                return false;

            try
            {
                JsonDocument.Parse(json);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private bool ValidateCSVHeaders(IFormFile file, List<string> requiredCSVAssetHeaders, List<string> requiredCSVIssueHeaders)
        {
            Logger.LogInformation("Validate CSV File '{File}' initiated", file.FileName);

            Logger.LogInformation("Get CSV Template");
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                BadDataFound = args =>
                {
                    throw new BadDataException(args.Field, args.RawRecord, args.Context, $"BadDataFound: Bad entry found at field {args.Field}, \n {args.RawRecord.Replace("\"", "'")}");
                }
            };

            List<string> importHeaders;
            var requiredHeaders = new List<string>();
            requiredHeaders.AddRange(requiredCSVAssetHeaders);
            requiredHeaders.AddRange(requiredCSVIssueHeaders);

            try
            {
                Logger.LogInformation("Read CSV Import");
                using (var reader = new StreamReader(file.OpenReadStream()))
                using (var csv = new CsvReader(reader, csvConfig))
                {
                    csv.Read();
                    csv.ReadHeader();
                    importHeaders = csv.HeaderRecord.ToList();
                }

                var missingHeaders = new List<string>();

                Logger.LogInformation("Find Missing Required CSV Headers");
                foreach (var requiredHeader in requiredHeaders)
                {
                    if (importHeaders.Any(x => x.Trim().Contains(requiredHeader.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }
                    missingHeaders.Add(requiredHeader);
                }

                Logger.LogInformation("Report Missing Required CSV Headers");
                if (missingHeaders.Count > 0)
                {
                    var errorMessage = new StringBuilder("CSV Validation Failed - ");
                    errorMessage.Append("Missing Required Template Headers: ");
                    foreach (var mHeader in missingHeaders)
                        errorMessage.Append($" {mHeader},");
                    Logger.LogError("{Msg}", errorMessage.ToString()[..(errorMessage.Length - 1)]);
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new UiApiClientValidationException("Error Processing CSV File", ex);
            }

            Logger.LogInformation("Validate CSV File '{File}' finished", file.FileName);
            return true;
        }

        private QueueAsset MapAndValidateAsset(CsvReader csv, Dictionary<string, int> map, IssueImporterRequest importRequest, string queueScanId, List<QueueAsset> queueAssets, QueueAsset defaultAsset, Engagement engagement)
        {
            Validate(csv, map, []);

            var versionId = GetStringValue("saltminer.asset.version_id", csv, map);
            var version = GetStringValue("saltminer.asset.version", csv, map);
            var name = GetStringValue("saltminer.asset.name", csv, map);
            QueueAsset queueAsset = null;

            //Does asset exist in current create list, and do I have valid values
            if (versionId != null && version != null && name != null && !importRequest.IsTemplate)
            {
                var id = Guid.NewGuid().ToString();
                var engagementAsset = new AssetFull()
                {
                    Name = new() { Value = CreateUniqueAssetCSVName(GetStringValue("saltminer.asset.name", csv, map), queueAssets), Name = "Name" },
                    Description = new() { Value = GetStringValue("saltminer.asset.description", csv, map), Name = "Description" },
                    AssetId = id,
                    ScanId = queueScanId,
                    VersionId = new() { Value = GetStringValue("saltminer.asset.version_id", csv, map), Name = "VersionId" },
                    Version = new() { Value = GetStringValue("saltminer.asset.version", csv, map), Name = "Version" },
                    SourceId = GetStringValue("saltminer.asset.source_id", csv, map),
                    Attributes = [],
                    Engagement = new()
                    {
                        Id = engagement.Id,
                        Name = engagement.Saltminer.Engagement.Name,
                        Attributes = engagement.Saltminer.Engagement.Attributes,
                        Customer = engagement.Saltminer.Engagement.Customer,
                        PublishDate = engagement.Saltminer.Engagement.PublishDate,
                        Subtype = engagement.Saltminer.Engagement.Subtype,
                    },
                    Host = GetStringValue("saltminer.asset.host", csv, map),
                    Ip = GetStringValue("saltminer.asset.ip", csv, map),
                    Scheme = GetStringValue("saltminer.asset.scheme", csv, map),
                    Port = GetIntValue("saltminer.asset.port", csv, map),
                    IsProduction = GetBoolValue("saltminer.asset.is_production", csv, map) ?? false,
                    IsSaltminerSource = true,
                    IsRetired = GetBoolValue("saltminer.asset.is_retired", csv, map) ?? false,
                    LastScanDaysPolicy = importRequest.LastScanDaysPolicy,
                    Timestamp = DateTime.UtcNow,
                    InventoryAssetKey = GetGuidValue("saltminer.asset_inv.key", csv, map),
                };

                engagementAsset.IsModelValid(importRequest.Regex, importRequest.FailedRegexSplat, true, null, AssetAttributeDefinitions);

                queueAsset = engagementAsset.GetQueueAsset(importRequest.AssetType, importRequest.Instance, importRequest.SourceType);
            }
            else
            {
                queueAsset = defaultAsset;
            }

            var processedAsset = queueAssets.FirstOrDefault(x => x.Saltminer.Asset.Name == queueAsset.Saltminer.Asset.Name && x.Saltminer.Asset.Version == queueAsset.Saltminer.Asset.Version && x.Saltminer.Asset.VersionId == queueAsset.Saltminer.Asset.VersionId);
            if (processedAsset != null)
            {
                return processedAsset;
            }

            var foundQueueAssets = DataClient.QueueAssetSearch(new SearchRequest
            {
                Filter = new Filter
                {
                    AnyMatch = false,
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Engagement.Id", engagement.Id },
                        { "Saltminer.Asset.Name", queueAsset.Saltminer.Asset.Name},
                        { "Saltminer.Asset.Version", queueAsset.Saltminer.Asset.Version },
                        { "Saltminer.Asset.VersionId", queueAsset.Saltminer.Asset.VersionId }
                    }
                }
            });

            if (foundQueueAssets.Success && foundQueueAssets.Data != null && foundQueueAssets.Data.Any())
            {
                queueAsset = foundQueueAssets.Data.FirstOrDefault();
            }

            queueAssets.Add(queueAsset);

            return queueAsset;
        }

        private QueueIssue MapAndValidateIssue(CsvReader csv, Dictionary<string, int> map, IssueImporterRequest importRequest, string queueScanId, string queueAssetId, string assetName, Engagement engagement)
        {
            Validate(csv, map, importRequest.RequiredCsvIssueHeaders);
            var id = Guid.NewGuid().ToString();
            var engagementIssue = new IssueFull
            {
                Id = id,
                Engagement = new()
                {
                    Id = engagement.Id,
                    Name = engagement.Saltminer.Engagement.Name,
                    Attributes = engagement.Saltminer.Engagement.Attributes,
                    Customer = engagement.Saltminer.Engagement.Customer,
                    PublishDate = engagement.Saltminer.Engagement.PublishDate,
                    Subtype = engagement.Saltminer.Engagement.Subtype
                },
                Name = new() { Value = GetStringValue("vulnerability.name", csv, map), Name = "Name" },
                Severity = new() { Value = GetStringValue("vulnerability.severity", csv, map), Name = "Severity" },
                AssetName = new() { Value = assetName, Name = "AssetName" },
                AssetId = queueAssetId,
                FoundDate = GetDateTimeValue("vulnerability.found_date", csv, map),
                TestStatus = new() { Value = EngagementHelper.ValidateTestStatus(GetStringValue("vulnerability.audit.auditor", csv, map), importRequest.TestStatusLookups), Name = "TestStatus" },
                IsSuppressed = new() { Value = GetBoolValue("vulnerability.is_suppressed", csv, map) ?? false, Name = "IsSuppressed" },
                VulnerabilityId = [GetStringValue("vulnerability.id", csv, map)],
                ScanId = queueScanId,
                RemovedDate = new() { Value = GetDateTimeValue("vulnerability.removed_date", csv, map), Name = "RemovedDate" },
                Location = new() { Value = GetStringValue("vulnerability.location", csv, map), Name = "Location" },
                LocationFull = new() { Value = GetStringValue("vulnerability.location_full", csv, map), Name = "LocationFull" },
                ReportId = GetStringValue("vulnerability.report_id", csv, map),
                Classification = GetStringValue("vulnerability.classification", csv, map),
                Description = new() { Value = GetStringValue("vulnerability.description", csv, map), Name = "Description" },
                Enumeration = GetStringValue("vulnerability.enumeration", csv, map),
                Proof = new() { Value = GetStringValue("vulnerability.proof", csv, map), Name = "Proof" },
                Details = new() { Value = GetStringValue("vulnerability.details", csv, map), Name = "Details" },
                TestingInstructions = new() { Value = GetStringValue("vulnerability.testing_instructions", csv, map), Name = "TestingInstructions" },
                Implication = new() { Value = GetStringValue("vulnerability.implication", csv, map), Name = "Implication" },
                Recommendation = new() { Value = GetStringValue("vulnerability.recommendation", csv, map), Name = "Recommendation" },
                References = new() { Value = GetStringValue("vulnerability.references", csv, map), Name = "References" },
                Reference = GetStringValue("vulnerability.reference", csv, map),
                Vendor = new() { Value = GetStringValue("vulnerability.scanner.vendor", csv, map), Name = "Vendor" },
                Product = new() { Value = GetStringValue("vulnerability.scanner.product", csv, map), Name = "Product" },
                ScannerId = GetStringValue("vulnerability.scanner.id", csv, map) ?? Guid.NewGuid().ToString(),
                Audited = GetBoolValue("vulnerability.audit.audited", csv, map) ?? false,
                Auditor = GetStringValue("vulnerability.audit.auditor", csv, map),
                LastAudit = GetDateTimeValue("vulnerability.audit.last_audit", csv, map),
                Category = [ "Application" ],
                Attributes = [],
                Base = GetFloatValue("vulnerability.score.base", csv, map),
                Version = GetStringValue("vulnerability.score.version", csv, map),
                Environmental = GetFloatValue("vulnerability.score.environmental", csv, map),
                Temporal = GetFloatValue("vulnerability.score.temporal", csv, map),
                LockInfo = null,
                IsHistorical = false,
                Timestamp = DateTime.UtcNow,
            };

            engagementIssue.IsModelValid(importRequest.Regex, importRequest.FailedRegexSplat, true, null, IssueAttributeDefinitions, TestedDropdowns);
            var queueIssue = engagementIssue.GetQueueIssue(importRequest.UiBaseUrl);
            queueIssue.Vulnerability.SourceSeverity = GetStringValue("vulnerability.source_severity", csv, map);
            queueIssue.Vulnerability.IsFiltered = GetBoolValue("vulnerability.is_filtered", csv, map) ?? false;
            queueIssue.Vulnerability.Scanner.ApiUrl = GetStringValue("vulnerability.scanner.api_url", csv, map);
            Logger.LogDebug("UI Base URL: {Url}", importRequest.UiBaseUrl);
            queueIssue.Vulnerability.Scanner.GuiUrl = $"{importRequest.UiBaseUrl}/engagements/{engagement.Id}/scanner/{engagementIssue.ScannerId}";
            queueIssue.Saltminer.Source = new SourceInfo
            {
                Analyzer = GetStringValue("saltminer.source.analyzer", csv, map),
                Confidence = GetFloatValue("saltminer.source.confidence", csv, map),
                Impact = GetFloatValue("saltminer.source.impact", csv, map),
                IssueStatus = GetStringValue("saltminer.source.status", csv, map),
                Kingdom = GetStringValue("saltminer.source.kingdom", csv, map),
                Likelihood = GetFloatValue("saltminer.source.likelihood", csv, map)
            };

            return queueIssue;
        }

        private static void Validate(CsvReader csv, Dictionary<string, int> map, List<string> required)
        {
            foreach (var header in required)
            {
                var valid = true;

                if (header.Contains("date", StringComparison.OrdinalIgnoreCase))
                {
                    var date = GetDateTimeValue(header, csv, map);
                    if (date == null)
                        valid = false;
                }
                else if (header.Contains("is_", StringComparison.OrdinalIgnoreCase))
                {
                    var value = GetBoolValue(header, csv, map);
                    if (value == null)
                        valid = false;
                }
                else
                {
                    var value = GetStringValue(header, csv, map);
                    if (string.IsNullOrEmpty(value))
                        valid = false;
                }

                if (!valid)
                {
                    throw new UiApiClientValidationException($"All records required to have a valid value for {header}.");
                }
            }
        }

        private void SendBatchAddQueueAssets(QueueAsset queueAsset, int csvImportBatchSize, bool flush = false)
        {
            Logger.LogInformation("Sending Asset batch");
            if (queueAsset != null)
            {
                QueueAssetsBatch.Add(queueAsset);
            }

            if (QueueAssetsBatch.Count > 0 && (QueueAssetsBatch.Count % csvImportBatchSize == 0 || flush))
            {
                var response = DataClient.QueueAssetAddUpdateBulk(QueueAssetsBatch);
                if (response.Success)
                {
                    Logger.LogInformation("Send: Created {Count} Queue Assets.", QueueAssetsBatch.Count);
                    QueueAssetsBatch = [];
                }
                else
                {
                    throw new UiApiClientValidationException(string.Join(" Bulk Error: ", response.BulkErrors.Values));
                }
            }
        }

        private void SendBatchAddQueueIssues(IEnumerable<QueueIssue> queueIssues, int csvImportBatchSize, bool flush = false)
        {
            Logger.LogInformation("Sending Issue batch");
            if ((queueIssues == null || !queueIssues.Any()) && flush)
            {
                if (QueueIssuesBatch.Count > 0)
                {
                    BulkQueueIssueAdd();
                }
            }
            else if (queueIssues != null && queueIssues.Any())
            {
                foreach (var queueIssue in queueIssues)
                {
                    QueueIssuesBatch.Add(queueIssue);

                    if (QueueIssuesBatch.Count % csvImportBatchSize == 0)
                    {
                        BulkQueueIssueAdd();
                    }
                }
            }
        }

        private void BulkQueueIssueAdd()
        {
            var response = DataClient.QueueIssuesAddUpdateBulk(QueueIssuesBatch);
            if (response.Success)
            {
                Logger.LogInformation("Send: Created {Count} Queue Issues.", QueueIssuesBatch.Count);
                QueueIssuesBatch = [];
            }
            else
            {
                throw new UiApiClientValidationException(string.Join(" Bulk Error: ", response.BulkErrors.Values));
            }
        }

        private static string GetStringValue(string column, CsvReader csv, Dictionary<string, int> map)
        {
            if (map.ContainsKey(column))
            {
                return string.IsNullOrEmpty(csv[map[column]]) ? null : csv[map[column]];
            }
            return null;
        }

        private static DateTime? GetDateTimeValue(string column, CsvReader csv, Dictionary<string, int> map)
        {
            if (map.TryGetValue(column, out int value) && !string.IsNullOrEmpty(csv[value]))
            {
                var result = csv[value].ToDate()?.ToUniversalTime() ?? csv[value].ToDate("yyyy-MM-dd");
                if (result.HasValue)
                {
                    return result.Value.ToUniversalTime();
                }

                throw new UiApiClientValidationException($"{column}: {csv[value]} is not in a valid format('yyyy-MM-dd').");
            }
            return null;
        }

        private static int GetIntValue(string column, CsvReader csv, Dictionary<string, int> map)
        {
            if (map.TryGetValue(column, out int value) && !string.IsNullOrEmpty(csv[value]))
            {
                return Convert.ToInt32(csv[value]);
            }

            return 0;
        }

        private static float GetFloatValue(string column, CsvReader csv, Dictionary<string, int> map)
        {
            if (map.TryGetValue(column, out int value) && !string.IsNullOrEmpty(csv[value]) && float.TryParse(csv[map[column]], out float f))
            {
                return f;
            }
            return 0;
        }

        private static bool? GetBoolValue(string column, CsvReader csv, Dictionary<string, int> map)
        {
            if (map.TryGetValue(column, out int value) && !string.IsNullOrEmpty(csv[value]))
            {
                return Convert.ToBoolean(csv[value]);
            }
            return false;
        }

        private static string GetGuidValue(string column, CsvReader csv, Dictionary<string, int> map)
        {
            if (map.ContainsKey(column))
            {
                return string.IsNullOrEmpty(csv[map[column]]) ? Guid.NewGuid().ToString() : csv[map[column]];
            }
            return null;
        }

        private static string CreateUniqueAssetCSVName(string name, List<QueueAsset> queueAssets)
        {
            var baseName = name;
            var counter = 0;

            while (queueAssets.Any(x => x.Saltminer.Asset.Name == name))
            {
                counter++;
                name = $"{baseName} - csv {counter}";
            }
            return name;
        }
    }

    public class IssueImporterRequest
    {
        public IFormFile File { get; set; }
        public int ImportBatchSize { get; set; }
        public List<string> RequiredCsvAssetHeaders { get; set; }
        public List<string> RequiredCsvIssueHeaders { get; set; }
        public string FileRepo { get; set; }
        public string UiBaseUrl { get; set; }
        public string Regex { get; set; }
        public string FailedRegexSplat { get; set; }
        public string TemplatePath { get; set; }
        public int MaxImportFileSize { get; set; }
        public string Instance { get; set; }
        public string SourceType { get; set; }
        public string AssetType { get; set; }
        public string LastScanDaysPolicy { get; set; }
        public string EngagementId { get; set; }
        public string UserName { get; set; }
        public string UserFullName { get; set; }
        public string DefaultQueueAssetId { get; set; } = null;
        public bool IsTemplate { get; set; } = false;
        public bool FromQueue { get; set; } = false;
        public string InventoryAssetKeyAttribute { get; set; } = string.Empty;
        public List<LookupValue> TestStatusLookups { get; set; } = [];
    }

    public class IssueImporterResponse
    {
        public IssueImporterResponse(bool isQueued, bool success)
        {
            IsQueued = isQueued;
            Success = success;
        }

        public IssueImporterResponse(bool success)
        {
            Success = success;
        }

        public bool IsQueued { get; set; } = false;
        public bool Success { get; set; } = false;

    }
}