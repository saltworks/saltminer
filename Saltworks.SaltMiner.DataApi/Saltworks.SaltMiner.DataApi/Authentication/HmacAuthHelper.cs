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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
﻿using System.Reflection;

namespace Saltworks.SaltMiner.DataApi.Authentication;

public static class HmacAuthHelper
{
    public static string GetPayload(HttpRequest request)
    {
        // Because body is signed, have to read directly from stream to get exact whitespace
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, System.Text.Encoding.UTF8, leaveOpen: true);
        var payload = reader.ReadToEndAsync().Result;
        request.Body.Position = 0;
        return payload;
    }

    public static bool Authenticate(string type, Dictionary<string, string> secrets, HttpRequest request, ILogger logger = null)
    {
        try
        {
            var ha = FindMatchingAuthenticator(request.Headers);
            if (ha == null)
            {
                logger?.LogError("Unable to find HMAC authenticator for this web hook call.");
                return false;
            }
            if (!secrets.TryGetValue(type, out var secret))
            {
                logger?.LogWarning("Web hook call matched to authenticator '{Auth}' but found no matching secret in WebhookSecrets dictionary for '{Key}'.", ha.GetType().Name, type);
                return false;
            }
            return ha.IsAuthentic(secret, request.Headers, GetPayload(request));
        }
        catch (WebhookValidationException ex)
        {
            logger?.LogError(ex, "Webhook validation exception thrown for '{Type}' webhook: {Msg}", type, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failure to perform HMAC authentication for '{Wtype}' webhook: [{Etype}] {Msg}", type, ex.GetType().Name, ex.InnerException?.Message ?? ex.Message);
            return false;
        }
    }

    private static IHmacAuthenticator FindMatchingAuthenticator(IHeaderDictionary requestHeaders)
    {
        // Get the executing assembly
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Find all types that implement IHmacAuthenticator
        var authenticatorTypes = assembly.GetTypes()
            .Where(t => typeof(IHmacAuthenticator).IsAssignableFrom(t)
                     && !t.IsInterface
                     && !t.IsAbstract);

        // Create instances and check for header match
        foreach (var type in authenticatorTypes)
        {
            try
            {
                // Instantiate the type (assuming it has a parameterless constructor)
                if (Activator.CreateInstance(type) is IHmacAuthenticator instance && requestHeaders.ContainsKey(instance.MatchHeader))
                {
                    return instance;
                }
            }
            catch (Exception)
            {
                // Nothing to do, don't care about any failure here
            }
        }

        // Return null if no match is found
        return null;
    }
}
