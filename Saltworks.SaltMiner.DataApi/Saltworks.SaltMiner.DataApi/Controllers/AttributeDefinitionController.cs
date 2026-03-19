/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
*
* ----
*/

﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.DataApi.Contexts;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Authentication;

namespace Saltworks.SaltMiner.DataApi.Controllers;

[Route("[controller]")]
[Produces("application/json")]
[Auth]
[ApiController]
public class AttributeDefinitionController(AttributeDefinitionContext context, ILogger<AttributeDefinitionController> logger) : ApiControllerBase(context, logger)
{
    private AttributeDefinitionContext Context => ContextBase as AttributeDefinitionContext;
    private readonly string AttributeDefinitionIndex = AttributeDefinition.GenerateIndex();

    /// <summary>
    /// Updates one or more AttributeDefinition(s) using update by query
    /// </summary>
    /// <returns>Count of docs affected and success flag</returns>
    /// <response code="202">Returns a response object indicating success and count of affected docs</response>
    [Auth(Role.Admin, Role.Pentester)]
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
        return Ok(Context.Search<AttributeDefinition>(AttributeDefinitionIndex, search));
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
