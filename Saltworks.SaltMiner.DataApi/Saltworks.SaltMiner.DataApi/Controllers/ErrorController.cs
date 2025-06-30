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
