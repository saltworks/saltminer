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
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.DataApi.Contexts;


namespace Saltworks.SaltMiner.DataApi.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth(Role.ServiceManager, Role.Pentester, Role.Admin)]
    [ApiController]
    public class RoleController(RoleContext context, ILogger<RoleController> logger) : ApiControllerBase(context, logger)
    {
        private RoleContext Context => ContextBase as RoleContext;
        private readonly string RoleIndex = AppRole.GenerateIndex();

        /// <summary>
        /// Updates one or more Role(s) using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<AppRole> request)
        {
            Logger.LogInformation("Update By Query action called");
            return Accepted(Context.UpdateByQuery(request, RoleIndex));
        }

        /// <summary>
        /// Returns a single Application Role
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<AppRole>))]
        [HttpGet("{id}")]
        public ActionResult<DataItemResponse<AppRole>> Get(string id)
        {
            Logger.LogInformation("Get action called for id '{Id}'", id);
            return Ok(Context.Get<AppRole>(id, RoleIndex));
        }

        /// <summary>
        /// Returns Application Role list
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<AppRole>))]
        [HttpGet("[action]")]
        public ActionResult<DataItemResponse<AppRole>> List()
        {
            Logger.LogInformation("Get role list action called");
            return Ok(Context.Search<AppRole>(new(), RoleIndex));
        }

        /// <summary>
        /// Returns a list of Application Roles
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<AppRole>))]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<AppRole>> Search([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search action called");
            return Ok(Context.Search<AppRole>(search, RoleIndex));
        }

        /// <summary>
        /// Adds or Updates an Application Role, including creating the security role in Elasticsearch.
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(DataItemResponse<Lookup>))]
        [HttpPost]
        public ActionResult<DataItemResponse<AppRole>> Post([FromBody] DataItemRequest<AppRole> request)
        {
            Logger.LogInformation("Post action called");
            return Accepted(Context.AddUpdate(request));
        }

        /// <summary>
        /// Deletes an Application Role entity, including removing the security role in Elasticsearch.
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpDelete("{id}")]
        public ActionResult<NoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete action called for id '{Id}'", id);
            return Ok(Context.Delete(id));
        }
    }
}
