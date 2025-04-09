/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-04-09
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿namespace Saltworks.SaltMiner.Ui.Api.Extensions
{
    public static class HttpContextExtensions
    {
        private const string PROTO_HEADER = "X-Forwarded-Proto";
        private const string HOST_HEADER = "Host";
        private const string HOSTX_HEADER = "X-Forwarded-Host";
        
        /// <summary>
        /// Returns r-proxy base URL if headers present, otherwise returns request scheme and host
        /// </summary>
        public static string GetBaseUrl(this HttpContext context, ILogger logger = null)
        {
            logger?.LogDebug("context.Request.Headers.ContainsKey(HOST_HEADER): {B1}, context.Request.Headers.ContainsKey(HOSTX_HEADER): {B2}, context.Request.Headers.ContainsKey(PROTO_HEADER): {B3}", context.Request.Headers.ContainsKey(HOST_HEADER), context.Request.Headers.ContainsKey(HOSTX_HEADER), context.Request.Headers.ContainsKey(PROTO_HEADER));
            if ((context.Request.Headers.ContainsKey(HOST_HEADER) || context.Request.Headers.ContainsKey(HOSTX_HEADER)) && context.Request.Headers.ContainsKey(PROTO_HEADER))
            {
                var host = context.Request.Headers.ContainsKey(HOST_HEADER) ? context.Request.Headers[HOST_HEADER][0] : context.Request.Headers[HOSTX_HEADER][0];
                return $"{context.Request.Headers[PROTO_HEADER][0]}://{host}";
            }
            return $"{context.Request.Scheme}://{context.Request.Host}";
        }
    }
}
