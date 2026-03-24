/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
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
