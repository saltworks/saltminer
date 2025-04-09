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
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.DataApi.Contexts;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth]
    [ApiController]
    public class EventlogController : ApiControllerBase
    {
        private EventlogContext Context => ContextBase as EventlogContext;
        private string EventIndex = Eventlog.GenerateIndex();

        public EventlogController(ILogger<EventlogController> logger, EventlogContext context) : base(context, logger)
        {
        }

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

            return Accepted(Context.AddUpdate(request, EventIndex));
        }
    }
}
