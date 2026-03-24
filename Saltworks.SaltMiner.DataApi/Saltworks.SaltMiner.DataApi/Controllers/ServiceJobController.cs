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
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.DataApi.Controllers;

[Route("[controller]")]
[Produces("application/json")]
[Auth(Role.ServiceManager, Role.Pentester, Role.Admin)]
[ApiController]
public class ServiceJobController(ServiceJobContext context, ILogger<ServiceJobController> logger) : ApiControllerBase(context, logger)
{
    private ServiceJobContext Context => ContextBase as ServiceJobContext;
    private readonly string ServiceJobIndex = ServiceJob.GenerateIndex();

    /// <summary>
    /// Returns a list of service jobs
    /// </summary>
    /// <returns>The list inside a response object</returns>
    /// <response code="200">Returns a batch from a search request</response>
    [ProducesResponseType(200, Type = typeof(DataResponse<ServiceJob>))]
    [HttpPost("[action]")]
    public ActionResult<DataResponse<ServiceJob>> Search([FromBody] SearchRequest search)
    {
        Logger.LogInformation("Search action called");

        return Ok(Context.Search<ServiceJob>(ServiceJobIndex, search));
    }

    /// <summary>
    /// Adds or Updates a Service Job
    /// </summary>
    /// <returns>The updated entity</returns>
    /// <response code="202">Returns a response object containing the updated entity</response>
    [ProducesResponseType(202, Type = typeof(DataItemResponse<ServiceJob>))]
    [HttpPost]
    public ActionResult<DataItemResponse<ServiceJob>> Post([FromBody] DataItemRequest<ServiceJob> request)
    {
        Logger.LogInformation("Post action called");

        var rsp = Context.AddUpdate(request, ServiceJobIndex);
        if (rsp.Success)
            return Accepted(rsp);
        else
            return StatusCode(rsp.StatusCode, rsp);
    }

    /// <summary>
    /// Deletes a Service Job entity
    /// </summary>
    /// <returns>Non data response</returns>
    /// <response code="200">Returns response indicating success</response>
    [ProducesResponseType(200, Type = typeof(NoDataResponse))]
    [HttpDelete("{id}")]
    public ActionResult<NoDataResponse> Delete(string id)
    {
        Logger.LogInformation("Delete action called for id '{Id}'", id);

        return Ok(Context.Delete<ServiceJob>(id, ServiceJobIndex));
    }

    /// <summary>
    /// Returns a single Service Job
    /// </summary>
    /// <returns>The item inside a response object</returns>
    /// <response code="200">Returns the requested object</response>
    [ProducesResponseType(200, Type = typeof(DataResponse<ServiceJob>))]
    [HttpGet("{id}")]
    public ActionResult<DataItemResponse<ServiceJob>> Get(string id)
    {
        Logger.LogInformation("Get action called for service job id '{Id}'", id);


        return Ok(Context.Get<ServiceJob>(id, ServiceJobIndex));
    }
}
