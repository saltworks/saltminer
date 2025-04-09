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
    /// Supports operations with QueueIssues
    /// </summary>
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth]
    [ApiController]
    public class QueueIssueController : ApiControllerBase
    {
        private QueueIssueContext Context => ContextBase as QueueIssueContext;
        private string QueueIssueIndex = QueueIssue.GenerateIndex();

        public QueueIssueController(QueueIssueContext context, ILogger<QueueIssueController> logger) : base(context, logger)
        {
        }

        /// <summary>
        /// Adds/Updates one or more QueueIssue(s)
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk")]
        public ActionResult<BulkResponse> AddUpdateBulk([FromBody] DataRequest<QueueIssue> request)
        {
            Logger.LogInformation("Add/Update Bulk action called");

            return Accepted(Context.AddUpdateBulk(request));
        }

        /// <summary>
        /// Updates one or more QueueIssue using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<QueueIssue> request)
        {
            Logger.LogInformation("Update By Query action called");

            return Accepted(Context.UpdateByQuery(request, QueueIssue.GenerateIndex()));
        }

        /// <summary>
        /// Adds or Updates one
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(NoDataResponse))]
        [HttpPost]
        public ActionResult<DataItemResponse<QueueIssue>> Post([FromBody] DataItemRequest<QueueIssue> request)
        {
            Logger.LogInformation("Post action called");

            return Accepted(Context.AddUpdate(request));
        }

        /// <summary>
        /// Deletes QueueIssue by Id
        /// </summary>
        /// <param name="id">Identifier of the target entity</param>
        /// <returns>Response object indicating success</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpDelete("{id}")]
        public ActionResult<NoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete action called for id {id}", id);

            return Ok(Context.Delete(id));
        }

        /// <summary>
        /// Count of Issues by ScanId
        /// </summary>
        /// <returns>Count</returns>
        /// <response code="200">Returns a response object indicating success and count</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpGet("queuescan/{id}/count")]
        public ActionResult<NoDataResponse> IssueCountByQueueScanId(string id)
        {
            Logger.LogInformation("Issue count action called for queue scan id {id}", id);

            return Ok(Context.CountByQueueScanId(id));
        }

        /// <summary>
        /// Returns a list of QueueIssues
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<QueueIssue>))]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<QueueIssue>> Search([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search action called");

            return Ok(Context.Search<QueueIssue>(search, QueueIssueIndex));
        }

        /// <summary>
        /// Returns a QueueIssue given id
        /// </summary>
        /// <returns>The response object</returns>
        [ProducesResponseType(200, Type = typeof(DataItemResponse<QueueIssue>))]
        [HttpGet("{id}")]
        public ActionResult<DataItemResponse<QueueIssue>> Get(string id)
        {
            Logger.LogInformation("Get action called for queue issue id {id}", id);
            return Ok(Context.Get<QueueIssue>(id, QueueIssueIndex));
        }

        /// <summary>
        /// Returns an Queue Issue by ID and Locks
        /// </summary>
        /// <returns>The issue inside a response object</returns>
        /// <response code="200">Returns the issue inside a response object</response>
        /// <remarks>Use search to get by ScanId</remarks>
        [ProducesResponseType(200, Type = typeof(DataItemResponse<QueueIssue>))]
        [HttpGet("{id}/lock/{userName}")]
        public ActionResult<DataItemResponse<QueueIssue>> GetAndLock(string id, string userName)
        {
            Logger.LogInformation("Get and Lock action called for id {id} for user {userName}", id, userName);

            return Ok(Context.GetAndLock(id, userName));
        }

        /// <summary>
        /// Refresh an Issue Lock
        /// </summary>
        /// <returns>The issue inside a response object</returns>
        /// <response code="200">Returns the issue inside a response object</response>
        /// <remarks>Use search to get by ScanId</remarks>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpGet("{id}/lock/{userName}/refresh")]
        public ActionResult RefreshLock(string id, string userName)
        {
            Logger.LogInformation("Get and Lock action called for id {id} for user {userName}", id, userName);
            
            RefreshLock(id, userName);

            return Ok();
        }
    }
}
