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
using Saltworks.SaltMiner.DataApi.Contexts;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Authentication;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth]
    [ApiController]
    public class ConfigController : ApiControllerBase
    {
        private ConfigContext Context => ContextBase as ConfigContext;
        private readonly string ConfigIndex = Config.GenerateIndex();

        public ConfigController(ConfigContext context, ILogger<ConfigController> logger) : base(context, logger)
        {
        }

        /// <summary>
        /// Returns a single Config
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(DataItemResponse<Config>))]
        [HttpGet("{id}")]
        [Auth(Role.Admin, Role.Config)]
        public ActionResult<DataItemResponse<Config>> Get(string id)
        {
            Logger.LogInformation("Get action called for id '{id}'", id);
            return Ok(Context.Get<Config>(id, ConfigIndex));
        }

        /// <summary>
        /// Returns a all Configs
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested objects</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<Config>))]
        [HttpGet("all")]
        [Auth(Role.Admin)]
        public ActionResult<DataResponse<Config>> GetAll()
        {
            Logger.LogInformation("GetAll action called");
            return Ok(Context.GetAll());
        }

        /// <summary>
        /// Adds or Updates an Config
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        /// <remarks>Any child type of config can be sent in</remarks>
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
            Logger.LogInformation("Delete action called for id '{id}'", id);
            return Ok(Context.Delete<Config>(id, ConfigIndex));
        }

        /// <summary>
        /// Deletes an Config entity by type
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpDelete("type/{type}")]
        [Auth(Role.Admin)]
        public ActionResult<NoDataResponse> DeleteByType(string type)
        {
            Logger.LogInformation("Delete action called for type '{type}'", type);
            return Ok(Context.DeleteByType(type));
        }
    }
}
