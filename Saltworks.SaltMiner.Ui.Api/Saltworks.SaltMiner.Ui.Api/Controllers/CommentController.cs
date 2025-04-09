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
using Saltworks.SaltMiner.Ui.Api.Authentication;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient.ViewModels;

namespace Saltworks.SaltMiner.Ui.Api.Controllers
{
    [Authorize(SysRole.SuperUser, SysRole.PentestAdmin, SysRole.Pentester)]
    [Route("[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class CommentController : ApiControllerBase
    {
        private readonly CommentContext Context;
        public CommentController(CommentContext context, ILogger<CommentController> logger) : base(context, logger)
        {
            Context = context;
        }

        /// <summary>
        /// Returns a single Comment
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<UiComment>))]
        [HttpGet("{id}")]
        public ActionResult<UiDataItemResponse<UiComment>> Get(string id)
        {
            Logger.LogInformation("Get action called for Comment '{Id}'", id);
            return Ok(Context.Get(id));
        }

        /// <summary>
        /// Comments Search
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<UiComment>))]
        [HttpPost("[action]")]
        public ActionResult<UiDataResponse<UiComment>> Search([FromBody] CommentSearch search)
        {
            Logger.LogInformation("Search action called for Comments");
            return Ok(Context.Search(search));
        }

        /// <summary>
        /// Updates Comment Message only and sends to any address attached
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(UiDataItemResponse<UiComment>))]
        [HttpPost("edit/message")]
        public ActionResult<UiDataItemResponse<UiComment>> Edit([FromBody] CommentEdit request)
        {
            Logger.LogInformation("Edit action called for Comment '{Id}'", request.Id);
            return Accepted(Context.Edit(request));
        }

        /// <summary>
        /// Adds Comment and sends to any address attached
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(UiDataItemResponse<UiComment>))]
        [HttpPost("new")]
        public ActionResult<UiDataItemResponse<UiComment>> New([FromBody] CommentNotice request)
        {
            Logger.LogInformation("New action called for Comment");
            return Accepted(Context.New(request, (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]));
        }

        /// <summary>
        /// Deletes an Comment entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpDelete("{id}")]
        public ActionResult<UiNoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete action called for Comment '{Id}'", id);
            return Ok(Context.Delete(id));
        }
    }
}
