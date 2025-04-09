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

﻿using Microsoft.AspNetCore.Http;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Saltworks.SaltMiner.DataApi.Authentication
{
    public interface IHmacAuthenticator
    {
        string GetHexHashed(string secret, string message);
        bool IsAuthentic(string secret, IHeaderDictionary headers, string payload);
        string MatchHeader { get; }
    }
    
    public class FortifySscHmacAuthenticator: IHmacAuthenticator
    {
        public string MatchHeader => "X-SSC-Signature";
        public string GetHexHashed(string secret, string message)
        {
            var hash = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            return Convert.ToHexString(hash.ComputeHash(Encoding.UTF8.GetBytes(message)));
        }

        public bool IsAuthentic(string secret, IHeaderDictionary headers, string payload)
        {
            if (!headers.TryGetValue(MatchHeader, out var vals))
                return false;
            var hexHashed = vals[0].Replace("sha256=", "");
            if (!headers.TryGetValue("Date", out vals))
                return false;
            var message = vals[0] + payload;
            return GetHexHashed(secret, message) == hexHashed;
        }
    }
}
