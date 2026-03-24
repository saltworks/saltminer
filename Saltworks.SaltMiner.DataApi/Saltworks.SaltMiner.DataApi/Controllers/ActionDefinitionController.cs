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

            return Ok(Context.Search<ActionDefinition>(ActionDefinitionIndex, search));
        }

    }
}
