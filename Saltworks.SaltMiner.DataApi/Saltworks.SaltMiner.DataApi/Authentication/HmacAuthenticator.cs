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
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Saltworks.SaltMiner.DataApi.Authentication
{
    public interface IHmacAuthenticator
    {
        string GetHashed(string secret, string message);
        bool IsAuthentic(string secret, IHeaderDictionary headers, string payload);
        string MatchHeader { get; }
    }

    public class FortifySscHmacAuthenticator: IHmacAuthenticator
    {
        public string MatchHeader => "X-SSC-Signature";
        internal bool IgnoreDateSkew { get; set; } = false;
        public string GetHashed(string secret, string message)
        {
            byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            using var hmac = new HMACSHA256(secretBytes);
            var hash = hmac.ComputeHash(messageBytes);
            var computedHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            return computedHash;
        }

        public bool IsAuthentic(string secret, IHeaderDictionary headers, string payload)
        {
            // X-SSC-Signature is already known to exist
            var dateHeader = headers.Date.ToString();

            // Validate date skew (≤5 min)
            var styles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
            if (DateTime.TryParseExact(dateHeader, "ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture, styles, out DateTime reqDate))
            {
                TimeSpan skew = DateTime.UtcNow - reqDate;
                if (Math.Abs(skew.TotalMinutes) > 15 && !IgnoreDateSkew)
                    throw new WebhookValidationException("Date skew too large");
            }
            else
            {
                throw new WebhookValidationException("Invalid date header format");
            }

            // Validate signature
            if (!headers.TryGetValue(MatchHeader, out var vals))
                return false;
            var hdrHashed = vals[0].Replace("sha256=", "");
            return GetHashed(secret, payload + dateHeader) == hdrHashed;
        }
    }
}
