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
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.DataApi.Contexts;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth]
    [ApiController]
    public class RegisterController : ApiControllerBase
    {
        private readonly RegisterContext Context;
        public RegisterController(RegisterContext context, ILogger<RegisterController> logger) : base(context, logger)
        {
            Context = context;
        }

        /// <summary>
        /// Returns role of caller
        /// </summary>
        /// <returns>The response object will indicate success and message will contain the role</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpGet("[action]")]
        public ActionResult<NoDataResponse> Role()
        {
            Logger.LogDebug("Role action called");
            
            return Ok(Context.GetRole());
        }
    }
}
