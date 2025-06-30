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
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Import;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient.ViewModels;

namespace Saltworks.SaltMiner.Ui.Api.Controllers
{
    [Authorize(SysRole.SuperUser, SysRole.PentestAdmin, SysRole.Pentester)]
    [Route("[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class IssueController : ApiControllerBase
    {
        private readonly IssueContext Context;
        private readonly CommentContext CommentContext;
        public IssueController(IssueContext context, CommentContext commentContext, ILogger<IssueController> logger) : base(context, logger)
        {
            Context = context;
            CommentContext = commentContext;

            CommentContext.Controller = this;
        }

        /// <summary>
        /// Issue search with page primer data
        /// </summary>
        /// <returns>Matching docs, scroll info, and primer data</returns>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<IssuePrimer>))]
        [HttpGet("primer")]
        public ActionResult<UiDataItemResponse<IssuePrimer>> Primer(string engagementId)
        {
            Logger.LogInformation("Primer action called on Issues for Engagement '{Id}'", engagementId);
            return Ok(Context.Primer(engagementId));
        }

        /// <summary>
        /// Issue search
        /// </summary>
        /// <returns>Matching docs and scroll info</returns>
        /// <response code="202">Returns a response object containing results and scroll info</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<IssueFull>))]
        [HttpPost("search")]
        public ActionResult<UiDataResponse<IssueFull>> Search(IssueSearch request)
        {
            Logger.LogInformation("Search action called on Issues for Engagement '{Id}'", request.EngagementId);
            return Ok(Context.Search(request));
        }

        /// <summary>
        /// Template Issue search
        /// </summary>
        /// <returns>Matching docs and scroll info</returns>
        /// <response code="202">Returns a response object containing results and scroll info</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<IssueFull>))]
        [HttpPost("template/search")]
        public ActionResult<UiDataResponse<IssueFull>> TemplateSearch(TemplateIssueSearch request)
        {
            Logger.LogInformation("Template Search action called");
            return Ok(Context.TemplateSearch(request));
        }

        /// <summary>
        /// Template Issue Add
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="202">Returns a response object containing results and scroll info</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<IssueFull>))]
        [HttpPost("{issueId}/engagement/{engagementId}/asset/{assetId}/template")]
        public ActionResult<UiDataItemResponse<IssueFull>> TemplateAdd(string issueId, string engagementId, string assetId)
        {
            Logger.LogInformation("Template Issue Add for Engagement '{Id}'", engagementId);
            return Ok(Context.TemplateAdd(issueId, engagementId, assetId, (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]));
        }

