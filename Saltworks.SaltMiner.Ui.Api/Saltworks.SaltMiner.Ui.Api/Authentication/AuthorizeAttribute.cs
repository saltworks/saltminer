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
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.UiApiClient;

namespace Saltworks.SaltMiner.Ui.Api.Authentication
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAttribute(params SysRole[] roles) : Attribute, IAuthorizationFilter
    {
        private readonly IList<SysRole> ReqRoles = roles ?? [];

        // Design decision: override AuthorizationAttribute so can support config.KibanaAuthBypass
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            IServiceProvider services = context.HttpContext.RequestServices;
            var settings = services.GetService<UiApiConfig>() ?? throw new UiApiConfigurationException("Settings cannot be null");
            var logger = services.GetService<ILogger<AuthorizeAttribute>>();
            
            if (context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any())
            {
                logger.LogDebug("Anonymous authentication set, user passed");
                return; // Anonymous ok, don't need to check at all
            }

            if (context.HttpContext.Request.Headers.TryGetValue(settings.ReportingApiKeyHeader, out var reportUserApiKey) && reportUserApiKey == settings.ReportingApiKey)
            {
                logger.LogDebug("Report user api key found, cookie monster get no cookie.");
                return; // Report API key present, ok
            }

            var user = (KibanaUser)context.HttpContext.Items[KibanaMiddleware.USER_TAG];
            if (user != null && !ReqRoles.Any() && user.Roles.Count != 0)
            {
                logger.LogDebug("Any role accepted, user passed");
                return; // Any role ok
            }

            // This sketchy bit of linq determines if any of the required roles match any of the presented user roles.  If no match, then 401
            if (user == null)
            {
                logger.LogDebug("User not unauthorized response");
                throw new UiApiUnauthorizedException("Unauthorized");
            } 
            
            if (ReqRoles.Any() && !ReqRoles.Any(reqRole => user.Roles.Exists(usrRole => SaltMiner.Core.Extensions.EnumExtensions.GetDescription(reqRole) == usrRole)))
            {
                logger.LogDebug("User role(s): [{Roles}] not accepted, unauthorized response", string.Join(',', user.Roles));
                throw new UiApiForbiddenException("Forbidden");
            }
        }
    }
}