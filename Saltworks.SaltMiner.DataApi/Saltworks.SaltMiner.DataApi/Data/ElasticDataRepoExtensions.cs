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
using Saltworks.SaltMiner.ElasticClient;
using System.Linq;

namespace Saltworks.SaltMiner.DataApi.Data;

    public static class ElasticDataRepoExtensions
    {
        private const string ERROR = "Error";
        public static DataResponse<T> ToDataResponse<T>(this IElasticClientResponse<T> result) where T : class => new() 
        {
            Data = result?.Results.Select(r => r.Document) ?? [],
            PagingInfo = result?.PagingInfo,
            Affected = result?.CountAffected ?? 0,
            Message = result?.Message,
            ErrorMessages = !result?.IsSuccessful == true ? new() { result?.Message } : null,
            ErrorType = !result?.IsSuccessful == true ? ERROR : null,
            StatusCode = result?.HttpStatus ?? 500
        };

        public static DataItemResponse<T> ToDataItemResponse<T>(this IElasticClientResponse<T> result) where T : class => new()
        {
            Affected = result?.CountAffected ?? 0,
            Message = result?.Message,
            ErrorMessages = !result?.IsSuccessful == true ? new() { result?.Message } : null,
            ErrorType = !result?.IsSuccessful == true ? ERROR : null,
            StatusCode = result?.HttpStatus ?? 500,
            Data = result?.Result?.Document
        };

        public static BulkResponse ToBulkResponse(this IElasticClientResponse result) => new()
        {
            Affected = result?.CountAffected ?? 0,
            Message = result?.Message,
            ErrorMessages = !result?.IsSuccessful == true ? new() { "Please see Bulk Errors" } : null,
            ErrorType = !result?.IsSuccessful == true ? ERROR : null,
            StatusCode = result?.HttpStatus ?? 500,
            BulkErrors = result?.BulkErrorMessages
        };

        public static NoDataResponse ToNoDataResponse<T>(this IElasticClientResponse<T> result) where T : class => new()
        {
            Affected = result?.CountAffected ?? 0,
            Message = result?.Message,
            ErrorMessages = !result?.IsSuccessful == true ? new() { result?.Message } : null,
            ErrorType = !result?.IsSuccessful == true ? ERROR : null,
            StatusCode = result?.HttpStatus ?? 500
        };

        public static NoDataResponse ToNoDataResponse(this IElasticClientResponse result) => new()
        {
            Affected = result?.CountAffected ?? 0,
            Message = result?.Message,
            ErrorMessages = !result?.IsSuccessful == true ? new() { result?.Message } : null,
            ErrorType = !result?.IsSuccessful == true ? ERROR : null,
            StatusCode = result?.HttpStatus ?? 500
        };
    }
