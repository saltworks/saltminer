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
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Contexts;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.DataApi.Authentication;

namespace Saltworks.SaltMiner.DataApi.Controllers;

[Route("[controller]")]
[Produces("application/json")]
[Auth]
[ApiController]
public class InventoryAssetController(InventoryAssetContext context, ILogger<InventoryAssetController> logger) : ApiControllerBase(context, logger)
{ 
    private InventoryAssetContext Context => ContextBase as InventoryAssetContext;
    private readonly string InventoryAssetIndex = InventoryAsset.GenerateIndex();

    /// <summary>
    /// Updates one or more InventoryAsset(s) using update by query
    /// </summary>
    /// <returns>Count of docs affected and success flag</returns>
    /// <response code="202">Returns a response object indicating success and count of affected docs</response>
    [ProducesResponseType(202, Type = typeof(BulkResponse))]
    [HttpPost("bulk/query")]
    public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<InventoryAsset> request)
    {
        Logger.LogInformation("Update By Query action called");
        return Accepted(Context.UpdateByQuery(request, InventoryAssetIndex));
    }

    /// <summary>
    /// Returns a single InventoryAsset
    /// </summary>
    /// <returns>The item inside a response object</returns>
    /// <response code="200">Returns the requested object</response>
    [ProducesResponseType(200, Type = typeof(DataResponse<InventoryAsset>))]
    [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin)]
    [HttpGet("{id}")]
    public ActionResult<DataItemResponse<InventoryAsset>> Get(string id)
    {
        Logger.LogInformation("Get action called for id {id}", id);
        return Ok(Context.Get<InventoryAsset>(id, InventoryAssetIndex));
    }

    /// <summary>
    /// Returns a single InventoryAsset
    /// </summary>
    /// <returns>The item inside a response object</returns>
    /// <response code="200">Returns the requested object</response>
    [ProducesResponseType(200, Type = typeof(DataResponse<InventoryAsset>))]
    [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin)]
    [HttpGet("key/{key}")]
    public ActionResult<DataItemResponse<InventoryAsset>> GetByKey(string key)
    {
        Logger.LogInformation("Get action called for inventory key {key}", key);
        return Ok(Context.GetByKey(key));
    }

    /// <summary>
    /// InventoryAsset search
    /// </summary>
    /// <returns>Matching docs and scroll info</returns>
    /// <response code="202">Returns a response object containing results and scroll info</response>
    [ProducesResponseType(202, Type = typeof(DataResponse<InventoryAsset>))]
    [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin)]
    [HttpPost("[action]")]
    public ActionResult<DataResponse<InventoryAsset>> Search([FromBody] SearchRequest request)
    {
        Logger.LogInformation("Post action called");
        return Ok(Context.Search<InventoryAsset>(InventoryAssetIndex, request));
    }

    /// <summary>
    /// Adds or Updates an InventoryAsset entity
    /// </summary>
    /// <returns>The item inside a response object</returns>
    /// <response code="202">Returns a response object containing the newly added or updated item</response>
    [ProducesResponseType(202, Type = typeof(DataItemResponse<InventoryAsset>))]
    [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin)]
    [HttpPost]
    public ActionResult<DataItemResponse<InventoryAsset>> Post([FromBody] DataItemRequest<InventoryAsset> request)
    {
        Logger.LogInformation("Post action called");
        return Accepted(Context.AddUpdate(request, InventoryAssetIndex));
    }

    /// <summary>
    /// Deletes an InventoryAsset entity
    /// </summary>
    /// <returns>Non data response</returns>
    /// <response code="200">Returns response indicating success</response>
    [ProducesResponseType(200, Type = typeof(NoDataResponse))]
    [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin)]
    [HttpDelete("{id}")]
    public ActionResult<NoDataResponse> Delete(string id)
    {
        Logger.LogInformation("Delete action called for id {id}", id);
        return Ok(Context.Delete<InventoryAsset>(id,  InventoryAssetIndex));
    }

    /// <summary>
    /// Add Dirty InventoryAsset entity
    /// </summary>
    /// <returns>The item inside a response object</returns>
    /// <response code="200">Returns a response object containing the newly added or updated item</response>
    [ProducesResponseType(200, Type = typeof(DataItemResponse<InventoryAsset>))]
    [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin)]
    [HttpPost("[action]")]
    public ActionResult<DataItemResponse<InventoryAsset>> Dirty([FromBody] DataItemRequest<InventoryAsset> request)
    {
        Logger.LogInformation("Dirty action called");
        return Ok(Context.AddDirty(request));
    }

    /// <summary>
    /// Refresh InventoryAsset entities by SourceType
    /// </summary>
    /// <returns>Non data response</returns>
    /// <response code="200">Returns response indicating success</response>
    [ProducesResponseType(200, Type = typeof(NoDataResponse))]
    [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin)]
    [HttpPost("[action]/{sourceType}")]
    public ActionResult<NoDataResponse> Refresh(string sourceType)
    {
        Logger.LogInformation("Dirty action called");
        return Ok(Context.Refresh(sourceType));
    }
}
