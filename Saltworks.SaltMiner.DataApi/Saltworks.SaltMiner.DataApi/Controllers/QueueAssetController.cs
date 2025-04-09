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
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.DataApi.Contexts;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    /// <summary>
    /// Supports operations with QueueAssets
    /// </summary>
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth]
    [ApiController]
    public class QueueAssetController : ApiControllerBase
    {
        private QueueAssetContext Context => ContextBase as QueueAssetContext;
        private string QueueAssetIndex = QueueAsset.GenerateIndex();

        public QueueAssetController(ILogger<QueueAssetController> logger, QueueAssetContext context) : base(context, logger)
        {
        }

        /// <summary>
        /// Returns a QueueAsset given id
        /// </summary>
        /// <returns>The response object</returns>
        [ProducesResponseType(200, Type = typeof(DataItemResponse<QueueAsset>))]
        [HttpGet("{id}")]
        public ActionResult<DataItemResponse<QueueAsset>> Get(string id)
        {
            Logger.LogInformation("Get action called for queue asset id {id}", id);
            return Ok(Context.Get<QueueAsset>(id, QueueAssetIndex));
        }

        /// <summary>
        /// Returns a QueueAsset given a Source Type and SourceId
        /// </summary>
        /// <returns>The response object</returns>
        [ProducesResponseType(200, Type = typeof(DataResponse<QueueAsset>))]
        [HttpGet("{sourceType}/{sourceId}")]
        public ActionResult<DataItemResponse<QueueAsset>> Get(string sourceType, string sourceId)
        {
            Logger.LogInformation("Get action called for source type {sourceType} and source id {sourceId}", sourceType, sourceId);
            return Ok(Context.GetBySourceType(sourceType, sourceId));
        }

        /// <summary>
        /// Adds or Updates a QueueAsset
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(DataItemRequest<QueueAsset>))]
        [HttpPost]
        public ActionResult<DataItemResponse<QueueAsset>> Post([FromBody] DataItemRequest<QueueAsset> request)
        {
            Logger.LogInformation("Post action called");
            return Accepted(Context.AddUpdate(request));
        }

        /// <summary>
        /// Adds/Updates one or more QueueAsset(s)
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk")]
        public ActionResult<BulkResponse> AddUpdateBulk([FromBody] DataRequest<QueueAsset> request)
        {
            Logger.LogInformation("Add/Update Bulk action called");
            return Accepted(Context.AddUpdateBulk(request));
        }

        /// <summary>
        /// Updates one or more QueueAsset(s) using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<QueueAsset> request)
        {
            Logger.LogInformation("Update By Query action called");
            return Accepted(Context.UpdateByQuery(request, QueueAssetIndex));
        }

        /// <summary>
        /// Deletes an QueueSAsset entity by Id
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpDelete("{id}")]
        public ActionResult<NoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete action called for id {id}", id);
            return Ok(Context.Delete<QueueAsset>(id, QueueAssetIndex));
        }

        /// <summary>
        /// Returns a list of Assets
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<QueueAsset>))]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<QueueAsset>> Search([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search action called");

            return Ok(Context.Search<QueueAsset>(search, QueueAssetIndex));
        }
    }
}
