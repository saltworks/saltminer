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
