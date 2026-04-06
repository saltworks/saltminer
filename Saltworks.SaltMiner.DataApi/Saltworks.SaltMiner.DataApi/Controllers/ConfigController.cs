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
public class ConfigController(ConfigContext context, ILogger<ConfigController> logger) : ApiControllerBase(context, logger)
{
    private ConfigContext Context => ContextBase as ConfigContext;
    private readonly string ConfigIndex = Config.GenerateIndex();

    /// <summary>
    /// Returns a single config setting by ID
    /// </summary>
    /// <returns>The item inside a response object</returns>
    /// <response code="200">Returns the requested object</response>
    [ProducesResponseType(200, Type = typeof(DataItemResponse<Config>))]
    [HttpGet("{id}")]
    [Auth(Role.Admin, Role.Config)]
    public ActionResult<DataItemResponse<Config>> Get(string id)
    {
        Logger.LogInformation("Get action called for id '{Id}'", id);
        return Ok(Context.Get<Config>(id, ConfigIndex));
    }

    /// <summary>
    /// Returns config settings by section and optional subsection.
    /// </summary>
    /// <returns>The matching settings inside a response object</returns>
    /// <param name="section">The section to return</param>
    /// <param name="subsection">The optional subsection to return</param>
    /// <response code="200">Returns the requested data</response>
    [ProducesResponseType(200, Type = typeof(DataResponse<Config>))]
    [HttpGet("section/{section}/{subsection?}")]
    [Auth(Role.Admin)]
    public ActionResult<DataResponse<Config>> GetSection(string section, string subsection="")
    {
        Logger.LogInformation("GetSection action called");
        return Ok(Context.GetBySectionSubsection(section, subsection));
    }

    /// <summary>
    /// Adds or Updates a config setting
    /// </summary>
    /// <returns>The updated entity</returns>
    /// <response code="202">Returns a response object containing the updated entity</response>
    [ProducesResponseType(202, Type = typeof(DataItemResponse<Config>))]
    [HttpPost]
    [Auth(Role.Admin, Role.Config)]
    public ActionResult<DataItemResponse<Config>> Post([FromBody] DataItemRequest<Config> request)
    {
        Logger.LogInformation("Post action called");
        return Accepted(Context.AddUpdate(request, ConfigIndex));
    }

    /// <summary>
    /// Deletes an Config entity
    /// </summary>
    /// <returns>Non data response</returns>
    /// <response code="200">Returns response indicating success</response>
    [ProducesResponseType(200, Type = typeof(NoDataResponse))]
    [HttpDelete("{id}")]
    [Auth(Role.Admin)]
    public ActionResult<NoDataResponse> Delete(string id)
    {
        Logger.LogInformation("Delete action called for id '{Id}'", id);
        return Ok(Context.Delete<Config>(id, ConfigIndex));
    }
}
