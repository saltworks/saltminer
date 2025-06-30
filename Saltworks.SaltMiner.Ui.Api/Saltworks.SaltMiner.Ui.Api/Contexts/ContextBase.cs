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

﻿using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.Ui.Api.Controllers;
using Saltworks.SaltMiner.Ui.Api.Extensions;
using Saltworks.SaltMiner.Ui.Api.Models;
using System.Text.RegularExpressions;
using Saltworks.SaltMiner.UiApiClient.ViewModels;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Attributes;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Responses;

namespace Saltworks.SaltMiner.Ui.Api.Contexts
{
    public class ContextBase(IServiceProvider services, ILogger logger)
    {
        protected readonly UiApiConfig Config = services.GetRequiredService<UiApiConfig>();
        protected readonly DataClient.DataClient DataClient = services.GetRequiredService<DataClientFactory<DataClient.DataClient>>().GetClient();
        protected readonly ILogger Logger = logger;
        private FileHelper _FileHelper;
        private readonly FieldInfoCache FieldInfoCache = services.GetRequiredService<FieldInfoCache>();
        private List<SearchFilter> _searchFilters = null;
        private List<Lookup> _lookups = null;

        protected FileHelper FileHelper
        {
            get
            {
                _FileHelper ??= new FileHelper(DataClient, Logger);
                return _FileHelper;
            }
        }

        protected List<SearchFilter> SearchFilters {
            get
            {
                _searchFilters ??= DataClient.SearchFilterSearch(new()).Data.ToList();
                return _searchFilters;
            }
        }

        protected virtual List<FieldFilter> SearchDisplays { get; }
        protected virtual List<SearchFilterValue> SearchFilterValues { get; }
        protected virtual List<FieldFilter> SortDisplays { get; }
        protected virtual List<SearchFilterValue> SortFilterValues { get; }

        protected List<Lookup> Lookups
        {
            get
            {
                _lookups ??= DataClient.LookupSearch(new()).Data.ToList();
                return _lookups;
            }
        }

        protected List<LookupValue> SeverityDropdowns => Lookups?.Find(x => x.Type == LookupType.SeverityDropdown.ToString())?.Values ?? [];
        protected List<LookupValue> TestedDropdowns => GetTestedList(Lookups?.Find(x => x.Type == LookupType.TestedDropdown.ToString()));
        protected List<LookupValue> SubtypeDropdowns => Lookups?.Find(x => x.Type == LookupType.EngagementSubTypeDropdown.ToString())?.Values ?? [];
        protected List<LookupValue> ServiceJobCommandDropdowns => Lookups?.Find(x => x.Type == LookupType.ServiceJobCommandOptions.ToString())?.Values ?? [];

        protected List<AttributeDefinition> AllAttributeDefinitions => FieldInfoCache.GetAttributeDefinitions(DataClient) ?? [];
        protected List<ActionDefinition> AllActionDefinitions => FieldInfoCache.GetActionDefinitions(DataClient) ?? [];
        protected List<AttributeDefinitionValue> AttributeDefinitions(AttributeDefinitionType type) => AllAttributeDefinitions.FirstOrDefault(ad => ad.Type.Equals(type.ToString("g")))?.Values.ToList() ?? [];
        protected FieldInfo FieldInfo(FieldInfoEntityType type) => FieldInfoCache.GetFieldInfo(type, CurrentUserRoles, DataClient);

        protected string ApiBaseUrl => $"{Controller.HttpContext.GetBaseUrl()}/{Config.NginxRoute}";
        protected string UiBaseUrl => string.IsNullOrEmpty(Config.UiBaseUrl) ? $"{Controller.HttpContext.GetBaseUrl()}/smpgui" : Config.UiBaseUrl;
        protected string KibanaBaseUrl
        {
            get
            {
                if (string.IsNullOrEmpty(Config.KibanaBaseUrl))
                {
                    Config.KibanaBaseUrl = Controller.HttpContext.GetBaseUrl();
                    Logger.LogDebug("Kibana base URL derived from Controller context: {Url}", Config.KibanaBaseUrl);
                }
                else
                {
                    Logger.LogDebug("Kibana base URL derived from config: {Url}", Config.KibanaBaseUrl);
                }
                return Config.KibanaBaseUrl;
            }
        }

