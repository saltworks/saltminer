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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Saltworks.SaltMiner.DataApi.Authentication
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthAttribute : Attribute, IAuthorizationFilter
    {
        private readonly IList<Role> ReqRoles;

        public AuthAttribute(params Role[] roles)
        {
            ReqRoles = roles ?? Array.Empty<Role>();
        }

        // Design decision: override AuthorizationAttribute so can avoid full Authentication/Authorization system for API key auth
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            IServiceProvider services = context.HttpContext.RequestServices;
            var logger = services.GetService<ILogger<AuthAttribute>>();

            var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
            if (allowAnonymous)
            {
                logger.LogDebug("Anonymous authentication set, user passed");
                return; // Anonymous ok, don't need to check at all
            }

            var user = context.HttpContext.User;
            if (user != null && !ReqRoles.Any() && user.Claims.Any(c => c.Type == ClaimTypes.Role))
            {
                logger.LogDebug("Any role accepted, user passed");
                return; // Any role ok
            }
            
            if (user == null || !user.Claims.Any(c => c.Type == ClaimTypes.Role) || user?.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value == "")
            {
                logger.LogDebug("User not unauthorized response");
                throw new ApiUnauthorizedException("Unauthorized");
            } 

            // This sketchy bit of linq determines if any of the required roles match any of the presented user roles.  If no match, then 401
            if (ReqRoles.Any() && !ReqRoles.Any(reqRole => user.IsInRole(reqRole.ToString("g"))))
            {
                var rl = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
                logger.LogDebug("User role(s): [{Roles}] not accepted, unauthorized response", string.Join(',', rl));
                throw new ApiForbiddenException("Forbidden");
            }
        }
    }
}