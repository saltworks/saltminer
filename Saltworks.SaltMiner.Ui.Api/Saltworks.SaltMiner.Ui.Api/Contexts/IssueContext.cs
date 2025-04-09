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

﻿using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.Ui.Api.Models;
using System.Text.RegularExpressions;
using Saltworks.SaltMiner.UiApiClient.ViewModels;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Import;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Import.CustomImporter;
using Saltworks.SaltMiner.UiApiClient.Responses;

namespace Saltworks.SaltMiner.Ui.Api.Contexts
{
    public class IssueContext : ContextBase
    {
        private List<FieldFilter> _SearchDisplays = [];
        private List<FieldFilter> _SortDisplays = [];
        protected override List<SearchFilterValue> SearchFilterValues => SearchFilters?.Find(x => x.Type == SearchFilterType.IssueSearchFilters.ToString())?.Filters ?? [];
        protected override List<FieldFilter> SearchDisplays
        {
            get
            {
                if ((_SearchDisplays ?? []).Count == 0)
                    _SearchDisplays = SearchFilterValues?.Select(x => new FieldFilter(x)).ToList() ?? [];
                return _SearchDisplays;
            }
        }
        protected override List<SearchFilterValue> SortFilterValues => SearchFilters?.Find(x => x.Type == SearchFilterType.IssueSortFilters.ToString())?.Filters ?? [];
        protected override List<FieldFilter> SortDisplays
        {
            get
            {
                if ((_SortDisplays ?? []).Count == 0)
                    _SortDisplays = SortFilterValues?.Select(x => new FieldFilter(x)).ToList() ?? [];
                return _SortDisplays;
            }
        }
        protected List<LookupValue> AddItemDropdowns => Lookups?.Find(x => x.Type == LookupType.AddItemDropdown.ToString())?.Values ?? [];
        protected List<LookupValue> ReportTemplateDropdowns => Lookups?.Find(x => x.Type == LookupType.ReportTemplateDropdown.ToString())?.Values ?? [];
        protected List<LookupValue> EngagementTypeDropdowns => Lookups?.Find(x => x.Type == LookupType.EngagementTypeDropdown.ToString())?.Values ?? [];

        protected readonly IssueImporter IssueImporter;
        protected readonly CustomIssueImporter CustomIssueImporter;
        protected readonly AttachmentHelper AttachmentHelper;

        private FieldInfo _MyFieldInfo = null;
        private FieldInfo MyFieldInfo
        {
            get
            {
                _MyFieldInfo ??= FieldInfo(FieldInfoEntityType.Issue);
                return _MyFieldInfo;
            }
        }

        public IssueContext(IServiceProvider services, ILogger<IssueContext> logger) : base(services, logger)
        {
            IssueImporter = new IssueImporter(DataClient, Logger);
            CustomIssueImporter = new CustomIssueImporter(DataClient, Logger);
            AttachmentHelper = new AttachmentHelper(DataClient, Logger);
        }

        public UiDataItemResponse<string> GetCSVTemplate()
        {
            Logger.LogInformation("Get CSV Template initiated");
            return new UiDataItemResponse<string>(FileHelper.SearchFile(Config.CsvTemplateFileName, Config.TemplateRepository));
        }

        public UiDataItemResponse<string> GetCSVTestFile()
        {
            Logger.LogInformation("Get CSV Test File initiated");
            return new UiDataItemResponse<string>(FileHelper.SearchFile("csv_import_small_sample.csv", Config.TemplateRepository));
        }

        public UiDataItemResponse<string> GetEngagementIssueImportJSON()
        {
            Logger.LogInformation("Get Engagement Issue Import initiated");
            return new UiDataItemResponse<string>(FileHelper.SearchFile(Config.EngagementIssueImportTemplateFileName, Config.TemplateRepository));
        }

        public UiDataItemResponse<string> GetTemplateImportJSON()
        {
            Logger.LogInformation("Get Template Import initiated");
            return new UiDataItemResponse<string>(FileHelper.SearchFile(Config.TemplateImportTemplateFileName, Config.TemplateRepository));
        }

