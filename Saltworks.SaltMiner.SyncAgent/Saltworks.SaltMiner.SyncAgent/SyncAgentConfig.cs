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

﻿using Microsoft.Extensions.Configuration;
using Saltworks.SaltMiner.Core.Common;
using Saltworks.SaltMiner.SourceAdapters.Core;
using System.Collections.Generic;
using System.IO;

namespace Saltworks.SaltMiner.SyncAgent
{
    // ConfigBase class provides decryption support for configuration properties ending in -password, -key, and -secret
    // Encryption support requires the use of an EncryptionKey and EncryptionIV that can be generated along with encrypted data using the SaltMiner CLI.
    // ConfigBase may offer additional config-related features in the near future as well
    public class SyncAgentConfig : ConfigBase
    {

        /// <summary>
        /// Binds and decrypts in one easy step! Now with all natural grape flavor!
        /// </summary>
        /// <param name="config"></param>
        public SyncAgentConfig(IConfiguration config, string filePath)
        {
            config.Bind(this);

            this.PublicLicenseKey = File.ReadAllText(this.KeyPath);

            CheckEncryption(this, filePath, "AgentConfig");

            DecryptProperties(this);
        }
        public SyncAgentConfig()
        {
        }

        public List<SourceAdapterConfig> SourceConfigs { get; set; }
        public string DataApiBaseUrl { get; set; }
        public bool DataApiVerifySsl { get; set; } = true;
        public string DataApiKey { get; set; }
        public string DataApiKeyHeader { get; set; } = "Authorization";
        public int DataApiTimeoutSec { get; set; } = 10;
        public string PublicLicenseKey { get; set; }
        public string KeyPath { get; set; } = "license.lnf";
        public string CommunityPath { get; set; } = "community.blt";
        public bool LogSrcApiCallsAsInfo { get; set; } = false;
        public bool LogSrcApiErrorInfo { get; set; } = false;
        public string ApiProxyUri { get; set; } = "";
        public string ApiProxyUser { get; set; } = "";
        public string ApiProxyPassword { get; set; } = "";
        public bool ApiProxyBypassOnLocal { get; set; } = false;
    }
}
