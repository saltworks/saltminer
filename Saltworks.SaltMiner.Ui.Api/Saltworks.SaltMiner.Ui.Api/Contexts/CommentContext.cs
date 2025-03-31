using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Email;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient.ViewModels;
using System.Net.Mail;

namespace Saltworks.SaltMiner.Ui.Api.Contexts
{
    public class CommentContext(IServiceProvider services, ILogger<CommentContext> logger, AssetContext assetContext, IssueContext issueContext) : ContextBase(services, logger)
    {
        private readonly AssetContext _AssetContext = assetContext;
        private readonly IssueContext _IssueContext = issueContext;

        private AssetContext AssetContext
        {
            get
            {
                if (_AssetContext != null && _AssetContext.Controller == null)
                    _AssetContext.Controller = Controller;
                return _AssetContext;
            }
        }

        private IssueContext IssueContext
        {
            get
            {
                if (_IssueContext != null && _IssueContext.Controller == null)
                    _IssueContext.Controller = Controller;
                return _IssueContext;
            }
        }

        protected override List<SearchFilterValue> SortFilterValues => SearchFilters?.Find(x => x.Type == SearchFilterType.CommentSortFilters.ToString())?.Filters ?? [];

        public UiDataItemResponse<UiComment> New(CommentNotice request, KibanaUser user)
        {
            Logger.LogInformation("New Comment initiated");

            request.Request.IsModelValid(Config.ApiFieldRegex);

            ValidateEmailAddress(request.MentionAddresses);
            
            Logger.LogInformation("New Comment");
            var result = DataClient.CommentAddUpdate(request.Request.TransformNewComment("User", user?.UserName ?? "Testing", user?.FullName ?? "Testing"));

            DataClient.RefreshIndex(Comment.GenerateIndex());

            if (request.MentionAddresses != null && request.MentionAddresses.Count > 0)
            {
                Logger.LogInformation("Comment contains Mentions");
                Mention(result.Data, request.MentionAddresses);
            }

            return new UiDataItemResponse<UiComment>(new UiComment(result.Data, UiApiConfig.AppVersion), result);
        }

        public void AssetComment(string assetId, string action, KibanaUser user)
        {
            Logger.LogInformation("{Action} Asset Comment initiated", action);

            General.ValidateIdAndInput(assetId, Config.ApiFieldRegex, "Id");

            Logger.LogInformation("Asset Get");
            var asset = AssetContext.Get(assetId);

            var request = new CommentNotice
            {
                Request = new CommentNew
                {
                    AssetId = assetId,
                    EngagementId = asset.Data.Engagement.Id,
                    Message = $"{action} Asset."
                }
            };

            DataClient.CommentAddUpdate(request.Request.TransformNewComment($"{action} Asset", user?.UserName ?? "Testing", user?.FullName ?? "Testing"));
        }

        public void AssetCommentAddBulk(List<QueueAsset> queueAssets, string action, KibanaUser user)
        {
            Logger.LogInformation("{Action} Asset Comment initiated", action);
            
            var comments = new List<Comment>();

            foreach (var asset in queueAssets)
            {
                var request = new CommentNotice
                {
                    Request = new CommentNew
                    {
                        AssetId = asset.Id,
                        EngagementId = asset.Saltminer.Engagement.Id,
                        Message = $"{action} Asset."
                    }
                };

                comments.Add(request.Request.TransformNewComment($"{action} Asset", user?.UserName ?? "Testing", user?.FullName ?? "Testing"));
            }

            DataClient.CommentAddUpdateBulk(comments);
        }

        public void IssueComment(string issueId, string action, KibanaUser user)
        {
            Logger.LogInformation("{Action} Issue Comment initiated", action);

            General.ValidateIdAndInput(issueId, Config.ApiFieldRegex, "Id");

            Logger.LogInformation("Issue Get");
            var issue = IssueContext.Get(issueId, FieldInfo(FieldInfoEntityType.Issue));

            var request = new CommentNotice
            {
                Request = new CommentNew
                {
                    IssueId = issueId,
                    AssetId = issue.Data.AssetId,
                    EngagementId = issue.Data.Engagement.Id,
                    Message = $"{action} Issue.",
                }
            };

            DataClient.CommentAddUpdate(request.Request.TransformNewComment($"{action} Issue", user?.UserName ?? "Testing", user?.FullName ?? "Testing"));
        }

