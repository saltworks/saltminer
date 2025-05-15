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
using System.Collections.Generic;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    /// <summary>
    /// Supports operations with QueueScans
    /// </summary>
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth]
    [ApiController]
    public class QueueScanController(ILogger<QueueScanController> logger, QueueScanContext context) : ApiControllerBase(context, logger)
    {
        private QueueScanContext Context => ContextBase as QueueScanContext;

        /// <summary>
        /// Adds or Updates a QueueScan - only include one in Documents
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(DataItemRequest<QueueScan>))]
        [HttpPost]
        public ActionResult<DataItemResponse<QueueScan>> Post([FromBody] DataItemRequest<QueueScan> request)
        {
            Logger.LogInformation("Post action called");

            return Accepted(Context.AddUpdate(request));
        }

        /// <summary>
        /// Adds/Updates one or more QueueScan(s)
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk")]
        public ActionResult<BulkResponse> AddUpdateBulk([FromBody] DataRequest<QueueScan> request)
        {
            Logger.LogInformation("Add/Update Bulk action called");
            return Accepted(Context.AddUpdateBulk(request));
        }

        /// <summary>
        /// Adds/Updates one or more QueueScan/QueueAsset/QueueIssue
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulkqueue")]
        public ActionResult<BulkResponse> BulkQueue([FromBody] QueueDataRequest request)
        {
            Logger.LogInformation("Add/Update Bulk action called");
            return Accepted(Context.AddUpdateQueueBulk(request));
        }


        /// <summary>
        /// Updates one or more QueueScan(s) using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<QueueScan> request)
        {
            Logger.LogInformation("Update By Query action called");
            return Accepted(Context.UpdateByQuery(request, QueueScan.GenerateIndex()));
        }

        /// <summary>
        /// Returns a list of QueueScans
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<QueueScan>))]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<QueueScan>> Search([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search action called");

            return Ok(Context.Search(search));
        }

        /// <summary>
        /// Returns a single QueueScan
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(DataItemResponse<QueueScan>))]
        [HttpGet("{Id}")]
        public ActionResult<DataItemResponse<QueueScan>> Get(string id)
        {
            Logger.LogInformation("Get action called");

            return Ok(Context.Get(id));
        }

        /// <summary>
        /// Returns a single QueueScan
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(DataItemResponse<QueueScan>))]
        [HttpGet("engagement/{Id}")]
        public ActionResult<DataItemResponse<QueueScan>> GetByEngagement(string id)
        {
            Logger.LogInformation("GetByEngagement action called");

            return Ok(Context.GetByEngagement(id));
        }

        /// <summary>
        /// Sets status for given id and new status value
        /// </summary>
        /// <param name="id">queue scan ID</param>
        /// <param name="status">new status</param>
        /// <param name="lockId">if included, lock this queue scan to the provided lock ID</param>
        /// <returns>Container indicating success or failure</returns>
        /// <remarks>
        /// This update uses locking to ensure consistency.  Status should be one of these values:  Loading, Pending, Processing, Complete, Cancel, Error.
        /// State transition rules will be enforced, for example Agent should only use Loading/Pending/Cancel.
        /// </remarks>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpGet("[action]/{Id}/{status}")]
        public ActionResult<NoDataResponse> Status(string id, string status, [FromQuery] string lockId = "")
        {
            Logger.LogInformation("Status action called for id '{Id}' and status '{Status}'", id, status);
            return Ok(Context.UpdateStatus(id, status, lockId));
        }

        /// <summary>
        /// Clears lock ID for all queue scans currently set for the given lock ID
        /// </summary>
        /// <param name="lockId">Lock ID to unlock</param>
        /// <returns>Response indicating success or failure and count of affected queue scans</returns>
        [Auth(Role.Admin, Role.Manager)]
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpPost("[action]/{lockId}")]
        public ActionResult<NoDataResponse> Unlock(string lockId)
        {
            Logger.LogInformation("Unlock action called for lock ID '{LockId}'", lockId);
            return Ok(Context.Unlock(lockId));
        }

        /// <summary>
        /// Deletes an QueueScan entity by Id
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpDelete("{Id}")]
        public ActionResult<NoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete action called for id {Id}", id);

            return Ok(Context.Delete(id));
        }

        /// <summary>
        /// Deletes a QueueScan entity by Id and all assets and issues associated with it.
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpDelete("all/{Id}")]
        public ActionResult<NoDataResponse> DeleteAll(string id)
        {
            Logger.LogInformation("DeleteAll action called for id {Id}", id);

            return Ok(Context.DeleteAllQueueByQueueScan(id));
        }

        /// <summary>
        /// Deletes multiple QueueScan entities by Id and all assets and issues associated with them.
        /// </summary>
        /// <param name="ids">List of queue scan IDs to remove</param>
        /// <returns>Non data response indicating the number of IDs attempted (results not guaranteed)</returns>
        /// <response code="200">Returns response indicating success</response>
        [Auth(Role.Admin, Role.Manager)]
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpPost("all/deletelist")]
        public ActionResult<NoDataResponse> DeleteAllByList([FromBody]List<string> ids)
        {
            Logger.LogInformation("DeleteAll action called for {Count} ids", ids.Count);

            return Ok(Context.DeleteAllQueueByQueueScan(ids));
        }

        /// <summary>
        /// Deletes multiple QueueScan entities by Id and all assets and issues associated with them.
        /// </summary>
        /// <param name="request">Search request that will work for all 3 queue entities</param>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpPost("all/delete")]
        public ActionResult<NoDataResponse> DeleteByQuery([FromBody]SearchRequest request)
        {
            Logger.LogInformation("DeleteByQuery action called");

            return Ok(Context.DeleteBulk(request));
        }
    }
}
