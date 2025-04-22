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
using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.Ui.Api.Helpers;
using Saltworks.SaltMiner.Ui.Api.Models;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;
using Saltworks.SaltMiner.Ui.Api.Authentication;
using Saltworks.SaltMiner.Ui.Api.Extensions;
using Saltworks.SaltMiner.UiApiClient.ViewModels;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Import;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Attributes;

namespace Saltworks.SaltMiner.Ui.Api.Contexts
{
    public class EngagementContext : ContextBase
    {
        private readonly string FullEngagementFile = "FullEngagement.json";
        private readonly EngagementImporter EngagementImporter;
        private readonly EngagementHelper EngagementHelper;
        private readonly AttachmentHelper AttachmentHelper;
        private List<FieldFilter> _SearchDisplays = [];
        private List<FieldFilter> _SortDisplays = [];

        protected override List<SearchFilterValue> SearchFilterValues => SearchFilters?.Find(x => x.Type == SearchFilterType.EngagementSearchFilters.ToString())?.Filters ?? [];
        protected override List<FieldFilter> SearchDisplays
        {
            get
            {
                if (_SearchDisplays.Count == 0)
                    _SearchDisplays = SearchFilterValues?.Select(x => new FieldFilter(x)).ToList() ?? [];
                return _SearchDisplays;
            }
        }
        protected override List<SearchFilterValue> SortFilterValues => SearchFilters?.Find(x => x.Type == SearchFilterType.EngagementSortFilters.ToString())?.Filters ?? [];
        protected override List<FieldFilter> SortDisplays
        {
            get
            {
                if (_SortDisplays.Count == 0)
                    _SortDisplays = SortFilterValues?.Select(x => new FieldFilter(x)).ToList() ?? [];
                return _SortDisplays;
            }
        }

        public EngagementContext(IServiceProvider services, ILogger<EngagementContext> logger) : base(services, logger)
        {
            EngagementImporter = new EngagementImporter(DataClient, Logger);
            EngagementHelper = new EngagementHelper(DataClient, Logger);
            AttachmentHelper = new AttachmentHelper(DataClient, Logger);
        }

        private FieldInfo _MyFieldInfo = null;
        private FieldInfo MyFieldInfo
        {
            get
            {
                _MyFieldInfo ??= FieldInfo(FieldInfoEntityType.Engagement);
                return _MyFieldInfo;
            }
        }

        public UiNoDataResponse GenerateReport(string id, string template, KibanaUser user)
        {
            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "Id");
            General.ValidateInput(template, Config.ApiFieldRegex, "template");

            var engagementResponse = DataClient.EngagementGet(id);
            if (!engagementResponse.Success || engagementResponse.Data == null)
            {
                throw new UiApiNotFoundException($"Engagement {id} does not exist.");
            }

            var jobQueue = new Job
            {
                Status = Job.JobStatus.Pending.ToString("g"),
                Type = Job.JobType.EngagementReport.ToString("g"),
                TargetId = id,
                User = user?.UserName ?? "elastic",
                UserFullName = user?.FullName ?? "elastic",
                Message = string.Empty,
                Attributes = new Dictionary<string, string>
                {
                    { "Template", template }
                }
            };

            DataClient.JobAddUpdate(jobQueue);

            return new UiNoDataResponse(1);
        }

        public UiDataItemResponse<EngagementPrimer> Primer()
        {
            Logger.LogInformation("Engagement Primer: {Msg}", GeneralExtensions.SearchUIPagingLoggerMessage("Engagement", 0, Config.DefaultPageSize, 1));

            return new UiDataItemResponse<EngagementPrimer>(new EngagementPrimer(Config.GuiFieldRegex)
            {
                SearchFilters = SearchDisplays,
                SortFilterOptions = SortDisplays,
                SubtypeDropdowns = SubtypeDropdowns,
                CreatedHeader = Config.EngagementCreatedHeader,
                EngagementHeader = Config.EngagementNameHeader,
                CustomerHeader = Config.EngagementCustomerHeader
            });
        }

        public UiDataResponse<EngagementSummary> Search(EngagementSearch request)
        {
            Logger.LogDebug("Search for Engagements initiated");

            request.IsModelValid(Config.ApiFieldRegex);

            var filters = new Dictionary<string, string>();
            if (request.SearchFilters != null)
            {
                foreach (var filter in request.SearchFilters)
                {
                    filter.Value = Regex.Replace(filter.Value, @"[\+\-\=\&\|\>\<\!\(\)\{\}\[\]\^""\~\*\?\:\/]", " ");
                }
                Helpers.SearchFilters.AddFilters(filters, SearchFilterValues, request.SearchFilters);
            }

            var searchRequest = new SearchRequest()
            {
                UIPagingInfo = new UIPagingInfo(request.Pager?.Size ?? Config.DefaultPageSize, 1)
                {
                    SortFilters = Helpers.SearchFilters.MapSortFilters(request.Pager?.SortFilters, SortFilterValues) ?? []
                },
                Filter = new()
                {
                    AnyMatch = true,
                    FilterMatches = filters,
                    SubFilter = new()
                    {
                        AnyMatch = false,
                        FilterMatches = new Dictionary<string, string> { 
                            { "Saltminer.Engagement.Subtype", SaltMiner.DataClient.Helpers.BuildExcludeTermsFilterValue([ "Template" ]) },
                        }
                    }
                }
            };

            if (!request.ShowHistorical)
            {
                searchRequest.Filter.SubFilter.FilterMatches.Add("Saltminer.Engagement.Status", SaltMiner.DataClient.Helpers.BuildExcludeTermsFilterValue([ "Historical" ]));
            }

            if (searchRequest.UIPagingInfo.SortFilters.Count == 0)
            {
                searchRequest.UIPagingInfo.SortFilters.Add("Saltminer.Engagement.Status", true);
                searchRequest.UIPagingInfo.SortFilters.Add("LastUpdated", false);
            }

            //loop until page found
            var response = DataClient.EngagementSearch(searchRequest);

            while (response.Success && response.Data != null && response.Data.Any())
            {
                searchRequest.UIPagingInfo = response.UIPagingInfo;
                if ((request.Pager?.Page == null || request.Pager.Page == 1 || request.Pager.Page == 0) || searchRequest.UIPagingInfo.Page == request.Pager.Page)
                {
                    Logger.LogInformation("{Msg}", GeneralExtensions.SearchUIPagingLoggerMessage("Engagement", filters.Count, response.UIPagingInfo.Size, response.UIPagingInfo.Page));
                    var engagements = response.Data.Select(x => new EngagementSummary(x, MyFieldInfo)).ToList();
                    foreach (var engagement in engagements)
                    {
                        engagement.IssueCount = GetEngagementIssueCount(engagement.Id).Data;
                    }
                    return new UiDataResponse<EngagementSummary>(engagements, response, SortFilterValues, response.UIPagingInfo, false);
                }

                searchRequest.UIPagingInfo.Page++;
                searchRequest.AfterKeys = response.AfterKeys;

                response = DataClient.EngagementSearch(searchRequest);
            }

            return new UiDataResponse<EngagementSummary>([]);
        }