        public void IssueCommentAddBulk(List<QueueIssue> queueIssues, string action, KibanaUser user)
        {
            Logger.LogInformation("{Action} Issue Comment initiated", action);

            var comments = new List<Comment>();
            
            foreach (var issue in queueIssues)
            {

                var request = new CommentNotice
                {
                    Request = new CommentNew
                    {
                        IssueId = issue.Id,
                        AssetId = issue.Saltminer.QueueAssetId,
                        EngagementId = issue.Saltminer.Engagement.Id,
                        Message = $"{action} Issue.",
                    }
                };

                comments.Add(request.Request.TransformNewComment($"{action} Issue", user?.UserName ?? "Testing", user?.FullName ?? "Testing"));
            }

            DataClient.CommentAddUpdateBulk(comments);
        }

        public void EngagementComment(string engagementId, string action, KibanaUser user)
        {
            Logger.LogInformation("{Action} Engagement Comment initiated", action);

            General.ValidateIdAndInput(engagementId, Config.ApiFieldRegex, "Id");

            var request = new CommentNotice
            {
                Request = new CommentNew
                {
                    EngagementId = engagementId,
                    Message = $"{action} Engagement.",
                }
            };

            DataClient.CommentAddUpdate(request.Request.TransformNewComment($"{action} Engagement", user?.UserName ?? "Testing", user?.FullName ?? "Testing"));
        }

        public void CSVComment(string engagementId, KibanaUser user)
        {
            Logger.LogInformation("Issue CSV Import Comment initiated");

            General.ValidateIdAndInput(engagementId, Config.ApiFieldRegex, "Id");

            var request = new CommentNotice
            {
                Request = new CommentNew
                {
                    EngagementId = engagementId,
                    Message = $"Issue CSV Import.",
                }
            };

            DataClient.CommentAddUpdate(request.Request.TransformNewComment($"Issue CSV Import", user?.UserName ?? "Testing", user?.FullName ?? "Testing"));
        }

        public void JSONComment(string engagementId, KibanaUser user)
        {
            Logger.LogInformation("Issue JSON Import Comment initiated");

            General.ValidateIdAndInput(engagementId, Config.ApiFieldRegex, "Id");

            var request = new CommentNotice
            {
                Request = new CommentNew
                {
                    EngagementId = engagementId,
                    Message = $"Issue JSON Import.",
                }
            };

            DataClient.CommentAddUpdate(request.Request.TransformNewComment($"Issue JSON Import", user?.UserName ?? "Testing", user?.FullName ?? "Testing"));
        }

        public UiDataItemResponse<UiComment> Edit(CommentEdit request)
        {
            Logger.LogInformation("Get Comment for '{Id}' initiated", request.Id);

            request.IsModelValid(Config.ApiFieldRegex);

            ValidateEmailAddress(request.MentionAddresses);

            Logger.LogInformation("Get Comment '{Id}'", request.Id);
            var comment = DataClient.CommentGet(request.Id).Data;
            comment.Saltminer.Comment.Message = request.Message;

            Logger.LogInformation("Edit Comment '{Id}'", request.Id);
            var result = DataClient.CommentAddUpdate(comment);

            if (request.MentionAddresses != null && request.MentionAddresses.Count != 0)
            {
                Logger.LogInformation("Comment contains Mentions");
                Mention(result.Data, request.MentionAddresses);
            }
            return new UiDataItemResponse<UiComment>(new UiComment(result.Data, UiApiConfig.AppVersion), result);
        }

        public UiNoDataResponse Delete(string id)
        {
            Logger.LogInformation("Delete Comment for '{Id}' initiated", id);

            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "Id");

            Logger.LogInformation("Delete Comment '{Id}'", id);
            var response = DataClient.CommentDelete(id);
            if(!response.Success || response.Affected == 0)
            {
                throw new UiApiNotFoundException($"Comment {id} does not exist.");
            }

