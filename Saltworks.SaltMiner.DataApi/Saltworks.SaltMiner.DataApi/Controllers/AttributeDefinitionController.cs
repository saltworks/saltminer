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
    public class AttributeDefinitionController : ApiControllerBase
    {
        private AttributeDefinitionContext Context => ContextBase as AttributeDefinitionContext;
        private string AttributeDefinitionIndex = AttributeDefinition.GenerateIndex();

        public AttributeDefinitionController(AttributeDefinitionContext context, ILogger<AttributeDefinitionController> logger) : base(context, logger)
        {
        }

        /// <summary>
        /// Updates one or more AttributeDefinition(s) using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<AttributeDefinition> request)
        {
            Logger.LogInformation("Update By Query action called");
            return Accepted(Context.UpdateByQuery(request, AttributeDefinitionIndex));
        }

        /// <summary>
        /// Returns a single Attribute Definition
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<AttributeDefinition>))]
        [HttpGet("{id}")]
        public ActionResult<DataItemResponse<AttributeDefinition>> Get(string id)
        {
            Logger.LogInformation("Get action called for id '{id}'", id);


            return Ok(Context.Get<AttributeDefinition>(id, AttributeDefinitionIndex));
        }

        /// <summary>
        /// Returns a list of Attribute Definitions
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<AttributeDefinition>))]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<AttributeDefinition>> Search([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search action called");

            return Ok(Context.Search<AttributeDefinition>(search, AttributeDefinitionIndex));
        }

        /// <summary>
        /// Adds or Updates an Attribute Definition
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(DataItemResponse<Lookup>))]
        [HttpPost]
        [Auth(Role.Admin, Role.Pentester)]
        public ActionResult<DataItemResponse<AttributeDefinition>> Post([FromBody] DataItemRequest<AttributeDefinition> request)
        {
            Logger.LogInformation("Post action called");

            return Accepted(Context.AddUpdate(request, AttributeDefinitionIndex));
        }

        /// <summary>
        /// Deletes an Attribute Definition entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpDelete("{id}")]
        [Auth(Role.Admin)]
        public ActionResult<NoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete action called for id '{id}'", id);

            return Ok(Context.Delete<AttributeDefinition>(id, AttributeDefinitionIndex));
        }
    }
}
