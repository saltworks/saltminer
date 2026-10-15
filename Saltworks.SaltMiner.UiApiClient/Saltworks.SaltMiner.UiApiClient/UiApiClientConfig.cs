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
