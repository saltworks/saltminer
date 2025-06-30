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
using Saltworks.SaltMiner.UiApiClient.Helpers;

namespace Saltworks.SaltMiner.UiApiClient.Responses
{
    public class UiDataResponse<T> : DataResponse<T> where T : class
    {
        public List<FieldFilter> SortOptions { get; set; } = [];
        public UiPager Pager { get; set; } = new();

        public UiDataResponse(IEnumerable<T> data) : base(data) { }
        public UiDataResponse(IEnumerable<T> data, Response response, UiPager pager) : base(data)
        {
            StatusCode = response.StatusCode;
            Message = response.Message;
            Affected = response.Affected;
            Pager = pager;
            UIPagingInfo = pager.ToDataPager();
        }
        public UiDataResponse(IEnumerable<T> data, UIPagingInfo dataPager, bool isQueue = false) : base(data)
        {
            StatusCode = 200;
            Affected = data.Count();
            Pager = dataPager != null ? new(dataPager, [], isQueue) : null;
            UIPagingInfo = Pager.ToDataPager();
        }
        public UiDataResponse(IEnumerable<T> data, List<SearchFilterValue> sortFilters, UIPagingInfo dataPager, bool isQueue = false) : base(data)
        {
            StatusCode = 200;
            Affected = data.Count();
            Pager = dataPager != null ? new(dataPager, sortFilters, isQueue) : null;
            UIPagingInfo = Pager.ToDataPager();
            SortOptions = sortFilters?.Select(f => new FieldFilter(f))?.ToList();
        }
        public UiDataResponse(IEnumerable<T> data, Response response, List<SearchFilterValue> sortFilters, UIPagingInfo dataPager, bool isQueue = false) : base(data, dataPager)
        {
            StatusCode = response.StatusCode;
            Message = response.Message;
            Affected = response.Affected;
            Pager = dataPager != null ? new(dataPager, sortFilters, isQueue) : null;
            SortOptions = sortFilters?.Select(f => new FieldFilter(f))?.ToList();
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
