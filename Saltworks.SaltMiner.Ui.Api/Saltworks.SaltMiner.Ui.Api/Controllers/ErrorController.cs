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

﻿using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Saltworks.SaltMiner.UiApiClient;

namespace Saltworks.SaltMiner.Ui.Api.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("[controller]")]
    public class ErrorController(ILogger<ErrorController> logger) : ControllerBase
    {
        private readonly ILogger Logger = logger;

        [Route("")]
        public ActionResult Error()
        {
            Logger.LogWarning("Error action called");

            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (exceptionHandlerPathFeature?.Error != null)
            {
                Logger.LogError("[{TYPE}] {MSG}", exceptionHandlerPathFeature.Error.GetType().Name, exceptionHandlerPathFeature.Error.Message);
                Logger.LogDebug("Stack trace: {TRACE}", exceptionHandlerPathFeature.Error.StackTrace);
            }

            // Handle other known exception types here (if any)

            // Handle known ApiException types, or create a default JsonResult
            return GetErrorJsonResult(exceptionHandlerPathFeature?.Error ?? new UiApiException("Unknown error"));
        }

        private JsonResult GetErrorJsonResult(Exception exception)
        {
            var error = exception.ToErrorResponse();

            Logger.LogError(exception, "{Msg}", error.Message);

            return new JsonResult(error)
            {
                StatusCode = error.StatusCode
            };
        }
    }
}
