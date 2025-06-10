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
    public class RegisterController(RegisterContext context, ILogger<RegisterController> logger) : ApiControllerBase(context, logger)
    {
        private readonly RegisterContext Context = context;

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

        /// <summary>
        /// Returns a count of active manager instances
        /// </summary>
        /// <returns>The response object will indicate success and message will contain the count</returns>
        /// <response code="200">Returns the requested object</response>
        [Auth(Authentication.Role.Manager)]
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpGet("[action]")]
        public ActionResult<NoDataResponse> ManagerIdCount()
        {
            Logger.LogDebug("Manager ID count action called");
            return Ok(Context.GetMgrInstanceCount());
        }

        /// <summary>
        /// Returns new manager instance ID
        /// </summary>
        /// <returns>The response object will indicate success and message will contain the instance ID</returns>
        /// <response code="200">Returns the requested object</response>
        [Auth(Authentication.Role.Manager)]
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpGet("[action]")]
        public ActionResult<NoDataResponse> ManagerId()
        {
            Logger.LogDebug("Manager ID action called");
            return Ok(Context.NewMgrInstance());
        }

        /// <summary>
        /// Removes manager instance ID from active instance list
        /// </summary>
        /// <returns>The response object will indicate success</returns>
        /// <response code="200">Returns the requested object</response>
        [Auth(Authentication.Role.Manager)]
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpDelete("[action]/{id}")]
        public ActionResult<NoDataResponse> ManagerId(string id)
        {
            Logger.LogDebug("Manager remove ID action called");
            return Ok(Context.DelMgrInstance(id));
        }
    }
}