        public IssueImporterResponse ProcessImport(IFormFile file, string engagementId, string userName, string userFullName, string defaultQueueAssetId = null, bool isTemplate = false)
        {
            return IssueImporter.ProcessImport(new IssueImporterRequest
            {
                AssetType = UiApiConfig.AssetType,
                SourceType = UiApiConfig.SourceType,
                Instance = UiApiConfig.Instance,
                ImportBatchSize = Config.ImportBatchSize,
                RequiredCsvAssetHeaders = Config.RequiredCsvAssetHeaders,
                RequiredCsvIssueHeaders = Config.RequiredCsvIssueHeaders,
                Regex = Config.ApiFieldRegex,
                FailedRegexSplat = Config.FailedRegexSplat,
                DefaultQueueAssetId = defaultQueueAssetId,
                File = file,
                EngagementId = engagementId,
                UserName = userName,
                UserFullName = userFullName,
                IsTemplate = isTemplate,
                FileRepo = Config.FileRepository,
                LastScanDaysPolicy = Config.LastScanDaysPolicy,
                MaxImportFileSize = Config.MaxImportFileSize,
                TemplatePath = Config.TemplateRepository,
                UiBaseUrl = UiBaseUrl,
                InventoryAssetKeyAttribute = Config.InventoryAssetKeyAttribute,
                TestStatusLookups = TestedDropdowns
            });
        }


        public UiDataItemResponse<Job> QueueCustomImport(IFormFile file, string engagementId, string importerId, KibanaUser user, string defaultQueueAssetId)
        {
            var job = new Job
            {
                Status = Job.JobStatus.Pending.ToString("g"),
                Type = Job.JobType.PenCustomIssuesImport.ToString("g"),
                FileName = FileHelper.CreateFileAsync(file, user.UserName, user.FullName, Config.FileRepository).GetAwaiter().GetResult(),
                TargetId = engagementId,
                Attributes = new Dictionary<string, string>
                    {
                        { "DefaultQueueAssetId", defaultQueueAssetId },
                        { "UiBaseUrl", UiBaseUrl },
                        { "FileRepo", Config.FileRepository },
                        { "Regex", Config.ApiFieldRegex },
                        { "LastScanDaysPolicy", Config.LastScanDaysPolicy },
                        { "TemplatePath", Config.TemplateRepository },
                        { "IsTemplate", "false" },
                        { "ImporterId", importerId },
                        { "ImportBatchSize", Config.ImportBatchSize.ToString() },
                        { "Instance", UiApiConfig.Instance },
                        { "SourceType", UiApiConfig.SourceType },
                        { "AssetType", UiApiConfig.AssetType },
                    },
                User = user.UserName,
                UserFullName = user.FullName,
            };

            var response = DataClient.JobAddUpdate(job);

            CustomIssueImporter.ProcessImport(job.FileName, importerId, new IssueImporterRequest
            {
                AssetType = response.Data.Attributes["AssetType"],
                SourceType = response.Data.Attributes["SourceType"],
                Instance = response.Data.Attributes["Instance"],
                Regex = response.Data.Attributes["Regex"],
                DefaultQueueAssetId = response.Data.Attributes["DefaultQueueAssetId"],
                EngagementId = response.Data.TargetId,
                UserName = response.Data.User,
                UserFullName = response.Data.UserFullName,
                FileRepo = response.Data.Attributes["FileRepo"],
                LastScanDaysPolicy = response.Data.Attributes["LastScanDaysPolicy"],
                TemplatePath = Config.TemplateRepository,
                UiBaseUrl = response.Data.Attributes["UiBaseUrl"],
                TestStatusLookups = TestedDropdowns
            });
            return new UiDataItemResponse<Job>(response.Data, response);
        }

        public UiDataItemResponse<IssuePrimer> Primer(string engagementId)
        {
            Logger.LogInformation("SearchPrimer Engagement Issues initiated for Engagement '{Id}'", engagementId);

            General.ValidateIdAndInput(engagementId, Config.ApiFieldRegex, "Id");

            Logger.LogInformation("Search Assets for Engagement '{Id}'", engagementId);
            var engagement = DataClient.EngagementGet(engagementId).Data;
            var assets = GetAllAssetsByEngagement(engagementId, engagement.Saltminer.Engagement.Status.Equals(EngagementStatus.Draft.ToString("g"))).Data?.Select(x => x.ToDropdownItem()).ToList();
            
            var result = new IssuePrimer(Config.GuiFieldRegex)
            {
                SearchFilters = SearchDisplays,
                AddItemDropdown = AddItemDropdowns,
                SubtypeDropdown = SubtypeDropdowns,
                ReportTemplateDropdown = ReportTemplateDropdowns,
                SeverityDropdown = SeverityDropdowns,
                TestedDropdown = TestedDropdowns,
                IssueStateDropdown = [ 
                    new() { Display = "Active", Value = "isActive", Order = 1 }, 
                    new() { Display = "Not Active", Value = "notActive", Order = 2 }, 
                    new() { Display = "Removed", Value = "isRemoved", Order = 3 }, 
                    new() { Display = "Suppressed", Value = "isSuppressed", Order = 4 } 
                ],
                SortFilterOptions = SortDisplays,
                AssetDropdown = assets,
                AttributeDefinitions = AttributeDefinitions(AttributeDefinitionType.Engagement),
                ValidFileExtensions = Config.ValidFileExtensions
            };

            return new UiDataItemResponse<IssuePrimer>(result);
        }

