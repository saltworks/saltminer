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
        public string GetHashed(string secret, string message)
        {
            //var hashSecret = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            //var hash = hashSecret.ComputeHash(Encoding.UTF8.GetBytes(message));
            //var hashed = BitConverter.ToString(hash).Replace("-", "").ToLower();
            ////var hashed = Convert.ToHexString(hash);
            //return hashed;
            byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            // Act
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
            if (DateTime.TryParseExact(dateHeader, "ddd, dd MMM yyyy HH:mm:ss zzz", new CultureInfo("en-us"), DateTimeStyles.AssumeUniversal, out DateTime reqDate))
            {
                TimeSpan skew = DateTime.UtcNow - reqDate;
                if (Math.Abs(skew.TotalSeconds) > 300)
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
            return GetHashed(secret, dateHeader + payload) == hdrHashed;
        }
    }
}
