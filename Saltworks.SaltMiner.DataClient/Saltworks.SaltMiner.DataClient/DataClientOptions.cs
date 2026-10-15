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