        public UiDataResponse<IssueFull> Search(IssueSearch request)
        {
            return EngagementIssueSearch(request);
        }

        public UiDataResponse<IssueFull> TemplateSearch(TemplateIssueSearch request)
        {
            Logger.LogInformation("Template Search Engagement Issues initiated");


            var template = (DataClient.EngagementSearch(new SearchRequest()
            {
                Filter = new()
                {
                    AnyMatch = true,
                    FilterMatches = new Dictionary<string, string> { { "Saltminer.Engagement.Subtype", "Template" } },
                }
            })?.Data?.FirstOrDefault()) ?? throw new UiApiNotFoundException($"No Template Engagement Found.");
            request.IsModelValid(Config.ApiFieldRegex);

            if (request.SearchFilters != null)
            {
                foreach (var filter in request.SearchFilters)
                {
                    filter.Value = Regex.Replace(filter.Value, @"[\+\-\=\&\|\>\<\!\(\)\{\}\[\]\^""\~\*\?\:\/]", " ");
                }
            }

            request.Pager ??= new UiPager(Config.DefaultPageSize, 1);

            if (request.Pager?.SortFilters == null || request.Pager.SortFilters.Count == 0)
            {
                request.Pager.SortFilters.Add("Severity", true);
                request.Pager.SortFilters.Add("Name", true);
                request.Pager.SortFilters.Add("Id", true);
            }

            // Handles role permissions
            var qResult = QueueIssuesSearch(new IssueSearch
            {
                EngagementId = template.Id,
                Pager = request.Pager,
                SearchFilters = request.SearchFilters
            });

            if (qResult.Success && qResult.Data != null && qResult.Data.Any())
            {
                qResult.UIPagingInfo.Total = qResult.Pager.Total;
                var lst = qResult.Data.Select(x => new IssueFull(x, UiApiConfig.AppVersion, MyFieldInfo));
                return new UiDataResponse<IssueFull>(lst, qResult, SortFilterValues, qResult.UIPagingInfo, true);
            }

            return new UiDataResponse<IssueFull>(null);
        }

        public UiDataItemResponse<IssueFull> TemplateAdd(string queueIssueId, string engagementId, string assetId, KibanaUser user)
        {
            Logger.LogInformation("Template Issue Add Engagement initiated");

            Logger.LogInformation("Get Queue Issue '{Id}'", queueIssueId);
            var issueResponse = DataClient.QueueIssueGet(queueIssueId);
            if (!issueResponse.Success || issueResponse.Data == null)
            {
                throw new UiApiNotFoundException($"Queue Issue {queueIssueId} does not exist.");
            }

            var engagementResponse = DataClient.EngagementGet(engagementId);
            if (!engagementResponse.Success || engagementResponse.Data == null)
            {
                throw new UiApiNotFoundException($"Engagement {engagementId} does not exist.");
            }

            var scanResponse = DataClient.QueueScanGetByEngagement(engagementId);
            if (!scanResponse.Success || scanResponse.Data == null)
            {
                throw new UiApiNotFoundException($"Queue Scan for engagement {engagementId} does not exist.");
            }

            var newIssue = issueResponse.Data;
            newIssue.Id = null;
            newIssue.Lock = null;
            
            newIssue.Saltminer.Engagement.Id = engagementResponse.Data.Id;
            newIssue.Saltminer.Engagement.Subtype = engagementResponse.Data.Saltminer.Engagement.Subtype;
            newIssue.Saltminer.Engagement.Name = engagementResponse.Data.Saltminer.Engagement.Name;

            newIssue.Saltminer.QueueScanId = scanResponse.Data.Id;
            newIssue.Saltminer.QueueAssetId = assetId;
            newIssue.Timestamp = DateTime.UtcNow;
            newIssue.Vulnerability.FoundDate = DateTime.UtcNow;
            newIssue.Vulnerability.RemovedDate = null;
            newIssue.Vulnerability.Scanner.Id = Guid.NewGuid().ToString();

            Logger.LogInformation("New QueueIssue");
            var response = DataClient.QueueIssueAddUpdate(newIssue);

            DataClient.RefreshIndex(QueueIssue.GenerateIndex());

            Logger.LogInformation("Edit Engagement '{Id}' with new Issue Counts", newIssue.Saltminer.Engagement.Id);

            var attachmentList = GetAllIssueAttachments(queueIssueId).Data.ToList();

            SetAttachments(response.Data.Saltminer.Engagement.Id, response.Data.Id, attachmentList.Where(attachment => attachment.IsMarkdown).Select(info => info.Attachment).ToList(), user, true, true);
            SetAttachments(response.Data.Saltminer.Engagement.Id, response.Data.Id, attachmentList.Where(attachment => !attachment.IsMarkdown).Select(info => info.Attachment).ToList(), user);

            var dto = new IssueFull(response.Data, UiApiConfig.AppVersion, MyFieldInfo);
            return new UiDataItemResponse<IssueFull>(dto);
        }

