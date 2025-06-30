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
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth(Role.ServiceManager, Role.Pentester, Role.Admin)]
    [ApiController]
    public class ServiceJobController : ApiControllerBase
    {
        private ServiceJobContext Context => ContextBase as ServiceJobContext;
        private readonly string ServiceJobIndex = ServiceJob.GenerateIndex();

        public ServiceJobController(ServiceJobContext context, ILogger<ServiceJobController> logger) : base(context, logger)
        {
        }

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

            return Ok(Context.Search<ServiceJob>(search, ServiceJobIndex));
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

            return Accepted(Context.AddUpdate(request, ServiceJobIndex));
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
            Logger.LogInformation("Delete action called for id '{id}'", id);

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
            Logger.LogInformation("Get action called for service job id '{id}'", id);


            return Ok(Context.Get<ServiceJob>(id, ServiceJobIndex));
        }
    }
}
