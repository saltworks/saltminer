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