        internal ApiControllerBase Controller { get; set; }
        protected List<AppRole> AllAppRoles => FieldInfoCache.GetAppRoles(DataClient);
        protected IEnumerable<AppRole> AppRolesFromRoles(IEnumerable<string> roles) => AllAppRoles.Where(r => roles.Contains(r.Name));
        protected KibanaUser GetCurrentUserOrThrow()
        {
            var user = Controller.CurrentUser;
            if (string.IsNullOrEmpty(user?.UserName))
                throw new UiApiUnauthorizedException();
            return user;
        }
        protected IEnumerable<string> CurrentUserRoles => DebugUserRoles ?? Controller.CurrentUser?.Roles ?? [];
        internal IEnumerable<string> DebugUserRoles { get; set; } = null;

        #region File Utility

        internal async Task<DataItemResponse<string>> CreateFileAsync(IFormFile file, string user, string userFullName, bool isAttachment = false)
        {
            return new DataItemResponse<string>(await FileHelper.CreateFileAsync(file, user, userFullName, Config.FileRepository, isAttachment));
        }

        internal void DeleteFile(string fileId, bool isAttachment = false)
        {
            FileHelper.DeleteFile(fileId, Config.FileRepository, isAttachment);
        }

        #endregion

        #region Attachments

        internal NoDataResponse DeleteAllEngagementAttachments(string id)
        {
            var result = DataClient.AttachmentDeleteAllEngagement(id);

            return new NoDataResponse(result.Affected);
        }

        internal NoDataResponse DeleteIssueAttachments(string id)
        {
            var result = DataClient.AttachmentDeleteAllIssue(id);

            return new NoDataResponse(result.Affected);
        }

        internal NoDataResponse SetAttachments(string engagementId, string issueId, List<UiAttachmentInfo> attachments, KibanaUser user, bool isMarkdown = false, bool deleteExisting = false)
        {
            if (deleteExisting)
            {
                if (string.IsNullOrEmpty(issueId))
                {
                    DataClient.AttachmentDeleteAllEngagement(engagementId, isMarkdown, true);
                }
                else
                {
                    DataClient.AttachmentDeleteAllIssue(issueId, isMarkdown);
                }
            }

            if (attachments.Count > 0)
            {
                foreach (var attachment in attachments)
                {
                    var attachmentResults = DataClient.AttachmentSearch(new SearchRequest
                    {
                        Filter = new Filter
                        {
                            FilterMatches = new Dictionary<string, string>
                            {
                                { "Saltminer.Attachment.FileId", attachment.FileId }
                            }
                        }
                    });

                    var attachmentDelta = new Attachment();

                    if (attachmentResults.Success && attachmentResults.Data != null && attachmentResults.Data.Any())
                    {
                        attachmentDelta = attachmentResults.Data.First();
                    }
                    else
                    {
                        attachmentDelta = new Attachment
                        {
                            Saltminer = new SaltMinerAttachmentInfo
                            {
                                User = user?.UserName ?? attachmentDelta.Saltminer.User,
                                UserFullName = user?.FullName ?? attachmentDelta.Saltminer.UserFullName,
                                Attachment = new AttachmentInfo
                                {
                                    FileName = attachment.FileName,
                                    FileId = attachment.FileId
                                }
                            }
                        };
                    }

                    attachmentDelta.Saltminer.Engagement = engagementId == null ? null : new IdInfo
                    {
                        Id = engagementId
                    };

                    attachmentDelta.Saltminer.Issue = issueId == null ? null : new IdInfo
                    {
                        Id = issueId
                    };

                    attachmentDelta.Saltminer.IsMarkdown = isMarkdown;

                    DataClient.AttachmentAddUpdate(attachmentDelta);
                }
            }

            return new NoDataResponse(attachments.Count);
        }

