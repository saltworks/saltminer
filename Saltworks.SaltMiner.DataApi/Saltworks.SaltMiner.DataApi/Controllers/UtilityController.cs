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
using Microsoft.AspNetCore.Http;
using System.Text.Json.Nodes;
using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.DataApi.Controllers;

[Route("[controller]")]
[Produces("application/json")]
[Auth]
[ApiController]
public class UtilityController(UtilityContext context, ILogger<UtilityController> logger) : ApiControllerBase(context, logger)
{
    private UtilityContext Context => ContextBase as UtilityContext;

    [HttpGet("[action]/{value}")]
    [Auth(Role.Admin)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<NoDataResponse> Encrypt(string value)
    {
        Logger.LogInformation("Encrypt action called");
        return Ok(Context.Encrypt(value));
    }

    /// <summary>
    /// Returns API version
    /// </summary>
    [HttpGet("[action]")]
    [ProducesResponseType(200, Type = typeof(NoDataResponse))]
    public ActionResult<NoDataResponse> Version()
    {
        Logger.LogInformation("Version action called");
        return Ok(Context.Version());
    }

    /// <summary>
    /// Returns cluster task count
    /// </summary>
    [HttpGet("[action]")]
    [ProducesResponseType(200, Type = typeof(NoDataResponse))]
    public ActionResult<NoDataResponse> Tasks()
    {
        Logger.LogInformation("Tasks action called");
        return Ok(Context.Version());
    }

    /// <summary>
    /// Create complete backup of Elastic and zip to file
    /// </summary>
    /// <returns>Zip file path of the backup data</returns>
    [HttpGet("Backup/Create")]
    [Auth(Role.Admin)]
    public ActionResult CreateBackup()
    {
        var fileStream = Context.CreateBackup();
        return File(fileStream, "application/octet-stream", "elastic-backup.zip");
    }

    /// <summary>
    /// Restores complete backup of Elastic
    /// </summary>
    /// <returns>NoDataResponse with boolean indicating success</returns>
    [HttpPost("Backup/Restore")]
    [Auth(Role.Admin)]
    public ActionResult<NoDataResponse> RestoreBackup(IFormFile file)
    {
        return Ok(Context.RestoreBackup(file));
    }

    /// <summary>
    /// Receives new queue sync items (webhook events), storing them in a "queue" for later processing
    /// </summary>
    /// <param name="source">Webhook source key (must be configured)</param>
    [Consumes("application/json")]
    [HttpPost("Webhook/{source}")]
    public ActionResult<NoDataResponse> AddSyncQueueItem(string source)
    {
        return Ok(Context.AddQueueSyncItem(source, Request));
    }

    /// <summary>
    /// Retrieves webhook events, "deleting" (marking them as status deleted) them from the "queue" (webhook queue index)
    /// </summary>
    /// <param name="source">Webhook source to retrieve</param>
    /// <returns>Up to configured limit of unretrieved queue sync items (webhook events)</returns>
    [HttpGet("Webhook/{source}")]
    [Auth(Role.Admin, Role.Agent)]
    public ActionResult<DataResponse<QueueSyncItem>> GetSyncQueueItems(string source)
    {
        return Ok(Context.GetQueueSyncItems(source));
    }
}
