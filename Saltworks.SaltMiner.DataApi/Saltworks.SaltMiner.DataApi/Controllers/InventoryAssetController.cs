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
using Saltworks.SaltMiner.DataApi.Contexts;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.DataApi.Authentication;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth]
    [ApiController]
    public class InventoryAssetController : ApiControllerBase
    { 
        private InventoryAssetContext Context => ContextBase as InventoryAssetContext;
        private string InventoryAssetIndex = InventoryAsset.GenerateIndex();

        public InventoryAssetController(InventoryAssetContext context, ILogger<InventoryAssetController> logger) : base(context, logger)
        {
        }

        /// <summary>
        /// Updates one or more InventoryAsset(s) using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<InventoryAsset> request)
        {
            Logger.LogInformation("Update By Query action called");
            return Accepted(Context.UpdateByQuery(request, InventoryAssetIndex));
        }

        /// <summary>
        /// Returns a single InventoryAsset
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<InventoryAsset>))]
        [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin)]
        [HttpGet("{id}")]
        public ActionResult<DataItemResponse<InventoryAsset>> Get(string id)
        {
            Logger.LogInformation("Get action called for id {id}", id);
            return Ok(Context.Get<InventoryAsset>(id, InventoryAssetIndex));
        }

        /// <summary>
        /// Returns a single InventoryAsset
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<InventoryAsset>))]
        [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin)]
        [HttpGet("key/{key}")]
        public ActionResult<DataItemResponse<InventoryAsset>> GetByKey(string key)
        {
            Logger.LogInformation("Get action called for inventory key {key}", key);
            return Ok(Context.GetByKey(key));
        }

        /// <summary>
        /// InventoryAsset search
        /// </summary>
        /// <returns>Matching docs and scroll info</returns>
        /// <response code="202">Returns a response object containing results and scroll info</response>
        [ProducesResponseType(202, Type = typeof(DataResponse<InventoryAsset>))]
        [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin)]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<InventoryAsset>> Search([FromBody] SearchRequest request)
        {
            Logger.LogInformation("Post action called");
            return Ok(Context.Search<InventoryAsset>(request, InventoryAssetIndex));
        }

        /// <summary>
        /// Adds or Updates an InventoryAsset entity
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="202">Returns a response object containing the newly added or updated item</response>
        [ProducesResponseType(202, Type = typeof(DataItemResponse<InventoryAsset>))]
        [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin)]
        [HttpPost]
        public ActionResult<DataItemResponse<InventoryAsset>> Post([FromBody] DataItemRequest<InventoryAsset> request)
        {
            Logger.LogInformation("Post action called");
            return Accepted(Context.AddUpdate(request, InventoryAssetIndex));
        }

        /// <summary>
        /// Deletes an InventoryAsset entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin)]
        [HttpDelete("{id}")]
        public ActionResult<NoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete action called for id {id}", id);
            return Ok(Context.Delete<InventoryAsset>(id,  InventoryAssetIndex));
        }

        /// <summary>
        /// Add Dirty InventoryAsset entity
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns a response object containing the newly added or updated item</response>
        [ProducesResponseType(200, Type = typeof(DataItemResponse<InventoryAsset>))]
        [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin)]
        [HttpPost("[action]")]
        public ActionResult<DataItemResponse<InventoryAsset>> Dirty([FromBody] DataItemRequest<InventoryAsset> request)
        {
            Logger.LogInformation("Dirty action called");
            return Ok(Context.AddDirty(request));
        }

        /// <summary>
        /// Refresh InventoryAsset entities by SourceType
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin)]
        [HttpPost("[action]/{sourceType}")]
        public ActionResult<NoDataResponse> Refresh(string sourceType)
        {
            Logger.LogInformation("Dirty action called");
            return Ok(Context.Refresh(sourceType));
        }
    }
}
