using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Responses;

namespace Saltworks.SaltMiner.Ui.Api.Contexts
{
    public class JobContext(IServiceProvider services, ILogger<JobContext> logger) : ContextBase(services, logger)
    {
        public UiNoDataResponse DeleteReport(string id)
        {
            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "id");
            DataClient.JobDelete(id);
            return new UiNoDataResponse(1);
        }

        public UiDataResponse<Job> PullPendingJobs(UiPager paging = null, string type = null)
        {
            var request = new SearchRequest
            {
                UIPagingInfo = paging != null ? paging.ToDataPager() : new UIPagingInfo
                {
                    Page = 1,
                    Size = 10
                },
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Status", Job.JobStatus.Pending.ToString("g") }
                    }
                }
            };

            if (!string.IsNullOrEmpty(type))
            {
                request.Filter.FilterMatches.Add("Type", type);
            }

            var result = DataClient.JobSearch(request);

            return new UiDataResponse<Job>(result.Data, result, SortFilterValues, result.UIPagingInfo, false);
        }

        public UiDataItemResponse<Job> UpdateQueue(Job queue, KibanaUser user)
        {
            queue.User ??= user.UserName;
            queue.UserFullName ??= user.FullName;

            var result = DataClient.JobAddUpdate(queue);

            DataClient.RefreshIndex(Job.GenerateIndex());

            return new UiDataItemResponse<Job>(result.Data);
        }

        public UiDataResponse<Job> GetJobs(JobSearch searchRequest, bool isFinished = false)
        {
            var request = new SearchRequest
            {
                UIPagingInfo = searchRequest.Pager != null ? searchRequest.Pager.ToDataPager() : new UIPagingInfo
                {
                    Page = 1,
                    Size = 10,
                    SortFilters = new Dictionary<string, bool>
                    {
                        { "timestamp", false }
                    }
                },
                Filter = new Filter
                {
                    AnyMatch = true,
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Status", isFinished ?
                            SaltMiner.DataClient.Helpers.BuildTermsFilterValue([Job.JobStatus.Complete.ToString("g"), Job.JobStatus.Error.ToString("g")]) : 
                            SaltMiner.DataClient.Helpers.BuildTermsFilterValue([Job.JobStatus.Pending.ToString("g"), Job.JobStatus.Processing.ToString("g")])
                        }
                    }
                }
            };

            request.UIPagingInfo.Page = 1;

            if (!string.IsNullOrEmpty(searchRequest.Type))
            {
                request.Filter.FilterMatches.Add("Type", searchRequest.Type);
            }

            request.UIPagingInfo.SortFilters = new Dictionary<string, bool> { { "timestamp", false } };

            //loop until page found
            var response = DataClient.JobSearch(request);

            while (response.Success && response.Data != null && response.Data.Any())
            {
                request.UIPagingInfo = response.UIPagingInfo;
                if ((searchRequest?.Pager?.Page == null || searchRequest.Pager.Page == 1 || searchRequest.Pager.Page == 0) || request.UIPagingInfo.Page == searchRequest.Pager.Page)
                {
                    return new UiDataResponse<Job>(response.Data, response, SortFilterValues, response.UIPagingInfo, false);
                }

                request.UIPagingInfo.Page++;
                request.AfterKeys = response.AfterKeys;

                response = DataClient.JobSearch(request);
            }

            return new UiDataResponse<Job>([]);
        }
    }
}
