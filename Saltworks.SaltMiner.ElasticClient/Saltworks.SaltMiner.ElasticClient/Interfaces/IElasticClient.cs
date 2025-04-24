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

﻿using Saltworks.Common.Data;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.ElasticClient
{
    public interface IElasticClient
    {
        /// <summary>
        /// Get count of running tasks on cluster.  Useful for when you are about to add a bunch of "nowait" tasks.
        /// </summary>
        /// <returns>Response object with number of running tasks</returns>
        Task<IElasticClientResponse> GetClusterTaskCountAsync();
        /// <summary>
        /// Copy one index to another, creating the destination index if necessary
        /// </summary>
        /// <param name="sourceIndex">Source of the copy</param>
        /// <param name="destinationIndex">Destination for the copy</param>
        /// <returns>A response object with IsSuccessful set based on success</returns>
        IElasticClientResponse ReIndex(string sourceIndex, string destinationIndex);
        /// <summary>
        /// Refresh an existing index
        /// </summary>
        /// <param name="indexName">Specify index name</param>
        /// <param name="pauseMs">Wait this many ms before refresh</param>
        /// <returns>A response object with IsSuccessful set based on success</returns>
        IElasticClientResponse RefreshIndex(string indexName, int pauseMs = 1000);
        /// <summary>
        /// Flush an existing index
        /// </summary>
        /// <param name="indexName">Specify index name</param>
        /// <returns>A response object with IsSuccessful set based on success</returns>
        IElasticClientResponse FlushIndex(string indexName);
        /// <summary>
        /// Check for existing index
        /// </summary>
        /// <param name="indexName">Specify index name</param>
        /// <returns>A response object with IsSuccessful set based on success</returns>
        IElasticClientResponse CheckForIndex(string indexName);
        /// <summary>
        /// Get all indexes
        /// </summary>
        /// <returns>List of all indexes</returns>
        List<string> GetAllIndexes();
        /// <summary>
        /// Get all templates
        /// </summary>
        /// <returns>List of all templates</returns>
        List<string> GetAllTemplates();
        /// <summary>
        /// Creates a new index
        /// </summary>
        /// <param name="indexName">Specify index name</param>
        /// <param name="mapping">Specify json mappings</param>
        /// <param name="force">If set, overwrite existing index</param>
        /// <returns>A response object with IsSuccessful set based on success</returns>
        IElasticClientResponse CreateIndex(string indexName, string mapping = null, bool force = false);
        /// <summary>
        /// Deletes an existing index
        /// </summary>
        /// <param name="indexName">Specify index name</param>
        /// <returns>A response object with IsSuccessful set based on success</returns>
        IElasticClientResponse DeleteIndex(string indexName);
        /// <summary>
        /// Checks for active_issue_* alias on index
        /// </summary>
        /// <param name="indexName">Specify index name</param>
        /// <returns>A response object with IsSuccessful set based on success</returns>
        IElasticClientResponse CheckActiveIssueAlias(string indexName);
        /// <summary>
        /// Add ctive_issue_* alias on existing index
        /// </summary>
        /// <param name="indexName">Specify index name</param>
        /// <param name="alias">Specify alias to add</param>
        /// <returns>A response object with IsSuccessful set based on success</returns>
        IElasticClientResponse AddActiveIssueAlias(string indexName, string alias);
        /// <summary>
        /// Add/Update Index Template
        /// </summary>
        /// <param name="templateName">Specify template name</param>
        /// <param name="template">Specify template to add</param>
        /// <returns>A response object with IsSuccessful set based on whether the index has a template</returns>
        IElasticClientResponse AddUpdateIndexTemplate(string templateName, string template);
        /// <summary>
        /// Add/Update Index Policy
        /// </summary>
        /// <param name="policyName">Specify index policy name</param>
        /// <param name="policy">Specify index policy to add</param>
        /// <returns>A response object with IsSuccessful set based on whether the index has a policy</returns>
        IElasticClientResponse AddUpdateIndexPolicy(string policyName, string policy);
        /// <summary>
        /// Check Index Template Exists
        /// </summary>
        /// <param name="templateName">Specify template name</param>
        /// <returns>A response object with IsSuccessful set based on whether the index has a template</returns>
        IElasticClientResponse CheckIndexTemplateExists(string templateName);
        /// <summary>
        /// Get Index Template
        /// </summary>
        /// <param name="templateName">Specify template name</param>
        /// <returns>String of JSON Result</returns>
        string GetIndexTemplate(string templateName);
        /// <summary>
        /// Updates an index mapping, using the type provided to generate the mappings
        /// </summary>
        /// <param name="indexName">Specify index</param>
        /// <param name="newMapping">Mapping to be updated</param>
        /// <param name="newIndexName">Specify new index name</param>
        /// <returns>A response object with IsSuccessful set based on success</returns>
        IElasticClientResponse UpdateIndexMapping(string indexName, string newMapping = null, string newIndexName = null);
        /// <summary>
        /// Updates an index mapping, using the type provided to generate the mappings
        /// </summary>
        /// <param name="indexName">Specify index</param>
        /// <param name="newIndexName">Specify new index name</param>
        /// <returns>A response object with IsSuccessful set based on success</returns>
        IElasticClientResponse UpdateIndexName(string indexName, string newIndexName);
        /// <summary>
        /// Get an index mapping
        /// </summary>
        /// <param name="indexName">Specify index</param>
        /// <returns>Index Mapping</returns>
        string GetIndexMapping(string indexName);
        /// <summary>
        /// Returns elasticsearch cluster license level - Trial, Basic, Enterprise
        /// </summary>
        IElasticClientResponse GetClusterLicenseLevel();
        /// <summary>
        /// Add or update (upsert) document of type T
        /// </summary>
        /// <typeparam name="T">Type of the document is used to select (or create) the index</typeparam>
        /// <param name="doc">Document to add or update</param>
        /// <param name="index">Specify index</param>
        /// <returns>A response object with the added document, including concurrency information</returns>
        IElasticClientResponse<T> AddUpdate<T>(T doc, string index) where T : SaltMinerEntity;
        /// <summary>
        /// Runs a scripted update in bulk, passing in parameter objects for each item to be updated and the script to apply to all
        /// </summary>
        /// <typeparam name="T1">SaltMinerEntity type of the entities to be updated</typeparam>
        /// <typeparam name="T2">Type of update object to be passed as a param object - it should be a POCO.</typeparam>
        /// <param name="docs">Doc collection to be updated</param>
        /// <param name="indexNameFn">Delegate used to produce the index name</param>
        /// <param name="script">Painless script performing the update, i.e. 'ctx._source.field = params.update;' (or params.update.property - params is the ES params collection name)</param>
        /// <param name="updateObject">The parameter object to be used in the update</param>
        /// <param name="updateObjectName">The parameter object name referenced in the script, i.e. 'update'</param>
        /// <returns>A response indicating success or detailing errors</returns>
        IElasticClientResponse BulkPartialUpdate<T1, T2>(IEnumerable<T1> docs, Func<T1, string> indexNameFn, string script, T2 updateObject, string updateObjectName="update") where T1 : SaltMinerEntity where T2 : class;
        /// <summary>
        /// Add or update (upsert) multiple queue documents
        /// </summary>
        /// <param name="docs">Queue scan/asset/issue(s) to add or update</param>
        /// <returns>A response object containing a success flag and number of affected documents</returns>
        /// <remarks>Will throw an error if any document is not a QueueScan/QueueAsset/QueueIssue</remarks>
        IElasticClientResponse AddUpdateBulkQueue(IEnumerable<SaltMinerEntity> docs);
        /// <summary>
        /// Add or update (upsert) multiple documents of type T
        /// </summary>
        /// <typeparam name="T">Type of the document is used to select (or create) the index</typeparam>
        /// <param name="docs">Documents to add or update</param>
        /// <param name="index">Specify index</param>
        /// <returns>A response object containing a success flag and number of affected documents</returns>
        IElasticClientResponse AddUpdateBulk<T>(IEnumerable<T> docs, string index) where T : SaltMinerEntity;
        /// <summary>
        /// Bulk update passed docs, using locking and partial update
        /// </summary>
        /// <typeparam name="T">SaltMinerEntity type to be updated</typeparam>
        /// <typeparam name="U">Update object type - should be a poco</typeparam>
        /// <param name="dtos">Documents to be updated, including concurrency locking information and index</param>
        /// <param name="script">Painless script assigning the update value(s), i.e. 'ctx._source.field = params.update;' (or params.update.property - params is the ES params collection name)</param>
        /// <param name="updateObject">Update object to use - should be a poco</param>
        /// <param name="updateObjectName">Name of update object to use in script (i.e. 'update')</param>
        /// <returns></returns>
        IElasticClientResponse UpdatePartialBulkWithLocking<T, U>(IEnumerable<DataDto<T>> dtos, string script, U updateObject, string updateObjectName = "update") where T : SaltMinerEntity where U : class;
        /// <summary>
        /// Updates a document, checking to see that it is unchanged since retrieval, and failing if so
        /// </summary>
        /// <typeparam name="T">Type of the document is used to select the index</typeparam>
        /// <param name="doc">Document to update</param>
        /// <param name="index">Specify index</param>
        /// <param name="primary">Primary term, or ID of last node to update this document</param>
        /// <param name="seq">Sequence number, or ID of last sequence number with which this document was updated</param>
        /// <returns>A response object containing the updated document, including updated concurrency information</returns>
        IElasticClientResponse<T> UpdateWithLocking<T>(T doc, string index, long? primary, long? seq) where T : SaltMinerEntity;
        /// <summary>
        /// Updates a document without checking to see that it is unchanged since retrieval
        /// </summary>
        /// <typeparam name="T">Type of the document is used to select the index</typeparam>
        /// <param name="doc">Document to update</param>
        /// <param name="index">Specify index</param>
        /// <returns>A response object containing the updated document, including updated concurrency information</returns>
        /// <remarks>You can use AddUpdate instead, but under the covers this is a PUT to elasticsearch</remarks>
        IElasticClientResponse<T> Update<T>(T doc, string index) where T : SaltMinerEntity;
        /// <summary>
        /// Removes a document by ID
        /// </summary>
        /// <typeparam name="T">Type of the document is used to select the index</typeparam>
        /// <param name="id">Identifier of the document to remove</param>
        /// <param name="indexName">Specify the index to update</param>
        /// <returns>A response object containing a success flag and count of affected docs (hopefully only 1!)</returns>
        IElasticClientResponse<T> Delete<T>(string id, string indexName) where T : SaltMinerEntity;
        /// <summary>
        /// Removes multiple documents by their IDs
        /// </summary>
        /// <typeparam name="T">Type of the document is used to select the index</typeparam>
        /// <param name="ids">Identifiers of the documents to remove</param>
        /// <param name="indexName">Specify the index to update</param>
        /// <returns>A response object containing a success flag and count of affected docs</returns>
        IElasticClientResponse DeleteBulk<T>(IEnumerable<string> ids, string indexName) where T : SaltMinerEntity;
        /// <summary>
        /// Removes multiple documents by search parameters
        /// </summary>
        /// <typeparam name="T">Type of the document is used to select the index</typeparam>
        /// <param name="searchRequest">Search parameters (filters, etc.)</param>
        /// <param name="indexName">Specify the index to update</param>
        /// <param name="ignoreConflicts">If true, ignore conflicts in the data (throws error on conflict otherwise).</param>
        /// <param name="waitForCompletion">If false, throw the request over the wall and keep on truckin'; otherwise block until operation is complete.</param>
        /// <returns>A response object containing a success flag and count of affected docs</returns>
        /// <remarks>New and untested as of this build</remarks>
        IElasticClientResponse<T> DeleteByQuery<T>(SearchRequest searchRequest, string indexName, bool ignoreConflicts = false, bool waitForCompletion = true) where T : SaltMinerEntity;
        /// <summary>
        /// Searches the specified index based on the passed elasticsearch query
        /// </summary>
        /// <typeparam name="T">Selects the index and determines the result type</typeparam>
        /// <param name="query">Elasticsearch query body</param>
        /// <param name="indexName">Specify the index to query</param>
        /// <param name="pagingInfo">Pagination settings for the query</param>
        /// <seealso cref="IDataRepositoryPitPagingInfo"/>
        /// <returns>A response object containing the requested results and paging info for further results</returns>
        /// <remarks>Not yet clear if supports lucene, but probably does support DSL (json)</remarks>
        IElasticClientResponse<T> SearchByQuery<T>(string query, string indexName, List<object> afterKeys, PitPagingInfo pagingInfo) where T : SaltMinerEntity;
        /// <summary>
        /// Searches the specified index based on the passed elasticsearch query
        /// </summary>
        /// <typeparam name="T">Selects the index and determines the result type</typeparam>
        /// <param name="query">Elasticsearch query body</param>
        /// <param name="indexName">Specify the index to query</param>
        /// <param name="pagingInfo">Pagination settings for the query</param>
        /// <seealso cref="IDataRepositoryUIPagingInfo"/>
        /// <returns>A response object containing the requested results and paging info for further results</returns>
        /// <remarks>Not yet clear if supports lucene, but probably does support DSL (json)</remarks>
        IElasticClientResponse<T> SearchByQuery<T>(string query, string indexName, List<object> afterKeys, UIPagingInfo pagingInfo) where T : SaltMinerEntity;
        /// <summary>
        /// Updates the specified index based on the passed elasticsearch query
        /// </summary>
        /// <typeparam name="T">Selects the index and determines the result type</typeparam>
        /// <param name="query">Elasticsearch query body</param>
        /// <param name="indexName">Specify the index to query</param>
        /// <param name="updateScript">Specify the update script</param>
        /// <param name="wait">Whether to wait for update to complete before returning</param>
        /// <returns>A response object containing the requested results and paging info for further results</returns>
        /// <remarks>Not yet clear if supports lucene, but probably does support DSL (json)</remarks>
        IElasticClientResponse<T> UpdateByQuery<T>(string query, string indexName, string updateScript, bool wait = true) where T : SaltMinerEntity;
        /// <summary>
        /// Updates the specified index based on the passed elasticsearch query
        /// </summary>
        /// <typeparam name="T">Selects the index and determines the result type</typeparam>
        /// <param name="searchRequest">Update query request used to build the query</param>
        /// <param name="indexName">Specify the index to query</param>
        /// <param name="wait">Whether to wait for update to complete before returning</param>
        /// <returns>A response object containing the requested results and paging info for further results</returns>
        /// <remarks>Not yet clear if supports lucene, but probably does support DSL (json)</remarks>
        IElasticClientResponse<T> UpdateByQuery<T>(UpdateQueryRequest<T> searchRequest, string indexName, bool wait = true) where T : SaltMinerEntity;
        IElasticClientResponse<T> Search<T>(SearchRequest searchRequest, string indexName) where T : SaltMinerEntity;
        string SearchForJson(SearchRequest searchRequest, string indexName);
        IElasticClientResponse<ElasticClientCompositeAggregate> SearchWithCompositeAgg(IElasticClientRequestAggregation agg, SearchRequest searchRequest, string indexName);
        /// <summary>
        /// Returns a single document by ID
        /// </summary>
        /// <typeparam name="T">The type of document to return (and index to select if not specified)</typeparam>
        /// <param name="id">Identifier for the requested document</param>
        /// <param name="indexName">Specify the index</param>
        /// <returns>A response object containing the requested document, including concurrency information</returns>
        IElasticClientResponse<T> Get<T>(string id, string indexName) where T : SaltMinerEntity;
        /// <summary>
        /// Count for documents by search parameters
        /// </summary>
        /// <typeparam name="T">Type of the document is used to select the index</typeparam>
        /// <param name="searchParams">Dictionary of field, [values] to form into query filters (using AND for multiple)</param>
        /// <param name="indexName">Specify the index to search</param>
        /// <returns>A response object containing the count</returns>
        IElasticClientResponse<T> Count<T>(SearchRequest searchRequest, string indexName) where T : SaltMinerEntity;
        /// <summary>
        /// Registers the backup repository name
        /// </summary>
        /// <param name="backupRepoName">Specify name of the backup repository</param>
        /// <param name="backupLocation">Specify the backup file location</param>
        /// <returns>A response object with IsSuccessful set based on success</returns>
        IElasticClientResponse RegisterBackupRepository(string backupRepoName, string backupLocation);
        /// <summary>
        /// Deletes the backup repository name
        /// </summary>
        /// <param name="backupRepoName">Specify name of the backup repository</param>
        /// <returns>A response object with IsSuccessful set based on success</returns>
        IElasticClientResponse DeleteBackupRepository(string backupRepoName);
        /// <summary>
        /// Creates a complete backup of Elastic
        /// </summary>
        /// <param name="backupRepoName">Specify name of the backup repository</param>
        /// <param name="backupName">Specify name of the backup</param>
        /// <returns>A response object with IsSuccessful set based on success</returns>
        IElasticClientResponse CreateBackup(string backupRepoName, string backupName);
        /// <summary>
        /// Executes a Enrich Policy by name
        /// </summary>
        /// <param name="policyName">Specify name of the policy</param>
        /// <returns>A response object with IsSuccessful set based on success</returns>
        IElasticClientResponse ExecuteEnrichPolicy(string policyName);
        /// <summary>
        /// Restores a complete backup of Elastic
        /// </summary>
        /// <param name="backupRepoName">Specify name of the backup repository</param>
        /// <param name="backupName">Specify name of the backup</param>
        /// <returns>A response object with IsSuccessful set based on success</returns>
        IElasticClientResponse RestoreBackup(string backupRepoName, string backupName);
        /// <summary>
        /// Builds a single field aggregate (sum, max, avg, etc)
        /// </summary>
        /// <param name="name">Name of the aggregate</param>
        /// <param name="field">Field to aggregate</param>
        /// <param name="type">Aggregate type</param>
        /// <returns>The newly created single-field aggregate</returns>
        IElasticClientRequestAggregate BuildRequestAggregate(string name, string field, ElasticAggregateType type);
        /// <summary>
        /// Builds an aggregation suitable for querying with aggregates
        /// </summary>
        /// <seealso cref="SearchWithBucketAgg(IElasticClientRequestAggregation, string, Dictionary{string, string}, Dictionary{string, bool})"/>
        /// <param name="name">Name for the aggregation</param>
        /// <param name="bucketField">Field to use as buckets</param>
        /// <param name="aggregates">Collection of single-field aggregates to execute</param>
        /// <returns>The newly created aggregation</returns>
        /// <remarks>Currently this structure and the related SearchWithAgg() method only handle bucket type aggregation and only one level deep</remarks>
        IElasticClientRequestAggregation BuildRequestAggregation(string name, string bucketField, IEnumerable<IElasticClientRequestAggregate> aggregates);
        /// <summary>
        /// Gets list of bucket keys and counts for a given set of fields in an index
        /// </summary>
        /// <typeparam name="T">SaltMiner entity type needed to make generic stuff work, but not used for results currently</typeparam>
        /// <param name="sourceFields">List of fields in the index (or index pattern) to group by</param>
        /// <param name="aggregates">List of aggregates</param>
        /// <param name="indexName">Index or index pattern to query</param>
        /// <param name="pagingInfo">Pagination settings for the query - uses the ScrollKeys property only.</param>
        /// <seealso cref="IDataRepositoryPitPagingInfo"/>
        /// <returns>A list of key values (joined by | for multiple grouping fields) and counts of matching documents</returns>
        IElasticClientResponse<ElasticClientCompositeAggregate> GetCompositeAggregate<T>(SearchRequest searchRequest, IEnumerable<string> sourceFields, IEnumerable<IElasticClientRequestAggregate> aggregates, string indexName) where T : SaltMinerEntity;
        /// <summary>
        /// Creates or updates a security role
        /// </summary>
        /// <param name="roleName">Name of the role</param>
        /// <param name="role">The role to add</param>
        /// <returns>A response object that specifies if the role was created or updated</returns>
        IElasticClientResponse UpsertRole(string roleName, string role);
        /// <summary>
        /// Deletes a security role - caution!
        /// </summary>
        /// <param name="roleName">Name of the role to remove</param>
        /// <returns>A response object that indicates if the role was removed or not.</returns>
        IElasticClientResponse DeleteRole(string roleName);
        /// <summary>
        /// Checks to see if a security role exists by name
        /// </summary>
        /// <param name="roleName">Name of the role to find</param>
        /// <returns>A response object that indicates if the role was found or not.</returns>
        IElasticClientResponse RoleExists(string roleName);
        /// <summary>
        /// Creates an enrichment policy
        /// </summary>
        /// <param name="enrichmentName">Name of the enrichment</param>
        /// <param name="indexName">Name of the index that gets the enrichment policy</param>
        /// <param name="enrichment">The enrichment to add</param>
        /// <returns>A response object that specifies if the enrichment was created</returns>
        IElasticClientResponse CreateEnrichment(string enrichmentName, string indexName, string enrichment);
        /// <summary>
        /// Creates an ingest pipeline
        /// </summary>
        /// <param name="pipelineName">Name of the ingest pipeline</param>
        /// <param name="pipeline">The ingest pipeline to add</param>
        /// <returns>A response object that specifies if the ingest pipeline was created</returns>
        IElasticClientResponse CreateIngestPipeline(string pipelineName, string pipeline);
    }
}
