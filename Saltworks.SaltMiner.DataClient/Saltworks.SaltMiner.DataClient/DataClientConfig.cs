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

﻿namespace Saltworks.SaltMiner.DataClient
{
    public class DataClientConfig
    {
        /// <summary>
        /// How many times to retry a failed API call (if failure is a server error)
        /// </summary>
        public int ApiClientRetryCount { get; set; } = 3;
        /// <summary>
        /// How long (in seconds) to wait between retries in a retry situation
        /// </summary>
        public int ApiClientRetryDelaySec { get; set; } = 10;
        /// <summary>
        /// Disables automatic initial connection attempt by the DataClient. Defaults to false.
        /// </summary>
        /// <remarks>Useful for debug/tests, as you can take actions on objects before a DataClient connection is attempted.</remarks>
        public bool DisableInitialConnection { get; set; } = false;
    }
}
