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
using Saltworks.SaltMiner.Ui.Api.Authentication;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient.ViewModels;

namespace Saltworks.SaltMiner.Ui.Api.Controllers
{
    [Authorize(SysRole.SuperUser, SysRole.Pentester, SysRole.PentestAdmin)]
    [Route("[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class EngagementController : ApiControllerBase
    {
        private readonly EngagementContext Context;
        private readonly CommentContext CommentContext;
        public EngagementController(EngagementContext context, CommentContext commentContext, ILogger<EngagementController> logger) : base(context, logger)
        {
            Context = context;
            CommentContext = commentContext;

            CommentContext.Controller = this;
        }

        /// <summary>
        /// Generate engagement report
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpGet("{id}/report")]
        public ActionResult<UiNoDataResponse> GenerateReport(string id, string template)
        {
            Logger.LogInformation("Generate report action called for Engagement '{Id}' and Report Type '{Type}'", id, template);
            return Ok(Context.GenerateReport(id, template, (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]));
        }

        /// <summary>
        /// Gets engagements and primer data for engagement page
        /// </summary>
        /// <returns>Matching docs, scroll info, and primer data</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<EngagementPrimer>))]
        [HttpGet("primer")]
        public ActionResult<UiDataItemResponse<EngagementPrimer>> Primer()
        {
            Logger.LogInformation("Primer action called for Engagements");
            return Ok(Context.Primer());
        }

        /// <summary>
        /// Search engagements
        /// </summary>
        /// <returns>Matching docs and scroll info</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<EngagementSummary>))]
        [HttpPost("search")]
        public ActionResult<UiDataResponse<EngagementSummary>> Search(EngagementSearch request)
        {
            Logger.LogInformation("Search action called for Engagements");
            return Ok(Context.Search(request));
        }

        /// <summary>
        /// Gets Template Engagement
        /// </summary>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<EngagementSummary>))]
        [HttpGet("template/primer")]
        public ActionResult<UiDataItemResponse<EngagementSummary>> Template()
        {
            Logger.LogInformation("Template action called for Engagement");

            var response = Context.Template();

            return Ok(response);
        }

        /// <summary>
        /// Adds engagement and queue scan entities
        /// </summary>
        /// <returns>Matching docs and scroll info</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<EngagementSummary>))]
        [HttpPost("create")]
        public ActionResult<UiDataItemResponse<EngagementSummary>> Create(EngagementNew request)
        {
            Logger.LogInformation("Create action called for Engagement");

            var response = Context.Create(request);
            if (response.Success)
            {
                CommentContext.EngagementComment(response.Data.Id, "Create", (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]);
            }

            return Ok(response);
        }

        /// <summary>
        /// Get Engagement Summary data
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<EngagementSummary>))]
        [HttpGet("{id}/summary")]
        public ActionResult<UiDataItemResponse<EngagementSummary>> Summary(string id)
        {
            Logger.LogInformation("Summary action called for Engagement '{Id}'", id);
            return Ok(Context.Summary(id));
        }

        /// <summary>
        /// Updates engagement summary
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<EngagementSummary>))]
        [HttpPost("summary/edit")]
        public ActionResult<UiDataItemResponse<EngagementSummary>> SummaryEdit(EngagementSummaryEdit request)
        {
            Logger.LogInformation("SummaryEdit action called for Engagement '{Id}'", request?.Id);
            
            var user = (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG];
            var response = Context.SummaryEdit(request, user);
            if (response.Success)
            {
                CommentContext.EngagementComment(response.Data.Id, "Edit", user);
            }

            return Ok(response);
        }

        /// <summary>
        /// Get full engagement
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<EngagementSummary>))]
        [HttpGet("{id}")]
        public ActionResult<UiDataItemResponse<EngagementFull>> Full(string id)
        {
            Logger.LogInformation("Full action called for Engagement '{Id}'", id);
            return Ok(Context.FullEngagement(id));
        }

        /// <summary>
        /// Checkout engagement
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the new engagementId Created</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<EngagementSummary>))]
        [HttpPost("{id}/checkout")]
        public async Task<ActionResult<UiDataItemResponse<string>>> CheckoutAsync(string id)
        {
            Logger.LogInformation("Checkout action called for Engagement '{Id}'", id);

            var user = (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG];

            var response = await Context.CheckoutAsync(id, user);
            if (response.Success)
            {
                CommentContext.EngagementComment(id, "Checkout", user);
            }

            return Ok(response);
        }

        /// <summary>
        /// Publish engagement
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [Authorize(SysRole.PentestAdmin, SysRole.SuperUser)]
        [HttpPost("{id}/queue")]
        public ActionResult<UiNoDataResponse> Publish(string id)
        {
            Logger.LogInformation("Publish action called for Engagement '{Id}'", id);

            var response = Context.Queue(id);
            if (response.Success)
            {
                CommentContext.EngagementComment(id, "Publish", (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]);
            }

            return Ok(response);
        }

        /// <summary>
        /// Reset publish
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [Authorize(SysRole.PentestAdmin, SysRole.SuperUser)]
        [HttpPost("{id}/reset")]
        public ActionResult<UiNoDataResponse> ResetPublish(string id)
        {
            Logger.LogInformation("Reset publish action called for Engagement '{Id}'", id);

            var response = Context.ResetPublish(id);
            if (response.Success)
            {
                CommentContext.EngagementComment(id, "Reset publish", (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]);
            }

            return Ok(response);
        }

        /// <summary>
        /// Export engagement data to zip file
        /// </summary>
        /// <returns>Zip file of engagement data</returns>
        [HttpGet("{id}/export")]
        public ActionResult ExportEngagement(string id)
        {
            Logger.LogInformation("ExportEngagement action called for Engagement '{Id}'", id);

            var fileContent = Context.ExportEngagement(id);

            if (fileContent == null)
            {
                return NoContent();
            }

            CommentContext.EngagementComment(id, "Export", (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]);

            return File(fileContent, "application/octet-stream", "engagement.zip");
        }

        /// <summary>
        /// Export engagement data to zip file
        /// </summary>
        /// <returns>Zip file of engagement data</returns>
        [HttpGet("{id}/issue-import-export")]
        public ActionResult ExportImportIssuesByEngagement(string id)
        {
            Logger.LogInformation("ExportImportIssuesByEngagement action called for Engagement '{Id}'", id);

            var fileContent = Context.ExportImportIssuesByEngagement(id);

            if (fileContent == null)
            {
                return NoContent();
            }

            CommentContext.EngagementComment(id, "ExportImportIssuesByEngagement", (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]);

            return File(fileContent, "application/octet-stream", "issueImport.json");
        }

        /// <summary>
        /// Import zip file containing engagement data to create new engagement (ignore any ids)
        /// </summary>
        /// <returns>UiNoDataResponse with boolean indicating success</returns>
        [HttpPost("import/new")]
        public async Task<ActionResult<UiDataItemResponse<string>>> ImportNewEngagementAsync(IFormFile file)
        {
            Logger.LogInformation("ImportNewEngagement action called");

            if (file != null && file.Length > 0)
            {
                if (file.FileName.EndsWith(".zip"))
                {
                    var user = (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG];

                    var response = await Context.ImportEngagementFromFileAsync(file, user, true);
                    if (response.Success)
                    {
                        CommentContext.EngagementComment(response.Data, "Import New", user);
                    }

                    return Ok(response);
                }
                throw new UiApiClientValidationException("Invalid File Type");
            }
            throw new UiApiClientValidationException("No File Attached");
        }

        /// <summary>
        /// Download JSON Template File
        /// </summary>
        /// <returns>JSON Template</returns>
        [HttpGet("import/template")]
        public IActionResult GetTemplateImport()
        {
            Logger.LogInformation("GetTemplateImport action called");

            var filePath = Context.GetEngagementImportJSON().Data;

            return File(System.IO.File.ReadAllBytes(filePath), "application/octet-stream", Path.GetFileName(filePath));
        }

        /// <summary>
        /// Import zip file containing engagement data to add or update (using ids provided)
        /// </summary>
        /// <returns>UiNoDataResponse with boolean indicating success</returns>
        [HttpPost("import")]
        public async Task<ActionResult<UiDataItemResponse<string>>> ImportEngagementAsync(IFormFile file)
        {
            Logger.LogInformation("ImportEngagement action called");

            if (file != null && file.Length > 0)
            {
                if (file.FileName.EndsWith(".zip"))
                {
                    var user = (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG];

                    var response = await Context.ImportEngagementFromFileAsync(file, user);
                    if (response.Success)
                    {
                        CommentContext.EngagementComment(response.Data, "Import", user);
                    }

                    return Ok(response);
                }
                throw new UiApiClientValidationException("Invalid File Type");
            }
            throw new UiApiClientValidationException("No File Attached");
        }

        /// <summary>
        /// Cancel engagement
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpPost("{id}/cancel")]
        public ActionResult<UiNoDataResponse> Cancel(string id)
        {
            Logger.LogInformation("Cancel action called for Engagement '{Id}'", id);

            CommentContext.EngagementComment(id, "Cancel", (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]);

            var response = Context.Cancel(id, (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]);

            return Ok(response);
        }

        /// <summary>
        /// Delete engagement
        /// </summary>oops
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpDelete("{id}/delete")]
        public ActionResult<UiNoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete action called for Engagement '{Id}'", id);

            CommentContext.EngagementComment(id, "Delete", (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]);

            var response = Context.Delete(id);

            return Ok(response);
        }

        /// <summary>
        /// Delete engagement
        /// </summary>oops
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpDelete("group/{id}/delete")]
        public ActionResult<UiNoDataResponse> DeleteAllByGroupId(string id)
        {
            Logger.LogInformation("Delete action called for Engagement Group '{Id}'", id);

            CommentContext.EngagementComment(id, "DeleteAllByGroupId", (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]);

            var response = Context.DeleteGroup(id);

            return Ok(response);
        }

        /// <summary>
        /// Set Attachments for engagement
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpPost("{id}/attachments")]
        public ActionResult<UiNoDataResponse> SetAttachments(string id, List<UiAttachmentInfo> attachments)
        {
            Logger.LogInformation("Set Attachments for Engagement {Id}", id);

            var user = (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG];
            CommentContext.EngagementComment(id, "Set Attachments", user);

            return Ok(Context.SetAttachments(id, null, attachments, user, false, true));
        }

        /// <summary>
        /// Delete all attachments for engagement
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpDelete("{id}/attachments")]
        public ActionResult<UiNoDataResponse> DeleteAttachment(string id)
        {
            Logger.LogInformation("Delete Attachments for Engagement {Id}", id);

            CommentContext.EngagementComment(id, "Delete Attachment", (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]);

            return Ok(Context.DeleteAllEngagementAttachments(id));
        }
    }
}
