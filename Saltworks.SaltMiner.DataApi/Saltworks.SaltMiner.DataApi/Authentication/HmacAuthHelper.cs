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

﻿using System.Reflection;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.DataApi.Authentication;

public static class HmacAuthHelper
{
    public static bool Authenticate(string type, Dictionary<string, string> secrets, IHeaderDictionary headers, string payload, ILogger logger = null)
    {
        try
        {
            var ha = FindMatchingAuthenticator(headers);
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
            return ha.IsAuthentic(secret, headers, payload);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failure to perform HMAC authentication for this web hook call: [{Type}] {Msg}", ex.GetType().Name, ex.InnerException?.Message ?? ex.Message);
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
