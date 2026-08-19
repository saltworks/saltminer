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
using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.Manager
{
    // ConfigBase class provides decryption support for configuration properties ending in -password, -key, and -secret
    // Encryption support requires the use of an EncryptionKey and EncryptionIV that can be generated along with encrypted data using the SaltMiner CLI.
    // ConfigBase may offer additional config-related features in the near future as well
    public class ManagerConfig : ConfigBase
    {
        public ManagerConfig() { }
        
        /// <summary>
        /// Binds and decrypts in one easy step! Now with all natural grape flavor!
        /// </summary>
        /// <param name="config"></param>
        public ManagerConfig(IConfiguration config, string filePath)
        {
            config.Bind(this);
            CheckEncryption(this, filePath, "ManagerConfig");
            DecryptProperties(this);
        }
        public string DataApiBaseUrl { get; set; }
        public bool DataApiVerifySsl { get; set; } = true;
        public string DataApiKey { get; set; }
        public string DataApiKeyHeader { get; set; } = "Authorization";
        public int DataApiTimeoutSec { get; set; } = 10;
        /// <summary>Cache the api host's resolved address and prefer it over DNS - see DataClientConfig.</summary>
        public bool DataApiHostCacheEnabled { get; set; } = true;
        /// <summary>Where to cache it.  Delete the file to force a fresh lookup.</summary>
        public string DataApiHostCacheFile { get; set; } = "/tmp/data-api-host.json";
        public int QueueProcessorInstances { get; set; } = 1;
        public int QueueProcessorQueueBatchSize { get; set; } = 1000;
        public int QueueProcessorMaxErrors { get; set; } = 3;
        public int QueueProcessorIssueBatchSize { get; set; } = 1000;
        public bool QueueProcessorOneScanOneAssessmentType { get; set; } = true;
        public bool QueueProcessorDisableExistingIssuesCountChecking { get; set; } = false;
        public int QueueProcessorMaxRecentSourceIdCount { get; set; } = 250;
        public int QueueProcessorMaxRecentAssetCount { get; set; } = 10;
        public int QueueProcessorInstanceBailoutCount { get; set; } = 300;
        public int SnapshotProcessorBatchSize { get; set; } = 1000;
        public int SnapshotProcessorMaxErrors { get; set; } = 3;
        public int SnapshotProcessorErrorRetryDelaySec { get; set; } = 60;
        public int SnapshotProcessorApiBatchSize { get; set; } = 100;
        public int CleanupCompleteAfterHours { get; set; } = 48;
        public int CleanupErrorAfterHours { get; set; } = 168;
        public int CleanupProcessingAfterHours { get; set; } = 12;
        public int CleanupLoadingAfterHours { get; set; } = 48;
        public int CleanupProcessorBatchSize { get; set; } = 500;
        public int CleanupProcessorBatchDelayMs { get; set; } = 0;
        public int CleanupProcessorMaxTaskCount { get; set; } = 200;
        public int CleanupProcessorMaxOrphanSearch { get; set; } = 500000;
        public string[] CleanupProcessorDisableForStatus { get; set; } = [ QueueScan.QueueScanStatus.Loading.ToString("g") ];
        public string WebUiBaseUrl { get; set; }
        public bool ProcessNoScan { get; set; } = true;
        public string IssuesActiveIndexTemplate { get; set; } = "issues_[assetType]_[sourceType]_[instance]";
    }
}