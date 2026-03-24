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
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.DataApi.Contexts;

namespace Saltworks.SaltMiner.DataApi.Controllers;

[Route("[controller]")]
[Produces("application/json")]
[Auth]
[ApiController]
public class CustomImporterController(ILogger<CustomImporterController> logger, CustomImporterContext context) : ApiControllerBase(context, logger)
{
    private CustomImporterContext Context => ContextBase as CustomImporterContext;
    private string CustomImporterIndex = CustomImporter.GenerateIndex();

    /// <summary>
    /// Returns a list of CustomImporters
    /// </summary>
    /// <returns>The list inside a response object</returns>
    /// <response code="200">Returns a batch from a search request</response>
    [ProducesResponseType(200, Type = typeof(DataResponse<CustomImporter>))]
    [HttpPost("[action]")]
    public ActionResult<DataResponse<CustomImporter>> Search([FromBody] SearchRequest search)
    {
        Logger.LogInformation("Search action called");

        return Ok(Context.Search<CustomImporter>(CustomImporterIndex, search));
    }

    /// <summary>
    /// Returns a CustomImporter by ID
    /// </summary>
    /// <returns>The CustomImporter inside a response object</returns>
    /// <response code="200">Returns the CustomImporter inside a response object</response>
    [ProducesResponseType(200, Type = typeof(DataItemResponse<CustomImporter>))]
    [HttpGet("{id}")]
    public ActionResult<DataItemResponse<CustomImporter>> Get(string id)
    {
        Logger.LogInformation("Get action called for id {id}", id);

        return Ok(Context.Get<CustomImporter>(id, CustomImporter.GenerateIndex()));
    }

    /// <summary>
    /// Deletes an CustomImporter entity by Id
    /// </summary>
    /// <returns>Non data response</returns>
    /// <response code="200">Returns response indicating success</response>
    [ProducesResponseType(200, Type = typeof(NoDataResponse))]
    [HttpDelete("{id}")]
    public ActionResult<NoDataResponse> Delete(string id)
    {
        Logger.LogInformation("Delete action called for id {id}", id);

        return Ok(Context.Delete<CustomImporter>(id, CustomImporterIndex));
    }

    /// <summary>
    /// Adds or Updates a CustomImporter - only include one in Documents
    /// </summary>
    /// <returns>Count of docs affected and success flag</returns>
    /// <response code="202">Returns a response object indicating success and count of affected docs</response>
    [ProducesResponseType(202, Type = typeof(DataItemRequest<CustomImporter>))]
    [HttpPost]
    public ActionResult<DataItemResponse<CustomImporter>> Post([FromBody] DataItemRequest<CustomImporter> request)
    {
        Logger.LogInformation("Post action called");

        return Accepted(Context.AddUpdate(request, CustomImporterIndex));
    }
}
