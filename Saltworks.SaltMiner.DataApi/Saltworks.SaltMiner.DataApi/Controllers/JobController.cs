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
    [Auth(Role.JobManager, Role.Pentester)]
    [ApiController]
    public class JobController : ApiControllerBase
    {
        private JobContext Context => ContextBase as JobContext;
        private string JobIndex = Job.GenerateIndex();

        public JobController(ILogger<JobController> logger, JobContext context) : base(context, logger)
        {
        }

        /// <summary>
        /// Returns a list of Jobs
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<Job>))]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<Job>> Search([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search action called");

            return Ok(Context.Search<Job>(search, JobIndex));
        }

        /// <summary>
        /// Returns a Job by ID
        /// </summary>
        /// <returns>The job inside a response object</returns>
        /// <response code="200">Returns the job inside a response object</response>
        [ProducesResponseType(200, Type = typeof(DataItemResponse<Issue>))]
        [HttpGet("{id}")]
        public ActionResult<DataItemResponse<Job>> Get(string id)
        {
            Logger.LogInformation("Get action called for id {id}", id);

            return Ok(Context.Get<Job>(id, Job.GenerateIndex()));
        }

        /// <summary>
        /// Adds/Updates one or more Job(s)
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk")]
        public ActionResult<BulkResponse> AddUpdateBulk([FromBody] DataRequest<Job> request)
        {
            Logger.LogInformation("Add/Update Bulk action called");
            return Accepted(Context.AddUpdateBulk(request, JobIndex));
        }

        /// <summary>
        /// Updates one or more Job(s) using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<Job> request)
        {
            Logger.LogInformation("Update By Query action called");
            return Accepted(Context.UpdateByQuery(request, JobIndex));
        }

        /// <summary>
        /// Deletes an Job entity by Id
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpDelete("{id}")]
        public ActionResult<NoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete action called for id {id}", id);

            return Ok(Context.Delete<Job>(id, JobIndex));
        }

        /// <summary>
        /// Adds or Updates a Job - only include one in Documents
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(DataItemRequest<Job>))]
        [HttpPost]
        public ActionResult<DataItemResponse<Job>> Post([FromBody] DataItemRequest<Job> request)
        {
            Logger.LogInformation("Post action called");

            return Accepted(Context.AddUpdate(request, JobIndex));
        }

        /// <summary>
        /// Updates status and status message for a given job
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <remarks>
        /// This update uses locking to ensure consistency.  Status should be one of these values:  Pending, Processing, Complete, Error.
        /// </remarks>
        [ProducesResponseType(202, Type = typeof(DataItemRequest<Job>))]
        [HttpPost("status")]
        public ActionResult<DataItemResponse<Job>> Status([FromBody] DataItemRequest<Job> request)
        {
            Logger.LogInformation("Status action called for job id '{id}' and status '{status}'", request.Entity.Id, request.Entity.Status);
            return Accepted(Context.UpdateStatus(request));
        }
    }
}
