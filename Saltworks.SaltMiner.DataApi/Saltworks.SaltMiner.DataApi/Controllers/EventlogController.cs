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
public class EventlogController(ILogger<EventlogController> logger, EventlogContext context) : ApiControllerBase(context, logger)
{
    private EventlogContext Context => ContextBase as EventlogContext;
    private static string EventIndex() => Eventlog.GenerateIndex();

    /// <summary>
    /// Adds or Updates an Event
    /// </summary>
    /// <returns>Count of docs affected and success flag</returns>
    /// <response code="202">Returns a response object indicating success and count of affected docs</response>
    [ProducesResponseType(202, Type = typeof(DataItemRequest<Eventlog>))]
    [HttpPost]
    public ActionResult<DataItemResponse<Eventlog>> Post([FromBody] DataItemRequest<Eventlog> request)
    {
        Logger.LogInformation("Post action called");

        var rsp = Context.AddUpdate(request, EventIndex());
        if (rsp.Success)
            return Accepted(rsp);
        else
            return StatusCode(rsp.StatusCode, rsp);
    }
}
