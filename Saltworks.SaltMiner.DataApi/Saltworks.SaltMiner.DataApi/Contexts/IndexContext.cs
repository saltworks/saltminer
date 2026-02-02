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

using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.Core.Data;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.ElasticClient;
using Saltworks.SaltMiner.Core.Entities;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

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
            // Using TestItem as a way to get the Core assembly to then get the SaltMinerEntity type
            if (typeof(TestItem).Assembly.GetType(request.TypeName) is not Type t || !typeof(SaltMinerEntity).IsAssignableFrom(t))
            {
                throw new ApiValidationException($"Invalid type '{request.TypeName}'");
            }
            if (request?.Documents == null || !request.Documents.Any())
                throw new ApiValidationMissingArgumentException("Missing/invalid documents");

            Logger.LogInformation("BulkAddUpdate: {Count} docs for type '{Type}'", request.Documents.Count(), request.TypeName);

            // Deserialize documents to their correct type and create a strongly-typed list
            var listType = typeof(List<>).MakeGenericType(t);
            var deserializedDocs = Activator.CreateInstance(listType) as System.Collections.IList
                ?? throw new ApiValidationException("Failed to create typed list");
            
            foreach (var doc in request.Documents)
            {
                var deserialized = doc.Deserialize(t, JsonSerializerOptions.Web);
                deserializedDocs.Add(deserialized);
            }

            // Use reflection to call the generic BulkAddUpdate<T> method with the correct type
            // This preserves the derived type information instead of casting to SaltMinerEntity
            var method = typeof(IElasticClient).GetMethod(nameof(IElasticClient.BulkAddUpdate)) ?? throw new ApiValidationException("Could not find BulkAddUpdate method");
            var genericMethod = method.MakeGenericMethod(t);
            var result = (IElasticClientResponse)genericMethod.Invoke(ElasticClient, [deserializedDocs, index])
                ?? throw new ApiValidationException("BulkAddUpdate returned null");

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