        public UiDataItemResponse<EngagementSummary> SummaryEdit(EngagementSummaryEdit request, KibanaUser user)
        {
            Logger.LogInformation("Summary Edit for Engagement '{Id}' initiated", request?.Id);

            if (string.IsNullOrEmpty(request?.Id))
            {
                throw new UiApiClientValidationException("Id not present in request.");
            }
            request.IsModelValid(Config.ApiFieldRegex, Config.FailedRegexSplat, false, SubtypeDropdowns, MyFieldInfo.AttributeDefinitions.ToList());

            Logger.LogInformation("Get Engagement '{Id}'", request.Id);
            var engagement = DataClient.EngagementGet(request.Id);

            if (!engagement.Success || engagement.Data == null)
            {
                throw new UiApiNotFoundException($"Engagement {request.Id} does not exist.");
            }
            if (engagement.Data.Saltminer.Engagement.Status != EnumExtensions.GetDescription(EngagementStatus.Draft))
            {
                throw new UiApiClientValidationException($"Engagement {request.Id} has been published.");
            }
            if (engagement.Data.Saltminer.Engagement.Name != request.Name && !EngagementHelper.VerifyUniqueEngagementName(request.Name, UiApiConfig.AssetType))
            {
                throw new UiApiClientValidationException("Engagement Name must be unique.");
            }

            bool isChange = false;

            if (engagement.Data.Saltminer.Engagement.Name != request.Name)
            {
                engagement.Data.Saltminer.Engagement.Name = request.Name;
                isChange = true;
            }
            if (engagement.Data.Saltminer.Engagement.Summary != request.Summary)
            {
                engagement.Data.Saltminer.Engagement.Summary = request.Summary;
                isChange = true;
            }
            if (engagement.Data.Saltminer.Engagement.Customer != request.Customer)
            {
                engagement.Data.Saltminer.Engagement.Customer = request.Customer;
                isChange = true;
            }
            if (engagement.Data.Saltminer.Engagement.Subtype != request.Subtype)
            {
                engagement.Data.Saltminer.Engagement.Subtype = request.Subtype;
                isChange = true;
            }
            if (!engagement.Data.Saltminer.Engagement.Attributes.DictionaryEqual(request.Attributes))
            {
                engagement.Data.Saltminer.Engagement.Attributes = request.Attributes;
                isChange = true;
            }

            var attachments = GetAllEngagementOnlyAttachments(request.Id, true)?.Data?.ToList() ?? [];
            var currentAttachmentsInfo = attachments.Select(x => x.Attachment).ToList();
            var requestAttachmentsInfo = GetMarkdownAttachments(request, MyFieldInfo.AttributeDefinitions.ToList());

            var currNotReq = currentAttachmentsInfo.Except(requestAttachmentsInfo).ToList();
            var reqNotCurr = requestAttachmentsInfo.Except(currentAttachmentsInfo).ToList();

            if (currNotReq.Count + reqNotCurr.Count > 0)
            {
                SetAttachments(request.Id, null, requestAttachmentsInfo, user, true, true);
            }


            if (isChange)
            {
                Logger.LogInformation("Edit Engagement '{Id}'", request.Id);
                DataClient.EngagementAddUpdate(engagement.Data);

                var engagementInfo = new EngagementInfo
                {
                    Attributes = engagement.Data.Saltminer.Engagement.Attributes,
                    Customer = engagement.Data.Saltminer.Engagement.Customer,
                    Id = engagement.Data.Id,
                    Name = engagement.Data.Saltminer.Engagement.Name,
                    PublishDate = engagement.Data.Saltminer.Engagement.PublishDate,
                    Subtype = engagement.Data.Saltminer.Engagement.Subtype
                };

                UpdateQueueScanEngagementData(engagementInfo);
                UpdateQueueAssetEngagementData(engagementInfo);
                UpdateQueueIssueEngagementData(engagementInfo);
            }

            return Summary(request.Id);
        }

        public UiNoDataResponse Delete(string engagementId)
        {
            Logger.LogInformation("Delete Engagement for '{Id}' initiated", engagementId);

            General.ValidateIdAndInput(engagementId, Config.ApiFieldRegex, "Id");

            Logger.LogInformation("Delete Engagement for '{Id}'", engagementId);
            var response = DataClient.EngagementDelete(engagementId);
            if (!response.Success || response.Affected == 0)
            {
                throw new UiApiNotFoundException($"Engagement {engagementId} does not exist.");
            }

            DataClient.RefreshIndex(Engagement.GenerateIndex());

            return new UiNoDataResponse(1);
        }

        public UiNoDataResponse DeleteGroup(string groupId)
        {
            Logger.LogInformation("Delete Engagement for group for '{Id}'", groupId);
            
            General.ValidateIdAndInput(groupId, Config.ApiFieldRegex, "Id");
            
            var response = DataClient.EngagementGroupDelete(groupId);
            if (!response.Success || response.Affected == 0)
            {
                throw new UiApiNotFoundException($"Engagement Group {groupId} does not exist.");
            }

            DataClient.RefreshIndex(Engagement.GenerateIndex());

            return new UiNoDataResponse(1);
        }

        public UiNoDataResponse ResetPublish(string id)
        {
            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "Id");

            var engagementResponse = DataClient.EngagementGet(id);

            if (!engagementResponse.Success || engagementResponse.Data == null)
            {
                throw new UiApiNotFoundException($"Engagement {id} does not exist.");
            }

            if (engagementResponse.Data.Saltminer.Engagement.Status != EnumExtensions.GetDescription(EngagementStatus.Error))
            {
                throw new UiApiClientValidationException($"This Engagement {id} is not in an Error status and can not be reset for publish.");
            }

            var engagement = engagementResponse.Data;