        public UiDataItemResponse<IssueGetScanner> GetByScannerId(string engagementId, string scannerId)
        {
            Logger.LogInformation("Get Engagement Issue by Scanner Id initiated for Scanner '{Id}'", scannerId);
            var issues = Search(new IssueSearch
            {
                EngagementId = engagementId,
                SearchFilters =
                [
                    new FieldFilter
                    {
                        Field = "Vulnerability.Scanner.Id",
                        Value = scannerId
                    }
                ],
                Pager = new UiPager
                {
                    Page = 1,
                    Size = 300,
                    SortFilters = new Dictionary<string, bool>
                        {
                            { "Severity", true },
                            { "Name", true },
                            { "Id", true }
                        }
                }
            });

            return new UiDataItemResponse<IssueGetScanner>
            {
                Affected = issues?.Data?.Count() ?? 0,
                Data = new IssueGetScanner(Config.ApiFieldRegex)
                {
                    Issue = issues?.Data.FirstOrDefault()
                }
            };
        }

        public UiDataItemResponse<IssueFull> New(IssueEdit request, KibanaUser user)
        {
            Logger.LogInformation("New Engagement Issues initiated for Engagement '{Id}'", request.EngagementId);
            request.IsModelValid(Config.ApiFieldRegex, Config.FailedRegexSplat, false, null, AttributeDefinitions(AttributeDefinitionType.Issue), TestedDropdowns);
            Logger.LogInformation("Get Engagement '{Id}'", request.EngagementId);
            var engagementResponse = DataClient.EngagementGet(request.EngagementId);
            if (!engagementResponse.Success || engagementResponse.Data == null)
            {
                throw new UiApiNotFoundException($"Engagement {request.EngagementId} does not exist.");
            }

            var engagement = engagementResponse.Data;

            Logger.LogInformation("Get Queue Scan for Engagement '{Id}'", request.EngagementId);
            var scanResponse = DataClient.QueueScanGetByEngagement(request.EngagementId);
            if (!scanResponse.Success || scanResponse.Data == null)
            {
                throw new UiApiNotFoundException($"Queue Scan for Engagement {request.EngagementId} does not exist.");
            }

            var scan = scanResponse.Data;

            Logger.LogInformation("Get CustomIssues");
            request.TestStatus = EngagementHelper.ValidateTestStatus(request.TestStatus, TestedDropdowns);
            var newIssue = request.CreateNewIssue(engagement.Saltminer.Engagement, scan.Id, UiBaseUrl);

            Logger.LogInformation("New QueueIssue");
            var response = DataClient.QueueIssueAddUpdate(newIssue);

            DataClient.RefreshIndex(QueueIssue.GenerateIndex());

            SetAttachments(request.EngagementId, response.Data.Id, GetMarkdownAttachments(request, AttributeDefinitions(AttributeDefinitionType.Issue)), user, true, true);

            Logger.LogInformation("Edit Engagement '{Id}' with new Issue Counts", request.EngagementId);

            var dto = new IssueFull(response.Data, UiApiConfig.AppVersion, MyFieldInfo);
            return new UiDataItemResponse<IssueFull>(dto);
        }

