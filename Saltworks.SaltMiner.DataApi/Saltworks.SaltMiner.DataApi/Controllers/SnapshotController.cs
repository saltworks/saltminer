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
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth(Role.Admin, Role.Manager)]
    [ApiController]
    public class SnapshotController : ApiControllerBase
    {
        private SnapshotContext Context => ContextBase as SnapshotContext;

        public SnapshotController(SnapshotContext context, ILogger<SnapshotController> logger) : base(context, logger)
        {
        }

        /// <summary>
        /// Returns a list of Snapshots by search request
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<Snapshot>))]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<Snapshot>> Search([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search action called");
            
            return Ok(Context.Search<Snapshot>(search, SearchFilter.GenerateIndex()));
        }

        /// <summary>
        /// Adds or Updates a Snapshot Monthly
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(DataItemResponse<Snapshot>))]
        [HttpPost("monthly")]
        public ActionResult<DataItemResponse<Snapshot>> Monthly([FromBody] DataItemRequest<Snapshot> request)
        {
            Logger.LogInformation("Post action called");

            return Accepted(Context.AddUpdate(request));
        }

        /// <summary>
        /// Adds or Updates a Snapshot Daily
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(DataItemResponse<Snapshot>))]
        [HttpPost("daily")]
        public ActionResult<DataItemResponse<Snapshot>> Daily([FromBody] DataItemRequest<Snapshot> request)
        {
            Logger.LogInformation("Post action called");

            return Accepted(Context.AddUpdate(request, true));
        }

        /// <summary>
        /// Updates one or more Snapshot(s) using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<Snapshot> request)
        {
            Logger.LogInformation("Update By Query action called");
            return Accepted(Context.UpdateByQuery(request, Snapshot.GenerateIndex(request.AssetType)));
        }

        /// <summary>
        /// Adds./Updates a Snapshot Daily Batch
        /// </summary>
        /// <returns>The updated entities</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("daily/bulk")]
        public ActionResult<BulkResponse> AddUpdateBulkDaily([FromBody] DataRequest<Snapshot> request)
        {
            Logger.LogInformation("Add/Update Bulk Daily action called");

            return Accepted(Context.AddUpdateBulk(request, true));
        }

        /// <summary>
        /// Adds/Updates a Snapshot Monthly batch
        /// </summary>
        /// <returns>The updated entities</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("monthly/bulk")]
        public ActionResult<BulkResponse> AddUpdateBulkMonthly([FromBody] DataRequest<Snapshot> request)
        {
            Logger.LogInformation("Add/Update Bulk Monthly action called");

            return Accepted(Context.AddUpdateBulk(request));
        }

        /// <summary>
        /// Deletes AppVersionSnapshots by query
        /// </summary>
        /// <param name="request">Search criteria for the target docs</param>
        /// <returns>A response object indicating success and affected records</returns>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Admin, Role.Manager, Role.Pentester)]
        [HttpDelete()]
        public ActionResult<NoDataResponse> Delete([FromBody] SearchRequest request)
        {
            Logger.LogInformation("Delete action called");
            
            return Ok(Context.DeleteByQuery(request));
        }

        /// <summary>
        /// Returns issue counts grouped by asset type, source type, source_id, and vulnerability name
        /// </summary>
        /// <param name="request">Request supports PagingInfo.Size, PagingInfo.ScrollKeys, and FilterMatches</param>
        /// <returns>A dictionary response of type string, double that represents the key values (joined with |) and counts</returns>
        [ProducesResponseType(200, Type = typeof(DataDictionaryResponse<string, double?>))]
        [HttpPost("[action]")]
        public ActionResult<DataDictionaryResponse<string, Dictionary<string, double?>>> Counts([FromBody] SearchRequest request)
        {
            Logger.LogInformation("Counts action called");
            
            return Ok(Context.SnapshotAggregates(request));
        }
    }
}
