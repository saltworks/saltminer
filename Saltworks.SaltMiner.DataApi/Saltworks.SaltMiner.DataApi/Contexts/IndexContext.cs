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

using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.Core.Data;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.ElasticClient;
using System.Linq;
using System;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class IndexContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<IndexContext> logger) : 
        ContextBase(config, dataRepository, factory, logger)
    {

        /// <summary>
        /// Bulk Add/Update
        /// </summary>
        /// <param name="request">DataRequest containing documents to add/update</param>
        /// <param name="index">The index name for which to add/update the documents</param>
        public virtual NoDataResponse BulkAddUpdate(JsonDataRequest request, string index)
        {
            if (request?.Documents == null || !request.Documents.Any())
                throw new ApiValidationMissingArgumentException("Missing/invalid documents");
            if (string.IsNullOrEmpty(request.TypeName))
                throw new ApiValidationMissingArgumentException("Missing/invalid type name");

            Logger.LogInformation("BulkAddUpdate: {Count} docs for type '{Type}'", request.Documents.Count(), request.TypeName);

            var result = ElasticClient.BulkAddUpdate(request.Documents, request.TypeName, index);
            return result.ToNoDataResponse();
        }

        public virtual JsonDataResponse Search(JsonSearchRequest request, string indexName)
        {
            if (string.IsNullOrEmpty(request.TypeName))
                throw new ApiValidationMissingArgumentException("Search request TypeName required.");
            Logger.LogInformation("Search: type '{Type}'", request.TypeName);
            var searchResponse = ElasticClient.Search(indexName, request);
            var rsp = new JsonDataResponse
            {
                TypeName = request.TypeName,
                Data = searchResponse.Results.Select(d => d.Document),
                PagingInfo = searchResponse.PagingInfo,
                StatusCode = searchResponse.HttpStatus,
                ErrorMessages = searchResponse.IsSuccessful ? [] : [searchResponse.Message]
            };
            return rsp;
        }

        /// <summary>
        /// Closes PIT search in elasticsearch
        /// </summary>
        /// <param name="id">PIT ID</param>
        public virtual NoDataResponse ClosePitSearch(string id)
        {
            ArgumentException.ThrowIfNullOrEmpty(id);
            Logger.LogInformation("Closing PIT search with ID '{Id}'", id);
            var rsp = ElasticClient.ClosePitSearch(id);
            if (rsp.IsSuccessful)
                return new NoDataResponse(1, "");
            else
                return new NoDataResponse(rsp.HttpStatus, "Elasticsearch", "Failed to close point in time search context.");
        }

        /// <summary>
        /// Deletes Index by name
        /// </summary>
        /// <param name="indexName">The indexName of the entity to return</param>
        /// <returns>NoDataResponse with boolean indicating success</returns>
        public virtual NoDataResponse DeleteIndex(string indexName)
        {
            Logger.LogInformation("DeleteIndex: '{IndexName}'", indexName);
            var result = ElasticClient.IndexDelete(indexName);
            return result.ToNoDataResponse();
        }

        /// <summary>
        /// Checks and Adds active_issue_soourcetpye alias
        /// </summary>
        /// <param name="indexName">The index name for which to update alias (i.e. issues_app_saltworks.ssc_ssc1)</param>
        /// <returns>NoDataResponse with boolean indicating success</returns>
        public virtual NoDataResponse ActiveIssueAlias(string indexName)
        {
            Logger.LogInformation("ActiveIssueAlias: '{IndexName}'", indexName);
            return DataRepo.ActiveIssueAlias(indexName, Config.DataIssueIndexDefaultAlias.Replace("[indexName]", indexName));
        }

        /// <summary>
        /// Refresh Index by name
        /// </summary>
        /// <param name="indexName">The indexName to refresh</param>
        /// <returns>NoDataResponse with boolean indicating success</returns>
        public virtual NoDataResponse RefreshIndex(string indexName)
        {
            Logger.LogInformation("RefreshIndex: '{IndexName}'", indexName);
            var result = ElasticClient.IndexRefresh(indexName);
            return result.ToNoDataResponse();
        }

        /// <summary>
        /// Check for Index by name
        /// </summary>
        /// <param name="indexName">The indexName to refresh</param>
        /// <returns>NoDataResponse with boolean indicating success</returns>
        public virtual NoDataResponse CheckForIndex(string indexName)
        {
            Logger.LogInformation("CheckForIndex: '{IndexName}'", indexName);
            var result = ElasticClient.IndexExists(indexName);
            return result.ToNoDataResponse();
        }
    }
}
