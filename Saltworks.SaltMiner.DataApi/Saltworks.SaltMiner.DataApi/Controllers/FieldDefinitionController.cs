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
[Auth(Role.ServiceManager, Role.Pentester, Role.Admin)]
[ApiController]
public class FieldDefinitionController(FieldDefinitionContext context, ILogger<FieldDefinitionController> logger) : ApiControllerBase(context, logger)
{
    private FieldDefinitionContext Context => ContextBase as FieldDefinitionContext;
    private readonly string FieldDefinitionIndex = FieldDefinition.GenerateIndex();

    /// <summary>
    /// Returns a list of field definitions
    /// </summary>
    /// <returns>The list inside a response object</returns>
    /// <response code="200">Returns a batch from a search request</response>
    [ProducesResponseType(200, Type = typeof(DataResponse<FieldDefinition>))]
    [HttpPost("[action]")]
    public ActionResult<DataResponse<FieldDefinition>> Search([FromBody] SearchRequest search)
    {
        Logger.LogInformation("Search action called");

        return Ok(Context.Search<FieldDefinition>(FieldDefinitionIndex, search));
    }

    /// <summary>
    /// Returns a single Field Definition
    /// </summary>
    /// <returns>The item inside a response object</returns>
    /// <response code="200">Returns the requested object</response>
    [ProducesResponseType(200, Type = typeof(DataResponse<FieldDefinition>))]
    [HttpGet("{id}")]
    public ActionResult<DataItemResponse<FieldDefinition>> Get(string id)
    {
        Logger.LogInformation("Get action called");


        return Ok(Context.Get<FieldDefinition>(id, FieldDefinitionIndex));
    }

    /// <summary>
    /// Returns a multiple Field Definitions by entity
    /// </summary>
    /// <returns>The item inside a response object</returns>
    /// <response code="200">Returns the requested object</response>
    [ProducesResponseType(200, Type = typeof(DataResponse<FieldDefinition>))]
    [HttpGet("entity/{entity}")]
    public ActionResult<DataItemResponse<FieldDefinition>> GetFieldDefinitionsByEntity(string entity)
    {
        Logger.LogInformation("Get action called");


        return Ok(Context.GetFieldDefinitionsByType(entity));
    }

    /// <summary>
    /// Adds or Updates a Field Definition
    /// </summary>
    /// <returns>The updated entity</returns>
    /// <response code="202">Returns a response object containing the updated entity</response>
    [ProducesResponseType(202, Type = typeof(DataItemResponse<FieldDefinition>))]
    [HttpPost]
    public ActionResult<DataItemResponse<FieldDefinition>> Post([FromBody] DataItemRequest<FieldDefinition> request)
    {
        Logger.LogInformation("Post action called");

        return Accepted(Context.AddUpdate(request, FieldDefinitionIndex));
    } 

    /// <summary>
    /// Deletes a Field Definition
    /// </summary>
    /// <returns>Non data response</returns>
    /// <response code="200">Returns response indicating success</response>
    [ProducesResponseType(200, Type = typeof(NoDataResponse))]
    [HttpDelete("{id}")]
    public ActionResult<NoDataResponse> Delete(string id)
    {
        Logger.LogInformation("Delete action called");

        return Ok(Context.Delete<FieldDefinition>(id, FieldDefinitionIndex));
    }
}
