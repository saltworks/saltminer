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
            Pager = new(pagingInfo.Size, pagingInfo.Page)
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