        internal List<UiAttachmentInfo> GetMarkdownAttachments<T>(T request, List<AttributeDefinitionValue> attributeOptions = null) where T : class
        {
            var markDownList = new List<string>();

            var type = request.GetType();

            foreach (var pi in type.GetProperties())
            {
                var attrs = Attribute.GetCustomAttributes(pi);
                if (pi.PropertyType == typeof(Dictionary<string, string>) || (pi.PropertyType == typeof(string) && attrs != null && attrs.Length > 0 && Array.Exists(attrs, x => x is MarkdownAttribute)))
                {
                    if (pi.PropertyType == typeof(string) && pi.GetValue(request) != null)
                    {
                        markDownList.Add(pi.GetValue(request).ToString());
                    }
                    else if (pi.GetValue(request) != null)
                    {
                        foreach (var kvp in pi.GetValue(request) as Dictionary<string, string>)
                        {
                            var matchedAttribute = attributeOptions.Find(x => x.Name == kvp.Key);
                            if (matchedAttribute != null && matchedAttribute.Type.Contains("markdown", StringComparison.OrdinalIgnoreCase) && kvp.Value != null)
                            {
                                markDownList.Add(kvp.Value);
                            }
                        }
                    }
                }
            }

            return GetAttachmentData(markDownList);
        }