        public async Task<UiDataItemResponse<IssueFull>> Clone(string queueIssueId, KibanaUser user)
        {
            Logger.LogInformation("Clone Engagement Issue initiated for queue issue '{Id}'", queueIssueId);

            Logger.LogInformation("Get Queue Issue '{Id}'", queueIssueId);
            var issueResponse = DataClient.QueueIssueGet(queueIssueId);
            if (!issueResponse.Success || issueResponse.Data == null)
            {
                throw new UiApiNotFoundException($"Queue Issue {queueIssueId} does not exist.");
            }

            var newIssue = issueResponse.Data;
            newIssue.Id = null;
            newIssue.IsCloned = true;
            newIssue.Vulnerability.Scanner.Id = Guid.NewGuid().ToString();

            Logger.LogInformation("New QueueIssue");
            var response = DataClient.QueueIssueAddUpdate(newIssue);

            DataClient.RefreshIndex(QueueIssue.GenerateIndex());

            Logger.LogInformation("Edit Engagement '{Id}' with new Issue Counts", newIssue.Saltminer.Engagement.Id);

            var attachmentsResponse = GetAllIssueAttachments(queueIssueId, null);

            await AttachmentHelper.CloneIssueAttachmentsAsync(response.Data.Saltminer.Engagement.Id, response.Data.Id, attachmentsResponse.Data.Where(attachment => attachment.IsMarkdown).Select(info => info.Attachment).ToList(), user?.UserName ?? "", user?.FullName ?? "", Config.FileRepository, ApiBaseUrl, Config.FileRepository, true);
            await AttachmentHelper.CloneIssueAttachmentsAsync(response.Data.Saltminer.Engagement.Id, response.Data.Id, attachmentsResponse.Data.Where(attachment => !attachment.IsMarkdown).Select(info => info.Attachment).ToList(), user?.UserName ?? "", user?.FullName ?? "", Config.FileRepository, ApiBaseUrl, Config.FileRepository);

            return new UiDataItemResponse<IssueFull>(new IssueFull(response.Data, UiApiConfig.AppVersion, MyFieldInfo));
        }

        public UiDataItemResponse<IssueFull> Edit(IssueEdit request, KibanaUser user)
        {
            Logger.LogInformation("Edit Engagement Issue initiated for Queue Issue '{Id}'", request.Id);
            request.IsModelValid(Config.ApiFieldRegex, Config.FailedRegexSplat, false, null, AttributeDefinitions(AttributeDefinitionType.Issue), TestedDropdowns);

            Logger.LogDebug("Get Queue Issue '{Id}'", request.Id);
            var issue = DataClient.QueueIssueGet(request.Id);
            if (!issue.Success || issue.Data == null)
            {
                throw new UiApiNotFoundException($"QueueIssue {request.Id} does not exist.");
            }

            Logger.LogDebug("Get CustomIssue Fields");
            Logger.LogDebug("Edit Queue Issue '{Id}'", request.Id);

            issue.Data.Vulnerability.TestStatus = EngagementHelper.ValidateTestStatus(issue.Data.Vulnerability.TestStatus, TestedDropdowns);
            var dataRequest = request.UpdateQueueIssue(issue.Data);
            var response = DataClient.QueueIssueAddUpdate(dataRequest);

            SetAttachments(response.Data.Saltminer.Engagement.Id, response.Data.Id, GetMarkdownAttachments(request, AttributeDefinitions(AttributeDefinitionType.Issue)), user, true, true);

            DataClient.RefreshIndex(QueueIssue.GenerateIndex());

            var dto = new IssueFull(response.Data, UiApiConfig.AppVersion, MyFieldInfo);
            return new UiDataItemResponse<IssueFull>(dto);
        }

