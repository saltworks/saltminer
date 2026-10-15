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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.DataApi.Extensions;
using Saltworks.SaltMiner.DataApi.Models;
using System;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("[controller]")]
    public class ErrorController : ControllerBase
    {
        private readonly ILogger Logger;

        public ErrorController(ILogger<ErrorController> logger, IServiceProvider serviceProvider)
        {
            Logger = logger;
            var config = serviceProvider.GetService<ApiConfig>();
        }

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
            return GetErrorJsonResult(exceptionHandlerPathFeature?.Error ?? new ApiException("Unknown error"));
        }

        [Route("test")]
        public ActionResult Test()
        {
            throw new ApiException("Darn it all...");
        }

        private JsonResult GetErrorJsonResult(Exception ex)
        {
            var er = ex.ToErrorResponse();
            
            Logger.LogError(ex, er.Message);
            
            return new JsonResult(er)
            {
                StatusCode = er.StatusCode
            };
        }
    }
}
