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
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.Core.Data;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth]
    [ApiController]
    public class QueueLogController : ApiControllerBase
    {
        private QueueLogContext Context => ContextBase as QueueLogContext;
        private string QueueLogIndex = QueueLog.GenerateIndex();

        public QueueLogController(QueueLogContext context, ILogger<QueueLogController> logger) : base(context, logger)
        {
        }

        /// <summary>
        /// Updates one or more QueueLog(s) using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<QueueLog> request)
        {
            Logger.LogInformation("Update By Query action called");
            return Accepted(Context.UpdateByQuery(request, QueueLogIndex));
        }

        /// <summary>
        /// Returns a list of log messages by query
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a list of log messages</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<QueueLog>))]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<QueueLog>> Search([FromBody] SearchRequest request)
        {
            Logger.LogInformation("Search action called");
            
            return Ok(Context.Search<QueueLog>(request, QueueLogIndex));
        }

        /// <summary>
        /// Returns a log message by id
        /// </summary>
        /// <returns>The response object</returns>
        /// <response code="200">Returns a log message by id</response>
        [ProducesResponseType(200, Type = typeof(DataItemResponse<QueueLog>))]
        [HttpGet("{id}")]
        public ActionResult<DataItemResponse<QueueLog>> Get(string id)
        {
            Logger.LogInformation("Get action called for id {id}", id);
           
            return Ok(Context.Get<QueueLog>(id, QueueLogIndex));
        }

        /// <summary>
        /// Adds or Updates a QueueLog message
        /// </summary>
        /// <returns>The updated item</returns>
        /// <response code="202">Returns a response object containing the added or updated item</response>
        [ProducesResponseType(202, Type = typeof(DataItemResponse<QueueLog>))]
        [HttpPost]
        public ActionResult<DataItemResponse<QueueLog>> Post([FromBody] DataItemRequest<QueueLog> request)
        {
            Logger.LogInformation("Post action called");
           
            return Accepted(Context.AddUpdate(request, QueueLogIndex));
        }

        /// <summary>
        /// Deletes an QueueLog entity by Id
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpDelete("{id}")]
        public ActionResult<NoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete action called for id {id}", id);
            
            return Ok(Context.Delete<QueueLog>(id, QueueLogIndex));
        }

        /// <summary>
        /// Get and Mark Read all UN-Read messages
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<QueueLog>))]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<QueueLog>> Read([FromQuery]bool leaveUnread = false)
        {
            Logger.LogInformation("Read action called");
            
            return Ok(Context.Read(leaveUnread));
        }
    }
}
