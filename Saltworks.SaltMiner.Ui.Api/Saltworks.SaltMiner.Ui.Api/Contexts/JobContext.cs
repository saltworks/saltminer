/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
*/

using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Responses;

namespace Saltworks.SaltMiner.Ui.Api.Contexts;
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
            PagingInfo = paging != null ? paging.ToPagingInfo() : new(10),
            Filter = new Filter
            {
                FilterMatches = new Dictionary<string, string>
                {
                    { "Status", Job.JobStatus.Pending.ToString("g") }
                }
            },
            SortKeys = new() { { "Timestamp", false } }
        };

        if (!string.IsNullOrEmpty(type))
            request.Filter.FilterMatches.Add("Type", type);
        
        var result = DataClient.JobSearch(request);
        return new UiDataResponse<Job>(result.Data, result.PagingInfo);
    }

    public UiDataItemResponse<Job> UpdateQueue(Job queue, KibanaUser user)
    {
        queue.User ??= user.UserName;
        queue.UserFullName ??= user.FullName;
        queue.Status = Job.JobStatus.Pending.ToString("g");  // tell job manager there's an update to process

        var result = DataClient.JobAddUpdate(queue);

        DataClient.RefreshIndex(Job.GenerateIndex());

        return new UiDataItemResponse<Job>(result.Data);
    }

    public UiDataResponse<Job> GetJobs(JobSearch searchRequest, bool isFinished = false)
    {
        var request = new SearchRequest
        {
            PagingInfo = searchRequest.Pager?.ToPagingInfo() ?? new(10),
            SortKeys = new() { { "timestamp", false } },
            Filter = new Filter { AnyMatch = true }
        };
        List<string> terms = isFinished ? 
            [Job.JobStatus.Complete.ToString("g"), Job.JobStatus.Error.ToString("g")] :
            [Job.JobStatus.Pending.ToString("g"), Job.JobStatus.Processing.ToString("g")];
        request.Filter.AddTermsFilterMatch("Status", terms);

        if (!string.IsNullOrEmpty(searchRequest.Type))
            request.Filter.FilterMatches.Add("Type", searchRequest.Type);

        var response = DataClient.JobSearch(request);
        return new UiDataResponse<Job>(response.Data, response.PagingInfo);
    }
}
