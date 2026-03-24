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

﻿using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Requests;

namespace Saltworks.SaltMiner.UiApiClient.Responses
{
    public class UiDataResponse<T> : DataResponse<T> where T : class
    {
        /// <summary>
        /// All possible sort options go here, not just the selected one(s)
        /// </summary>
        public List<FieldFilter> SortOptions { get; set; } = [];
        public UiPager Pager { get; set; } = new();
        public UiDataResponse(IEnumerable<T> data) : base(data) { }
        public UiDataResponse(IEnumerable<T> data, PagingInfo pagingInfo, IEnumerable<FieldFilter> sortOptions = null) : base(data, pagingInfo) 
        {
            Pager = new(pagingInfo.Size ?? 0, pagingInfo.Page)
            {
                Total = Convert.ToInt32(pagingInfo.TotalHits)
            };
            SortOptions = sortOptions?.ToList() ?? [];
            PagingInfo = pagingInfo;
        }
        public UiDataResponse(DataResponse<T> response, GenericSearch search, IEnumerable<FieldFilter> sortOptions = null) : base(response?.Data)
        {
            var pi = response?.PagingInfo ?? search.Pager.ToPagingInfo();
            StatusCode = response.StatusCode;
            Message = response.Message;
            Affected = response.Affected;
            Pager = new(pi, search.Pager.SortFilters);
            SortOptions = sortOptions?.ToList() ?? [];
            PagingInfo = pi;
        }
    }

    public class UiDataItemResponse<T> : DataItemResponse<T> where T : class
    {
        public UiDataItemResponse() { }
        public UiDataItemResponse(T data) : base(data) { }
        public UiDataItemResponse(T data, Response response) : base(data)
        {
            Affected = response.Affected;
            Message = response.Message;
            ErrorMessages = response.ErrorMessages;
            StatusCode = response.StatusCode;
            ErrorType = response.ErrorType;
        }
    }

    public class UiBulkResponse : UiNoDataResponse
    {
        public Dictionary<string, string> BulkErrors { get; set; }
        public UiBulkResponse() : base() { }
        public UiBulkResponse(long affected, string message = null, Dictionary<string, string> bulkErrors = null) : base(affected, message)
        {
            Affected = affected;
            Message = message;
            BulkErrors = bulkErrors;
        }
        public UiBulkResponse(BulkResponse response) : base()
        {
            Affected = response.Affected;
            Message = response.Message;
            BulkErrors = response.BulkErrors;
        }
    }

    public class UiNoDataResponse : NoDataResponse
    {
        public UiNoDataResponse() : base() { }
        public UiNoDataResponse(long affected, string message = "") : base(affected, message) { }
        public UiNoDataResponse(NoDataResponse response) : base(response.Affected, response.Message) { }
    }
}
