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

