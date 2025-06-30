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

﻿using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.DataApi.Contexts;

namespace Saltworks.SaltMiner.DataApi.Models
{
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        private readonly ILogger<ValidateModelAttribute> Logger;
        private readonly ErrorContext ErrorContext;

        public ValidateModelAttribute(ILogger<ValidateModelAttribute> logger, ErrorContext errorContext)
        {
            Logger = logger;
            ErrorContext = errorContext;
        }

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (!filterContext.ModelState.IsValid)
            {
                var modelErrors = ErrorContext.HarvestModelErrors(filterContext.ModelState);
                throw new ApiValidationException(modelErrors);
            }
        }
    }
}
