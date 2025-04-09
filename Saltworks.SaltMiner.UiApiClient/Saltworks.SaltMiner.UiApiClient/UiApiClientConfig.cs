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

﻿namespace Saltworks.SaltMiner.UiApiClient
{
    public class UiApiClientConfig
    {
        /// <summary>
        /// How many times to retry a failed API call (if failure is a server error)
        /// </summary>
        public int UiApiApiRetryCount { get; set; } = 3;
        /// <summary>
        /// How long (in seconds) to wait between retries in a retry situation
        /// </summary>
        public int UiApiApiDelaySec { get; set; } = 10;
        /// <summary>
        /// Reporting service API key (if applicable)
        /// </summary>
        public string ReportingApiKey { get; set; }
        /// <summary>
        /// Header in which to put the reporting service API key
        /// </summary>
        public string ReportingApiAuthHeader { get; set; }
    }
}
