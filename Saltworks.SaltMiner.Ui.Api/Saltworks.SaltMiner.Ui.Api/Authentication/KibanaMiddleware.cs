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