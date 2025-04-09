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
    public class AttachmentController : ApiControllerBase
    {
        private AttachmentContext Context => ContextBase as AttachmentContext;

        public AttachmentController(AttachmentContext context, ILogger<AttachmentController> logger) : base(context, logger)
        {
        }

        /// <summary>
        /// Adds or Updates a Attachment - only include one in Documents
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(DataItemRequest<Attachment>))]
        [HttpPost]
        public ActionResult<DataItemResponse<Attachment>> AddUpdate([FromBody] DataItemRequest<Attachment> request)
        {
            Logger.LogInformation("Post action called");

            return Accepted(Context.AddUpdate(request, Attachment.GenerateIndex()));
        }

        /// <summary>
        /// Adds/Updates one or more Attachment(s)
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk")]
        public ActionResult<BulkResponse> AddUpdateBulk([FromBody] DataRequest<Attachment> request)
        {
            Logger.LogInformation("Add/Update action called");
            return Accepted(Context.AddUpdateBulk(request, Attachment.GenerateIndex()));
        }

        /// <summary>
        /// Updates one or more Attachment(s) using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<Attachment> request)
        {
            Logger.LogInformation("Update By Query action called");
            return Accepted(Context.UpdateByQuery(request, Attachment.GenerateIndex()));
        }

        /// <summary>
        /// Returns a single Attachment
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<Attachment>))]
        [HttpGet("{id}")]
        public ActionResult<DataItemResponse<Attachment>> Get(string id)
        {
            Logger.LogInformation("Get action called for id '{id}'", id);


            return Ok(Context.Get<Attachment>(id, Attachment.GenerateIndex()));
        }

        /// <summary>
        /// Returns a list of Attachments
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<Comment>))]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<Attachment>> Search([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search action called");

            return Ok(Context.Search<Attachment>(search, Attachment.GenerateIndex()));
        }

        /// <summary>
        /// Deletes an Attachment
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Admin, Role.Pentester, Role.Manager)]
        [HttpDelete("{id}")]
        public ActionResult<NoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete attachment action called for id '{id}'", id);

            return Ok(Context.Delete<Attachment>(id, Attachment.GenerateIndex()));
        }

        /// <summary>
        /// Deletes an Attachments by Engagement
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Admin, Role.Pentester, Role.Manager)]
        [HttpDelete("engagement/{id}/all")]
        public ActionResult<NoDataResponse> DeleteAllEngagement(string id)
        {
            Logger.LogInformation("Delete all engagement attachments action called for id '{id}'", id);

            return Ok(Context.DeleteAllEngagement(id));
        }

        /// <summary>
        /// Deletes an Attachments on Engagement Level Only
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Admin, Role.Pentester, Role.Manager)]
        [HttpDelete("engagement/{id}")]
        public ActionResult<NoDataResponse> DeleteEngagementOnly(string id)
        {
            Logger.LogInformation("Delete only engagement level attachments action called for id '{id}'", id);

            return Ok(Context.DeleteAllEngagement(id, true));
        }

        /// <summary>
        /// Deletes an Markdown Attachments by Engagement
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Admin, Role.Pentester, Role.Manager)]
        [HttpDelete("engagement/{id}/all/markdown")]
        public ActionResult<NoDataResponse> DeleteAllEngagementMarkdown(string id)
        {
            Logger.LogInformation("Delete all engagement mardown attachments action called for id '{id}'", id);

            return Ok(Context.DeleteAllEngagement(id, false, true));
        }

        /// <summary>
        /// Deletes an Markdwon Attachments on Engagement Level Only
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Admin, Role.Pentester, Role.Manager)]
        [HttpDelete("engagement/{id}/markdown")]
        public ActionResult<NoDataResponse> DeleteEngagementMarkdownOnly(string id)
        {
            Logger.LogInformation("Delete only engagement level mardown attachments action called for id '{id}'", id);

            return Ok(Context.DeleteAllEngagement(id, true, true));
        }

        /// <summary>
        /// Deletes an Attachments by Issue
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Admin, Role.Pentester, Role.Manager)]
        [HttpDelete("issue/{id}")]
        public ActionResult<NoDataResponse> DeleteAllIssue(string id)
        {
            Logger.LogInformation("Delete all issues attachments action called for id '{id}'", id);

            return Ok(Context.DeleteAllIssue(id, null));
        }

        /// <summary>
        /// Deletes all Martkdwon Attachments by Issue
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Admin, Role.Pentester, Role.Manager)]
        [HttpDelete("issue/{id}/markdown")]
        public ActionResult<NoDataResponse> DeleteAllIssueMarkdown(string id)
        {
            Logger.LogInformation("Delete all issues markdown attachments action called for id '{id}'", id);

            return Ok(Context.DeleteAllIssue(id, true));
        }

        /// <summary>
        /// Deletes all NonMartkdwon Attachments by Issue
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Admin, Role.Pentester, Role.Manager)]
        [HttpDelete("issue/{id}/non-markdown")]
        public ActionResult<NoDataResponse> DeleteAllIssueNonMarkdown(string id)
        {
            Logger.LogInformation("Delete all issues non markdown attachments action called for id '{id}'", id);

            return Ok(Context.DeleteAllIssue(id, false));
        }
    }
}
