/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-04-09
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿using Microsoft.AspNetCore.Mvc;
using Saltworks.SaltMiner.Ui.Api.Authentication;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Responses;

namespace Saltworks.SaltMiner.Ui.Api.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class UtilityController(UtilityContext context, ILogger<UtilityController> logger) : ApiControllerBase(context, logger)
    {
        private readonly UtilityContext Context = context;

        /// <summary>
        /// Create complete backup of Elastic and zip the data to file
        /// </summary>
        /// <returns>Zip file of Elastic backup/snapshot data</returns>
        [Authorize(SysRole.SuperUser)]
        [HttpPost("Backup/Create")]
        public ActionResult CreateBackup()
        {
            Logger.LogInformation("CreateBackup action called");

            var response = Context.CreateBackup();
            var content = response.Result.GetContentAsync().Result;

            return File(content, "application/octet-stream", "elastic-backup.zip");
        }

        /// <summary>
        /// Restores complete backup of Elastic
        /// </summary>
        /// <returns>UiNoDataResponse with boolean indicating success</returns>
        [Authorize(SysRole.SuperUser)]
        [HttpPost("Backup/Restore")]
        public ActionResult<UiNoDataResponse> RestoreBackup(IFormFile file)
        {
            Logger.LogInformation("RestoreBackup action called");

            if (file != null && file.Length > 0)
            {
                if (file.FileName.EndsWith(".zip"))
                {
                    return Ok(Context.RestoreBackup(file));
                }

                throw new UiApiClientValidationException("Invalid File Type");
            }

            throw new UiApiClientValidationException("No File Attached");
        }

        /// <summary>
        /// Returns API version
        /// </summary>
        [HttpGet("[action]")]
        [Authorize(SysRole.SuperUser, SysRole.PentestAdmin, SysRole.Pentester)]
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        public ActionResult<UiNoDataResponse> Version()
        {
            Logger.LogInformation("Version action called");
            return Ok(Context.Version());
        }

        /// <summary>
        /// Returns "up" response
        /// </summary>
        [HttpGet("[action]")]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        public ActionResult<UiNoDataResponse> Status()
        {
            Logger.LogInformation("Status action called");
            return Ok(new UiNoDataResponse(0, "Up"));
        }

        /// <summary>
        /// Returns test of input string against validation
        /// </summary>
        [HttpPost("[action]")]
        [Authorize(SysRole.SuperUser, SysRole.PentestAdmin, SysRole.Pentester)]
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        public ActionResult<UiNoDataResponse> TextValidation([FromBody] TextValidation textToTest)
        {
            Logger.LogInformation("TextValidation action called");
            return Ok(Context.TextValidation(textToTest));
        }
    }
}
