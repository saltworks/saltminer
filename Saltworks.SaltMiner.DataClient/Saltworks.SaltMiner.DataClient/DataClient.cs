using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.Utility.ApiHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.DataClient
{
    public class DataClient : IDisposable
    {
        public ApiClient ApiClient { get; }
        private ILogger Logger { get; }
        public DataClientConfig RunConfig { get; }

        #region Ctor

        internal DataClient(ApiClient client, ILogger logger, DataClientConfig runConfig)
        {
            Logger = logger;
            ApiClient = client;
            RunConfig = runConfig;

            try
            {
                Logger.LogDebug("DataClient initialization starting");
                if (!RunConfig.DisableInitialConnection)
                {
                    var result = ApiClient.Get<NoDataResponse>("register/role");
                    if (!result.IsSuccessStatusCode)
                        throw new DataClientInitializationException($"Data client failed to initialize: {result.HttpResponse.ReasonPhrase}");
                    var response = result.Content;
                    if (response.Success)
                        Logger.LogInformation("DataClient init: {Msg} role", response.Message);
                    if (!response.Success)
                        throw new DataClientInitializationException($"Invalid API key in configuration (message: '{response.Message}')");
                }
            }
            catch (ApiClientException ex)
            {
                throw new DataClientInitializationException($"ApiClient error during initialization: {ex.Message}. ApiClient response content: {ex.ResponseContent}", ex);
            }
            catch (TaskCanceledException ex)
            {
                var msg = ex.Message;

                if (ex.InnerException != null)
                {
                    msg = ex.InnerException.Message;
                }

                throw new DataClientInitializationException($"Data client failed to initialize due to a timeout: {msg}", ex);
            }
            catch (DataClientInitializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DataClientInitializationException($"Data client failed to initialize: {ex.Message}", ex);
            }

            Logger.LogDebug("DataClient initialization complete");
        }

        #endregion

        #region Scan

        /// <summary>
        /// Updates Scan using update by query
        /// </summary>
        /// <param name="request">reqeust used in updates</param>
        /// <returns>true if successful</returns>
        public BulkResponse ScanUpdateByQuery(UpdateQueryRequest<Scan> request)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("scan/bulk/query", request)).Content;
        }

        /// <summary>
        /// Gets a scan document by ID
        /// </summary>
        /// <param name="id">Identifier for the target entity</param>
        /// <param name="assetType">AssetType for the target entity</param>
        /// <param name="sourceType">SourceType for the target entity</param>
        /// <param name="instance">instance for the target entity</param>
        /// <returns>The requested scan in a container</returns>
        public DataItemResponse<Scan> ScanGet(string id, string assetType, string sourceType, string instance)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));
            return CheckRetry(() => ApiClient.Get<DataItemResponse<Scan>>($"scan/{id}/{assetType}/{sourceType}/{instance}")).Content;
        }

        /// <summary>
        /// Gets a Scan document by Engagement ID
        /// </summary>
        /// <param name="id">The identifer of the target entity</param>
        /// <returns>Response container including the result</returns>
        public DataItemResponse<Scan> ScanGetByEngagement(string id)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<Scan>>($"scan/engagement/{id}")).Content;
        }

        /// <summary>
        /// Search for scans
        /// </summary>
        /// <param name="searchRequest">Search criteria</param>
        /// <returns>A batch of results and paging (scroll) information</returns>
        public DataResponse<Scan> ScanSearch(SearchRequest searchRequest)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<Scan>>("scan/search", searchRequest), true).Content;
        }

        /// <summary>
        /// Add/Update a scan document
        /// </summary>
        /// <param name="scan">The entity to add or update</param>
        /// <returns>The updated entity</returns>
        public DataItemResponse<Scan> ScanAddUpdate(Scan scan)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<Scan>>("scan", new DataItemRequest<Scan> { Entity = scan, Id = scan.Id })).Content;
        }

        /// <summary>
        /// Deletes a scan by ID
        /// </summary>
        /// <param name="id">The ID of the target entity</param>
        /// <param name="assetType">The AssetType of the target entity</param>
        /// <param name="sourceType">The Source Type of the target entity</param>
        /// <param name="instance">The Instance of the target entity</param>
        /// <returns>Success NoDataResponse with success flag</returns>
        public NoDataResponse ScanDelete(string id, string assetType, string sourceType, string instance)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"scan/{id}/{assetType}/{sourceType}/{instance}")).Content;
        }

        /// <summary>
        /// Count of scans for a given asset ID
        /// </summary>
        /// <param name="assetId">Target asset ID</param>
        /// <returns>NoDataResponse with Count of the scans</returns>
        public NoDataResponse ScansCountByAssetId(string assetId)
        {
            return CheckRetry(() => ApiClient.Get<NoDataResponse>($"scan/count/asset/{assetId}")).Content;
        }

        /// <summary>
        /// Count of scans for a given asset inventory key
        /// </summary>
        /// <param name="inventoryKey">Target asset inventory key</param>
        /// <returns>NoDataResponse with Count of the scans</returns>
        public NoDataResponse ScansCountByInventoryAssetKey(string inventoryKey)
        {
            return CheckRetry(() => ApiClient.Get<NoDataResponse>($"scan/count/assetinv/{inventoryKey}")).Content;
        }

        #endregion

        #region QueueScan

        /// <summary>
        /// Add/update QueueScan
        /// </summary>
        /// <param name="queueScan">new or updated entity</param>
        /// <returns>The entity as updated from the datasource</returns>
        public DataItemResponse<QueueScan> QueueScanAddUpdate(QueueScan queueScan)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<QueueScan>>("queuescan", new DataItemRequest<QueueScan> { Entity = queueScan })).Content;
        }

        /// <summary>
        /// Add/update queue documents (scans/assets/issues) in bulk.
        /// </summary>
        /// <param name="queueDocs">Queue data documents - any other type will throw an error.</param>
        /// <remarks>Only minimal validation performed with this method (no verification of queue scan ID for example).</remarks>
        public BulkResponse QueueAddUpdateBulk(IEnumerable<SaltMinerEntity> queueDocs)
        {
            try
            {
                return QueueAddUpdateBulkAsync(queueDocs).Result;
            }
            catch (AggregateException ex)
            {
                throw GetFirstFromAggregateException(ex);
            }
        }

        /// <summary>
        /// Add/update queue documents (scans/assets/issues) in bulk.
        /// </summary>
        /// <param name="queueDocs">Queue data documents - any other type will throw an error.</param>
        /// <remarks>Only minimal validation performed with this method (no verification of queue scan ID for example).</remarks>
        public async Task<BulkResponse> QueueAddUpdateBulkAsync(IEnumerable<SaltMinerEntity> queueDocs)
        {
            if (queueDocs.Any(q => q is not QueueScan && q is not QueueAsset && q is not QueueIssue))
                throw new DataClientException("One or more queue documents passed are of the wrong type");
            var rsp =  await CheckRetryAsync<BulkResponse>(async () =>
            {
                var req = new QueueDataRequest()
                {
                    QueueScans = queueDocs.OfType<QueueScan>(),
                    QueueAssets = queueDocs.OfType<QueueAsset>(),
                    QueueIssues = queueDocs.OfType<QueueIssue>()
                };
                return (await ApiClient.PostAsync<BulkResponse>("queuescan/bulkqueue", req));
            });
            return rsp.Content;
        }

        /// <summary>
        /// Add/Update queue scans
        /// </summary>
        /// <param name="queueScans">Queue scans to add</param>
        /// <returns>BulkResponse with Count of successful inserts</returns>
        public BulkResponse QueueScanAddUpdateBulk(IEnumerable<QueueScan> queueScans)
        {
            try
            {
                return QueueScanAddUpdateBulkAsync(queueScans).Result;
            }
            catch (AggregateException ex)
            {
                throw GetFirstFromAggregateException(ex);
            }
        }
        
        /// <summary>
        /// Add/Update queue scans asynchronously
        /// </summary>
        /// <param name="queueScans">Queue scans to add</param>
        /// <returns>BulkResponse with Count of successful inserts</returns>
        public async Task<BulkResponse> QueueScanAddUpdateBulkAsync(IEnumerable<QueueScan> queueScans)
        {
            var rsp = await CheckRetryAsync<BulkResponse>(async() =>
            {
                return await ApiClient.PostAsync<BulkResponse>("queuescan/bulk", new DataRequest<QueueScan> { Documents = queueScans });
            });
            return rsp.Content;
        }

        /// <summary>
        /// Find QueueScan documents by criteria
        /// </summary>
        /// <param name="searchRequest">Search request parameters</param>
        /// <returns>Response container including results and pagination info (if needed)</returns>
        public DataResponse<QueueScan> QueueScanSearch(SearchRequest searchRequest)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<QueueScan>>($"queuescan/search", searchRequest), true).Content;
        }

        /// <summary>
        /// Gets a QueueScan document by ID
        /// </summary>
        /// <param name="id">The identifer of the target entity</param>
        /// <returns>Response container including the result</returns>
        public DataItemResponse<QueueScan> QueueScanGet(string id)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<QueueScan>>($"queuescan/{id}")).Content;
        }

        /// <summary>
        /// Gets a QueueScan document by Engagement ID
        /// </summary>
        /// <param name="id">The identifer of the target entity</param>
        /// <returns>Response container including the result</returns>
        public DataItemResponse<QueueScan> QueueScanGetByEngagement(string id)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<QueueScan>>($"queuescan/engagement/{id}")).Content;
        }

        /// <summary>
        /// Updates status of a QueueScan document using locking to ensure consistency
        /// </summary>
        /// <param name="id">Identity of entity to update</param>
        /// <param name="newStatus">New status to set</param>
        /// <param name="lockId">If included, lock this queue scan to the indicated lock ID</param>
        /// <returns>true if successful</returns>
        public NoDataResponse QueueScanUpdateStatus(string id, QueueScan.QueueScanStatus newStatus, string lockId = "")
        {
            try
            {
                return QueueScanUpdateStatusAsync(id, newStatus, lockId).Result;
            }
            catch (AggregateException ex)
            {
                throw GetFirstFromAggregateException(ex);
            }
        }

        /// <summary>
        /// Updates status of a QueueScan document using locking to ensure consistency - asynchronously
        /// </summary>
        /// <param name="id">Identity of entity to update</param>
        /// <param name="newStatus">New status to set</param>
        /// <param name="lockId">If included, lock this queue scan to the indicated lock ID</param>
        /// <returns>true if successful</returns>
        public async Task<NoDataResponse> QueueScanUpdateStatusAsync(string id, QueueScan.QueueScanStatus newStatus, string lockId = "")
        {
            var qry = $"queuescan/status/{id}/{newStatus:g}";
            if (!string.IsNullOrEmpty(lockId))
                qry = $"{qry}?lockid={lockId}";
            var rsp = await CheckRetryAsync(async () =>
            {
                return await ApiClient.GetAsync<NoDataResponse>(qry);
            });
            return rsp.Content;
        } 

        /// <summary>
        /// Updates QueueScans using update by query
        /// </summary>
        /// <param name="request">reqeust used in updates</param>
        /// <returns>true if successful</returns>
        public BulkResponse QueueScanUpdateByQuery(UpdateQueryRequest<QueueScan> request)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("queuescan/bulk/query", request)).Content;
        }

        /// <summary>
        /// Deletes a QueueScan document by ID
        /// </summary>
        /// <param name="id">The identifier of the target entity</param>
        /// <returns>Boolean value indicating success</returns>
        public NoDataResponse QueueScanDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"queuescan/{id}")).Content;
        }

        /// <summary>
        /// Deletes a QueueScan document by ID and  all Assets and Issues associated.
        /// </summary>
        /// <param name="queueScanId">The identifier of the target entity</param>
        /// <returns>Boolean value indicating success</returns>
        public NoDataResponse QueueScanDeleteAll(string queueScanId)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"queuescan/all/{queueScanId}")).Content;
        }

        /// <summary>
        /// Deletes QueueScans/Assets/Issues by search request.
        /// </summary>
        /// <returns>Boolean value indicating success</returns>
        public NoDataResponse QueueScanDeleteAll(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<NoDataResponse>($"queuescan/all/delete", search)).Content;
        }

        #endregion

        #region QueueLog

        /// <summary>
        /// Updates QueueLog using update by query
        /// </summary>
        /// <param name="request">reqeust used in updates</param>
        /// <returns>true if successful</returns>
        public BulkResponse QueueLogUpdateByQuery(UpdateQueryRequest<QueueLog> request)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("queuelog/bulk/query", request)).Content;
        }

        /// <summary>
        /// Gets a queue log entry by ID
        /// </summary>
        /// <param name="id">Identifier for the target entity</param>
        /// <returns>The requested entity</returns>
        public DataItemResponse<QueueLog> QueueLogGet(string id)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<QueueLog>>($"queuelog/{id}")).Content;
        }

        /// <summary>
        /// Search for QueueLog documents matching passed criteria
        /// </summary>
        /// <param name="search">Filter(s) to use for the search</param>
        /// <returns>Response object containing the results</returns>
        public DataResponse<QueueLog> QueueLogSearch(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<QueueLog>>($"queuelog/search", search), true).Content;
        }

        /// <summary>
        /// Adds/Updates a QueueLog entity
        /// </summary>
        /// <param name="log">The entity to update</param>
        /// <returns>The updated entity</returns>
        public DataItemResponse<QueueLog> QueueLogAddUpdate(QueueLog log)
        {
            return ApiClient.Post<DataItemResponse<QueueLog>>("queuelog", new DataItemRequest<QueueLog> { Entity = log, Id = log.Id }).Content;
        }

        /// <summary>
        /// Deletes a QueueLog entity
        /// </summary>
        /// <param name="id">The target entity ID</param>
        /// <returns>true if successful</returns>
        public NoDataResponse QueueLogDelete(string id)
        {
            return ApiClient.Delete<NoDataResponse>($"queuelog/{id}").Content;
        }

        /// <summary>
        /// Returns unread QueueLog messages, marking them read
        /// </summary>
        /// <returns>The requested messages</returns>
        public DataResponse<QueueLog> QueueLogRead()
        {
            return ApiClient.Post<DataResponse<QueueLog>>($"queuelog/read", null).Content;
        }

        #endregion

        #region QueueIssue

        /// <summary>
        /// Updates QueueIssues using update by query
        /// </summary>
        /// <param name="request">reqeust used in updates</param>
        /// <returns>true if successful</returns>
        public BulkResponse QueueIssueUpdateByQuery(UpdateQueryRequest<QueueIssue> request)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("queueissue/bulk/query", request)).Content;
        }

        /// <summary>
        /// Returns all queue issues (in batches) for a given asset inventory key
        /// </summary>
        /// <param name="InventoryAssetKey">Asset Inventory Key identifier</param>
        /// <param name="request">SearchRequest</param>
        /// <returns></returns>
        public DataResponse<QueueIssue> QueueIssuesGetByInventoryAssetKey(string InventoryAssetKey, SearchRequest request)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<QueueIssue>>($"queueissue/asset-inventory/{InventoryAssetKey}", request), true).Content;
        }

        /// <summary>
        /// Returns queue issue for a given queue issue ID
        /// </summary>
        /// <param name="queueIssueId">QueueIssue identifier</param>
        /// <returns></returns>
        public DataItemResponse<QueueIssue> QueueIssueGet(string queueIssueId)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<QueueIssue>>($"queueissue/{queueIssueId}")).Content;
        }

        /// <summary>
        /// Add/Update batch of issues
        /// </summary>
        /// <param name="batch">Issues to add</param>
        /// <returns>BulkResponse with Count of successful inserts</returns>
        /// <remarks>All ScanIds must match for this operation</remarks>
        public BulkResponse QueueIssuesAddUpdateBulk(IEnumerable<QueueIssue> batch)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("queueissue/bulk", new DataRequest<QueueIssue> { Documents = batch })).Content;
        }

        /// <summary>
        /// Add/Update issue
        /// </summary>
        /// <param name="queueIssue">Issue to add or update</param>
        /// <returns>DataItemResponse[QueueIssue] with queue issue</returns>
        public DataItemResponse<QueueIssue> QueueIssueAddUpdate(QueueIssue queueIssue)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<QueueIssue>>("queueissue", new DataItemRequest<QueueIssue> { Entity = queueIssue })).Content;
        }

        /// <summary>
        /// Deletes a queue issue by ID
        /// </summary>
        /// <param name="id">Identifier for the target entity</param>
        /// <returns>NoDataRespons with boolean indicating success</returns>
        public NoDataResponse QueueIssueDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"queueissue/{id}")).Content;
        }

        /// <summary>
        /// Count of queue issues for a given queue scan ID
        /// </summary>
        /// <param name="queueScanId">Target queue scan ID</param>
        /// <returns>NoDataResponse with Count of the issues</returns>
        public NoDataResponse QueueIssueCountByScan(string queueScanId)
        {
            return CheckRetry(() => ApiClient.Get<NoDataResponse>($"queueissue/queuescan/{queueScanId}/count")).Content;
        }

        /// <summary>
        /// Searches for QueueIssues by filter(s)
        /// </summary>
        /// <param name="search">The filter request that defines the search parameters</param>
        /// <returns>The first set of results and scrolling/paging information</returns>
        /// <remarks>Pass in both request details and paging info to get the next set of results</remarks>
        public DataResponse<QueueIssue> QueueIssueSearch(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<QueueIssue>>($"queueissue/search", search), true).Content;
        }

        /// <summary>
        /// Get queue issue and Lock
        /// </summary>
        /// <param name="queueIssueId">QueueIssue Identifier</param>
        /// <param name="userName">User to be locked for</param>
        /// <returns>Object representing request doc and lock it</returns>
        public DataItemResponse<QueueIssue> QueueIssueGetAndLock(string queueIssueId, string userName)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<QueueIssue>>($"queueissue/{queueIssueId}/lock/{userName}")).Content;
        }
        /// <summary>
        /// Refresh queue issue lock
        /// </summary>
        /// <param name="queueIssueId">QueueIssue Identifier</param>
        /// <param name="userName">User to be locked for</param>
        /// <returns>NoDataResponse with success flag</returns>
        public NoDataResponse QueueIssueLockRefresh(string queueIssueId, string userName)
        {
            return CheckRetry(() => ApiClient.Get<NoDataResponse>($"queueissue/{queueIssueId}/lock/{userName}/refresh")).Content;
        }

        #endregion

        #region QueueAsset

        /// <summary>
        /// Returns queue asset for a given source type and source Id
        /// </summary>
        /// <param name="sourceType">Source Type</param>
        /// <param name="sourceId">Source Id</param>
        /// <returns></returns>
        public DataItemResponse<QueueAsset> QueueAssetGetBySourceType(string sourceType, string sourceId)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<QueueAsset>>($"queueasset/{sourceType}/{sourceId}")).Content;
        }

        /// <summary>
        /// Add/Update queue asset
        /// </summary>
        /// <param name="asset">Queue asset to add or update</param>
        /// <returns>The new/updated queue asset</returns>
        public DataItemResponse<QueueAsset> QueueAssetAddUpdate(QueueAsset asset)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<QueueAsset>>("queueasset", new DataItemRequest<QueueAsset> { Entity = asset })).Content;
        }

        /// <summary>
        /// Updates QueueAssets using update by query
        /// </summary>
        /// <param name="request">reqeust used in updates</param>
        /// <returns>true if successful</returns>
        public BulkResponse QueueAssetUpdateByQuery(UpdateQueryRequest<QueueAsset> request)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("queueasset/bulk/query", request)).Content;
        }

        /// <summary>
        /// Add/Update queue assets
        /// </summary>
        /// <param name="assets">Queue assets to add</param>
        /// <returns>BulkResponse with Count of successful inserts</returns>
        /// <remarks>ScanIds do not need to match for this operation</remarks>
        public BulkResponse QueueAssetAddUpdateBulk(IEnumerable<QueueAsset> assets)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("queueasset/bulk", new DataRequest<QueueAsset> { Documents = assets })).Content;
        }

        /// <summary>
        /// Deletes a QueueAsset document by ID
        /// </summary>
        /// <param name="id">The identifier of the target entity</param>
        /// <returns>NoDataResponse with boolean value indicating success</returns>
        public NoDataResponse QueueAssetDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"queueasset/{id}")).Content;
        }

        /// <summary>
        /// Gets an QueueAsset by ID, Type, and Source Type
        /// </summary>
        /// <param name="id">The ID to get</param>
        /// <returns>The requested item, along with concurrency info</returns>
        public DataItemResponse<QueueAsset> QueueAssetGet(string id)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<QueueAsset>>($"queueasset/{id}")).Content;
        }

        /// <summary>
        /// Searches for Assets by filter(s)
        /// </summary>
        /// <param name="search">The filter request that defines the search parameters</param>
        /// <returns>The first set of results and scrolling/paging information</returns>
        /// <remarks>Pass in both request details and paging info to get the next set of results</remarks>
        public DataResponse<QueueAsset> QueueAssetSearch(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<QueueAsset>>($"queueasset/search", search), true).Content;
        }

        #endregion

        #region Issue

        /// <summary>
        /// Updates Issues using update by query
        /// </summary>
        /// <param name="request">reqeust used in updates</param>
        /// <returns>true if successful</returns>
        public BulkResponse IssueUpdateByQuery(UpdateQueryRequest<Issue> request)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("issue/bulk/query", request)).Content;
        }

        /// <summary>
        /// Returns all issues (in batches) for a given scan ID
        /// </summary>
        /// <param name="InventoryAssetKey">Asset Inventory Key identifier</param>
        /// <param name="request">SearchRequest</param>
        /// <returns></returns>
        public DataResponse<Issue> IssuesGetByInventoryAssetKey(string InventoryAssetKey, SearchRequest request)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<Issue>>($"issue/asset-inventory/{InventoryAssetKey}", request), true).Content;
        }

        /// <summary>
        /// Search for issues by filtering
        /// </summary>
        /// <param name="search">The search request that defines the filters to use</param>
        /// <returns>Search results and paging info if appropriate</returns>
        public DataResponse<Issue> IssueSearch(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<Issue>>($"issue/search", search), true).Content;
        }

        /// <summary>
        /// Delete an issue by ID
        /// </summary>
        /// <param name="id">The ID of the target entity</param>
        /// <param name="assetType">The AssetType of the target entity</param>
        /// <param name="sourceType">The Source Type of the target entity</param>
        /// <param name="instance">The Instance of the target entity</param>
        /// <returns>true if successful</returns>
        public NoDataResponse IssueDelete(string id, string assetType, string sourceType, string instance)
        {
            return ApiClient.Delete<NoDataResponse>($"issue/{id}/{assetType}/{sourceType}/{instance}").Content;
        }

        /// <summary>
        /// Deletes issues by scanID
        /// </summary>
        /// <param name="scanId">The scan ID of the target entity</param>
        /// <param name="assetType">The AssetType of the target entity</param>
        /// <param name="sourceType">The Source Type of the target entity</param>
        /// <param name="instance">The Instance of the target entity</param>
        /// <returns>A NoDataResponse with count of the affected records</returns>
        public NoDataResponse IssuesDeleteByScan(string scanId, string assetType, string sourceType, string instance)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"issue/scan/{scanId}/{assetType}/{sourceType}/{instance}")).Content;
        }

        /// <summary>
        /// Adds/Updates multiple issues
        /// </summary>
        /// <param name="batch">Issue batch to add</param>
        /// <returns>BulkResponse with Count of documents affected</returns>
        public BulkResponse IssuesAddUpdateBulk(IEnumerable<Issue> batch)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("issue/bulk", new DataRequest<Issue> { Documents = batch })).Content;
        }

        /// <summary>
        /// Get issue
        /// </summary>
        /// <param name="issueId">Issue Identifier</param>
        /// <param name="assetType">Asset Type of issue</param>
        /// <param name="sourceType">Source Type of issue</param>
        /// <param name="instance">Instance of issue</param>
        /// <returns>Object representing request doc</returns>
        public DataItemResponse<Issue> IssueGet(string issueId, string assetType, string sourceType, string instance)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<Issue>>($"issue/{issueId}/{assetType}/{sourceType}/{instance}")).Content;
        }

        /// <summary>
        /// Deletes issues by Source Id
        /// </summary>
        /// <param name="sourceId">The Source ID of the target entity</param>
        /// <param name="assetType">The AssetType of the target entity</param>
        /// <param name="sourceType">The Source Type of the target entity</param>
        /// <param name="instance">The Instance of the target entity</param>
        /// <param name="assessmentType">The assessment type of the target entity</param>
        /// <returns>A NoDataResponse with count of the affected records</returns>
        public NoDataResponse IssuesDeleteBySourceId(string sourceId, string assetType, string sourceType, string instance, string assessmentType)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"issue/all/{sourceId}/{assetType}/{sourceType}/{instance}/{assessmentType}")).Content;
        }

        /// <summary>
        /// Deletes issues by Engagement Id, asset type, source type, instance and all associated scans and assets of the engagement
        /// </summary>
        /// <param name="engagementId">The Engagement ID of the target entity</param>
        /// <param name="assetType">The AssetType of the target entity</param>
        /// <param name="sourceType">The Source Type of the target entity</param>
        /// <param name="instance">The Instance of the target entity</param>
        /// <returns>A NoDataResponse with count of the affected records</returns>
        public NoDataResponse IssuesDeleteAllByEngagementId(string engagementId, string assetType, string sourceType, string instance)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"issue/engagement/{engagementId}/{assetType}/{sourceType}/{instance}")).Content;
        }


        #endregion

        #region Asset

        /// <summary>
        /// Updates Assets using update by query
        /// </summary>
        /// <param name="request">reqeust used in updates</param>
        /// <returns>true if successful</returns>
        public BulkResponse AssetUpdateByQuery(UpdateQueryRequest<Asset> request)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("asset/bulk/query", request)).Content;
        }

        /// <summary>
        /// Add/update an Asset
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>The updated entity, along with concurrency info</returns>
        public DataItemResponse<Asset> AssetAddUpdate(Asset entity)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<Asset>>("asset", new DataItemRequest<Asset> { Entity = entity, Id = entity.Id })).Content;
        }

        /// <summary>
        /// Gets an Asset by ID, Type, and Source Type
        /// </summary>
        /// <param name="id">The ID to get</param>
        /// <param name="assetType">The AssetType to get</param>
        /// <param name="sourceType">The Source Type to get</param>
        /// <param name="instance">The Instance to get</param>
        /// <returns>The requested item, along with concurrency info</returns>
        public DataItemResponse<Asset> AssetGet(string id, string assetType, string sourceType, string instance)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<Asset>>($"asset/{id}/{assetType}/{sourceType}/{instance}")).Content;
        }

        /// <summary>
        /// Searches for Assets by filter(s)
        /// </summary>
        /// <param name="search">The filter request that defines the search parameters</param>
        /// <returns>The first set of results and scrolling/paging information</returns>
        /// <remarks>Pass in both request details and paging info to get the next set of results</remarks>
        public DataResponse<Asset> AssetSearch(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<Asset>>($"asset/search", search), true).Content;
        }

        /// <summary>
        /// Deletes an Asset by ID, Type, and Source Type
        /// </summary>
        /// <param name="id">The ID to DELETE</param>
        /// <param name="assetType">The AssetType to DELETE</param>
        /// <param name="sourceType">The Source Type to DELETE</param>
        /// <param name="instance">The Instance to DELETE</param>
        /// <returns>true if successful</returns>
        public NoDataResponse AssetDelete(string id, string assetType, string sourceType, string instance)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"asset/{id}/{assetType}/{sourceType}/{instance}")).Content;
        }

        /// <summary>
        /// Asset Count By InventoryAssetKey
        /// </summary>
        /// <param name="InventoryAssetKey">The InventoryAssetKey to count on</param>
        /// <returns>The count of assets</returns>
        public NoDataResponse AssetCountByInventoryAssetKey(string InventoryAssetKey)
        {
            return CheckRetry(() => ApiClient.Get<NoDataResponse>($"asset/count/{InventoryAssetKey}")).Content;
        }

        /// <summary>
        /// Deletesan Asset by Source ID, Source Type, and Instance
        /// </summary>
        /// <param name="sourceId">The identifier of the target entity</param>
        /// <param name="sourceType">The source type of the target entity</param>
        /// <param name="instance">The instance of the target entity</param>
        /// <returns>Boolean value indicating success</returns>
        public NoDataResponse AssetDeleteAll(string sourceId, string sourceType, string instance)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"asset/all/{sourceId}/{sourceType}/{instance}")).Content;
        }

        #endregion

        #region Snapshot

        /// <summary>
        /// Updates AssetSnapshots using update by query
        /// </summary>
        /// <param name="request">reqeust used in updates</param>
        /// <returns>true if successful</returns>
        public BulkResponse SnapshotUpdateByQuery(UpdateQueryRequest<Snapshot> request)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("snapshot/bulk/query", request)).Content;
        }

        /// <summary>
        /// Add/update snapshot entity
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="isDaily">Is this a daily snapshot (else monthly)?</param>
        /// <returns>The updated entity in a DTO</returns>
        public DataItemResponse<Snapshot> SnapshotAddUpdate(Snapshot entity, bool isDaily = false)
        {
            var url = $"snapshot/monthly";
            if (isDaily)
            {
                url = $"snapshot/daily";
            }
            return CheckRetry(() => ApiClient.Post<DataItemResponse<Snapshot>>(url, new DataItemRequest<Snapshot> { Entity = entity, Id = entity.Id })).Content;
        }

        /// <summary>
        /// Add/Update snapshot batch
        /// </summary>
        /// <param name="entities">The entities to add</param>
        /// <param name="isDaily">Is this a daily snapshot (else monthly)?</param>
        /// <returns>BulkResponse with Number of inserted records</returns>
        public BulkResponse SnapshotAddUpdateBatch(List<Snapshot> entities, bool isDaily = false)
        {
            var url = $"snapshot/monthly/bulk";
            if (isDaily)
            {
                url = $"snapshot/daily/bulk";
            }
            return CheckRetry(() => ApiClient.Post<BulkResponse>(url, new DataRequest<Snapshot> { Documents = entities })).Content;
        }

        /// <summary>
        /// Return snapshots via search
        /// </summary>
        /// <param name="searchRequest">Search criteria for the snapshots to return</param>
        /// <returns>Requested snapshots</returns>
        public DataResponse<Snapshot> SnapshotSearch(SearchRequest searchRequest)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<Snapshot>>("snapshot/search", searchRequest)).Content;
        }

        /// <summary>
        /// Delete snapshots by search criteria
        /// </summary>
        /// <param name="searchRequest">Criteria used to select snapshot(s) to delete</param>
        /// <returns>NoDataResponse with Number of affected snapshots</returns>
        public NoDataResponse SnapshotDelete(SearchRequest searchRequest)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"snapshot", searchRequest), true).Content;
        }

        /// <summary>
        /// Returns count of issues grouped by asset type, source, source ID, and vulnerability name
        /// </summary>
        /// <param name="searchRequest">Request supports PagingInfo.Size, PagingInfo.AfterKeys, and FilterMatches</param>
        /// <returns>A dictionary response of type string, double that represents the key values (joined with |) and counts</returns>
        public DataDictionaryResponse<string, Dictionary<string, double?>> SnapshotCounts(SearchRequest searchRequest)
        {
            return CheckRetry(() => ApiClient.Post<DataDictionaryResponse<string, Dictionary<string, double?>>>($"snapshot/counts", searchRequest), true).Content;
        }

        #endregion

        #region InventoryAsset

        /// <summary>
        /// Updates InventoryAssets using update by query
        /// </summary>
        /// <param name="request">reqeust used in updates</param>
        /// <returns>true if successful</returns>
        public BulkResponse InventoryAssetsUpdateByQuery(UpdateQueryRequest<InventoryAsset> request)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("inventoryasset/bulk/query", request)).Content;
        }

        /// <summary>
        /// Add/update an InventoryAsset document
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>The updated entity, along with concurrency info</returns>
        public DataItemResponse<InventoryAsset> InventoryAssetAddUpdate(InventoryAsset entity)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<InventoryAsset>>("inventoryasset", new DataItemRequest<InventoryAsset> { Entity = entity, Id = entity.Id })).Content;
        }

        /// <summary>
        /// Gets an InventoryAsset document by ID
        /// </summary>
        /// <param name="id">The ID to get</param>
        /// <returns>The requested item, along with concurrency info</returns>
        public DataItemResponse<InventoryAsset> InventoryAssetGet(string id)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<InventoryAsset>>($"inventoryasset/{id}")).Content;
        }

        /// <summary>
        /// Gets an InventoryAsset document by InventoryKey
        /// </summary>
        /// <param name="key">The Inventory Key to get</param>
        /// <returns>The requested item, along with concurrency info</returns>
        public DataItemResponse<InventoryAsset> InventoryAssetGetByKey(string key)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<InventoryAsset>>($"inventoryasset/key/{key}")).Content;
        }

        /// <summary>
        /// Searches for InventoryAsset documents by filter(s)
        /// </summary>
        /// <param name="search">The filter request that defines the search parameters</param>
        /// <returns>The first set of results and scrolling/paging information</returns>
        /// <remarks>Pass in request details and paging info to get the next set of results</remarks>
        public DataResponse<InventoryAsset> InventoryAssetSearch(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<InventoryAsset>>($"inventoryasset/search", search), true).Content;
        }

        /// <summary>
        /// Deletes an InventoryAsset document by ID
        /// </summary>
        /// <param name="id">The ID to delete</param>
        /// <returns>true if successful</returns>
        public NoDataResponse InventoryAssetDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"inventoryasset/{id}")).Content;
        }

        /// <summary>
        /// Refresh All InventoryAsset documents marked 'IsDirty'
        /// </summary>
        /// <returns>true if successful</returns>
        public NoDataResponse InventoryAssetRefresh(string sourceType)
        {
            return CheckRetry(() => ApiClient.Post<NoDataResponse>($"inventoryasset/refresh/{sourceType}", null)).Content;
        }

        /// <summary>
        /// Updates an InventoryAsset document by to 'IsDirty'
        /// </summary>
        /// <param name="entity">The entity to update dirty</param>
        /// <returns>true if successful</returns>
        public DataItemResponse<InventoryAsset> InventoryAssetDirtyUpdate(InventoryAsset entity)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<InventoryAsset>>($"inventoryasset/dirty", new DataItemRequest<InventoryAsset> { Entity = entity, Id = entity.Id })).Content;
        }

        #endregion

        #region Index

        public NoDataResponse DeleteIndex(string indexName)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"index/{indexName}")).Content;
        }

        public NoDataResponse RefreshIndex(string indexName)
        {
            return CheckRetry(() => ApiClient.Post<NoDataResponse>($"index/refresh/{indexName}", null)).Content;
        }

        public NoDataResponse CheckForIndex(string indexName)
        {
            return CheckRetry(() => ApiClient.Post<NoDataResponse>($"index/exist/{indexName}", null)).Content;
        }

        public NoDataResponse ActiveIssueAlias(string indexName)
        {
            return CheckRetry(() => ApiClient.Post<NoDataResponse>($"index/alias/active-issue/{indexName}", null)).Content;
        }

        #endregion

        #region Licensing

        public NoDataResponse AddLicense(License license)
        {
            return CheckRetry(() => ApiClient.Post<NoDataResponse>($"license", new DataItemRequest<License> { Entity = license })).Content;
        }

        public DataItemResponse<License> GetLicense()
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<License>>($"license")).Content;
        }

        /// <summary>
        /// Returns Elasticsearch license type (basic, trial, enterprise)
        /// </summary>
        public NoDataResponse GetElasticLicenseType() => CheckRetry(() => ApiClient.Get<NoDataResponse>("license/elk")).Content;

        public NoDataResponse DeleteLicense()
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"license")).Content;
        }

        public DataItemResponse<Dictionary<string, int>> GetValidationCounts(string assetType, string sourceType, string instance, string assessmentType)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<Dictionary<string, int>>>($"license/counts/{assetType}/{sourceType}/{instance}/{assessmentType}")).Content;
        }

        #endregion

        #region Engagements

        /// <summary>
        /// Updates status of a Engagement document using locking to ensure consistency
        /// </summary>
        /// <param name="id">Identity of entity to update</param>
        /// <param name="newStatus">New status to set</param>
        /// <returns>true if successful</returns>
        public NoDataResponse EngagementUpdateStatus(string id, EngagementStatus newStatus)
        {
            return CheckRetry(() => ApiClient.Get<NoDataResponse>($"engagement/status/{id}/{newStatus.ToString("g")}")).Content;
        }

        /// <summary>
        /// Updates Engagements using update by query
        /// </summary>
        /// <param name="request">reqeust used in updates</param>
        /// <returns>true if successful</returns>
        public BulkResponse EngagementsUpdateByQuery(UpdateQueryRequest<Engagement> request)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("engagement/bulk/query", request)).Content;
        }

        /// <summary>
        /// Gets Grouped Parent Engagement
        /// </summary>
        /// <param name="id">The current engagement Id</param>
        /// <returns>Success Engagement with success flag</returns>
        public NoDataResponse SetHistoricalIssues(string id, string groupId)
        {
            return CheckRetry(() => ApiClient.Post<NoDataResponse>($"engagement/{id}/group/{groupId}/historical", null)).Content;
        }

        /// <summary>
        /// Gets Grouped Parent Engagement
        /// </summary>
        /// <param name="id">The current engagement Id</param>
        /// <returns>Success Engagement with success flag</returns>
        public NoDataResponse UnSetHistoricalIssues(string id)
        {
            return CheckRetry(() => ApiClient.Post<NoDataResponse>($"engagement/{id}/remove-historical", null)).Content;
        }

        /// <summary>
        /// Gets an Engagement by ID
        /// </summary>
        /// <param name="id">The ID to get</param>
        /// <returns>The requested item, along with concurrency info</returns>
        public DataItemResponse<Engagement> EngagementGet(string id)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<Engagement>>($"engagement/{id}")).Content;
        }

        /// <summary>
        /// Searches for Engagements by filter(s)
        /// </summary>
        /// <param name="search">The filter request that defines the search parameters</param>
        /// <returns>The first set of results and scrolling/paging information</returns>
        /// <remarks>Pass in both request details and paging info to get the next set of results</remarks>
        public DataResponse<Engagement> EngagementSearch(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<Engagement>>($"engagement/search", search), true).Content;
        }

        /// <summary>
        /// Deletes an Engagement by ID and all related records
        /// </summary>
        /// <param name="id">The ID to DELETE</param>
        /// <returns>true if successful</returns>
        public NoDataResponse EngagementDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"engagement/{id}")).Content;
        }

        /// <summary>
        /// Deletes an Engagement by Group ID and all related records
        /// </summary>
        /// <param name="id">The Group ID to DELETE</param>
        /// <returns>true if successful</returns>
        public NoDataResponse EngagementGroupDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"engagement/group/{id}")).Content;
        }

        /// <summary>
        /// Adds/Updates multiple engagements
        /// </summary>
        /// <param name="batch">Engagement batch to add</param>
        /// <returns>BulkResponse with Count of documents affected</returns>
        public BulkResponse EngagementAddUpdateBulk(IEnumerable<Engagement> batch)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("engagement/bulk", new DataRequest<Engagement> { Documents = batch })).Content;
        }

        /// <summary>
        /// Adds/updates single engagement
        /// </summary>
        /// <param name="engagement">Engagement  to add/update</param>
        /// <returns>DataItemResponse with enagement document</returns>
        public DataItemResponse<Engagement> EngagementAddUpdate(Engagement engagement)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<Engagement>>("engagement", new DataItemRequest<Engagement> { Entity = engagement })).Content;
        }

        /// <summary>
        /// Returns count of issues grouped by severity for a engagement
        /// </summary>
        /// <returns>A dictionary response of type string, double that represents the key values (joined with |) and counts</returns>
        public DataItemResponse<Dictionary<string, long?>> EngagementIssueCounts(string id, string assetType, string sourceType, string instance)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<Dictionary<string, long?>>> ($"engagement/{id}/{assetType}/{sourceType}/{instance}/issue/counts")).Content;
        }

        #endregion

        #region Job

        /// <summary>
        /// Updates Jobs using update by query
        /// </summary>
        /// <param name="request">reqeust used in updates</param>
        /// <returns>true if successful</returns>
        public BulkResponse JobsUpdateByQuery(UpdateQueryRequest<Job> request)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("job/bulk/query", request)).Content;
        }

        /// <summary>
        /// Add/update a Job
        /// </summary>
        /// <param name="job">new or updated entity</param>
        /// <returns>The entity as updated from the datasource</returns>
        public DataItemResponse<Job> JobAddUpdate(Job job)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<Job>>("job", new DataItemRequest<Job> { Entity = job })).Content;
        }

        /// <summary>
        /// Add/Update Jobs
        /// </summary>
        /// <param name="jobs">Jobs to add/update</param>
        /// <returns>BulkResponse with Count of successful adds</returns>
        public BulkResponse JobAddUpdateBulk(IEnumerable<Job> jobs)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("job/bulk", new DataRequest<Job> { Documents = jobs })).Content;
        }

        /// <summary>
        /// Find Job documents by criteria
        /// </summary>
        /// <param name="searchRequest">Search request parameters</param>
        /// <returns>Response container including results and pagination info (if needed)</returns>
        public DataResponse<Job> JobSearch(SearchRequest searchRequest)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<Job>>($"job/search", searchRequest), true).Content;
        }

        /// <summary>
        /// Deletes a Job document by ID
        /// </summary>
        /// <param name="id">The identifier of the target entity</param>
        /// <returns>Boolean value indicating success</returns>
        public NoDataResponse JobDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"job/{id}")).Content;
        }

        /// <summary>
        /// Get a Job document by ID
        /// </summary>
        /// <param name="id">The identifier of the target entity</param>
        /// <returns>Boolean value indicating success</returns>
        public DataItemResponse<Job> JobGet(string id)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<Job>>($"job/{id}")).Content;
        }

        /// <summary>
        /// Updates status of a Job queue item using locking to ensure consistency
        /// </summary>
        /// <param name="job">Identity of entity to update</param>
        /// <returns>true if successful</returns>
        public DataItemResponse<Job> JobUpdateStatus(Job job)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<Job>>($"job/status", new DataItemRequest<Job> { Entity = job }), true).Content;
        }

        #endregion

        #region CustomImporter

        /// <summary>
        /// Add/update a CustomImporter
        /// </summary>
        /// <param name="CustomImporter">new or updated entity</param>
        /// <returns>The entity as updated from the datasource</returns>
        public DataItemResponse<CustomImporter> CustomImporterAddUpdate(CustomImporter CustomImporter)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<CustomImporter>>("CustomImporter", new DataItemRequest<CustomImporter> { Entity = CustomImporter })).Content;
        }

        /// <summary>
        /// Find CustomImporter documents by criteria
        /// </summary>
        /// <param name="searchRequest">Search request parameters</param>
        /// <returns>Response container including results and pagination info (if needed)</returns>
        public DataResponse<CustomImporter> CustomImporterSearch(SearchRequest searchRequest)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<CustomImporter>>($"CustomImporter/search", searchRequest), true).Content;
        }

        /// <summary>
        /// Deletes a CustomImporter document by ID
        /// </summary>
        /// <param name="id">The identifier of the target entity</param>
        /// <returns>Boolean value indicating success</returns>
        public NoDataResponse CustomImporterDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"CustomImporter/{id}")).Content;
        }

        /// <summary>
        /// Get a CustomImporter document by ID
        /// </summary>
        /// <param name="id">The identifier of the target entity</param>
        /// <returns>Boolean value indicating success</returns>
        public DataItemResponse<CustomImporter> CustomImporterGet(string id)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<CustomImporter>>($"CustomImporter/{id}")).Content;
        }

        #endregion

        #region Comment 

        /// <summary>
        /// Updates Comments using update by query
        /// </summary>
        /// <param name="request">reqeust used in updates</param>
        /// <returns>true if successful</returns>
        public BulkResponse CommentsUpdateByQuery(UpdateQueryRequest<Comment> request)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("comment/bulk/query", request)).Content;
        }

        /// <summary>
        /// Add/update an Comment
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>The updated entity, along with concurrency info</returns>
        public DataItemResponse<Comment> CommentAddUpdate(Comment entity)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<Comment>>("comment", new DataItemRequest<Comment> { Entity = entity, Id = entity.Id })).Content;
        }

        /// <summary>
        /// Add/Update multiple Comments
        /// </summary>
        /// <param name="batch">Comment batch to add</param>
        /// <returns>The add entities, along with concurrency info</returns>
        public BulkResponse CommentAddUpdateBulk(IEnumerable<Comment> batch)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("comment/bulk", new DataRequest<Comment> { Documents = batch })).Content;
        }

        /// <summary>
        /// Gets an Comment by ID, Type, and Source Type
        /// </summary>
        /// <param name="id">The ID to get</param>
        /// <returns>The requested item, along with concurrency info</returns>
        public DataItemResponse<Comment> CommentGet(string id)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<Comment>>($"comment/{id}")).Content;
        }

        /// <summary>
        /// Searches for Comments by filter(s)
        /// </summary>
        /// <param name="search">The filter request that defines the search parameters</param>
        /// <returns>The first set of results and scrolling/paging information</returns>
        /// <remarks>Pass in both request details and paging info to get the next set of results</remarks>
        public DataResponse<Comment> CommentSearch(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<Comment>>($"comment/search", search), true).Content;
        }

        /// <summary>
        /// Deletes an Comment by ID, Type, and Source Type
        /// </summary>
        /// <param name="id">The ID to DELETE</param>
        /// <returns>true if successful</returns>
        public NoDataResponse CommentDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"comment/{id}")).Content;
        }

        /// <summary>
        /// Deletes an Comment by ID, Type, and Source Type
        /// </summary>
        /// <param name="id">The ID to DELETE</param>
        /// <returns>true if successful</returns>
        public NoDataResponse CommentDeleteAllEngagement(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"comment/engagement/{id}")).Content;
        }

        #endregion

        #region Attachment 

        /// <summary>
        /// Updates Attachments using update by query
        /// </summary>
        /// <param name="request">reqeust used in updates</param>
        /// <returns>true if successful</returns>
        public BulkResponse AttachmentsUpdateByQuery(UpdateQueryRequest<Attachment> request)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("attachment/bulk/query", request)).Content;
        }

        /// <summary>
        /// Add/update an Attachment
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>The updated entity, along with concurrency info</returns>
        public DataItemResponse<Attachment> AttachmentAddUpdate(Attachment entity)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<Attachment>>("attachment", new DataItemRequest<Attachment> { Entity = entity, Id = entity.Id })).Content;
        }

        /// <summary>
        /// Add/Update Bulk an Attachment
        /// </summary>
        /// <param name="entities">Attachments to add</param>
        /// <returns>BulkResponse with Count of successful adds</returns>
        public BulkResponse AttachmentAddUpdateBulk(IEnumerable<Attachment> entities)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("attachment/bulk", new DataRequest<Attachment> { Documents = entities })).Content;
        }

        /// <summary>
        /// Gets an Attachment by ID, Type, and Source Type
        /// </summary>
        /// <param name="id">The ID to get</param>
        /// <returns>The requested item, along with concurrency info</returns>
        public DataItemResponse<Attachment> AttachmentGet(string id)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<Attachment>>($"attachment/{id}")).Content;
        }

        /// <summary>
        /// Searches for Attachments by filter(s)
        /// </summary>
        /// <param name="search">The filter request that defines the search parameters</param>
        /// <returns>The first set of results and scrolling/paging information</returns>
        /// <remarks>Pass in both request details and paging info to get the next set of results</remarks>
        public DataResponse<Attachment> AttachmentSearch(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<Attachment>>($"attachment/search", search), true).Content;
        }

        /// <summary>
        /// Deletes an Attachment by ID, Type, and Source Type
        /// </summary>
        /// <param name="id">The ID to DELETE</param>
        /// <returns>true if successful</returns>
        public NoDataResponse AttachmentDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"attachment/{id}")).Content;
        }

        /// <summary>
        /// Deletes Attachments by Engagement Id
        /// </summary>
        /// <param name="id">The ID to DELETE</param>
        /// <param name="onlyEngagementLevel">True if engagements without issue ids are to be deleted</param>
        /// <param name="isMarkdown">True if engagements with markdown flag are to be deleted</param>
        /// <returns>true if successful</returns>
        public NoDataResponse AttachmentDeleteAllEngagement(string id, bool isMarkdown = false, bool onlyEngagementLevel = false)
        {
            var url = $"attachment/engagement/{id}";

            if (onlyEngagementLevel)
            {
                url += "/all";
            }

            if (isMarkdown)
            {
                url += "/markdown";
            }

            return CheckRetry(() => ApiClient.Delete<NoDataResponse>(url)).Content;
        }

        /// <summary>
        /// Deletes Attachments by Issue Id
        /// </summary>
        /// <param name="id">The ID to DELETE</param>
        /// <param name="isMarkdown">Set null for all, true for only markdown, false for only non-markdown</param>
        /// <returns>true if successful</returns>
        public NoDataResponse AttachmentDeleteAllIssue(string id, bool? isMarkdown = null)
        {
            return isMarkdown == null ?
                CheckRetry(() => ApiClient.Delete<NoDataResponse>($"attachment/issue/{id}")).Content :
                isMarkdown.Value ?
                    CheckRetry(() => ApiClient.Delete<NoDataResponse>($"attachment/issue/{id}/markdown")).Content :
                    CheckRetry(() => ApiClient.Delete<NoDataResponse>($"attachment/issue/{id}/non-markdown")).Content;
        }

        #endregion

        #region ServiceJob

        /// <summary>
        /// Searches for Service Job by filter(s)
        /// </summary>
        /// <param name="search">The filter request that defines the search parameters</param>
        /// <returns>The first set of results and scrolling/paging information</returns>
        /// <remarks>Pass in both request details and paging info to get the next set of results</remarks>
        public DataResponse<ServiceJob> ServiceJobSearch(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<ServiceJob>>($"servicejob/search", search), true).Content;
        }

        /// <summary>
        /// Deletes an Service Job by ID
        /// </summary>
        /// <param name="id">The ID to DELETE</param>
        /// <returns>true if successful</returns>
        public NoDataResponse ServiceJobDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"servicejob/{id}")).Content;
        }

        /// <summary>
        /// Get a Service Job by ID
        /// </summary>
        /// <param name="id">The ID to GET</param>
        /// <returns>true if successful</returns>
        public NoDataResponse ServiceJobGet(string id)
        {
            return CheckRetry(() => ApiClient.Get<NoDataResponse>($"servicejob/{id}")).Content;
        }

        /// <summary>
        /// Add/update a Service Job
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>The updated entity, along with concurrency info</returns>
        public DataItemResponse<ServiceJob> ServiceJobAddUpdate(ServiceJob entity)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<ServiceJob>>("servicejob", new DataItemRequest<ServiceJob> { Entity = entity, Id = entity.Id })).Content;
        }

        #endregion

        #region Lookup 

        /// <summary>
        /// Updates Lookups using update by query
        /// </summary>
        /// <param name="request">reqeust used in updates</param>
        /// <returns>true if successful</returns>
        public BulkResponse LookupsUpdateByQuery(UpdateQueryRequest<Lookup> request)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("lookup/bulk/query", request)).Content;
        }

        /// <summary>
        /// Add/update an Lookup
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>The updated entity, along with concurrency info</returns>
        public DataItemResponse<Lookup> LookupAddUpdate(Lookup entity)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<Lookup>>("lookup", new DataItemRequest<Lookup> { Entity = entity, Id = entity.Id })).Content;
        }

        /// <summary>
        /// Delete lookups by type
        /// </summary>
        /// <param name="type">The type to delete</param>
        /// <returns>true if successful</returns>
        public NoDataResponse LookupDeleteByType(string type)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"lookup/bulk/{type}")).Content;
        }

        /// <summary>
        /// Gets an Lookup by ID, Type, and Source Type
        /// </summary>
        /// <param name="id">The ID to get</param>
        /// <returns>The requested item, along with concurrency info</returns>
        public DataItemResponse<Lookup> LookupGet(string id)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<Lookup>>($"lookup/{id}")).Content;
        }

        /// <summary>
        /// Searches for Lookups by filter(s)
        /// </summary>
        /// <param name="search">The filter request that defines the search parameters</param>
        /// <returns>The first set of results and scrolling/paging information</returns>
        /// <remarks>Pass in both request details and paging info to get the next set of results</remarks>
        public DataResponse<Lookup> LookupSearch(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<Lookup>>($"lookup/search", search), true).Content;
        }

        /// <summary>
        /// Deletes an Lookup by ID, Type, and Source Type
        /// </summary>
        /// <param name="id">The ID to DELETE</param>
        /// <returns>true if successful</returns>
        public NoDataResponse LookupDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"lookup/{id}")).Content;
        }

        #endregion

        #region AttributeDefinition 

        /// <summary>
        /// Updates AttributeDefinitions using update by query
        /// </summary>
        /// <param name="request">reqeust used in updates</param>
        /// <returns>true if successful</returns>
        public BulkResponse AttributeDefinitionsUpdateByQuery(UpdateQueryRequest<AttributeDefinition> request)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("attributedefinition/bulk/query", request)).Content;
        }

        /// <summary>
        /// Add/update an Attribute Definition
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>The updated entity, along with concurrency info</returns>
        public DataItemResponse<AttributeDefinition> AttributeDefinitionAddUpdate(AttributeDefinition entity)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<AttributeDefinition>>("attributedefinition", new DataItemRequest<AttributeDefinition> { Entity = entity, Id = entity.Id })).Content;
        }

        /// <summary>
        /// Gets an Attribute Definition by ID
        /// </summary>
        /// <param name="id">The ID to get</param>
        /// <returns>The requested item, along with concurrency info</returns>
        public DataItemResponse<AttributeDefinition> AttributeDefinitionGet(string id)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<AttributeDefinition>>($"attributedefinition/{id}")).Content;
        }

        /// <summary>
        /// Searches for Attribute Definitions by filter(s)
        /// </summary>
        /// <param name="search">The filter request that defines the search parameters</param>
        /// <returns>The first set of results and scrolling/paging information</returns>
        /// <remarks>Pass in both request details and paging info to get the next set of results</remarks>
        public DataResponse<AttributeDefinition> AttributeDefinitionSearch(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<AttributeDefinition>>($"attributedefinition/search", search), true).Content;
        }

        /// <summary>
        /// Searches for Action Definitions by filter(s)
        /// </summary>
        /// <param name="search">The filter request that defines the search parameters</param>
        /// <returns>The first set of results and scrolling/paging information</returns>
        /// <remarks>Pass in both request details and paging info to get the next set of results</remarks>
        public DataResponse<ActionDefinition> ActionDefinitionSearch(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<ActionDefinition>>($"actiondefinition/search", search), true).Content;
        }

        /// <summary>
        /// Deletes an Attribute Definition by ID
        /// </summary>
        /// <param name="id">The ID to DELETE</param>
        /// <returns>true if successful</returns>
        public NoDataResponse AttributeDefinitionDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"attributedefinition/{id}")).Content;
        }

        #endregion

        #region SearchFilter  

        /// <summary>
        /// Updates SearchFilters using update by query
        /// </summary>
        /// <param name="request">reqeust used in updates</param>
        /// <returns>true if successful</returns>
        public BulkResponse SearchFiltersUpdateByQuery(UpdateQueryRequest<SearchFilter> request)
        {
            return CheckRetry(() => ApiClient.Post<BulkResponse>("searchfilter/bulk/query", request)).Content;
        }

        /// <summary>
        /// Add/update an SearchFilter
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>The updated entity, along with concurrency info</returns>
        public DataItemResponse<SearchFilter> SearchFilterAddUpdate(SearchFilter entity)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<SearchFilter>>("searchfilter", new DataItemRequest<SearchFilter> { Entity = entity, Id = entity.Id })).Content;
        }

        /// <summary>
        /// Gets an SearchFilter by ID, Type, and Source Type
        /// </summary>
        /// <param name="id">The ID to get</param>
        /// <returns>The requested item, along with concurrency info</returns>
        public DataItemResponse<SearchFilter> SearchFilterGet(string id)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<SearchFilter>>($"searchfilter/{id}")).Content;
        }

        /// <summary>
        /// Searches for SearchFilter by filter(s)
        /// </summary>
        /// <param name="search">The filter request that defines the search parameters</param>
        /// <returns>The first set of results and scrolling/paging information</returns>
        /// <remarks>Pass in both request details and paging info to get the next set of results</remarks>
        public DataResponse<SearchFilter> SearchFilterSearch(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<SearchFilter>>($"searchfilter/search", search), true).Content;
        }

        /// <summary>
        /// Deletes an SearchFilter by ID, Type, and Source Type
        /// </summary>
        /// <param name="id">The ID to DELETE</param>
        /// <returns>true if successful</returns>
        public NoDataResponse SearchFilterDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"searchfilter/{id}")).Content;
        }

        #endregion

        #region FieldDefinition  

        /// <summary>
        /// Searches for Field Definition by filter(s)
        /// </summary>
        /// <param name="search">The filter request that defines the search parameters</param>
        /// <returns>The first set of results and scrolling/paging information</returns>
        /// <remarks>Pass in both request details and paging info to get the next set of results</remarks>
        public DataResponse<FieldDefinition> FieldDefinitionSearch(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<FieldDefinition>>($"fielddefinition/search", search), true).Content;
        }

        /// <summary>
        /// Add/update a Field definition
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>The updated entity, along with concurrency info</returns>
        public DataItemResponse<FieldDefinition> FieldDefinitionAddUpdate(FieldDefinition entity)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<FieldDefinition>>("fielddefinition", new DataItemRequest<FieldDefinition> { Entity = entity, Id = entity.Id })).Content;
        }

        /// <summary>
        /// Get a Field Definition
        /// </summary>
        /// <param name="id">The ID of the Field Definition</param>
        /// <returns>The requested item, along with concurrency info</returns>
        public DataItemResponse<FieldDefinition> FieldDefinitionGet(string id)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<FieldDefinition>>($"fielddefinition/{id}")).Content;
        }


        /// <summary>
        /// Get a list of Field Definitions by entity
        /// </summary>
        /// <param name="entity">The field entity</param>
        /// <returns>The requested item, along with concurrency info</returns>
        public DataItemResponse<List<FieldDefinition>> FieldDefinitionsGetByEntityType(string entity)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<List<FieldDefinition>>>($"fielddefinition/entity/{entity}")).Content;
        }

        /// <summary>
        /// Deletes a Field Definition by ID
        /// </summary>
        /// <param name="id">The ID to DELETE</param>
        /// <returns>true if successful</returns>
        public NoDataResponse FieldDefinitionDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"fielddefinition/{id}")).Content;
        }

        #endregion

        #region Role

        /// <summary>
        /// Searches for role by filter(s)
        /// </summary>
        /// <param name="search">The filter request that defines the search parameters</param>
        /// <returns>The first set of results and scrolling/paging information</returns>
        /// <remarks>Pass in both request details and paging info to get the next set of results</remarks>
        public DataResponse<AppRole> RoleSearch(SearchRequest search)
        {
            return CheckRetry(() => ApiClient.Post<DataResponse<AppRole>>($"role/search", search), true).Content;
        }

        /// <summary>
        /// Deletes a role by ID
        /// </summary>
        /// <param name="id">The ID to DELETE</param>
        /// <returns>true if successful</returns>
        public NoDataResponse RoleDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"role/{id}")).Content;
        }

        /// <summary>
        /// Get a role by ID
        /// </summary>
        /// <param name="id">The ID to GET</param>
        /// <returns>true if successful</returns>
        public NoDataResponse RoleGet(string id)
        {
            return CheckRetry(() => ApiClient.Get<NoDataResponse>($"role/{id}")).Content;
        }

        /// <summary>
        /// Add/update a role
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>The updated entity, along with concurrency info</returns>
        public DataItemResponse<AppRole> RoleAddUpdate(AppRole entity)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<AppRole>>("role", new DataItemRequest<AppRole> { Entity = entity, Id = entity.Id })).Content;
        }

        #endregion

        #region Config  

        /// <summary>
        /// Add/update an Config
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>The updated entity, along with concurrency info</returns>
        public DataItemResponse<Config> ConfigAddUpdate(Config entity)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<Config>>("runConfig", new DataItemRequest<Config> { Entity = entity, Id = entity.Id })).Content;
        }

        /// <summary>
        /// Get all Configs (admin only)
        /// </summary>
        /// <returns>The requested item, along with concurrency info</returns>
        public DataResponse<Config> ConfigGetAll()
        {
            return CheckRetry(() => ApiClient.Get<DataResponse<Config>>($"runConfig/all")).Content;
        }

        /// <summary>
        /// Get an Config by Id
        /// </summary>
        /// <returns>The requested item, along with concurrency info</returns>
        public DataItemResponse<Config> ConfigGet(string id)
        {
            return CheckRetry(() => ApiClient.Get<DataItemResponse<Config>>($"runConfig/{id}")).Content;
        }

        /// <summary>
        /// Deletes an Config by ID (admin only)
        /// </summary>
        /// <param name="id">The ID to DELETE</param>
        /// <returns>true if successful</returns>
        public NoDataResponse ConfigDelete(string id)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"runConfig/{id}")).Content;
        }

        /// <summary>
        /// Deletes an Config by Type (admin only)
        /// </summary>
        /// <param name="type">The Type to DELETE</param>
        /// <returns>true if successful</returns>
        public NoDataResponse ConfigDeleteByType(string type)
        {
            return CheckRetry(() => ApiClient.Delete<NoDataResponse>($"runConfig/type/{type}")).Content;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Creates a backup of Elastic and creates a zip file of the data
        /// </summary>
        /// <returns>The zip file of data</returns>
        public async Task<ApiClientFileResponse> CreateBackup()
        {
            return await ApiClient.GetFileAsync($"utility/backup/create");
        }

        public async Task<ApiClientNoContentResponse> RestoreBackup(Stream fileStream, string fileName)
        {
            return await ApiClient.PostFileAsync($"utility/backup/restore", fileStream, fileName);
        }

        private static Exception GetFirstFromAggregateException(AggregateException ex)
        {
            if ((ex.InnerExceptions?.Count ?? 0) > 0)
                return ex.InnerExceptions[0];
            if (ex.InnerException != null)
                return ex.InnerException;
            return ex;
        }

        private ApiClientResponse<T> CheckRetry<T>(Func<ApiClientResponse<T>> retry, bool skipRetry = false) where T : Response
        {
            try
            {
                return CheckRetryAsync(async () => await Task.Run(retry), skipRetry).Result;
            }
            catch (AggregateException ex)
            {
                throw GetFirstFromAggregateException(ex);
            }

        }

        private async Task<ApiClientResponse<T>> CheckRetryAsync<T>(Func<Task<ApiClientResponse<T>>> retry, bool skipRetry = false) where T : Response
        {
            ApiClientResponse<T> response = null;
            var msg = "";
            // Stay in retry loop until retries exhausted or good response or bad response that isn't a 5xx error
            for (int i = 0; i < (skipRetry ? 1 : RunConfig.ApiClientRetryCount); i++)
            {
                try
                {
                    response = await retry();
                }
                catch (Exception ex)
                {
                    throw new DataClientException(ex.Message, ex);
                }

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                try
                {
                    if(response?.Content?.ErrorMessages != null && response.Content.ErrorMessages.Any())
                    {
                        msg = string.Join(",", response.Content.ErrorMessages);
                    }

                    msg = string.IsNullOrEmpty(msg) ? response?.Content?.Message ?? "" : msg;
                }
                catch (ApiClientSerializationException)
                {
                    msg = response.HttpResponse.ReasonPhrase;
                }

                if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge)
                {
                    msg = $"{msg}. Consider reducing the batch size if this request contains a batch of entities.";
                }

                if (string.IsNullOrEmpty(msg) && response?.RawContent != null)
                {
                    msg = $"Response raw content (up to 300 chars): {response?.RawContent?.Left(300)}";
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new DataClientValidationException(msg, response);
                }

                if (response.StatusCode < HttpStatusCode.InternalServerError)
                {
                    throw new DataClientResponseException(msg, response);
                }

                Logger.LogError("{Msg}", msg);
                Logger.LogInformation("Retry {Cur}/{Count} after {DelaySec} sec delay...", i + 1, RunConfig.ApiClientRetryCount, RunConfig.ApiClientRetryDelaySec);
                await Task.Delay(TimeSpan.FromSeconds(RunConfig.ApiClientRetryDelaySec));
            }

            Logger.LogDebug("Response raw content (up to 300 chars): {Response}", response?.RawContent?.Left(300));
            throw new DataClientResponseException(msg, response);
        }

        public NoDataResponse GetApiVersion()
        {
            return CheckRetry(() => ApiClient.Get<NoDataResponse>($"utility/version")).Content;
        }

        #endregion

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ApiClient.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Event

        /// <summary>
        /// Add an event
        /// </summary>
        /// <returns>The entity as updated from the datasource</returns>
        public DataItemResponse<Eventlog> EventAdd(Eventlog eventEntity)
        {
            return CheckRetry(() => ApiClient.Post<DataItemResponse<Eventlog>>("eventlog", new DataItemRequest<Eventlog> { Entity = eventEntity })).Content;
        }

        #endregion
    }
}
