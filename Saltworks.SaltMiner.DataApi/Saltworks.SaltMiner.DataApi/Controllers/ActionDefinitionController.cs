/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
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
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.DataApi.Contexts;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth(Role.ServiceManager, Role.Pentester, Role.Admin)]
    [ApiController]
    public class ActionDefinitionController : ApiControllerBase
    {
        private ActionDefinitionContext Context => ContextBase as ActionDefinitionContext;
        readonly string ActionDefinitionIndex = ActionDefinition.GenerateIndex();

        public ActionDefinitionController(ActionDefinitionContext context, ILogger<ActionDefinitionController> logger) : base(context, logger)
        {
        }

        /// <summary>
        /// Returns a list of action definitions
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<ActionDefinition>))]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<ActionDefinition>> Search([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search action called");

            return Ok(Context.Search<ActionDefinition>(search, ActionDefinitionIndex));
        }

    }
}