            return new UiNoDataResponse(1);
        }

        public UiDataResponse<UiComment> Search(CommentSearch request)
        {
            Logger.LogInformation("Search Comment initiated");

            request.IsModelValid(Config.ApiFieldRegex);

            var filters = new Dictionary<string, string>();

            if (!request.IncludeSystem)
            {
                filters.Add("Saltminer.Comment.Type", "User");
            }
           
            if (request.EngagementId == null)
            {
                throw new UiApiClientValidationMissingArgumentException($"Engagement id is required to search comments.");
            }

            filters.Add("Saltminer.Engagement.Id", request.EngagementId);

            if (!string.IsNullOrEmpty(request.AssetId))
            {
                filters.Add("Saltminer.Asset.Id", request.AssetId);
            }
           
            if (!string.IsNullOrEmpty(request.ScanId))
            {
                filters.Add("Saltminer.Scan.Id", request.ScanId);
            }
            
            if (!string.IsNullOrEmpty(request.IssueId))
            {
                filters.Add("Saltminer.Issue.Id", request.IssueId);
            }

            var searchRequest = new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = filters
                },
                UIPagingInfo = new UIPagingInfo(request.Pager?.Size ?? Config.DefaultPageSize, 1)
            };

            //search until page is found
            var response = DataClient.CommentSearch(searchRequest);

            while (response.Success && response.Data != null && response.Data.Any())
            {
                searchRequest.UIPagingInfo = response.UIPagingInfo;
                if ((request.Pager?.Page == null || request.Pager.Page == 1 || request.Pager.Page == 0) || searchRequest.UIPagingInfo.Page == request.Pager.Page)
                {
                    Logger.LogInformation("{Msg}", Extensions.GeneralExtensions.SearchUIPagingLoggerMessage("Comment", filters.Count, response.UIPagingInfo.Size, response.UIPagingInfo.Page));
                    return new UiDataResponse<UiComment>(response.Data.Select(x => new UiComment(x, UiApiConfig.AppVersion)).ToList(), response, SortFilterValues, response.UIPagingInfo, false);
                }

                searchRequest.UIPagingInfo.Page++;
                searchRequest.AfterKeys = response.AfterKeys;

                response = DataClient.CommentSearch(searchRequest);
            }

            return new UiDataResponse<UiComment>([]);
        }

        public UiDataItemResponse<UiComment> Get(string id)
        {
            Logger.LogInformation("Get Comment for '{Id}' initiated", id);

            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "Id");

            Logger.LogInformation("Get Comment '{Id}'", id);
            var result = DataClient.CommentGet(id);
            if (!result.Success || result.Data == null)
            {
                throw new UiApiNotFoundException($"Comment {id} does not exist.");
            }

            return new UiDataItemResponse<UiComment>(new UiComment(result.Data, UiApiConfig.AppVersion), result);
        }

        private void Mention(Comment comment, List<string> addresses)
        {
            foreach(var address in addresses)
            {
                Logger.LogInformation("Comment Mention Sent to '{Address}'", address);
                if(Config.EmailFromAddress == null || Config.EmailHost == null || Config.EmailUserName == null || Config.EmailPassword == null)
                {
                    throw new UiApiClientValidationMissingValueException("Email Configuration not set up, however Comment was added.");
                }

                var request = new EmailRequest(Config.EmailFromAddress, Config.EmailFromDisplay, address, address)
                {
                    Body = comment.Saltminer.Comment.Message,
                    Subject = $"{comment.Saltminer.Comment.UserFullName} mentioned you in a comment ({comment.Id})",
                    Host = Config.EmailHost,
                    Port = Config.EmailPort,
                    UserName = Config.EmailUserName,
                    Password = Config.EmailPassword
                };

                Email.Send(request);
            }
        }

        private static void ValidateEmailAddress(List<string> addresses)
        {
            if(addresses == null || addresses.Count == 0)
            {
                return;
            }

            foreach (var address in addresses)
            {
                try
                {
                    _ = new MailAddress(address);
                }
                catch (FormatException)
                {
                    throw new UiApiClientValidationException($"Mention address {address} is not a valid email address");
                }
            }
        }
    }
}
