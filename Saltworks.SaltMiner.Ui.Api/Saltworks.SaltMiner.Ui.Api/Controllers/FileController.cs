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
using Saltworks.SaltMiner.UiApiClient.Responses;

namespace Saltworks.SaltMiner.Ui.Api.Controllers
{
    [Authorize(SysRole.SuperUser, SysRole.PentestAdmin, SysRole.Pentester)]
    [Route("[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class FileController(FileContext context, ILogger<FileController> logger, UiApiConfig config) : ApiControllerBase(context, logger)
    {
        private readonly FileContext Context = context;
        private readonly UiApiConfig Config = config;

        /// <summary>
        /// Upload File
        /// </summary>
        /// <response code="200">Returns response indicating success</response>
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            Logger.LogInformation("Upload action called");

            if (file.Length > 0)
            {
                if (Config.ValidFileExtensions.Contains(Path.GetExtension(file.FileName)))
                {
                    var filePath = (await Context.CreateFileAsync(file, ((KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG])?.UserName, ((KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG])?.FullName)).Data;

                    return Ok(Path.GetFileName(filePath));
                }

                throw new UiApiClientValidationException("Invalid File Type");
            }

            throw new UiApiClientValidationException("No File Attached");
        }

        /// <summary>
        /// List all Files
        /// </summary>
        /// <response code="200">Returns response indicating success</response>
        [HttpGet("list")]
        public ActionResult<UiDataResponse<string>> List()
        {
            Logger.LogInformation("Upload action called");
            return Ok(Context.ListAllFiles());
        }

        /// <summary>
        /// Upload File Attachment
        /// </summary>
        /// <response code="200">Returns response indicating success</response>
        [HttpPost("upload/attachment")]
        public async Task<IActionResult> UploadAttachment(IFormFile file)
        {
            Logger.LogInformation("Upload action called");

            if (file.Length > 0)
            {
                if (Config.ValidFileExtensions.Contains(Path.GetExtension(file.FileName)))
                {
                    var filePath = (await Context.CreateFileAsync(file, ((KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG])?.UserName, ((KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG])?.FullName, true)).Data;

                    return Ok(Path.GetFileName(filePath));
                }

                throw new UiApiClientValidationException("Invalid File Type");
            }

            throw new UiApiClientValidationException("No File Attached");
        }

        /// <summary>
        /// Check File
        /// </summary>
        /// <returns>File</returns>
        [ProducesResponseType(200, Type = typeof(string))]
        [HttpGet("check/{fileId}")]
        public IActionResult Check(string fileId)
        {
            Logger.LogInformation("Check file action called for '{FileId}'", fileId);

            var path = Context.SearchFile(fileId).Data;
            if (path == null)
            {
                Logger.LogCritical("File '{FileId}' not found on server", fileId);
                return Ok("");
            }

            return Ok(path);
        }

        /// <summary>
        /// Download File
        /// </summary>
        /// <returns>File</returns>
        [HttpGet("{fileId}")]
        public IActionResult Download(string fileId)
        {
            Logger.LogInformation("Download action called for '{File}'", fileId);
            var path = Context.SearchFile(fileId).Data; 
            if(path == null)
            {
                Logger.LogCritical("File '{FileId}' not found on server", fileId);
                return null;
            }
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, 4096);
            var attachment = Context.GetAttachmentByFileId(fileId);
            return File(fs, "application/octet-stream", attachment?.FileName ?? fileId);
        }

        /// <summary>
        /// Download File Attachment
        /// </summary>
        /// <returns>File</returns>
        [HttpGet("{fileId}/attachment")]
        public IActionResult DownloadAttachment(string fileId)
        {
            Logger.LogInformation("Download action called for '{File}'", fileId);
            var path = Context.SearchFile(fileId).Data ?? throw new UiApiNotFoundException("Image not found on server");
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, 4096);
            var attachment = Context.GetAttachmentByFileId(fileId);
            return File(fs, "application/octet-stream", attachment?.FileName ?? fileId);
        }

        /// <summary>
        /// Delete File
        /// </summary>
        /// <returns>File</returns>
        [HttpDelete("{fileId}")]
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        public IActionResult Delete(string fileId)
        {
            Logger.LogInformation("Download action called for '{File}'", fileId);
            Context.DeleteFile(fileId);
            return Ok(new UiNoDataResponse(1, $"{fileId} successfully deleted"));
        }

        /// <summary>
        /// Delete File Attachment
        /// </summary>
        /// <returns>File</returns>
        [HttpDelete("{fileId}/attachment")]
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        public IActionResult DeleteAttachment(string fileId)
        {
            Logger.LogInformation("Download action called for '{File}'", fileId);
            Context.DeleteFile(fileId, true);
            return Ok(new UiNoDataResponse(1, $"{fileId} successfully deleted"));
        }
    }
}
