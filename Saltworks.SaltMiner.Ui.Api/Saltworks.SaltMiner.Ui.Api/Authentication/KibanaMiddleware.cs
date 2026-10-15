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

﻿using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.Ui.Api.Models;

namespace Saltworks.SaltMiner.Ui.Api.Authentication
{
    public class KibanaMiddleware(UiApiConfig config, RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;
        public const string USER_TAG = "PentestUser";
        private readonly UiApiConfig _config = config;

        // Design decision: roll our own custom user and stick it in the context
        // Could create a ClaimsPrincipal instead, but for our use case currently there's no
        // benefit to that extra complexity because we are rolling our own AuthorizeAttribute anyway
        public async Task Invoke(HttpContext context, AuthContext authContext)
        {
            var logger = context.RequestServices.GetService<ILogger<KibanaMiddleware>>();
            var cookie = string.IsNullOrEmpty(context.Request.Cookies[KibanaUser.CookieTag]) ? _config.BypassCookie ?? string.Empty : context.Request.Cookies[KibanaUser.CookieTag];

            logger.LogDebug("ByPassCookie: {Cookie}", _config.BypassCookie);
            logger.LogDebug("KibanaUser.COOKIE_TAG: {Tag}", KibanaUser.CookieTag + " Value: " + context.Request.Cookies[KibanaUser.CookieTag]);

            if (!string.IsNullOrEmpty(cookie))
            {
                logger.LogDebug("Auth cookie found, cookie monster happy!");
                context.Items[USER_TAG] = authContext.GetMe(cookie, context).Data;
            }
            else
            {
                logger.LogDebug("Auth cookie not found, cookie monster sad...");
                context.Items[USER_TAG] = null;
            }

            await _next(context);
        }
    }
}