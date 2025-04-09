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

﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.DataApi.Contexts;
using Saltworks.SaltMiner.Core.Data;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth]
    [ApiController]
    public class ScanController : ApiControllerBase
    {
        private ScanContext Context => ContextBase as ScanContext;

        public ScanController(ScanContext context, ILogger<ScanController> logger) : base(context, logger)
        {
        }

        /// <summary>
        /// Updates one or more Scan(s) using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<Scan> request)
        {
            Logger.LogInformation("Update By Query action called");
            return Accepted(Context.UpdateByQuery(request, Scan.GenerateIndex()));
        }

        /// <summary>
        /// Returns a single Scan
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(DataItemResponse<Scan>))]
        [HttpGet("engagement/{id}")]
        public ActionResult<DataItemResponse<Scan>> GetByEngagement(string id)
        {
            Logger.LogInformation("GetByEngagement action called");

            return Ok(Context.GetByEngagement(id));
        }

        /// <summary>
        /// Returns a single Scan
        /// </summary>
        /// <param name="id">Target entity ID</param>
        /// <param name="assetType">Target entity AssetType</param>
        /// <param name="sourceType">Target entity Source Type</param>
        /// <param name="instance">Target entity Instance</param>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<Scan>))]
        [Auth(Role.Manager, Role.Admin, Role.Pentester, Role.PentesterViewer)]
        [HttpGet("{id}/{assetType}/{sourceType}/{configName}")]
        public ActionResult<DataItemResponse<Scan>> Get(string id, string assetType, string sourceType, string instance)
        {
            Logger.LogInformation("Get action called for id '{id}', type '{assetType}', and source type '{sourceType}', and instance '{instance}'", id, assetType, sourceType, instance);
            
            return Ok(Context.Get<Scan>(id, Scan.GenerateIndex(assetType, sourceType, instance)));
        }

        /// <summary>
        /// Returns a list of Scans by search
        /// </summary>
        /// <param name="search">Search request payload</param>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<Scan>))]
        [Auth(Role.Manager, Role.Admin, Role.Pentester, Role.PentesterViewer, Role.Agent)]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<Scan>> Search([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search action called");
            
            return Ok(Context.Search(search));
        }

        /// <summary>
        /// Adds or Updates one or more Scan
        /// </summary>
        /// <param name="request">Request containing the payload entity</param>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(NoDataResponse))]
        [Auth(Role.Manager, Role.Admin)]
        [HttpPost]
        public ActionResult<DataItemResponse<Scan>> Post([FromBody] DataItemRequest<Scan> request)
        {
            Logger.LogInformation("Post action called");
           
            return Accepted(Context.AddUpdate(request, Scan.GenerateIndex(request.Entity.Saltminer.Asset.AssetType, request.Entity.Saltminer.Asset.SourceType, request.Entity.Saltminer.Asset.Instance)));
        }

        /// <summary>
        /// Deletes an Scan entity by id
        /// </summary>
        /// <param name="id">Target entity ID</param>
        /// <param name="assetType">Target entity AssetType</param>
        /// <param name="sourceType">Target entity Source Type</param>
        /// <param name="instance">Target entity Instance</param>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Manager, Role.Admin)]
        [HttpDelete("{id}/{assetType}/{sourceType}/{instance}")]
        public ActionResult<NoDataResponse> Delete(string id, string assetType, string sourceType, string instance)
        {
            Logger.LogInformation("Delete action called for id '{id}', type '{assetType}', and source type '{sourceType}', and instance '{instance}'", id, assetType, sourceType, instance);
        
            return Ok(Context.Delete<Scan>(id, Scan.GenerateIndex(assetType, sourceType, instance)));
        }

        /// <summary>
        /// Get counts of scans by asset id
        /// </summary>
        /// <returns>Count of scans found</returns>
        /// <param name="assetId">Asset Id for which to perform counts</param>
        /// <response code="200">Returns a response object indicating success and count of found docs</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Manager, Role.Admin)]
        [HttpGet("count/asset/{assetId}")]
        public ActionResult<NoDataResponse> CountByAssetId(string assetId)
        {
            Logger.LogInformation("Count of scans action called for Asset ID '{assetId}'", assetId);


            return Ok(Context.CountByAssetId(assetId));
        }

        /// <summary>
        /// Get counts of scans by asset inventory key
        /// </summary>
        /// <returns>Count of scans found</returns>
        /// <param name="InventoryAssetKey">Asset Inventory Key for which to perform counts</param>
        /// <response code="200">Returns a response object indicating success and count of found docs</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Manager, Role.Admin)]
        [HttpGet("count/InventoryAsset/{InventoryAssetKey}")]
        public ActionResult<NoDataResponse> CountByInventoryAssetKey(string InventoryAssetKey)
        {
            Logger.LogInformation("Count of scans action called for Asset Inventory key '{InventoryAssetKey}'", InventoryAssetKey);


            return Ok(Context.CountByInventoryAssetKey(InventoryAssetKey));
        }
    }
}