            DataClient.EngagementUpdateStatus(engagement.Id, EngagementStatus.Draft);
            var queueScan = DataClient.QueueScanGetByEngagement(engagement.Id).Data;

            DataClient.QueueScanUpdateStatus(queueScan.Id, QueueScanStatus.Loading);

            // delete all scans/assets/issues created during publish attempt
            DataClient.IssuesDeleteAllByEngagementId(engagement.Id, queueScan.Saltminer.Scan.AssetType, queueScan.Saltminer.Scan.SourceType, queueScan.Saltminer.Scan.Instance);

            return new UiNoDataResponse(1);
        }

        public UiNoDataResponse Queue(string id)
        {
            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "Id");

            var engagementResponse = DataClient.EngagementGet(id);
            var attributeDefinitions = MyFieldInfo.AttributeDefinitions;

            if (!engagementResponse.Success || engagementResponse.Data == null)
            {
                throw new UiApiNotFoundException($"Engagement {id} does not exist.");
            }

            if (engagementResponse.Data.Saltminer.Engagement.Status != EnumExtensions.GetDescription(EngagementStatus.Draft))
            {
                throw new UiApiClientValidationException($"This Engagement {id} is not in a Draft status and can not be queued for publish.");
            }

            var missingRequired = attributeDefinitions.Where(ad => ad.Required && engagementResponse.Data.Saltminer.Engagement.Attributes.Any(a => a.Key == ad.Name && string.IsNullOrEmpty(a.Value))).ToList();
            if (missingRequired.Count > 0)
            {
                var missingFields = string.Join(", ", missingRequired.Select(x => x.Display));
                Logger.LogError("Missing required attribute values: {Missing}. Cannot publish.", missingFields);
                throw new UiApiClientValidationMissingValueException("Missing required attribute values. Cannot Publish.");
            }

            var queueScan = DataClient.QueueScanGetByEngagement(id).Data;

            // if setting present, check for asset attribute key to add to inventory asset
            if (!string.IsNullOrEmpty(Config.InventoryAssetKeyAttribute))
            {
                var assetRequest = new SearchRequest()
                {
                    Filter = new Filter
                    {
                        FilterMatches = new Dictionary<string, string>
                        {
                            {
                                "Saltminer.Internal.QueueScanId", queueScan.Id
                            }
                        }
                    },
                    UIPagingInfo = new UIPagingInfo(300)
                };

                var queueAssets = new List<QueueAsset>();
                var queueAssetsResponse = DataClient.QueueAssetSearch(assetRequest);

                while (queueAssetsResponse.Success && queueAssetsResponse.Data != null && queueAssetsResponse.Data.Any())
                {
                    foreach (var asset in queueAssetsResponse.Data.Select(a => a.Saltminer))
                    {
                        var inventoryAssetKeyAttribute = EngagementHelper.GetInventoryAssetKeyValue(Config.InventoryAssetKeyAttribute, asset.Asset.Attributes);
                        asset.InventoryAsset = inventoryAssetKeyAttribute;
                    }
                    queueAssets.AddRange(queueAssetsResponse.Data);

                    DataClient.QueueAssetAddUpdateBulk(queueAssets);
                    queueAssets.Clear();

                    assetRequest.UIPagingInfo = queueAssetsResponse.UIPagingInfo;
                    assetRequest.UIPagingInfo.Page++;
                    assetRequest.AfterKeys = queueAssetsResponse.AfterKeys;

                    queueAssetsResponse = DataClient.QueueAssetSearch(assetRequest);
                }
            }

            DataClient.QueueScanUpdateStatus(queueScan.Id, QueueScanStatus.Pending);

            engagementResponse.Data.Saltminer.Engagement.Status = EnumExtensions.GetDescription(EngagementStatus.Queued);
            DataClient.EngagementAddUpdate(engagementResponse.Data);

            // Update parent (published) engagement status to 'Historical'
            var parentEngagementResp = DataClient.EngagementSearch(new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Engagement.GroupId", engagementResponse.Data.Saltminer.Engagement.GroupId },
                        { "Saltminer.Engagement.Status", EngagementStatus.Published.ToString("g") }
                    }
                }
            });
            var parentEngagement = parentEngagementResp.Data.FirstOrDefault();
            if (parentEngagement != null)
            {
                parentEngagement.Saltminer.Engagement.Status = EngagementStatus.Historical.ToString("g");
                DataClient.EngagementAddUpdate(parentEngagement);
                DataClient.SetHistoricalIssues(parentEngagement.Id, engagementResponse.Data.Saltminer.Engagement.GroupId);
            }
            
            DataClient.RefreshIndex(Engagement.GenerateIndex());


            return new UiNoDataResponse(1);
        }

        public async Task<UiDataItemResponse<string>> CheckoutAsync(string id, KibanaUser user)
        {
            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "Id");

            var fullEngagement = FullEngagement(id).Data;
            List<UiComment> engagementComments = fullEngagement.Comments.Where(x => x.ScanId == fullEngagement.Scan.ScanId).ToList();

            if (!Config.EngagementCheckoutWithSystemComments)
            {
                engagementComments = fullEngagement.Comments.Where(x => x.Type == "User" && x.ScanId == fullEngagement.Scan.ScanId).ToList();
            }

            if (fullEngagement.Status == EnumExtensions.GetDescription(EngagementStatus.Historical))
            {
                throw new UiApiClientValidationException($"This Engagement {id} is set as 'Historical' and can not be checked out.");
            }

            if (fullEngagement.Status != EnumExtensions.GetDescription(EngagementStatus.Published))
            {
                throw new UiApiClientValidationException($"This Engagement {id} has not been published and can not be checked out.");
            }

            var newSummaryRequest = new UiApiClient.Requests.EngagementNew()
            {
                Customer = fullEngagement.Customer,
                Name = EngagementHelper.CreateUniqueEngagementName(fullEngagement.Name, UiApiConfig.AssetType),
                Summary = fullEngagement.Summary,
                Subtype = fullEngagement.Subtype,
                GroupId = fullEngagement.GroupId
            };

            var newEngagement = Create(newSummaryRequest);

            await AttachmentHelper.CloneEngagementAttachmentsAsync(newEngagement.Data.Id, fullEngagement.Attachments.Where(x => x.IssueId == null).ToList(), fullEngagement.Attributes, user?.UserName, user?.FullName, Config.FileRepository, ApiBaseUrl);

            var newEngagementResponse = DataClient.EngagementGet(newEngagement.Data.Id);
            if (!newEngagementResponse.Success || newEngagementResponse.Data == null)
            {
                throw new UiApiNotFoundException($"Engagement {id} does not exist.");
            }
            var engagement = newEngagementResponse.Data;

            var newScan = DataClient.QueueScanGetByEngagement(engagement.Id).Data;

            engagement.Saltminer.Engagement.Attributes = fullEngagement.Attributes.ToDictionary(k => k.Name, v => v.Value);

            var queueScan = DataClient.QueueScanGetByEngagement(engagement.Id).Data;

            var assetBatch = new List<QueueAsset>();
            var issueBatch = new List<QueueIssue>();
            var commentBatch = new List<Comment>();

            foreach (var asset in fullEngagement.Assets)
            {
                var newAsset = asset.CloneRequest(engagement, queueScan.Id, UiApiConfig.AssetType, UiApiConfig.Instance, UiApiConfig.SourceType);

                assetBatch.Add(newAsset);

                if (assetBatch.Count >= Config.ImportBatchSize)
                {
                    DataClient.QueueAssetAddUpdateBulk(assetBatch);
                    assetBatch = [];
                }

                var engagementIssues = fullEngagement.Issues.Where(x => x.AssetId == asset.AssetId && !x.IsRemoved);

                if (Config.EngagementCheckoutWithClosedIssues)
                {
                    engagementIssues = fullEngagement.Issues.Where(x => x.AssetId == asset.AssetId);
                }

                foreach (var issue in engagementIssues)
                {
                    var attachmentList = GetAllIssueAttachments(issue.Id).Data.ToList();
                    issue.TestStatus.Value = EngagementHelper.ValidateTestStatus(issue.TestStatus.Value, TestedDropdowns);
                    var cloneIssue = issue.CloneRequest(engagement, queueScan.Id, newAsset.Id, UiBaseUrl);

                    //copy issue comments
                    foreach (var pubComment in engagementComments.Where(x => x.IssueId == issue.Id))
                    {
                        commentBatch.Add(CreateComment(pubComment, engagement.Id, newScan.Id, newAsset.Id, cloneIssue.Id));

                        if (commentBatch.Count >= Config.ImportBatchSize)
                        {
                            DataClient.CommentAddUpdateBulk(commentBatch);
                            commentBatch = [];
                        }
                    }


                    issueBatch.Add(cloneIssue);
                    SetAttachments(engagement.Id, cloneIssue.Id, attachmentList.Where(attachment => attachment.IsMarkdown).Select(info => info.Attachment).ToList(), user, true, true);
                    SetAttachments(engagement.Id, cloneIssue.Id, attachmentList.Where(attachment => !attachment.IsMarkdown).Select(info => info.Attachment).ToList(), user);

                    if (issueBatch.Count >= Config.ImportBatchSize)
                    {
                        DataClient.QueueIssuesAddUpdateBulk(issueBatch);
                        issueBatch = [];
                    }
                }

                //copy asset comments
                foreach (var pubComment in engagementComments.Where(x => x.IssueId == null && x.AssetId == asset.AssetId))
                {
                    commentBatch.Add(CreateComment(pubComment, engagement.Id, newScan.Id, newAsset.Id, null));

                    if (commentBatch.Count >= Config.ImportBatchSize)
                    {
                        DataClient.CommentAddUpdateBulk(commentBatch);
                        commentBatch = [];
                    }
                }
            }

            // copy engagement comments
            foreach (var pubComment in engagementComments.Where(x => x.AssetId == null && x.IssueId == null))
            {
                commentBatch.Add(CreateComment(pubComment, engagement.Id, newScan.Id, null, null));

                if (commentBatch.Count >= Config.ImportBatchSize)
                {
                    DataClient.CommentAddUpdateBulk(commentBatch);
                    commentBatch = [];
                }
            }
            
            if (assetBatch.Count > 0)
            {
                DataClient.QueueAssetAddUpdateBulk(assetBatch);
            }

            if (issueBatch.Count > 0)
            {
                DataClient.QueueIssuesAddUpdateBulk(issueBatch);
            }
            
            if (commentBatch.Count > 0)
            {
                DataClient.CommentAddUpdateBulk(commentBatch);
            }

            engagement.Saltminer.Engagement.GroupId = fullEngagement.GroupId;

            DataClient.EngagementAddUpdate(engagement);
            DataClient.QueueScanAddUpdate(queueScan);

            DataClient.EngagementAddUpdate(fullEngagement.ToEngagement());

            return new UiDataItemResponse<string>(engagement.Id);
        }

        public UiNoDataResponse Cancel(string id, KibanaUser user)
        {
            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "Id");

            var engagement = DataClient.EngagementGet(id);

            if (engagement.Data.Saltminer.Engagement.Status != EngagementStatus.Draft.ToString())
            {
                if (user.Roles.TrueForAll(x => x != EnumExtensions.GetDescription(SysRole.PentestAdmin) && x != EnumExtensions.GetDescription(SysRole.SuperUser)))
                {
                    throw new UiApiAuthException("You are only authorized to Cancel engagements in a 'Draft' state.");
                }
                engagement.Data.Saltminer.Engagement.Status = EnumExtensions.GetDescription(EngagementStatus.Historical);
                DataClient.EngagementAddUpdate(engagement.Data);
                DataClient.SetHistoricalIssues(engagement.Data.Id, engagement.Data.Saltminer.Engagement.GroupId);
            }
            else
            {
                var parentEngagement = DataClient.EngagementSearch(new SearchRequest
                {
                    Filter = new Filter
                    {
                        FilterMatches = new Dictionary<string, string> {
                        { 
                                "Saltminer.Engagement.GroupId", engagement.Data.Saltminer.Engagement.GroupId }
                        }
                    },
                    UIPagingInfo = new UIPagingInfo
                    {
                        Page = 1,
                        Size = 1,
                        SortFilters = new Dictionary<string, bool>
                        {
                            { "Saltminer.Engagement.PublishDate", false }
                        }
                    }
                });

                var parent = parentEngagement.Data.FirstOrDefault();

                if (parent != null)
                {
                    parent.Saltminer.Engagement.Status = EnumExtensions.GetDescription(EngagementStatus.Published);

                    DataClient.EngagementAddUpdate(parent);
                    DataClient.UnSetHistoricalIssues(parent.Id);
                }

                DataClient.EngagementDelete(id);
            }

            DataClient.RefreshIndex(Engagement.GenerateIndex());

            return new UiNoDataResponse(1);
        }

        public UiDataItemResponse<EngagementSummary> Summary(string engagementId)
        {
            Logger.LogInformation("Summary for Engagement '{Id}' initiated", engagementId);

            General.ValidateIdAndInput(engagementId, Config.ApiFieldRegex, "Id");

            Logger.LogInformation("Get Engagement '{Id}'", engagementId);
            var result = DataClient.EngagementGet(engagementId);
            if (!result.Success || result.Data == null)
            {
                throw new UiApiNotFoundException($"Engagement {engagementId} does not exist.");
            }

            var summary = new EngagementSummary(result.Data, MyFieldInfo)
            {
                Attachments = GetAllEngagementOnlyAttachments(engagementId, false).Data.ToList(),
                IssueCount = GetEngagementIssueCount(engagementId).Data
            };

            if (summary.Status == EngagementStatus.Published.ToString("g") || summary.Status == EngagementStatus.Historical.ToString("g"))
            {
                summary.ScanId = DataClient.ScanGetByEngagement(summary.Id).Data.Id;
            }
            else
            {
                summary.ScanId = DataClient.QueueScanGetByEngagement(summary.Id).Data.Id;
            }

            return new UiDataItemResponse<EngagementSummary>(summary, result);
        }

        public byte[] ExportEngagement(string engagementId)
        {
            Logger.LogInformation("Export initiated");

            General.ValidateIdAndInput(engagementId, Config.ApiFieldRegex, "Id");

            Logger.LogInformation("Get Full Engagement '{Id}'", engagementId);
            var fullEngagement = FullEngagement(engagementId);

            if (!fullEngagement.Success || fullEngagement.Data == null)
            {
                throw new UiApiNotFoundException($"Engagement {engagementId} does not exist.");
            }
            var fullEngagementExport = new EngagementExport(fullEngagement.Data)
            {
                Attributes = EngagementHelper.FilterInternalAndMergeAttributes(fullEngagement.Data.Attributes.ToDictionary(k => k.Name, v => v.Value), engagementId)
            };
            var content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(fullEngagementExport));
            Logger.LogInformation($"Zip data");

            using var zipContent = new MemoryStream();
            var archive = new ZipArchive(zipContent, ZipArchiveMode.Update);
            AddZipEntry(FullEngagementFile, content, archive);
            fullEngagementExport.Attachments.Select(x => x.Attachment).ToList().ForEach(a => ValidateAndAdd(a.FileId, archive));
            archive.Dispose();
            Logger.LogInformation("Export complete");
            return zipContent.ToArray();
        }

        public byte[] ExportImportIssuesByEngagement(string engagementId)
        {
            Logger.LogInformation("ExportImportIssuesByEngagement initiated");

            General.ValidateIdAndInput(engagementId, Config.ApiFieldRegex, "Id");

            var fullEngagement = FullEngagement(engagementId).Data;
            var result = new List<IssueImportSummary>();

            foreach(var asset in fullEngagement.Assets)
            {
                var issues = fullEngagement.Issues.Where(x => x.AssetId == asset.AssetId);
                result.AddRange(issues.Select((x) => {
                    var result = new IssueImportSummary(x, asset);
                    ScrubMarkdownAttachments(result.Issue, FieldInfo(FieldInfoEntityType.Issue).AttributeDefinitions.ToList());
                    return result;
                }));
            }
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result));
        }

        public async Task<UiDataItemResponse<string>> ImportEngagementFromFileAsync(IFormFile file, KibanaUser user, bool createNew = false)
        {
            return new UiDataItemResponse<string>(await EngagementImporter.ImportEngagementFromFileAsync(new EngagementImport
            {
                ApiBaseUrl = ApiBaseUrl,
                AssetType = UiApiConfig.AssetType,
                ImportBatchSize = Config.ImportBatchSize,
                CreateNew = createNew,
                File = file,
                FileRepo = Config.FileRepository,
                Instance = UiApiConfig.Instance,
                MaxImportFileSize = Config.MaxImportFileSize,
                SourceType = UiApiConfig.SourceType,
                UiBaseUrl = UiBaseUrl,
                UserName = user?.UserName ?? "", 
                UserFullName = user?.FullName ?? "",
                InventoryAssetKeyAttribute = Config.InventoryAssetKeyAttribute,
                TestStatusLookups = TestedDropdowns
            }));
        }

        public UiDataItemResponse<string> GetEngagementImportJSON()
        {
            Logger.LogInformation("Get Engagement Import initiated");
            return new UiDataItemResponse<string>(FileHelper.SearchFile(Config.EngagementImportTemplateFileName, Config.TemplateRepository));
        }

        public UiDataItemResponse<EngagementSummary> Create(UiApiClient.Requests.EngagementNew request)
        {
            Logger.LogInformation("Create Engagement initiated");

            request.IsModelValid(Config.ApiFieldRegex, Config.FailedRegexSplat, false, SubtypeDropdowns, MyFieldInfo.AttributeDefinitions.ToList());

            if (!EngagementHelper.VerifyUniqueEngagementName(request.Name, UiApiConfig.AssetType))
            {
                throw new UiApiClientValidationException("Engagement Name must be unique.");
            }

            Logger.LogInformation("New Engagement");
            var engagement = DataClient.EngagementAddUpdate(request.TransformNewEngagement());

            Logger.LogInformation("Creating New Queue Scan");
            var queueScan = EngagementHelper.CreateEngagementQueueScan(engagement.Data.Saltminer.Engagement.Name, engagement.Data.Id, UiApiConfig.SourceType, UiApiConfig.AssetType, UiApiConfig.Instance, engagement.Data.Saltminer.Engagement.Subtype, engagement.Data.Saltminer.Engagement.Customer);

            DataClient.RefreshIndex(Engagement.GenerateIndex());

            UpdateEngagementQueueScan(queueScan, engagement.Data);

            DataClient.RefreshIndex(GenerateIndex());
            
            return new UiDataItemResponse<EngagementSummary>(new EngagementSummary(engagement.Data, MyFieldInfo));
        }

        public DataItemResponse<EngagementSummary> Template()
        {
            Logger.LogInformation("Template Engagement initiated");

            var searchReqeust = new SearchRequest()
            {
                Filter = new()
                {
                    AnyMatch = true,
                    FilterMatches = new Dictionary<string, string> { { "Saltminer.Engagement.Subtype" , "Template" } },
                }
            };

            var template = DataClient.EngagementSearch(searchReqeust)?.Data?.FirstOrDefault();

            if (template != null)
            {
                return new DataItemResponse<EngagementSummary>(new EngagementSummary(template, MyFieldInfo));
            }

            Logger.LogInformation("Template Engagement");
            var engagement = DataClient.EngagementAddUpdate(new Engagement
            {
                Timestamp = DateTime.UtcNow,
                Saltminer = new SaltMinerEngagementWrapper()
                {
                    Engagement = new SaltMinerEngagementInfo
                    {
                        Name = "Template",
                        Customer = "Template",
                        Summary = "Template",
                        PublishDate = null,
                        Subtype = "Template",
                        Attributes = [],
                        GroupId = Guid.NewGuid().ToString(),
                        Status = EnumExtensions.GetDescription(EngagementStatus.Draft)
                    }
                }
            }).Data;
            DataClient.RefreshIndex(Engagement.GenerateIndex());

            Logger.LogInformation("Creating New Queue Scan");
            var queueScan = EngagementHelper.CreateEngagementQueueScan("Template", engagement.Id, UiApiConfig.SourceType, UiApiConfig.AssetType, UiApiConfig.Instance, "Template", "Template");

            Logger.LogInformation("New Queue Asset");
            DataClient.QueueAssetAddUpdate(new QueueAsset
            {
                Saltminer = new()
                {
                    Asset = new()
                    {
                        Attributes = [],
                        Description = "Template",
                        IsProduction = true,
                        Name = "Template",
                        Instance = UiApiConfig.Instance,
                        SourceType = UiApiConfig.SourceType,
                        IsSaltminerSource = true,
                        SourceId = Guid.NewGuid().ToString(),
                        Version = "Template",
                        VersionId = "Template",
                        AssetType = UiApiConfig.AssetType,
                        IsRetired = false,
                        LastScanDaysPolicy = Config.LastScanDaysPolicy,
                        Host = null,
                        Ip = null,
                        Port = 0,
                        Scheme = null,
                        ScanCount = 0
                    },
                    Internal = new()
                    {
                        QueueScanId = queueScan.Id,
                    },
                    Engagement = new()
                    {
                        Id = engagement.Id,
                        Name = engagement.Saltminer.Engagement.Name,
                        Subtype = engagement.Saltminer.Engagement.Subtype
                    }
                },
                Timestamp = DateTime.UtcNow
            });

            DataClient.RefreshIndex(QueueAsset.GenerateIndex());
            return new DataItemResponse<EngagementSummary>(new EngagementSummary(engagement, MyFieldInfo));
        }

        public DataItemResponse<EngagementFull> FullEngagement(string id)
        {
            Logger.LogInformation("Full Engagement '{Id}' initiated", id);
            EngagementFull fullEngagement = null;
            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "Id");

            Logger.LogDebug("Get Engagement '{Id}'", id);
            var engagement = DataClient.EngagementGet(id);
            if (!engagement.Success || engagement.Data == null)
            {
                throw new UiApiNotFoundException($"Engagement {id} does not exist.");
            }

            var commentRequest = new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "saltminer.engagement.id", id }
                    }
                },
                UIPagingInfo = new UIPagingInfo(100)
            };

            var comments = new List<Comment>();
            
            Logger.LogDebug("{Msg}", GeneralExtensions.SearchUIPagingLoggerMessage("Comment", commentRequest?.Filter?.FilterMatches?.Count ?? 0, commentRequest.UIPagingInfo.Size, commentRequest.UIPagingInfo.Page));
            var commentResponse = DataClient.CommentSearch(commentRequest);

            while (commentResponse.Success && commentResponse.Data != null && commentResponse.Data.Any())
            {
                comments.AddRange(commentResponse.Data.ToList());
                
                commentRequest.UIPagingInfo = commentResponse.UIPagingInfo;
                commentRequest.UIPagingInfo.Page++;
                commentRequest.AfterKeys = commentResponse.AfterKeys;

                Logger.LogDebug("{Msg}", GeneralExtensions.SearchUIPagingLoggerMessage("Comment", commentRequest?.Filter?.FilterMatches?.Count ?? 0, commentRequest.UIPagingInfo.Size, commentRequest.UIPagingInfo.Page));
                commentResponse = DataClient.CommentSearch(commentRequest);
            }

            var engagementAttachments = GetAllEngagementAttachments(id).Data.ToList();

            if (engagement.Data.Saltminer.Engagement.Status == EnumExtensions.GetDescription(EngagementStatus.Draft))
            {
                Logger.LogDebug("Get Queue Scan for Engagement '{Id}'", engagement.Data.Id);
                var queueScan = DataClient.QueueScanGetByEngagement(engagement.Data.Id);
                if (!queueScan.Success || queueScan.Data == null)
                {
                    throw new UiApiNotFoundException($"QueueScan for engagement {engagement.Data.Id} does not exist.");
                }

                var assetRequest = new SearchRequest() { 
                    Filter = new() { FilterMatches = new() { { "Saltminer.Internal.QueueScanId", queueScan.Data.Id } } },
                    UIPagingInfo = new UIPagingInfo(300) 
                };

                var queueAssets = new List<QueueAsset>();

                Logger.LogDebug("{Msg}", GeneralExtensions.SearchUIPagingLoggerMessage("QueueAsset", assetRequest?.Filter?.FilterMatches?.Count ?? 0, assetRequest.UIPagingInfo.Size, assetRequest.UIPagingInfo.Page));
                var queueAssetsResponse = DataClient.QueueAssetSearch(assetRequest);

                while (queueAssetsResponse.Success && queueAssetsResponse.Data != null && queueAssetsResponse.Data.Any())
                {
                    queueAssets.AddRange(queueAssetsResponse.Data);

                    assetRequest.UIPagingInfo = queueAssetsResponse.UIPagingInfo;
                    assetRequest.UIPagingInfo.Page++;
                    assetRequest.AfterKeys = queueAssetsResponse.AfterKeys;

                    Logger.LogDebug("{Msg}", GeneralExtensions.SearchUIPagingLoggerMessage("QueueAsset", assetRequest?.Filter?.FilterMatches?.Count ?? 0, assetRequest.UIPagingInfo.Size, assetRequest.UIPagingInfo.Page));
                    queueAssetsResponse = DataClient.QueueAssetSearch(assetRequest);
                }

                var issueRequest = new SearchRequest()
                {
                    Filter = new Filter()
                    {
                        AnyMatch = true,
                        FilterMatches = new Dictionary<string, string>
                        {
                            { "Saltminer.QueueScanId", queueScan.Data.Id }
                        }
                    },
                    UIPagingInfo = new UIPagingInfo
                    {
                        Page = 1,
                        Size = 300,
                        SortFilters = new Dictionary<string, bool>
                        {
                            { "Vulnerability.SeverityLevel", true },
                            { "Vulnerability.Name", true },
                            { "Vulnerability.Id", true }
                        }
                    },
                };

                var queueIssues = new List<QueueIssue>();

                Logger.LogDebug("{Msg}", GeneralExtensions.SearchUIPagingLoggerMessage("QueueIssue", issueRequest?.Filter?.FilterMatches?.Count ?? 0, issueRequest.UIPagingInfo.Size, issueRequest.UIPagingInfo.Page));
                var queueIssuesResponse = DataClient.QueueIssueSearch(issueRequest);

                while (queueIssuesResponse.Success && queueIssuesResponse.Data != null && queueIssuesResponse.Data.Any())
                {
                    queueIssues.AddRange(queueIssuesResponse.Data);

                    issueRequest.UIPagingInfo = queueIssuesResponse.UIPagingInfo;
                    issueRequest.UIPagingInfo.Page++;
                    issueRequest.AfterKeys = queueIssuesResponse.AfterKeys;

                    Logger.LogDebug("{Msg}", GeneralExtensions.SearchUIPagingLoggerMessage("QueueIssue", issueRequest?.Filter?.FilterMatches?.Count ?? 0, issueRequest.UIPagingInfo.Size, issueRequest.UIPagingInfo.Page));
                    queueIssuesResponse = DataClient.QueueIssueSearch(issueRequest);
                }

                fullEngagement = new EngagementFull(engagement.Data, queueScan.Data, queueAssets, queueIssues, comments, 
                    engagementAttachments.Select(x => x.TransformAttachment(x)).ToList(), UiApiConfig.AppVersion, MyFieldInfo, 
                    FieldInfo(FieldInfoEntityType.Asset), FieldInfo(FieldInfoEntityType.Issue));
            }
            else
            {
                Logger.LogDebug("Get Scan by engagement '{Id}'", engagement.Data.Id);
                var scan = DataClient.ScanGetByEngagement(engagement.Data.Id);
                
                if (!scan.Success || scan.Data == null)
                {
                    throw new UiApiNotFoundException($"Scan for engagement {engagement.Data.Id} does not exist.");
                }

                var assetRequest = new SearchRequest
                {
                    Filter = new()
                    {
                        FilterMatches = new Dictionary<string, string>
                        {
                            { "Saltminer.Engagement.Id", id }
                        }
                    },
                    AssetType = UiApiConfig.AssetType, 
                    SourceType = UiApiConfig.SourceType,
                    Instance = UiApiConfig.Instance,
                    UIPagingInfo = new UIPagingInfo(300)
                };

                var assets = new List<Asset>();
                
                Logger.LogDebug("{Msg}", GeneralExtensions.SearchUIPagingLoggerMessage("Asset", assetRequest?.Filter?.FilterMatches?.Count ?? 0, assetRequest.UIPagingInfo.Size, assetRequest.UIPagingInfo.Page));
                var assetsResponse = DataClient.AssetSearch(assetRequest);
                
                while (assetsResponse.Success && assetsResponse.Data != null && assetsResponse.Data.Any())
                {
                    assets.AddRange(assetsResponse.Data);

                    assetRequest.UIPagingInfo = assetsResponse.UIPagingInfo;
                    assetRequest.UIPagingInfo.Page++;
                    assetRequest.AfterKeys = assetsResponse.AfterKeys;

                    Logger.LogDebug("{Msg}", GeneralExtensions.SearchUIPagingLoggerMessage("Asset", assetRequest?.Filter?.FilterMatches?.Count ?? 0, assetRequest.UIPagingInfo.Size, assetRequest.UIPagingInfo.Page));
                    assetsResponse = DataClient.AssetSearch(assetRequest);
                }

                var issueRequest = new SearchRequest
                {
                    Filter = new()
                    {
                        FilterMatches = new Dictionary<string, string> { { "Saltminer.Engagement.Id", scan.Data.Saltminer.Engagement.Id } }
                    },
                    AssetType = UiApiConfig.AssetType,
                    SourceType = UiApiConfig.SourceType,
                    Instance = UiApiConfig.Instance,
                    UIPagingInfo = new UIPagingInfo
                    {
                        Page = 1,
                        Size = 300,
                        SortFilters = new Dictionary<string, bool>
                        {
                            { "Vulnerability.SeverityLevel", true },
                            { "Vulnerability.Name", true },
                            { "Vulnerability.Id", true }
                        }
                    },
                };

                var issues = new List<Issue>();

                Logger.LogDebug("{Msg}", GeneralExtensions.SearchUIPagingLoggerMessage("Issue", issueRequest?.Filter?.FilterMatches?.Count ?? 0, issueRequest.UIPagingInfo.Size, issueRequest.UIPagingInfo.Page));
                var issuesResponse = DataClient.IssueSearch(issueRequest);
                
                while (issuesResponse.Success && issuesResponse.Data != null && issuesResponse.Data.Any())
                {
                    issues.AddRange(issuesResponse.Data);

                    issueRequest.UIPagingInfo = issuesResponse.UIPagingInfo;
                    issueRequest.UIPagingInfo.Page++;
                    issueRequest.AfterKeys = issuesResponse.AfterKeys;

                    Logger.LogDebug("{Msg}", GeneralExtensions.SearchUIPagingLoggerMessage("Issue", issueRequest?.Filter?.FilterMatches?.Count ?? 0, issueRequest.UIPagingInfo.Size, issueRequest.UIPagingInfo.Page));
                    issuesResponse = DataClient.IssueSearch(issueRequest);
                }

                fullEngagement = new(engagement.Data, scan.Data, assets, issues, comments, 
                    engagementAttachments.Select(x => x.TransformAttachment(x)).ToList(), UiApiConfig.AppVersion, MyFieldInfo, 
                    FieldInfo(FieldInfoEntityType.Asset), FieldInfo(FieldInfoEntityType.Issue));
            }

            fullEngagement.IssueCount = GetEngagementIssueCount(id).Data;
            return new DataItemResponse<EngagementFull>(fullEngagement);
        }

        private static Comment CreateComment(UiComment comment, string engagementId, string scanId = null, string assetId = null, string issueId = null)
        {
            return new()
            {
                Saltminer = new()
                {
                    Comment = new()
                    {
                        Message = comment.Message,
                        Type = comment.Type,
                        User = comment.User,
                        UserFullName = comment.UserFullName,
                        Added = comment.Added
                    },
                    Engagement = new()
                    {
                        Id = engagementId
                    },
                    Scan = new()
                    {
                        Id = scanId
                    },
                    Asset = new()
                    {
                        Id = assetId
                    },
                    Issue = new()
                    {
                        Id = issueId
                    }
                }
            };
        }

        private void ValidateAndAdd(string quidName, ZipArchive archive)
        {
            var formatedPath = FileHelper.SearchFile(quidName, Config.FileRepository);
            if (!string.IsNullOrEmpty(formatedPath))
            {
                AddZipEntry(quidName, File.ReadAllBytes(formatedPath), archive);
            }
        }

        private static void AddZipEntry(string fileName, byte[] fileContent, ZipArchive archive)
        {
            var existingFile = archive.GetEntry(fileName);

            //if file doesn't exist in archive then add
            if (existingFile == null)
            {
                var entry = archive.CreateEntry(fileName);
                using var stream = entry.Open();
                stream.Write(fileContent, 0, fileContent.Length);
            }
        }

        private void UpdateQueueScanEngagementData(EngagementInfo engagement)
        {
            var request = new UpdateQueryRequest<QueueScan>
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Engagement.Id", engagement.Id }
                    }
                },
                ScriptUpdates = new Dictionary<string, object>
                {
                    { "Saltminer.Engagement.Name", engagement.Name },
                    { "Saltminer.Engagement.Subtype", engagement.Subtype },
                    { "Saltminer.Engagement.PublishDate", engagement.PublishDate },
                    { "Saltminer.Engagement.Customer", engagement.Customer },
                    { "Saltminer.Engagement.Attributes", engagement.Attributes }
                }
            };

            DataClient.QueueScanUpdateByQuery(request);
        }
        
        private void UpdateQueueAssetEngagementData(EngagementInfo engagement)
        {
            var request = new UpdateQueryRequest<QueueAsset>
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Engagement.Id", engagement.Id }
                    }
                },
                ScriptUpdates = new Dictionary<string, object>
                {
                    { "Saltminer.Engagement.Name", engagement.Name },
                    { "Saltminer.Engagement.Subtype", engagement.Subtype },
                    { "Saltminer.Engagement.PublishDate", engagement.PublishDate },
                    { "Saltminer.Engagement.Customer", engagement.Customer },
                    { "Saltminer.Engagement.Attributes", engagement.Attributes }
                }
            };

            DataClient.QueueAssetUpdateByQuery(request);
        }

        private void UpdateQueueIssueEngagementData(EngagementInfo engagement)
        {
            var request = new UpdateQueryRequest<QueueIssue>
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Engagement.Id", engagement.Id }
                    }
                },
                ScriptUpdates = new Dictionary<string, object>
                {
                    { "Saltminer.Engagement.Name", engagement.Name },
                    { "Saltminer.Engagement.Subtype", engagement.Subtype },
                    { "Saltminer.Engagement.PublishDate", engagement.PublishDate },
                    { "Saltminer.Engagement.Customer", engagement.Customer },
                    { "Saltminer.Engagement.Attributes", engagement.Attributes }
                }
            };

            DataClient.QueueIssueUpdateByQuery(request);
        }

        private void UpdateEngagementQueueScan(QueueScan queueScan, Engagement engagement)
        {
            queueScan.Saltminer.Engagement = new EngagementInfo
            {
                Id = engagement.Id,
                Name = engagement.Saltminer.Engagement.Name,
                Customer = engagement.Saltminer.Engagement.Customer,
                Subtype = engagement.Saltminer.Engagement.Subtype,
                PublishDate = engagement.Saltminer.Engagement.PublishDate,
                Attributes = engagement.Saltminer.Engagement.Attributes
            };

            Logger.LogInformation("Edit Queue Scan '{Id}'", queueScan.Id);
            DataClient.QueueScanAddUpdate(queueScan);

            DataClient.RefreshIndex(QueueScan.GenerateIndex());
        }
        
        private void ScrubMarkdownAttachments<T>(T request, List<AttributeDefinitionValue> attributeOptions = null) where T : class
        {
            var type = request.GetType();

            foreach (var pi in type.GetProperties())
            {
                var attrs = Attribute.GetCustomAttributes(pi);
                if (pi.PropertyType == typeof(Dictionary<string, string>) || (pi.PropertyType == typeof(string) && attrs != null && attrs.Length > 0 && attrs.Any(x => x is MarkdownAttribute)))
                {
                    if (pi.PropertyType == typeof(string) && pi.GetValue(request) != null)
                    {
                        pi.SetValue(request, ParseMarkdown(pi.GetValue(request).ToString()));
                    }
                    else if (pi.GetValue(request) != null)
                    {
                        var dict = (pi.GetValue(request) as Dictionary<string, string>);
                        List<string> keys = [];
                        foreach (var key in keys)
                        {
                            var matchedAttribute = attributeOptions.FirstOrDefault(x => x.Name == key);
                            if (matchedAttribute != null && matchedAttribute.Type.Contains("markdown", StringComparison.OrdinalIgnoreCase) && dict[key] != null)
                            {
                                dict[key] = ParseMarkdown(dict[key]);
                            }
                        }
                    }
                }
            }
        }

        private string ParseMarkdown(string markDown)
        {
            if (!string.IsNullOrEmpty(markDown))
            {
                var wordMatcher = new Regex(Config.MarkdownUrlRegex);
                var results = wordMatcher.Matches(markDown).Cast<Match>().Select(c => c.Value).ToList();

                if (results != null)
                {
                    foreach (var image in results)
                    {
                        #pragma warning disable S3220 // Method calls should not resolve ambiguously to overloads with "params" - false positive
                        var guid = image.Split('(', ')')[1];
                        #pragma warning restore S3220 // Method calls should not resolve ambiguously to overloads with "params"
                        markDown = markDown.Replace(guid, "Image Not Found");
                    }
                }
            }

            return markDown;
        }
    }
}
