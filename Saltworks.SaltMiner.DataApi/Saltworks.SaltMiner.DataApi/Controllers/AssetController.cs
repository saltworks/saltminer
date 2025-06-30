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

﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Contexts;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.DataApi.Authentication;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth]
    [ApiController]
    public class AssetController : ApiControllerBase
    {
        private AssetContext Context => ContextBase as AssetContext;

        public AssetController(AssetContext context, ILogger<AssetController> logger) : base(context, logger)
        {
        }

        /// <summary>
        /// Updates one or more Asset(s) using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<Asset> request)
        {
            Logger.LogInformation("Update By Query action called");
            return Accepted(Context.UpdateByQuery(request, Asset.GenerateIndex()));
        }

        /// <summary>
        /// Returns a single Asset
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<Asset>))]
        [HttpGet("{id}/{assetType}/{sourceType}/{instance}")]
        public ActionResult<DataItemResponse<Asset>> Get(string id, string assetType, string sourceType, string instance)
        {
            Logger.LogInformation("Get action called for id '{id}', type '{assetType}', and source type '{sourceType}', and instance '{instance}'", id, assetType, sourceType, instance);
            return Ok(Context.Get<Asset>(id, Asset.GenerateIndex(assetType, sourceType, instance)));
        }

        /// <summary>
        /// Returns a list of Assets
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<Asset>))]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<Asset>> Search([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search action called");
            return Ok(Context.Search<Asset>(search, Asset.GenerateIndex(search.AssetType, search.SourceType, search.Instance)));
        }

        /// <summary>
        /// Adds or Updates an Asset
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(DataItemResponse<Asset>))]
        [Auth(Role.Manager, Role.Admin)]
        [HttpPost]
        public ActionResult<DataItemResponse<Asset>> Post([FromBody] DataItemRequest<Asset> request)
        {
            Logger.LogInformation("Post action called");
            return Accepted(Context.AddUpdate(request, Asset.GenerateIndex(request.Entity.Saltminer.Asset.AssetType, request.Entity.Saltminer.Asset.SourceType, request.Entity.Saltminer.Asset.Instance)));
        }

        /// <summary>
        /// Deletes an Asset entity by ID, source type, and asset type.
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Manager, Role.Admin)]
        [HttpDelete("{id}/{assetType}/{sourceType}/{instance}")]
        public ActionResult<NoDataResponse> Delete(string id, string assetType, string sourceType, string instance)
        {
            Logger.LogInformation("Delete action called for id '{id}', type '{assetType}', and source type '{sourceType}', and config name '{instance}'", id, assetType, sourceType, instance);
            return Ok(Context.Delete<Asset>(id, Asset.GenerateIndex(assetType, sourceType, instance)));
        }

        /// <summary>
        /// Get counts of assets by asset inventory key
        /// </summary>
        /// <returns>Count of assets found</returns>
        /// <param name="InventoryAssetKey">Asset Inventory Key for which to perform counts</param>
        /// <response code="200">Returns a response object indicating success and count of found docs</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Manager, Role.Admin)]
        [HttpGet("count/{InventoryAssetKey}")]
        public ActionResult<NoDataResponse> CountByInventoryAssetKey(string InventoryAssetKey)
        {
            Logger.LogInformation("Count of assets action called for Asset Inventory key '{InventoryAssetKey}'", InventoryAssetKey);
            return Ok(Context.CountByInventoryAssetKey(InventoryAssetKey));
        }

        /// <summary>
        /// Deletes an asset entity by source id, source type, and instance and all scans and issues associated with it.
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpDelete("all/{sourceId}/{sourceType}/{instance}")]
        public ActionResult<NoDataResponse> DeleteAllBySourceId(string sourceId, string sourceType, string instance)
        {
            Logger.LogInformation("DeleteAll action called for id {id}", sourceType);

            return Ok(Context.DeleteAllBySourceId(sourceId, sourceType, instance));
        }
    }
}
