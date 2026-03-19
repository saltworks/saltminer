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
