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
using Saltworks.SaltMiner.DataApi.Contexts;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Authentication;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth]
    [ApiController]
    public class LookupController : ApiControllerBase
    {
        private LookupContext Context => ContextBase as LookupContext;
        private string LookupIndex = Lookup.GenerateIndex();

        public LookupController(LookupContext context, ILogger<LookupController> logger) : base(context, logger)
        {
        }

        /// <summary>
        /// Updates one or more Lookup(s) using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<Lookup> request)
        {
            Logger.LogInformation("Update By Query action called");
            return Accepted(Context.UpdateByQuery(request, LookupIndex));
        }

        /// <summary>
        /// Returns a single Lookup
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<Lookup>))]
        [HttpGet("{id}")]
        public ActionResult<DataItemResponse<Lookup>> Get(string id)
        {
            Logger.LogInformation("Get action called for id '{id}'", id);
            return Ok(Context.Get<Lookup>(id, LookupIndex));
        }

        /// <summary>
        /// Returns a list of Lookups
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<Lookup>))]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<Lookup>> Search([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search action called");
            return Ok(Context.Search<Lookup>(search, LookupIndex));
        }

        /// <summary>
        /// Adds or Updates an Lookup
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(DataItemResponse<Lookup>))]
        [HttpPost]
        [Auth(Role.Admin, Role.Pentester, Role.ServiceManager)]
        public ActionResult<DataItemResponse<Lookup>> Post([FromBody] DataItemRequest<Lookup> request)
        {
            Logger.LogInformation("Post action called");
            return Accepted(Context.AddUpdate(request, LookupIndex));
        } 

        /// <summary>
        /// Delete Lookups by Type
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(DataResponse<Lookup>))]
        [HttpDelete("bulk/{type}")]
        [Auth(Role.Admin, Role.Pentester)]
        public ActionResult<NoDataResponse> DeleteByType(string type)
        {
            Logger.LogInformation("Delete by type action called");
            return Accepted(Context.DeleteByType(type));
        }

        /// <summary>
        /// Deletes an Lookup entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpDelete("{id}")]
        [Auth(Role.Admin)]
        public ActionResult<NoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete action called for id '{id}'", id);
            return Ok(Context.Delete<Lookup>(id, LookupIndex));
        }
    }
}
