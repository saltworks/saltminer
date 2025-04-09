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
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Ui.Api.Authentication;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient.ViewModels;

namespace Saltworks.SaltMiner.Ui.Api.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    [ApiController]
    public class ReportController : ApiControllerBase
    {
        private readonly ReportContext ReportContext;
        private readonly EngagementContext EngagementContext;
        private readonly ScanContext ScanContext;
        private readonly AssetContext AssetContext;
        private readonly IssueContext IssueContext;
        private readonly UiApiConfig Config;

        public ReportController(ReportContext context, EngagementContext engagementContext, ScanContext scanContext, AssetContext assetContext, IssueContext issueContext, ILogger<ReportController> logger, UiApiConfig config) : base(context, logger)
        {
            ReportContext = context;
            EngagementContext = engagementContext;
            ScanContext = scanContext;
            AssetContext = assetContext;
            IssueContext = issueContext;
            Config = config;

            EngagementContext.Controller = this;
            ScanContext.Controller = this;
            AssetContext.Controller = this;
            IssueContext.Controller = this;
        }

        /// <summary>
        /// Get engagement summary for report
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<EngagementSummary>))]
        [HttpGet("summary/{engagementId}")]
        public ActionResult<UiDataItemResponse<EngagementSummary>> Summary(string engagementId)
        {
            Logger.LogInformation("Summary action called for Engagement '{Id}'", engagementId);
            return Ok(EngagementContext.Summary(engagementId));
        }

        /// <summary>
        /// Get issue severities
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<EngagementSummary>))]
        [HttpGet("issue/severities")]
        public ActionResult<UiDataResponse<LookupValue>> Severities()
        {
            Logger.LogInformation("Severities action called");
            return Ok(ReportContext.Severities());
        }

        /// <summary>
        /// Gets an engagment scan for report
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<ScanFull>))]
        [HttpGet("scan/{engagementId}")]
        public ActionResult<UiDataItemResponse<ScanFull>> ScanByEngagement(string engagementId)
        {
            Logger.LogInformation("Get action called on Scan for engagment '{Id}'", engagementId);
            return Ok(ScanContext.Get(engagementId));
        }

        /// <summary>
        /// Gets an engagment assets for report
        /// </summary>
        /// <returns>Matching docs and scroll info</returns>
        /// <response code="202">Returns a response object containing results and scroll info</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<AssetFull>))]
        [HttpGet("assets/{engagementId}")]
        public ActionResult<UiDataResponse<AssetFull>> AllAssets(string engagementId)
        {
            Logger.LogInformation("GetByEngagement action called for Assets on Engagement '{Id}'", engagementId);
            return Ok(AssetContext.GetAllAssetsByEngagement(engagementId));
        }

        /// <summary>
        /// Search engagement issues for report
        /// </summary>`
        /// <returns>Matching docs and scroll info</returns>
        /// <response code="202">Returns a response object containing results and scroll info</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<IssueFull>))]
        [HttpPost("issues")]
        public ActionResult<UiDataResponse<IssueFull>> SearchIssues([FromBody] IssueSearch request)
        {
            Logger.LogInformation("Search action called");
            return Ok(IssueContext.EngagementIssueSearch(request));
        }

        /// <summary>
        /// Add Attachment for engagement
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpPost("engagement/{id}/attachment")]
        public ActionResult<UiNoDataResponse> AddAttachment(string id, UiAttachmentInfo attachment)
        {
            Logger.LogInformation("Add Report Attachment for Engagement '{Id}'", id);

            return Ok(ReportContext.SetAttachments(id, null, [attachment], (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG], false, false));
        }

        /// <summary>
        /// Update Report Template Lookups
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpPost("templates")]
        public ActionResult<UiNoDataResponse> UpdateTemplateLookups(List<string> templateNames)
        {
            Logger.LogInformation("Update template lookups action called");
            return Ok(ReportContext.UpdateTemplateLookups(templateNames));
        }

        /// <summary>
        /// Upload File
        /// </summary>
        /// <response code="200">Returns response indicating success</response>
        [HttpPost("file/upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            Logger.LogInformation("Upload action called");

            if (file.Length > 0)
            {
                var filePath = (await ReportContext.CreateFileAsync(file, ((KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG])?.UserName, ((KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG])?.FullName, true)).Data;

                return Ok($"{Config.FileRepository}/{Path.GetFileName(filePath)}");
            }
            throw new UiApiClientValidationException("No File Attached");
        }

        /// <summary>
        /// Get Report Attachment by FileName
        /// </summary>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<UiAttachmentInfo>))]
        [HttpGet("attachment/{fileName}")]
        public ActionResult<UiDataItemResponse<UiAttachmentInfo>> GetReportAttachment(string fileName)
        {
            Logger.LogInformation("Get Attachment action called");
            return Ok(ReportContext.GetReportAttachment(fileName));
        }
    }
}
