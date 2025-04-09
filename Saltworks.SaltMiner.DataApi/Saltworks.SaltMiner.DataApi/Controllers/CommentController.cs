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
    public class CommentController : ApiControllerBase
    {
        private CommentContext Context => ContextBase as CommentContext;
        private string CommentIndex = Comment.GenerateIndex();

        public CommentController(CommentContext context, ILogger<CommentController> logger) : base(context, logger)
        {
        }

        /// <summary>
        /// Returns a single Comment
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<Comment>))]
        [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin, Role.JobManager, Role.Manager)]
        [HttpGet("{id}")]
        public ActionResult<DataItemResponse<Comment>> Get(string id)
        {
            Logger.LogInformation("Get action called for id '{id}'", id);


            return Ok(Context.Get<Comment>(id, CommentIndex));
        }

        /// <summary>
        /// Returns a list of Comments
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<Comment>))]
        [Auth(Role.Pentester, Role.PentesterViewer, Role.Admin, Role.JobManager, Role.Manager)]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<Comment>> Search([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search action called");

            return Ok(Context.Search<Comment>(search, CommentIndex));
        }

        /// <summary>
        /// Adds or Updates an Comment
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(DataItemResponse<Comment>))]
        [Auth(Role.Pentester, Role.Admin, Role.JobManager, Role.Manager)]
        [HttpPost]
        public ActionResult<DataItemResponse<Comment>> Post([FromBody] DataItemRequest<Comment> request)
        {
            Logger.LogInformation("Post action called");

            return Accepted(Context.AddUpdate(request, CommentIndex));
        }

        /// <summary>
        /// Adds/Updates one or more Comment
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [Auth(Role.Pentester, Role.Admin, Role.JobManager, Role.Manager)]
        [HttpPost("bulk")]
        public ActionResult<BulkResponse> AddUpdateBulk([FromBody] DataRequest<Comment> request)
        {
            Logger.LogInformation("Add/Update action called");

            return Accepted(Context.AddUpdateBulk(request, CommentIndex));
        }

        /// <summary>
        /// Updates one or more Comment using update by query
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<Comment> request)
        {
            Logger.LogInformation("Update By Query action called");

            return Accepted(Context.UpdateByQuery(request, CommentIndex));
        }

        /// <summary>
        /// Deletes an Comment entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Pentester, Role.Admin, Role.JobManager)]
        [HttpDelete("{id}")]
        public ActionResult<NoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete action called for id '{id}'", id);

            return Ok(Context.Delete<Comment>(id, CommentIndex));
        }

        /// <summary>
        /// Deletes an Comment entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Admin, Role.Pentester, Role.Manager, Role.JobManager)]
        [HttpDelete("engagement/{id}")]
        public ActionResult<NoDataResponse> DeleteAllEngagement(string id)
        {
            Logger.LogInformation("Delete all engagement action called for id '{id}'", id);

            return Ok(Context.DeleteAllEngagement<Comment>(id));
        }
    }
}
