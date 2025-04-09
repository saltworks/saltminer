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

﻿using System;

namespace Saltworks.SaltMiner.DataClient
{
    public class DataClientOptions
    {
        // ApiClient pass-through settings
        public string ApiKey { get; set; }
        public string ApiKeyHeader { get; set; } = "Authorization";
        public string ApiBaseAddress { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
        public bool VerifySsl { get; set; } = true;
        public bool LogExtendedErrorInfo { get; set; } = false;
        public bool LogApiCallsAsInfo { get; set; } = false;
        /// <summary>
        /// DataClient specific settings
        /// </summary>
        public DataClientConfig RunConfig { get; set; } = new();
    }
}
