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
using Saltworks.SaltMiner.ElasticClient;
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.DataApi.Data
{
    public static class ElasticDataRepoExtensions
    {
        public static DataResponse<T> ToDataResponse<T>(this IElasticClientResponse<T> result) where T : class => new() 
        {
            Data = result?.Results.Select(r => r.Document) ?? new List<T>(),
            PitPagingInfo = result?.UIPagingInfo == null ? result?.PitPagingInfo : null,
            UIPagingInfo = result?.UIPagingInfo != null ? result.UIPagingInfo : null,
            AfterKeys = result?.AfterKeys,
            Affected = result?.CountAffected ?? 0,
            Message = result?.Message,
            ErrorMessages = !result?.IsSuccessful == true ? new List<string> { result?.Message } : null,
            ErrorType = !result?.IsSuccessful == true ? "Elastic" : null,
            StatusCode = result?.HttpStatus ?? 500
        };

        public static NoDataResponse ToNoDataResponse<T>(this IElasticClientResponse<T> result) where T : class => new()
        {
            Affected = result?.CountAffected ?? 0,
            Message = result?.Message,
            ErrorMessages = !result?.IsSuccessful == true ? new List<string> { result?.Message } : null,
            ErrorType = !result?.IsSuccessful == true ? "Elastic" : null,
            StatusCode = result?.HttpStatus ?? 500
        };

        public static DataItemResponse<T> ToDataItemResponse<T>(this IElasticClientResponse<T> result) where T : class => new()
        {
            Affected = result?.CountAffected ?? 0,
            Message = result?.Message,
            ErrorMessages = !result?.IsSuccessful == true ? new List<string> { result?.Message } : null,
            ErrorType = !result?.IsSuccessful == true ? "Elastic" : null,
            StatusCode = result?.HttpStatus ?? 500,
            Data = result?.Result?.Document
        };

        public static BulkResponse ToBulkResponse(this IElasticClientResponse result) => new()
        {
            Affected = result?.CountAffected ?? 0,
            Message = result?.Message,
            ErrorMessages = !result?.IsSuccessful == true ? new List<string> { "Please see Bulk Errors" } : null,
            ErrorType = !result?.IsSuccessful == true ? "Elastic" : null,
            StatusCode = result?.HttpStatus ?? 500,
            BulkErrors = result?.BulkErrorMessages
        };

        public static NoDataResponse ToNoDataResponse(this IElasticClientResponse result) => new()
        {
            Affected = result?.CountAffected ?? 0,
            Message = result?.Message,
            ErrorMessages = !result?.IsSuccessful == true ? new List<string> { result?.Message } : null,
            ErrorType = !result?.IsSuccessful == true ? "Elastic" : null,
            StatusCode = result?.HttpStatus ?? 500
        };
    }
}