        public UiDataItemResponse<IssueFull> Get(string issueId, FieldInfo fieldInfo)
        {
            Logger.LogInformation("Get Engagement Issue initiated Issue '{Id}'", issueId);

            General.ValidateIdAndInput(issueId, Config.ApiFieldRegex, "Id");

            IssueFull result = null;

            try
            {
                Logger.LogInformation("Get Queue Issue '{Id}'", issueId);
                var queueIssue = DataClient.QueueIssueGet(issueId);
                result = new IssueFull(queueIssue.Data, UiApiConfig.AppVersion, fieldInfo);
            }
            catch (DataClientResponseException ex)
            {
                if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation(ex, "QueueIssue {Id} does not exist.", issueId);
                }
            }
            if (result == null)
            {
                try
                {
                    Logger.LogInformation("Get Issue '{Id}'", issueId);
                    var issue = DataClient.IssueGet(issueId, UiApiConfig.AssetType, UiApiConfig.SourceType, UiApiConfig.Instance);
                    result = new IssueFull(issue.Data, UiApiConfig.AppVersion, fieldInfo);
                }
                catch (DataClientResponseException ex)
                {
                    if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.LogInformation(ex, "Issue {Id} does not exist.", issueId);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return new UiDataItemResponse<IssueFull>(result);
        }

        public UiDataItemResponse<Issue> FullView(string issueId)
        {
            Logger.LogInformation("Fullview issue initiated for id '{Id}'", issueId);

            if (string.IsNullOrEmpty(issueId))
            {
                throw new UiApiClientValidationException("Id not present in request.");
            }

            DataItemResponse<Issue> response = new();
            try
            {
                Logger.LogInformation("Get Issue '{Id}'", issueId);
                response = DataClient.IssueGet(issueId, UiApiConfig.AssetType, UiApiConfig.SourceType, UiApiConfig.Instance);
                return new UiDataItemResponse<Issue>(response.Data);
            }
            catch (DataClientResponseException ex)
            {
                if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation(ex, "Issue {Id} does not exist.", issueId);
                }
                else
                {
                    throw;
                }
            }

            return new UiDataItemResponse<Issue>(response.Data);
        }

        public UiDataItemResponse<IssueEditPrimer> EditPrimer(string issueId)
        {
            Logger.LogInformation("EditPrimer Engagement Issue initiated for Queue Issue '{Id}'", issueId);

            var user = GetCurrentUserOrThrow();

            if (string.IsNullOrEmpty(issueId))
            {
                throw new UiApiClientValidationException("Id not present in request.");
            }
            var draft = true;
            LockInfo lockInfo = null;
            IssueFull issue = null;

            try
            {
                Logger.LogInformation("Get Issue '{Id}'", issueId);
                var response = DataClient.IssueGet(issueId, UiApiConfig.AssetType, UiApiConfig.SourceType, UiApiConfig.Instance);
                issue = new IssueFull(response.Data, UiApiConfig.AppVersion, MyFieldInfo);
                draft = false;
            }
            catch (DataClientResponseException ex)
            {
                if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation(ex, "Issue {Id} does not exist.", issueId);
                }
            }

            if (issue == null)
            {
                Logger.LogInformation("Get Queue Issue '{Id}'", issueId);
                var response = DataClient.QueueIssueGetAndLock(issueId, user.UserName);
                lockInfo = response.Data.Lock;
                issue = new IssueFull(response.Data, UiApiConfig.AppVersion, MyFieldInfo);
            }


            Logger.LogDebug("Search Assets for Engagement '{Id}'", issueId);
            var assets = GetAllAssetsByEngagement(issue.Engagement.Id, draft)?.Data.Select(x => x.ToDropdownItem()).ToList();
            var asset = assets.FirstOrDefault(a => a.AssetId == issue.AssetId);
            if (issue.AssetName == null && asset != null)
                issue.AssetName = new(asset.Name, "AssetName", MyFieldInfo);

            return new UiDataItemResponse<IssueEditPrimer>(new IssueEditPrimer(Config.GuiFieldRegex)
            {
                LockInfo = lockInfo,
                EngagementTypeDropdown = EngagementTypeDropdowns,
                SeverityDropdown = SeverityDropdowns,
                AttributeDefinitions = AttributeDefinitions(AttributeDefinitionType.Issue),
                TestedDropdowns = TestedDropdowns,
                AssetDropdown = assets,
                Issue = issue,
                ActionRestrictions = MyFieldInfo.GetActionPermissions().ToList(),
                Attachments = GetAllIssueAttachments(issue.Id, false).Data.ToList(),
                ValidFileExtensions = Config.ValidFileExtensions,
                IssueFieldsThatRequireComments = Config.IssueFieldsThatRequireComments
            });
        }

