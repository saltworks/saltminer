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
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Ui.Api.Authentication;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Responses;

namespace Saltworks.SaltMiner.Ui.Api.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    [ApiController]
    public class JobController(JobContext context, ILogger<JobController> logger, UiApiConfig config) : ApiControllerBase(context, logger)
    {
        private readonly JobContext JobContext = context;
        private readonly UiApiConfig Config = config;

        /// <summary>
        /// Pull pending job queue
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<Job>))]
        [HttpPost("pending")]
        public ActionResult<UiDataResponse<Job>> PullPendingJobs(string type = null, UiPager paging = null)
        {
            Logger.LogInformation("Pull pending jobs action called");
            return Ok(JobContext.PullPendingJobs(paging, type));
        }

        /// <summary>
        /// Pull pending job queue
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<Job>))]
        [HttpPost("finished")]
        public ActionResult<UiDataResponse<Job>> GetFinishedJobs(JobSearch request)
        {
            Logger.LogInformation("Pull pending jobs action called");
            return Ok(JobContext.GetJobs(request, true));
        }

        /// <summary>
        /// Pull pending job queue
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<Job>))]
        [HttpPost("non-finished")]
        public ActionResult<UiDataResponse<Job>> GetNonFinishedJobs(JobSearch request)
        {
            Logger.LogInformation("Pull pending jobs action called");
            return Ok(JobContext.GetJobs(request));
        }

        /// <summary>
        /// Update job queue status
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<Job>))]
        [HttpPost]
        public ActionResult<UiDataItemResponse<Job>> Update(Job queue)
        {
            Logger.LogInformation("Update job queue action called for job '{Id}'", queue.Id);
            return Ok(JobContext.UpdateQueue(queue, (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]));
        }

        /// <summary>
        /// Delete job
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpDelete("{id}")]
        public ActionResult<UiNoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete job queue action called for job '{Id}'", id);
            return Ok(JobContext.DeleteReport(id));
        }
    }
}
