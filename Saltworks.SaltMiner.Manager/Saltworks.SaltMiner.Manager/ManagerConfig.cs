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

﻿using Microsoft.Extensions.Configuration;
using Saltworks.SaltMiner.Core.Common;
using System.IO;

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

            this.PublicLicenseKey = File.ReadAllText(this.KeyPath);

            CheckEncryption(this, filePath, "ManagerConfig");

            DecryptProperties(this);
        }
        public string DataApiBaseUrl { get; set; }
        public bool DataApiVerifySsl { get; set; } = true;
        public string DataApiKey { get; set; }
        public string DataApiKeyHeader { get; set; } = "Authorization";
        public int DataApiTimeoutSec { get; set; } = 10;
        public int QueueProcessorInstances { get; set; } = 1;
        public int QueueProcessorQueueBatchSize { get; set; } = 500;
        public int QueueProcessorMaxErrors { get; set; } = 3;
        public int QueueProcessorIssueBatchSize { get; set; } = 1000;
        public bool QueueProcessorOneScanOneAssessmentType { get; set; } = true;
        public bool QueueProcessorDisableExistingIssuesCountChecking { get; set; } = false;
        public int QueueProcessorMaxRecentSourceIdCount { get; set; } = 250;
        public int QueueProcessorMaxRecentAssetCount { get; set; } = 10;
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
        public int IssueProcessingBatchSize { get; set; } = 500;
        public string WebUiBaseUrl { get; set; }
        public string PublicLicenseKey { get; set; }
        public string KeyPath { get; set; } = "license.lnf";
        public string CommunityPath { get; set; } = "community.blt";
        public bool ProcessNoScan { get; set; } = true;
        public string IssuesActiveIndexTemplate { get; set; } = "issues_[assetType]_[sourceType]_[instance]";
    }
}