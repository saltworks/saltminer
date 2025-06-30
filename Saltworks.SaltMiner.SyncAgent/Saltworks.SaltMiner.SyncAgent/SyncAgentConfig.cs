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
