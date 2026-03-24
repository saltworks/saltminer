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
