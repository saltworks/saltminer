/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-04-09
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿using System.Collections.Generic;
using Saltworks.SaltMiner.Core.Entities;
using System;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.ElasticClient;
using Saltworks.SaltMiner.DataApi.Models;

namespace Saltworks.SaltMiner.DataApi.Data
{
    public interface IDataRepo
    {
        /// <summary>
        /// Returns elasticsearch license type (basic, trial, enterprise).
        /// </summary>
        /// <returns></returns>
        NoDataResponse GetLicenseType();
        /// <summary>
        /// Updates the passed entity in the datasource, using passed locking info to assure no other updates came before
        /// </summary>
        /// <seealso cref="GetWithLocking{T}(string, string)"/>
        /// <typeparam name="T">Type of entity in the datasource</typeparam>
        /// <param name="entity">Entity to add/update</param>
        /// <param name="index">The index for the entity to be retrieved</param>
        /// <param name="lockInfo">Locking information used to insure a consistent update</param>
        /// <returns>The updated/inserted entity, including any updates made during the operation and updated locking info</returns>
        /// <remarks>If the lock fails due to the locking info being out of date, an exception will be thrown</remarks>
        Tuple<T, ILockingInfo> UpdateWithLocking<T>(T entity, string index, ILockingInfo lockInfo) where T : SaltMinerEntity;

        /// <summary>
        /// Returns index metadata for the passed template names
        /// </summary>
        /// <returns>The list of index metadata</returns>
        List<SaltMinerIndexData> GetMetadata(List<string> templateNames);

        /// <summary>
        /// Returns SaltMiner Index Mapping
        /// </summary>
        /// <returns>The Index Mapping Json</returns>
        string GetIndexMapping(string index);

        /// <summary>
        /// Returns SaltMiner Index Template
        /// </summary>
        /// <returns>The Index Template Json</returns>
        string GetIndexTemplate(string template);

        /// <summary>
        /// Re-index SaltMiner Index into another index
        /// </summary>
        /// <returns>Success</returns>
        IElasticClientResponse ReIndex(string indexName, string newIndexName);

        /// <summary>
        /// Delete SaltMiner Index
        /// </summary>
        /// <returns>Success</returns>
        IElasticClientResponse DeleteIndex(string indexName);

        /// <summary>
        /// Search SaltMiner Index and return json response
        /// </summary>
        /// <returns>Success</returns>
        string SearchForJson(SearchRequest request, string indexName);

        /// <summary>
        /// Updates SaltMiner Index Template
        /// </summary>
        /// <returns>The Index Template Json</returns>
        IElasticClientResponse UpdateIndexTemplate(string templateName, string newTemplate);

        /// <summary>
        /// Returns snapshot aggregates for the given grouping fields and asset type
        /// </summary>
        /// <param name="pager">Returns next batch of data if the ScrollKeys are included</param>
        /// <param name="sourceFields">Composite Source fields to use for aggregation</param>
        /// <param name="aggList">List to use for aggregations</param>
        /// <param name="assetType">Type of asset to query (* should work)</param>
        /// <returns>Result set including the keys and counts for the keys, as well as scroll information for getting the next batch</returns>
        ElasticAggResponse SnapshotAggregates(PitPagingInfo pager, IEnumerable<string> sourceFields, IEnumerable<IElasticClientRequestAggregate> aggList, string assetType);

        /// <summary>
        /// Returns snapshot aggregates for the given grouping fields and asset type
        /// </summary>
        /// <param name="pager">Returns next batch of data if the ScrollKeys are included</param>
        /// <param name="sourceFields">Composite Source fields to use for aggregation</param>
        /// <param name="aggList">List to use for aggregations</param>
        /// <param name="assetType">Type of asset to query (* should work)</param>
        /// <returns>Result set including the keys and counts for the keys, as well as scroll information for getting the next batch</returns>
        ElasticAggResponse EngagementIssueCountAggregates(string engagementId, PitPagingInfo pager, IEnumerable<string> sourceFields, IEnumerable<IElasticClientRequestAggregate> aggList, string assetType);

        /// <summary>
        /// Returns an entity of type T from the datasource by its id, including locking info needed to call UpdateWithLockingInfo
        /// </summary>
        /// <seealso cref="UpdateWithLocking{T}(T, string, ILockingInfo{T})"/>
        /// <typeparam name="T">Type of entity in the datasource</typeparam>
        /// <param name="id">The identifier for the entity to be retrieved</param>
        /// <param name="indexName">The index for the entity to be retrieved</param>
        /// <returns>Tuple containing the requested entity (or null if not found) and locking information</returns>
        Tuple<T, ILockingInfo> GetWithLocking<T>(string id, string indexName) where T : SaltMinerEntity;

        /// <summary>
        /// Returns a list of entities of type T that match the passed filter criteria on specified data index
        /// </summary>
        /// <typeparam name="T">Type of entity in the datasource</typeparam>
        /// <param name="request">The criteria for the search</param>
        /// <param name="indexName">The index for the entity to be retrieved</param>
        /// <returns>The list of results</returns>
        DataResponse<T> Search<T>(SearchRequest request, string indexName) where T : SaltMinerEntity;

        /// <summary>
        /// Returns aggregates for a specified group field
        /// </summary>
        /// <param name="groupField">The "bucket" or grouping field (returned with the results)</param>
        /// <param name="dataIndex">The index or pattern on which to run the search</param>
        /// <param name="fieldAggregates">One or more aggregate definitions (sum, avg, count) that each operate on a single field</param>
        /// <param name="request">[optional] filter criteria for the search</param>
        /// <returns></returns>
        IEnumerable<ElasticAggResponse> SingleGroupAggregation(string groupField, string dataIndex, Dictionary<string, string> fieldAggregates, SearchRequest request = null);

        /// <summary>
        /// Updates a list of T documents in specified index
        /// </summary>
        /// <typeparam name="T">Type of entity in the datasource</typeparam>
        /// <param name="docs">The documents to add/update</param>
        /// <param name="indexName">The index for the entity to be retrieved</param>
        /// <returns>Success</returns>
        BulkResponse AddUpdateBulk<T>(IEnumerable<T> docs, string indexName) where T : SaltMinerEntity;

        /// <summary>
        /// Adds alias to index
        /// </summary>
        /// <param name="indexName">The index for the entity to be retrieved</param>
        /// <param name="alias">Alias to add</param>
        /// <returns>Success</returns>
        NoDataResponse ActiveIssueAlias(string indexName, string alias);
    }

    public interface ILockingInfo
    {
        string Id { get; }
    }
}
