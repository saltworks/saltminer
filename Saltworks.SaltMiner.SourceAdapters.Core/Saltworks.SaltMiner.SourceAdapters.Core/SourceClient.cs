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

﻿using Microsoft.Extensions.Logging;
using Saltworks.Utility.ApiHelper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.SourceAdapters.Core
{
    // This class should help implementation classes with template elements more than real functionality
    public abstract class SourceClient
    {
        protected readonly ApiClient ApiClient;
        protected readonly ILogger Logger;

        protected SourceClient(ApiClient client, ILogger logger)
        {
            ApiClient = client;
            Logger = logger;
        }

        /// <summary>
        /// Helper to set ApiClient defaults
        /// </summary>
        /// <param name="baseAddress">Base API address</param>
        /// <param name="timeout">Base API timeout</param>
        /// <param name="headers">Default headers to use</param>
        /// <param name="exceptionOnFailure">Whether to throw an exception when the response is an error (not 20x).</param>
        public void SetApiClientDefaults(string baseAddress, int timeout, ApiClientHeaders headers = null, bool exceptionOnFailure = true)
        {
            ApiClient.BaseAddress = baseAddress;
            
            if (headers != null)
            {
                ApiClient.Options.DefaultHeaders.Headers.Clear();
                
                foreach (var h in headers.Headers)
                {
                    ApiClient.Options.DefaultHeaders.Add(h.Key, h.Value.First(), true);
                }
            }

            ApiClient.Options.ExceptionOnFailure = exceptionOnFailure;
            ApiClient.Options.Timeout = TimeSpan.FromSeconds(timeout);
        }
    }

    public class SourceClientResult<T> where T: class
    {
        public IEnumerable<T> Results { get; set; }
        public string Token { get; set; }
    }

    public class SourceClientIssueToken
    {
        public string SourceId { get; set; }
        public string Token { get; set; }
    }
}