        //Only use when you absolutely positive of uniqueness, currently only reporting
        internal UiAttachmentInfo GetAttachmentByFileName(string fileName)
        {
            var attachmentResults = DataClient.AttachmentSearch(new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Attachment.FileName", fileName }
                    }
                }
            });

            if (attachmentResults.Success && attachmentResults.Data != null && attachmentResults.Data.Any())
            {
                return new UiAttachmentInfo(attachmentResults.Data.First().Saltminer.Attachment);
            }

            return null;
        }

        internal UiAttachmentInfo GetAttachmentByFileId(string fileId)
        {
            fileId = Path.GetFileName(fileId);
            var attachmentResults = DataClient.AttachmentSearch(new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Attachment.FileId", fileId }
                    }
                }
            });

            if (attachmentResults.Success && attachmentResults.Data != null && attachmentResults.Data.Any())
            {
                return new UiAttachmentInfo(attachmentResults.Data.First().Saltminer.Attachment);
            }

            return null;
        }

        internal void DeleteAttachmentByFileId(string fileId)
        {
            fileId = Path.GetFileName(fileId);
            var attachmentResults = DataClient.AttachmentSearch(new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Attachment.FileId", fileId }
                    }
                }
            });

            if (attachmentResults.Success && attachmentResults.Data != null && attachmentResults.Data.Any())
            {
                DataClient.AttachmentDelete(attachmentResults.Data.First().Id);
            }
        }

        internal UiDataResponse<UiAttachment> GetAllIssueAttachments(string issueId, bool? isMarkdown = null)
        {
            Logger.LogInformation("Get Issue Attachments");
            var request = new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Issue.Id", issueId }
                    }
                },
                UIPagingInfo = new UIPagingInfo(Config.DefaultPageSize, 1)
            };

            if (isMarkdown.HasValue)
            {
                if (isMarkdown.Value)
                {
                    request.Filter.FilterMatches.Add("Saltminer.IsMarkdown", "true");
                }
                else
                {
                    request.Filter.FilterMatches.Add("Saltminer.IsMarkdown", "false");
                }
            }

            var result = new List<Attachment>();

            var response = DataClient.AttachmentSearch(request);

            while (response.Success && response.Data != null && response.Data.Any())
            {
                result.AddRange(response.Data);

                request.UIPagingInfo = response.UIPagingInfo;
                request.UIPagingInfo.Page = request.UIPagingInfo.Page++;
                request.AfterKeys = response.AfterKeys;

                response = DataClient.AttachmentSearch(request);
            }

            return new UiDataResponse<UiAttachment>(result.Select(x => new UiAttachment(x, UiApiConfig.AppVersion)).ToList());
        }

        internal UiDataResponse<UiAttachment> GetAllEngagementOnlyAttachments(string engagementId, bool? isMarkdown = null)
        {
            Logger.LogInformation("Get Issue Attachments");

            var request = new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Engagement.Id", engagementId },
                    },
                    SubFilter = new Filter
                    {
                        FilterMatches = new Dictionary<string, string>
                        {
                            { "Saltminer.Issue.Id", SaltMiner.DataClient.Helpers.BuildMustNotExistsFilterValue() }
                        },
                    }
                },
                UIPagingInfo = new UIPagingInfo(Config.DefaultPageSize, 1)
            };

            if (isMarkdown.HasValue)
            {
                if (isMarkdown.Value)
                {
                    request.Filter.FilterMatches.Add("Saltminer.IsMarkdown", "true");
                }
                else
                {
                    request.Filter.FilterMatches.Add("Saltminer.IsMarkdown", "false");
                }
            }

            var result = new List<Attachment>();

            var response = DataClient.AttachmentSearch(request);

            while (response.Success && response.Data != null && response.Data.Any())
            {
                result.AddRange(response.Data);
                request.UIPagingInfo = response.UIPagingInfo;
                request.UIPagingInfo.Page = request.UIPagingInfo.Page++;
                request.AfterKeys = response.AfterKeys;

                response = DataClient.AttachmentSearch(request);
            }

            return new UiDataResponse<UiAttachment>(result.Select(x => new UiAttachment(x, UiApiConfig.AppVersion)).ToList());
        }

        internal UiDataResponse<UiAttachment> GetAllEngagementAttachments(string engagementId)
        {
            Logger.LogInformation("Get Issue Attachments");

            var request = new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Engagement.Id", engagementId },
                    }
                },
                UIPagingInfo = new UIPagingInfo(Config.DefaultPageSize, 1)
            };

            var result = new List<Attachment>();

            var response = DataClient.AttachmentSearch(request);

            while (response.Success && response.Data != null && response.Data.Any())
            {
                result.AddRange(response.Data);
                request.UIPagingInfo = response.UIPagingInfo;
                request.UIPagingInfo.Page = request.UIPagingInfo.Page++;
                request.AfterKeys = response.AfterKeys;

                response = DataClient.AttachmentSearch(request);
            }

            return new UiDataResponse<UiAttachment>(result.Select(x => new UiAttachment(x, UiApiConfig.AppVersion)).ToList());
        }

        private List<UiAttachmentInfo> GetAttachmentData(List<string> markDownList)
        {
            var markDownAttachmentFileIds = ParseMarkdownForImages(markDownList, Config.MarkdownUrlRegex);

            var result = new List<UiAttachmentInfo>();

            foreach (var markDownAttachmentFileId in markDownAttachmentFileIds)
            {
                result.Add(new UiAttachmentInfo
                {
                    FileName = markDownAttachmentFileId,
                    FileId = markDownAttachmentFileId
                });
            }

            return result;
        }

        private static List<string> ParseMarkdownForImages(List<string> markdownList, string regExPattern)
        {
            var list = new List<string>();

            foreach (var markDown in markdownList)
            {
                if (string.IsNullOrEmpty(markDown))
                {
                    continue;
                }

                var wordMatcher = new Regex(regExPattern);
                var results = wordMatcher.Matches(markDown).Cast<Match>().Select(c => c.Value).ToList();

                foreach (var image in results)
                {
                    #pragma warning disable S3220 // Method calls should not resolve ambiguously to overloads with "params" - false positive
                    var guid = image.Split('(', ')')[1];
                    #pragma warning restore S3220 // Method calls should not resolve ambiguously to overloads with "params"
                    if (guid.Contains("http"))
                        guid = guid[(guid.LastIndexOf('/') + 1)..];

                    list.Add(guid);
                }
            }
            return list;
        }

        #endregion

        #region Assets

        internal UiDataResponse<AssetFull> EngagementAssetSearch(AssetSearch request)
        {
            Logger.LogInformation("Search Engagement Assets initiated");
            var fi = FieldInfo(FieldInfoEntityType.Asset);
            request.IsModelValid(Config.ApiFieldRegex, Config.FailedRegexSplat, false, null, fi.AttributeDefinitions.ToList());
            var engagement = DataClient.EngagementGet(request.EngagementId).Data;
            Response response = null;

            if(engagement.Saltminer.Engagement.Status != EnumExtensions.GetDescription(EngagementStatus.Draft))
            {
                var result = AssetsSearch(request);
                if (result.Success && (result.Data?.Any() ?? false))
                {
                    return new UiDataResponse<AssetFull>(result.Data.Select(x => new AssetFull(x, UiApiConfig.AppVersion, fi)).ToList(), result.UIPagingInfo);
                }
                response = result;
            } 
            else
            {
                var qResult = QueueAssetsSearch(request);
                if (qResult.Success && (qResult.Data?.Any() ?? false))
                {
                    return new UiDataResponse<AssetFull>(qResult.Data.Select(x => new AssetFull(x, UiApiConfig.AppVersion, fi)).ToList(), qResult.UIPagingInfo);
                }
                response = qResult;
            }
            return new UiDataResponse<AssetFull>(null)
            {
                ErrorMessages = response.ErrorMessages,
                ErrorType = response.ErrorType,
                StatusCode = response.StatusCode,
                Affected = response.Affected,
                Message = response.Message
            };
        }

        internal UiDataResponse<AssetFull> GetAllAssetsByEngagement(string engagementId, bool isDraft)
        {
            Logger.LogInformation("Search Asset initiated");
            var fi = FieldInfo(FieldInfoEntityType.Asset);
            General.ValidateIdAndInput(engagementId, Config.ApiFieldRegex, "Id");

            if (isDraft)
            {
                var queueAssetResponse = GetAllEngagementQueueAssets(engagementId, 20);

                if (queueAssetResponse.Success && queueAssetResponse.Data != null && queueAssetResponse.Data.Any())
                {
                    return new UiDataResponse<AssetFull>(queueAssetResponse.Data.Select(x => new AssetFull(x, UiApiConfig.AppVersion, fi)).ToList());
                }
            }
            else
            {
                var assetResponse = GetAllEngagementAssets(engagementId);

                if (assetResponse.Success && assetResponse.Data != null && assetResponse.Data.Any())
                {
                    return new UiDataResponse<AssetFull>(assetResponse.Data.Select(x => new AssetFull(x, UiApiConfig.AppVersion, fi)).ToList());
                }
            }
            return new UiDataResponse<AssetFull>(null);
        }

        internal UiDataResponse<QueueAsset> GetAllEngagementQueueAssets(string engagementId, int limit=0)
        {
            var request = GenerateEngagementAssetSearch(engagementId);
            var response = DataClient.QueueAssetSearch(request);
            var result = new List<QueueAsset>();

            while (response.Success && response.Data != null && response.Data.Any())
            {
                result.AddRange(response.Data);
                if (limit > 0 && result.Count > limit)
                {
                    return new UiDataResponse<QueueAsset>(result);
                }
                request.UIPagingInfo = response.UIPagingInfo;
                request.UIPagingInfo.Page = request.UIPagingInfo.Page++;
                request.AfterKeys = response.AfterKeys;

                response = DataClient.QueueAssetSearch(request);
            }

            return new UiDataResponse<QueueAsset>(result);
        }

        internal UiDataResponse<Asset> GetAllEngagementAssets(string engagementId)
        {
            var request = GenerateEngagementAssetSearch(engagementId);
            var response = DataClient.AssetSearch(request);
            var result = new List<Asset>();

            while (response.Success && response.Data != null && response.Data.Any())
            {
                result.AddRange(response.Data);
                //todo Eddie: Remove
                if (result.Count > 20)
                {
                    return new UiDataResponse<Asset>(result);
                }
                request.UIPagingInfo = response.UIPagingInfo;
                request.UIPagingInfo.Page = request.UIPagingInfo.Page++;
                request.AfterKeys = response.AfterKeys;

                response = DataClient.AssetSearch(request);
            }

            return new UiDataResponse<Asset>(result);
        }

        private UiDataResponse<QueueAsset> QueueAssetsSearch(AssetSearch request)
        {
            var searchRequest = GenerateEngagementAssetSearch(request.EngagementId, request.Paging);
            searchRequest.UIPagingInfo.SortFilters = Helpers.SearchFilters.MapSortFilters(request.Paging?.SortFilters, SortFilterValues);
            Helpers.SearchFilters.AddFilters(searchRequest.Filter.FilterMatches, SearchFilterValues, request.SearchFilters, true);

            var response = DataClient.QueueAssetSearch(searchRequest);
            var vals = new int[] { 0, 1 };
            var assets = new List<QueueAsset>();

            while (response.Success && response.Data != null && response.Data.Any())
            {
                searchRequest.UIPagingInfo = response.UIPagingInfo;
                if (vals.Contains(request.Paging?.Page ?? 0) || searchRequest.UIPagingInfo.Page == request.Paging.Page)
                {
                    Logger.LogInformation("{Msg}", GeneralExtensions.SearchUIPagingLoggerMessage("QueueAsset", searchRequest.Filter.FilterMatches?.Count ?? 0, response.UIPagingInfo.Size, response.UIPagingInfo.Page));
                    assets = response.Data.ToList();
                    break;
                }
                assets.AddRange(response.Data);
                searchRequest.UIPagingInfo.Page++;
                searchRequest.AfterKeys = response.AfterKeys;

                response = DataClient.QueueAssetSearch(searchRequest);
            }
            return new UiDataResponse<QueueAsset>(assets, SearchFilters?.Find(x => x.Type == SearchFilterType.IssueSortFilters.ToString())?.Filters ?? [], response.UIPagingInfo, true);
        }

        private UiDataResponse<Asset> AssetsSearch(AssetSearch request)
        {
            var searchRequest = GenerateEngagementAssetSearch(request.EngagementId, request.Paging);
            searchRequest.UIPagingInfo.SortFilters = Helpers.SearchFilters.MapSortFilters(request.Paging?.SortFilters, SortFilterValues);
            Helpers.SearchFilters.AddFilters(searchRequest.Filter.FilterMatches, SearchFilterValues, request.SearchFilters, false);

            var response = DataClient.AssetSearch(searchRequest);
            var vals = new int[] { 0, 1 };
            var assets = new List<Asset>();

            while (response.Success && response.Data != null && response.Data.Any())
            {
                searchRequest.UIPagingInfo = response.UIPagingInfo;
                if (vals.Contains(request.Paging?.Page ?? 0) || searchRequest.UIPagingInfo.Page == request.Paging.Page)
                {
                    Logger.LogInformation("{Msg}", Extensions.GeneralExtensions.SearchUIPagingLoggerMessage("Asset", searchRequest.Filter.FilterMatches?.Count ?? 0, response.UIPagingInfo.Size, response.UIPagingInfo.Page));
                    assets = response.Data.ToList();
                    break;
                }
                assets.AddRange(response.Data);
                searchRequest.UIPagingInfo.Page++;
                searchRequest.AfterKeys = response.AfterKeys;

                response = DataClient.AssetSearch(searchRequest);
            }

            return new UiDataResponse<Asset>(assets, SearchFilters?.Find(x => x.Type == SearchFilterType.IssueSortFilters.ToString())?.Filters ?? [], response.UIPagingInfo, true);
        }

        private SearchRequest GenerateEngagementAssetSearch(string engagementId, UiPager paging = null)
        {
            return new SearchRequest()
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Engagement.Id", engagementId }
                    }
                },
                UIPagingInfo = paging == null ? new UIPagingInfo(Config.DefaultPageSize, 1) : paging.ToDataPager(),
                AssetType = UiApiConfig.AssetType,
                SourceType = UiApiConfig.SourceType,
                Instance = UiApiConfig.Instance
            };
        }

        #endregion

        #region Issues

        internal UiDataResponse<IssueFull> EngagementIssueSearch(IssueSearch request)
        {
            Logger.LogInformation("Search Engagement Issues initiated");
            var fieldInfo = FieldInfo(FieldInfoEntityType.Issue);
            request.IsModelValid(Config.ApiFieldRegex, Config.FailedRegexSplat, false, null, FieldInfo(FieldInfoEntityType.Issue).AttributeDefinitions.ToList());
            Response response = null;

            if (request.SearchFilters != null)
            {
                foreach (var filter in request.SearchFilters)
                {
                    // not entirely sure the removal of these characters are needed.
                    // dash char has been removed since the removal caused GUID values to be incorrect
                    filter.Value = Regex.Replace(filter.Value, @"[\+\=\&\|\>\<\!\(\)\{\}\[\]\^""\~\*\?\:\/]", " ");
                }
            }

            var engagement = DataClient.EngagementGet(request.EngagementId).Data;
            if(engagement.Saltminer.Engagement.Status == EnumExtensions.GetDescription(EngagementStatus.Draft))
            {
                var qResult = QueueIssuesSearch(request);
                if (qResult.Success && (qResult.Data?.Any() ?? false))
                {
                    return new UiDataResponse<IssueFull>(qResult.Data.Select(x => new IssueFull(x, UiApiConfig.AppVersion, fieldInfo)).ToList(), qResult, qResult.Pager);
                }
                response = qResult;
            }
            else
            {
                var result = IssuesSearch(request);
                if (result.Success && (result.Data?.Any() ?? false))
                {
                    return new UiDataResponse<IssueFull>(result.Data.Select(x => new IssueFull(x, UiApiConfig.AppVersion, fieldInfo)).ToList(), result, result.Pager);
                }
                response = result;
            }
            return new UiDataResponse<IssueFull>(null)
            {
                ErrorMessages = response.ErrorMessages,
                ErrorType = response.ErrorType,
                StatusCode = response.StatusCode,
                Affected = response.Affected,
                Message = response.Message
            };
        }

        internal DataItemResponse<IssueCount> GetEngagementIssueCount(string engagementId)
        {
            var resposne = DataClient.EngagementIssueCounts(engagementId, UiApiConfig.AssetType, UiApiConfig.SourceType, UiApiConfig.Instance);


            resposne.Data.TryGetValue("Critical", out long? critical);
            resposne.Data.TryGetValue("High", out long? high);
            resposne.Data.TryGetValue("Medium", out long? medium);
            resposne.Data.TryGetValue("Low", out long? low);
            resposne.Data.TryGetValue("Info", out long? info);

            var result = new IssueCount
            {
                Critical = critical.HasValue ? (int)critical.Value : 0,
                High = high.HasValue ? (int)high.Value : 0,
                Medium = medium.HasValue ? (int)medium.Value : 0,
                Low = low.HasValue ? (int)low.Value : 0,
                Info = info.HasValue ? (int)info.Value : 0,
                Id = engagementId
            };

            return new DataItemResponse<IssueCount>(result);
        }

        internal UiDataResponse<QueueIssue> QueueIssuesSearch(IssueSearch request)
        {
            var filters = BuildEngagementIssueFilters(request);

            if (request.AssetFilters != null && request.AssetFilters.Count != 0)
            {
                filters.Add("Saltminer.QueueAssetId", SaltMiner.DataClient.Helpers.BuildTermsFilterValue(request.AssetFilters));
            }

            var searchRequest = new SearchRequest()
            {
                UIPagingInfo = new UIPagingInfo(request.Pager?.Size ?? Config.DefaultPageSize, 1)
                {
                    SortFilters = Helpers.SearchFilters.MapSortFilters(request.Pager?.SortFilters, SortFilterValues)
                },
                Filter = new()
                {
                    AnyMatch = false,
                    FilterMatches = filters,
                    SubFilter = BuildEngagementIssueSubFilter(request, true)
                }
            };

            var response = DataClient.QueueIssueSearch(searchRequest);
            var vals = new int[] { 0, 1 };
            var sort = SearchFilters?.Find(x => x.Type == SearchFilterType.IssueSortFilters.ToString())?.Filters ?? [];

            while (response.Success && response.Data != null && response.Data.Any())
            {
                searchRequest.UIPagingInfo = response.UIPagingInfo;
                if (vals.Contains(request.Pager?.Page ?? 0) || searchRequest.UIPagingInfo.Page == request.Pager.Page)
                {
                    Logger.LogInformation("{Msg}", GeneralExtensions.SearchUIPagingLoggerMessage("QueueIssue", filters?.Count ?? 0, response.UIPagingInfo.Size, response.UIPagingInfo.Page));
                    break;
                }
                searchRequest.UIPagingInfo.Page++;
                searchRequest.AfterKeys = response.AfterKeys;

                response = DataClient.QueueIssueSearch(searchRequest);
            }
            return new UiDataResponse<QueueIssue>(response.Data, sort, response.UIPagingInfo, true);
        }

        private UiDataResponse<Issue> IssuesSearch(IssueSearch request)
        {
            var filters = BuildEngagementIssueFilters(request);

            if (request.AssetFilters != null && request.AssetFilters.Count != 0)
            {
                filters.Add("Saltminer.Asset.Id", SaltMiner.DataClient.Helpers.BuildTermsFilterValue(request.AssetFilters));
            }

            var searchRequest = new SearchRequest()
            {
                UIPagingInfo = new UIPagingInfo(request.Pager?.Size ?? Config.DefaultPageSize, 1)
                {
                    SortFilters = Helpers.SearchFilters.MapSortFilters(request.Pager?.SortFilters, SortFilterValues)
                },
                Filter = new()
                {
                    FilterMatches = filters,
                    SubFilter = BuildEngagementIssueSubFilter(request)
                },
                AssetType = UiApiConfig.AssetType,
                SourceType = UiApiConfig.SourceType,
                Instance = UiApiConfig.Instance
            };

            var response = DataClient.IssueSearch(searchRequest);
            var sort = SearchFilters?.Find(x => x.Type == SearchFilterType.IssueSortFilters.ToString())?.Filters ?? [];
            var vals = new int[] { 0, 1 };

            while (response.Success && response.Data != null && response.Data.Any())
            {
                searchRequest.UIPagingInfo = response.UIPagingInfo;
                if (vals.Contains(request.Pager?.Page ?? 0) || searchRequest.UIPagingInfo.Page == request.Pager.Page)
                {
                    Logger.LogInformation("{Msg}", GeneralExtensions.SearchUIPagingLoggerMessage("Issue", filters?.Count ?? 0, response.UIPagingInfo.Size, response.UIPagingInfo.Page));
                    break;
                }
                searchRequest.UIPagingInfo.Page++;
                searchRequest.AfterKeys = response.AfterKeys;

                response = DataClient.IssueSearch(searchRequest);
            }
            return new UiDataResponse<Issue>(response.Data, sort, response.UIPagingInfo, false);
        }

        private Dictionary<string, string> BuildEngagementIssueFilters(IssueSearch request)
        {
            Logger.LogInformation("Building SearchFilters from search request.");

            var filters = new Dictionary<string, string>();

            if (request.SeverityFilters != null && request.SeverityFilters.Count != 0)
            {
                filters.Add("Vulnerability.Severity", SaltMiner.DataClient.Helpers.BuildTermsFilterValue(request.SeverityFilters));
            }

            if (request.TestStatusFilters != null && request.TestStatusFilters.Count != 0)
            {
                filters.Add("Vulnerability.TestStatus", SaltMiner.DataClient.Helpers.BuildTermsFilterValue(request.TestStatusFilters));
            }

            filters.Add("Saltminer.Engagement.Id", request.EngagementId);

            return filters;
        }

        private Filter BuildEngagementIssueSubFilter(IssueSearch request, bool isQueue = false)
        {
            Logger.LogInformation("Building SubFilters from search request.");

            Filter result = null;

            if (request.StateFilters != null && request.StateFilters.Count != 0)
            {
                result = new Filter
                {
                    AnyMatch = true,
                    FilterMatches = []
                };

                foreach (var status in request.StateFilters)
                {
                    switch (status.ToLower())
                    {
                        case "notactive":
                            result.FilterMatches.Add("Vulnerability.IsActive", "false");
                            break;
                        case "isactive":
                            result.FilterMatches.Add("Vulnerability.IsActive", "true");
                            break;
                        case "issuppressed":
                            result.FilterMatches.Add("Vulnerability.IsSuppressed", "true");
                            break;
                        case "isremoved":
                            result.FilterMatches.Add("Vulnerability.IsRemoved", "true");
                            break;
                        default:
                            break;
                    }
                }
            }

            if (request.SearchFilters != null)
            {
                var searchFilterResult = new Filter
                {
                    AnyMatch = true,
                    FilterMatches = []
                };

                Helpers.SearchFilters.AddFilters(searchFilterResult.FilterMatches, SearchFilterValues, request.SearchFilters, isQueue);
                
                if (searchFilterResult.FilterMatches.Count != 0)
                {
                    if (result == null)
                    {
                        result = searchFilterResult;
                    }
                    else
                    {
                        result.SubFilter = searchFilterResult;
                    }
                }

                var nonFilterResult = new Filter
                {
                    AnyMatch = true,
                    FilterMatches = []
                };

                Helpers.SearchFilters.AddNonFilters(nonFilterResult.FilterMatches, SearchFilterValues, request.SearchFilters);

                if (nonFilterResult.FilterMatches.Count != 0)
                {
                    if (result == null)
                    {
                        result = nonFilterResult;
                    }
                    else if (result.SubFilter == null)
                    {
                        result.SubFilter = nonFilterResult;
                    }
                    else
                    {
                        result.SubFilter.SubFilter = nonFilterResult;
                    }
                }
            }

            return result != null && result.FilterMatches != null && result.FilterMatches.Count != 0 ? result : null;
        }

        #endregion

        #region Lookups
        private List<LookupValue> GetTestedList(Lookup lookup)
        {
            if (lookup.Values == null)
            {
                return [];
            }

            if (lookup.Values.Exists(x => x.Value == "Found"))
            {
                return lookup.Values;
            }

            lookup.Values.Add(new LookupValue
            {
                Display = "Found",
                Value = "Found",
                Order = 1
            });

            DataClient.LookupAddUpdate(lookup);
            return lookup.Values;
        }
        #endregion
    }
}