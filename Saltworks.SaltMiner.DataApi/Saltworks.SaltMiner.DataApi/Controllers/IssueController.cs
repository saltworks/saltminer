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
    public class IssueController : ApiControllerBase
    {
        private IssueContext Context => ContextBase as IssueContext;

        public IssueController(IssueContext context, ILogger<IssueController> logger) : base(context, logger)
        {
        }

        /// <summary>
        /// Returns an Issue by ID
        /// </summary>
        /// <returns>The issue inside a response object</returns>
        /// <response code="200">Returns the issue inside a response object</response>
        /// <remarks>Use search to get by ScanId</remarks>
        [ProducesResponseType(200, Type = typeof(DataItemResponse<Issue>))]
        [Auth(Role.Manager, Role.Admin, Role.Pentester, Role.PentesterViewer)]
        [HttpGet("{id}/{assetType}/{sourceType}/{instance}")]
        public ActionResult<DataItemResponse<Issue>> Get(string id, string assetType, string sourceType, string instance)
        {
            Logger.LogInformation("Get action called for id '{Id}', asset type '{AssetType}', and source type '{SourceType}', and instance '{Instance}'", id, assetType, sourceType, instance);

            return Ok(Context.Get<Issue>(id, Issue.GenerateIndex(assetType, sourceType, instance)));
        }

        /// <summary>
        /// Returns a list of Issues
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        // Agent needs access to this so Wiz adapter (and possibly others later) can pull existing issues
        [ProducesResponseType(200, Type = typeof(DataResponse<Issue>))]
        [Auth(Role.Manager, Role.Admin, Role.Pentester, Role.PentesterViewer, Role.Agent)]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<Issue>> Search([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search action called");
           
            return Ok(Context.Search<Issue>(search, Issue.GenerateIndex(search.AssetType, search.SourceType, search.Instance)));
        }

        /// <summary>
        /// Adds/Updates one or more Issue
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [Auth(Role.Manager, Role.Admin)]
        [HttpPost("bulk")]
        public ActionResult<BulkResponse> AddUpdateBulk([FromBody] DataRequest<Issue> request)
        {
            Logger.LogInformation("Add/Update Bulk action called");
            
            return Accepted(Context.AddUpdateBulk(request));
        }

        /// <summary>
        /// Updates one or more Issue using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [Auth(Role.Manager, Role.Admin)]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<Issue> request)
        {
            Logger.LogInformation("Update By Query action called");

            return Accepted(Context.UpdateByQuery(request, Issue.GenerateIndex()));
        }

        /// <summary>
        /// Deletes an Issue entity by id
        /// </summary>
        /// <param name="id">Target entity ID</param>
        /// <param name="assetType">Target entity AssetType</param>
        /// <param name="sourceType">Target entity Source Type</param>
        /// <param name="instance">Target entity Instanceco</param>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Manager, Role.Admin)]
        [HttpDelete("{id}/{assetType}/{sourceType}/{instance}")]
        public ActionResult<NoDataResponse> Delete(string id, string assetType, string sourceType, string instance)
        {
            Logger.LogInformation("Delete action called for id '{Id}', type '{AssetType}', and source type '{SourceType}', and instance '{Instance}'", id, assetType, sourceType, instance);

            return Ok(Context.Delete<Issue>(id, Issue.GenerateIndex(assetType, sourceType, instance)));
        }



        /// <summary>
        /// Deletes an Issue entity by scan id
        /// </summary>
        /// <param name="id">Target scan ID</param>
        /// <param name="assetType">Target entity AssetType</param>
        /// <param name="sourceType">Target entity Source Type</param>
        /// <param name="instance">Target entity Instance</param>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Manager, Role.Admin)]
        [HttpDelete("scan/{id}/{assetType}/{sourceType}/{instance}")]
        public ActionResult<NoDataResponse> DeleteByScan(string id, string assetType, string sourceType, string instance)
        {
            Logger.LogInformation("Delete Scan action called for id '{Id}', type '{AssetType}', and source type '{SourceType}', and instance '{Instance}'", id, assetType, sourceType, instance);

            return Ok(Context.DeleteByScan(id, assetType, sourceType, instance));
        }


        /// <summary>
        /// Deletes issues by source id, source type, instance, and assessment type
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpDelete("all/{sourceId}/{assetType}/{sourceType}/{instance}/{assessmentType}")]
        public ActionResult<NoDataResponse> DeleteAllBySourceId(string sourceId, string assetType, string sourceType, string instance, string assessmentType)
        {
            Logger.LogInformation("Delete All action called for id {Id}", sourceId);

            return Ok(Context.DeleteAllBySourceId(sourceId, assetType, sourceType, instance, assessmentType));
        }

        /// <summary>
        /// Deletes issues by engagement id, source type, instance, and asset type and ALL associated scans and assets
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpDelete("engagement/{engagementId}/{assetType}/{sourceType}/{instance}")]
        public ActionResult<NoDataResponse> DeleteAllByEngagementId(string engagementId, string assetType, string sourceType, string instance)
        {
            Logger.LogInformation("Delete all by engagement action called for id {Id}", engagementId);

            return Ok(Context.DeleteAllByEngagementId(engagementId, assetType, sourceType, instance));
        }
    }
}