        public UiDataItemResponse<IssueEditPrimer> NewPrimer(string engagementId)
        {
            Logger.LogInformation("NewPrimer Engagement Issue initiated for Engagement '{Id}'", engagementId);

            if (string.IsNullOrEmpty(engagementId))
            {
                throw new UiApiClientValidationException("Id not present in request.");
            }

            Logger.LogInformation("Search Asset Engagements for Engagement '{Id}'", engagementId);
            var engagement = DataClient.EngagementGet(engagementId)?.Data ?? throw new UiApiClientValidationMissingArgumentException($"Engagement {engagementId} not found.");

            var result = new IssueEditPrimer(Config.GuiFieldRegex)
            {
                SeverityDropdown = SeverityDropdowns,
                AttributeDefinitions = AttributeDefinitions(AttributeDefinitionType.Issue),
                ActionRestrictions = MyFieldInfo.GetActionPermissions().ToList(),
                AssetDropdown = GetAllAssetsByEngagement(engagementId, true)?.Data?.Select(x => x.ToDropdownItem()).ToList(),
                IsTemplate = engagement.Saltminer.Engagement.Subtype == "Template",
                Issue = new(engagement, MyFieldInfo)
            };

            return new UiDataItemResponse<IssueEditPrimer>(result);

        }

        public UiDataItemResponse<LockInfo> RefreshEditLock(string issueId, string userName)
        {
            Logger.LogInformation("EditPrimer Engagement Issue initiated for Queue Issue '{Id}'", issueId);

            if (string.IsNullOrEmpty(issueId))
            {
                throw new UiApiClientValidationException("Id not present in request.");
            }

            Logger.LogInformation("Get Queue Issue '{Id}'", issueId);
            var issue = DataClient.QueueIssueGetAndLock(issueId, userName ?? "elastic");
            return new UiDataItemResponse<LockInfo>(issue.Data.Lock);
        }

        public UiNoDataResponse TemplateDelete(string issueId)
        {
            Logger.LogInformation("Delete Engagement Issue initiated for Queue Issue '{Id}'", issueId);

            General.ValidateIdAndInput(issueId, Config.ApiFieldRegex, "Id");

            Logger.LogInformation("Get Queue Issue '{Id}'", issueId);
            var issue = DataClient.QueueIssueGet(issueId);
            if (!issue.Success || issue.Data == null)
            {
                throw new UiApiNotFoundException($"QueueIssue {issueId} does not exist.");
            }

            //todo: Keith/Eddie Validate the engagement for the issue is a template engagement
            //get engagement from issue.engagement.id
            //validate engagement.subtype = template
            Logger.LogInformation("Delete Queue Issue '{Id}'", issueId);
            
            DataClient.QueueIssueDelete(issueId);
            DataClient.AttachmentDeleteAllIssue(issueId);

            DataClient.RefreshIndex(QueueIssue.GenerateIndex());

            return new UiNoDataResponse(1);
        }

        public UiNoDataResponse TemplateDeletes(DeleteById request)
        {
            Logger.LogInformation("Template Issues Delete initiated");

            request.IsModelValid(Config.ApiFieldRegex);

            foreach (var issueId in request.Ids)
            {
                //todo: Keith/Eddie Validate the engagementg for the issue is a template engagement
                //get engagement from issue.engagement.id
                //validdatge engagement.subtype = template
                DataClient.QueueIssueDelete(issueId);
                DataClient.AttachmentDeleteAllIssue(issueId);
            }

            DataClient.RefreshIndex(QueueIssue.GenerateIndex());

            return new UiNoDataResponse(request.Ids.Count);
        }

        public UiBulkResponse MarkRemoved(DeleteById request)
        {
            Logger.LogInformation("Mark Engagement Issues Removed initiated");

            request.IsModelValid(Config.ApiFieldRegex);

            var updateQueueIssues = new List<QueueIssue>();

            foreach (var issueId in request.Ids)
            {
                var queueIssue = DataClient.QueueIssueGet(issueId).Data;
                queueIssue.Vulnerability.RemovedDate = DateTime.UtcNow;
                updateQueueIssues.Add(queueIssue);
            }

            if (updateQueueIssues.Count > 0)
            {
                Logger.LogInformation("Edit Queue Issues");
                var response = DataClient.QueueIssuesAddUpdateBulk(updateQueueIssues);
                DataClient.RefreshIndex(QueueIssue.GenerateIndex());
                return new UiBulkResponse(response);
            }
            else
            {
                Logger.LogInformation("No Issues to Update.");
                return new UiBulkResponse(0, "No Issues to Update");
            }
        }
    }
}
