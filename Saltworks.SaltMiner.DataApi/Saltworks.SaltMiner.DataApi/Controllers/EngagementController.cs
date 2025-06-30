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
using System.Collections.Generic;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth]
    [ApiController]
    public class EngagementController : ApiControllerBase
    {
        private EngagementContext Context => ContextBase as EngagementContext;
        private readonly string EngagementIndex = Engagement.GenerateIndex();

        public EngagementController(EngagementContext context, ILogger<EngagementController> logger) : base(context, logger)
        {
        }

        /// <summary>
        /// Returns an Engagement by ID
        /// </summary>
        /// <returns>The engagement inside a response object</returns>
        /// <response code="200">Returns the engagement inside a response object</response>
        [ProducesResponseType(200, Type = typeof(DataItemResponse<Engagement>))]
        [Auth(Role.Manager, Role.Pentester, Role.PentesterViewer, Role.Admin, Role.JobManager)]
        [HttpGet("{id}")]
        public ActionResult<DataItemResponse<Engagement>> Get(string id)
        {
            Logger.LogInformation("Get action called for id {id}", id);

            return Ok(Context.Get<Engagement>(id, EngagementIndex));
        }

        /// <summary>
        /// Set Issues as Historical for all grouped engagements
        /// </summary>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin)]
        [HttpPost("{id}/group/{groupId}/historical")]
        public ActionResult<NoDataResponse> SetHistoricalIssues(string id, string groupId)
        {
            Logger.LogInformation("SetHistoricalIssues action called for id {id} and group id {groupId}", id, groupId);

            return Ok(Context.SetHistoricalIssues(id, groupId));
        }

        /// <summary>
        /// Unset Issues as Historical for engagement
        /// </summary>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin)]
        [HttpPost("{id}/remove-historical")]
        public ActionResult<NoDataResponse> UnSetHistoricalIssues(string id)
        {
            Logger.LogInformation("UnSetHistoricalIssues action called for id {id}", id);

            return Ok(Context.UnSetHistoricalIssues(id));
        }

        /// <summary>
        /// Returns a list of Engagements
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<Engagement>))]
        [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin, Role.JobManager)]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<Engagement>> Search([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search action called");
           
            return Ok(Context.Search<Engagement>(search, EngagementIndex));
        }

        /// <summary>
        /// Adds/Updates one or more Engagement
        /// </summary> 
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [Auth(Role.Pentester, Role.Admin)]
        [HttpPost("bulk")]
        public ActionResult<BulkResponse> AddUpdateBulk([FromBody] DataRequest<Engagement> request)
        {
            Logger.LogInformation("Add/Update action called");
            
            return Accepted(Context.AddUpdateBulk(request, EngagementIndex));
        }

        /// <summary>
        /// Updates one or more Engagement using update by query
        /// </summary> 
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [Auth(Role.Pentester, Role.Admin)]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<Engagement> request)
        {
            Logger.LogInformation("Update By Query action called");

            return Accepted(Context.UpdateByQuery(request, EngagementIndex));
        }

        /// <summary>
        /// Adds or Updates one Engagement
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(DataItemResponse<Engagement>))]
        [Auth(Role.Pentester, Role.Admin, Role.JobManager)]

        [HttpPost()]
        public ActionResult<DataItemResponse<Engagement>> Post([FromBody] DataItemRequest<Engagement> request)
        {
            Logger.LogInformation("Post action called");

            return Accepted(Context.AddUpdate(request, EngagementIndex));
        }

        /// <summary>
        /// Deletes an Engagement entity by id
        /// </summary>
        /// <param name="id">Target entity ID</param>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Pentester, Role.Admin, Role.JobManager)]
        [HttpDelete("{id}")]
        public ActionResult<NoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete action called for id '{id}'", id);

            return Ok(Context.Delete(id));
        }

        /// <summary>
        /// Deletes an Engagement entity by group id
        /// </summary>
        /// <param name="id">Target group ID</param>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Pentester, Role.Admin, Role.JobManager)]
        [HttpDelete("group/{id}")]
        public ActionResult<NoDataResponse> DeleteGroup(string id)
        {
            Logger.LogInformation("Delete action called for group id '{id}'", id);

            return Ok(Context.DeleteGroup(id));
        }

        /// <summary>
        /// Returns issue counts grouped by severity for engagement
        /// </summary>
        /// <param name="request">Request supports PagingInfo.Size, PagingInfo.ScrollKeys, and FilterMatches</param>
        /// <returns>A dictionary response of type string, double that represents the key values (joined with |) and counts</returns>
        [ProducesResponseType(200, Type = typeof(DataDictionaryResponse<string, long?>))]
        [HttpGet("{id}/{assetType}/{sourceType}/{instance}/issue/counts")]
        public ActionResult<DataItemResponse<Dictionary<string, long?>>> Counts(string id, string assetType, string sourceType, string instance)
        {
            Logger.LogInformation("Counts action called");

            return Ok(Context.IssueCounts(id, assetType, sourceType, instance));
        }

        /// <summary>
        /// Sets status for given id and new status value
        /// </summary>
        /// <param name="id">engagement ID</param>
        /// <param name="status">new status</param>
        /// <returns>Container indicating success or failure</returns>
        /// <remarks>
        /// This update uses locking to ensure consistency.  Status should be one of these values:  Draft, Queued, Processing, Published.
        /// </remarks>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpGet("[action]/{id}/{status}")]
        [Auth(Role.Pentester, Role.Admin, Role.Manager)]
        public ActionResult<NoDataResponse> Status(string id, string status)
        {
            Logger.LogInformation("Status action called for id '{id}' and status '{status}'", id, status);
            return Ok(Context.UpdateStatus(id, status));
        }
    }
}
