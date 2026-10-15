/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
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
