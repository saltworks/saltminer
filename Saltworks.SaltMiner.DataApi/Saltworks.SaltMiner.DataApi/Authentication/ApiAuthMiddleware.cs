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

﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.DataApi.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.DataApi.Authentication;

public class ApiAuthMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task Invoke(HttpContext context, ApiConfig config)
    {
        var logger = context.RequestServices.GetService<ILogger<ApiAuthMiddleware>>();

        // Auth here
        var ok = context.Request.Headers.TryGetValue(config.AuthHeader, out var apiKeyValues);
        var apiKey = "";
        var apiKeyValue = apiKeyValues.FirstOrDefault();

        if (ok && !string.IsNullOrEmpty(apiKeyValue))
        {
            apiKey = Regex.Replace(apiKeyValue, config.AuthType + " ?", "", RegexOptions.IgnoreCase);
        }

        // Lookup auth key
        var role = config.ApiKeys.FirstOrDefault(e => e.Key == apiKey).Value;
        
        // Validate role - if invalid, log config error (but don't throw one)
        if (Enum.TryParse<Role>(role, out var eRole))
        {
            role = eRole.ToString("g");
        }
        else
        {
            if (role != null)
            {
                var vRoles = string.Join(",", Enum.GetValues<Role>());
                logger.LogError("API key authenticated role '{Role}', but that role isn't valid.  Check config.  Roles are case sensitive and include: {Roles}", role, vRoles);
            }
            role = "";
        }

        // Make a few chars of the key available to logging
        var ending = apiKey.ToString();
        if (ending.Length > 4)
        {
            ending = ending[^5..];
        }

        // Build context user for use in controller auth / context methods
        context.User = new([
                new([
                    new Claim(ClaimTypes.Role, role)
                ])
            ]);

        logger?.LogDebug("Request successfully authenticated role '{Role}' and api key ending in '{Ending}'", role, ending);


        await _next(context);
    }
}