        /// <summary>
        /// Adds an Issue entity
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<IssueFull>))]
        [HttpPost("new")]
        public ActionResult<UiDataItemResponse<IssueFull>> New(IssueEdit request)
        {
            Logger.LogInformation("New Issue action called on Engagement '{Id}'", request.EngagementId);

            var user = (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG];

            var response = Context.New(request, user);
            if (response.Success)
            {
                CommentContext.IssueComment(response.Data.Id, "New", user);
            }

            return Ok(response);
        }

        /// <summary>
        /// New Issue Primer Data
        /// </summary>
        /// <returns>Matching docs, scroll info, and primer data</returns>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<IssueEditPrimer>))]
        [HttpGet("new/primer")]
        public ActionResult<UiDataItemResponse<IssueEditPrimer>> NewPrimer(string engagementId)
        {
            Logger.LogInformation("NewPrimer action called on Issues for Engagement '{Id}'", engagementId);
            return Ok(Context.NewPrimer(engagementId));
        }

        /// <summary>
        /// Updates an Issue entity
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<IssueFull>))]
        [HttpPost("edit")]
        public ActionResult<UiDataItemResponse<IssueFull>> Edit(IssueEdit request)
        {
            Logger.LogInformation("Edit Issue action called on Issue '{Issue}'", request.Id);

            var user = (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG];

            var response = Context.Edit(request, user);
            if (response.Success)
            {
                CommentContext.IssueComment(request.Id, "Edit", user);
            }

            return Ok(response);
        }

        /// <summary>
        /// Clone an Issue entity
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<IssueFull>))]
        [HttpPost("clone/{queueIssueId}")]
        public async Task<ActionResult<UiDataItemResponse<IssueFull>>> CloneAsync(string queueIssueId)
        {
            Logger.LogInformation("Clone Issue action called on Issue '{Issue}'", queueIssueId);

            var user = (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG];

            var response = await Context.Clone(queueIssueId, user);
            if (response.Success)
            {
                CommentContext.IssueComment(queueIssueId, "Clone", user);
            }

            return Ok(response);
        }

        /// <summary>
        /// Issue Edit Primer Data
        /// </summary>
        /// <returns>Primer data for Issue Edit page</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<IssueEditPrimer>))]
        [HttpGet("{issueId}/edit/primer")]
        public ActionResult<UiDataItemResponse<IssueEditPrimer>> EditPrimer(string issueId)
        {
            Logger.LogInformation("EditPrimer action called for Issue '{Id}'", issueId);
            return Ok(Context.EditPrimer(issueId));
        }

        /// <summary>
        /// Issue view
        /// </summary>
        /// <returns>Primer data for Issue Edit page</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<Issue>))]
        [HttpGet("{issueId}/fullview")]
        public ActionResult<UiDataItemResponse<Issue>> View(string issueId)
        {
            Logger.LogInformation("View action called for Issue '{Id}'", issueId);
            return Ok(Context.FullView(issueId));
        }


        /// <summary>
        /// Issue Get By Scanner Id
        /// </summary>
        /// <returns>Primer data for Issue Edit page</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<IssueGetScanner>))]
        [HttpGet("scanner/{scannerId}/engagement/{engagementId}")]
        public ActionResult<UiDataItemResponse<IssueGetScanner>> GetIssueByScannerId(string scannerId, string engagementId)
        {
            Logger.LogInformation("GetIssueByScannerId action called for Scanner '{Id}' and Engagement '{Eid}'", scannerId, engagementId);
            return Ok(Context.GetByScannerId(engagementId, scannerId));
        }

        /// <summary>
        /// Refresh lock on Edit Issue
        /// </summary>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpGet("{issueId}/edit/refresh")]
        public ActionResult<UiNoDataResponse> RefreshEditLock(string issueId)
        {
            Logger.LogInformation("RefreshEditLock action called for Issue '{Id}'", issueId);
            return Ok(Context.RefreshEditLock(issueId, ((KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG])?.UserName));
        }

        /// <summary>
        /// Deletes an Queue Issue entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpDelete("template/{issueId}")]
        public ActionResult<UiNoDataResponse> TemplateDelete(string issueId)
        {
            Logger.LogInformation("Delete action called on Template Issue '{Id}'", issueId);
            return Ok(Context.TemplateDelete(issueId));
        }

        /// <summary>
        /// Deletes an Queue Issue entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpPost("template/delete")]
        public ActionResult<UiNoDataResponse> TemplateDelete(DeleteById request)
        {
            Logger.LogInformation("Delete template issues action called");
            return Ok(Context.TemplateDeletes(request));
        }

        /// <summary>
        /// Marks issues with removed status
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpPost("remove")]
        public ActionResult<UiNoDataResponse> MarkRemoved(DeleteById request)
        {
            Logger.LogInformation("MarkRemoved action called");
            return Ok(Context.MarkRemoved(request));
        }

        /// <summary>
        /// Set Issue Attachments
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpPost("{issueId}/engagement/{engagementId}/attachments")]
        public ActionResult<UiNoDataResponse> SetAttachments(string issueId, string engagementId, List<UiAttachmentInfo> attachments)
        {
            Logger.LogInformation("Set Attachments for Issue {Id} and Engagement {EngagementId}", issueId, engagementId);

            var user = (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG];

            CommentContext.IssueComment(issueId, "Set Attachment", user);

            return Ok(Context.SetAttachments(engagementId, issueId, attachments, user, false, true));
        }

        /// <summary>
        /// Delete Issue Attachments
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpDelete("{id}/attachments")]
        public ActionResult<UiNoDataResponse> DeleteAttachment(string id)
        {
            Logger.LogInformation("Delete Attachments for Issue {Id}", id);

            CommentContext.EngagementComment(id, "Delete Attachment", (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]);

            return Ok(Context.DeleteIssueAttachments(id));
        }

        /// <summary>
        /// Import CSV or JSON. JSON requires Default Queue Asset Id. CSV Headers with * are required fields and will error without data.
        /// </summary>
        /// <response code="202">Returns a response object containing results and scroll info</response>
        [HttpPost("import/{engagementId}")]
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<IssueImporterResponse>))]
        public IActionResult Import(IFormFile file, string engagementId, string defaultQueueAssetId = null, bool isTemplate = false)
        {
            Logger.LogInformation("Import action called");

            if (file != null && file.Length > 0)
            {
                var result = Context.ProcessImport(file, engagementId, ((KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG])?.UserName, ((KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG])?.FullName, defaultQueueAssetId, isTemplate);

                return Ok(new UiDataItemResponse<IssueImporterResponse>(result));
            }
            else
            {
                throw new UiApiClientValidationException("No File Attached");
            }
        }

        /// <summary>
        /// Import CSV or JSON. JSON requires Default Queue Asset Id. CSV Headers with * are required fields and will error without data.
        /// </summary>
        /// <response code="202">Returns a response object containing results and scroll info</response>
        [HttpPost("import/custom")]
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<Job>))]
        public IActionResult QueueCustomImport(IFormFile file)
        {
            Logger.LogInformation("Import action called");

            if (file != null && file.Length > 0)
            {
                var result = Context.QueueCustomImport(file, "7ac0d032-16e5-4153-aaac-7eb5f646e538", "9a7812c4-f24f-4496-b6c4-dee13f32afe5", (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG], null);

                return Ok(result);
            }
            else
            {
                throw new UiApiClientValidationException("No File Attached");
            }
        }

        /// <summary>
        /// Download CSV Template. Headers with * are required fields and will error without data.
        /// </summary>
        /// <returns>CSV Template</returns>
        [HttpGet("import/csv/template")]
        public IActionResult CSVTemplate()
        {
            Logger.LogInformation("CSVTemplate action called");

            var filePath = Context.GetCSVTemplate().Data;

            return File(System.IO.File.ReadAllBytes(filePath), "application/octet-stream", Path.GetFileName(filePath));
        }

        /// <summary>
        /// Download CSV Sample File
        /// </summary>
        /// <returns>CSV Sample</returns>
        [HttpGet("import/csv/testfile")]
        public IActionResult CSVTestFile()
        {
            Logger.LogInformation("CSVTestFile action called");

            var filePath = Context.GetCSVTestFile().Data;

            return File(System.IO.File.ReadAllBytes(filePath), "application/octet-stream", Path.GetFileName(filePath));
        }

        /// <summary>
        /// Download JSON Template File
        /// </summary>
        /// <returns>JSON Template</returns>
        [HttpGet("import/json/template")]
        public IActionResult GetTemplateImportJSON()
        {
            Logger.LogInformation("GetTemplateImportJSON action called");

            var filePath = Context.GetTemplateImportJSON().Data;

            return File(System.IO.File.ReadAllBytes(filePath), "application/octet-stream", Path.GetFileName(filePath));
        }

        /// <summary>
        /// Download JSON Template File
        /// </summary>
        /// <returns>JSON Template</returns>
        [HttpGet("import/json/engagement")]
        public IActionResult GetEngagementIssueImportJSON()
        {
            Logger.LogInformation("GetEngagementIssueImportJSON action called");

            var filePath = Context.GetEngagementIssueImportJSON().Data;

            return File(System.IO.File.ReadAllBytes(filePath), "application/octet-stream", Path.GetFileName(filePath));
        }
    }
}
