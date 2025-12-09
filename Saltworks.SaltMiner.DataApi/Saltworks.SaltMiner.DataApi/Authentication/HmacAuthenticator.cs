/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
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
