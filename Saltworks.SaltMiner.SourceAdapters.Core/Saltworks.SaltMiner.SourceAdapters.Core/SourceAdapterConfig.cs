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

﻿using Saltworks.SaltMiner.Core.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace Saltworks.SaltMiner.SourceAdapters.Core
{
    public abstract class SourceAdapterConfig : ConfigBase
    {
        public string ConfigFileName { get; set; }
        public string ConfigDirectory { get; set; } = "SourceConfigs";
        public string Instance { get; set; }
        public string SourceType { get; set; }
        public bool IsSaltminerSource { get; set; } = false;
        public bool HasCustomAssembly { get; set; } = false;
        public string CustomAssemblyName { get; set; } = "N/A";
        public string CustomAssemblyType { get; set; } = "N/A";
        public int SendFailureCount { get; set; } = 3;
        public int SendFailureDeleteDays { get; set; } = 7;
        public int SendErrorMaxRetries { get; set; } = 5;
        public int SendCountValidationErrorRetries { get; set; } = 5;
        public int SendCountValidationErrorRetryDelaySec { get; set; } = 5;
        public bool LogNeedsUpdate { get; set; } = false;
        public List<string> SkipSourceIds { get; set; } = [];
        public int SyncHoldForSendThreshold { get; set; } = 400;
        public int SyncResumeWhenSendThreshold { get; set; } = 50;
        public int TestingAssetLimit { get; set; } = 0;
        /// <summary>
        /// The number of metrics to pull in a batch from the server when running the "first load" feature
        /// </summary>
        public int FirstLoadBatchSize { get; set; } = 5000;
        /// <summary>
        /// If set, will disable First Load feature which attempts to load from the server when missing local source metric data
        /// </summary>
        public bool DisableFirstLoad { get; set; } = false;
        /// <summary>
        /// If set, will disable retiring of assets that were not found in the source
        /// </summary>
        public bool DisableRetire { get; set; } = false;
        /// <summary>
        /// If set, will add a unique number sequence to vulnerability scanner Ids to prevent duplicates
        /// </summary>
        public bool EnableScannerIdNumberSequence { get; set; } = false;
        /// <summary>
        /// How many queue docs to send to the API at once
        /// </summary>
        public int QueueSendBatchSize { get; set; } = 10000;
        /// <summary>
        /// Send scan completion status updates (including flushing the current batch) once this many have collected
        /// </summary>
        public int QueueSendScanUpdateBatchSize { get; set; } = 100;
        /// <summary>
        /// How many scans to batch together when sending to the API
        /// </summary>
        public int SourceAbortErrorCount { get; set; } = 3;
        /// <summary>
        /// Delay in sec to wait when no queue scans are ready but loading is not complete
        /// </summary>
        public int StillLoadingDelay { get; set; } = 2;
        /// <summary>
        /// Delay in ms to wait for local queue operations to complete
        /// </summary>
        public int LoadingDelay { get; set; } = 0;
        public bool VerifySsl { get; set; } = true;
        public string LastScanDaysPolicy { get; set; } = "60";
        public bool FullSyncMaintEnabled { get; set; } = false;
        public int FullSyncBatchSize { get; set; } = 100;
        public bool DisableVersionChecking { get; set; } = false;
        public abstract string CurrentCompatibleApiVersion { get; }
        public abstract string MinimumCompatibleApiVersion { get; }

        public Dictionary<string, string> IssueSeverityMap { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// Has to be overridden so can call DecryptProperties for the child class
        /// </summary>
        public abstract string Serialize();
        /// <summary>
        /// The name of the attribute that will be used to define the asset inventory key
        /// </summary>
        public string InventoryAssetKeyAttribute { get; set; } = string.Empty;

        public static string ValidateConfigFileName(string directory, string fileName)
        {
            var filePath = Path.Combine(directory, fileName);

            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(directory, fileName + ".json");
            }

            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(directory, fileName + "Config.json");
            }

            if (!File.Exists(filePath))
            {
                throw new SourceConfigurationException($"Can not locate ConfigFileName {fileName} (directory '{directory}').");
            }

            return filePath;
        }

        public virtual void Validate()
        {
            var missingFields = Helpers.Extensions.IsAnyNullOrEmpty(this);
            var myFields = new string[] { nameof(ConfigFileName), nameof(ConfigDirectory), nameof(Instance), nameof(SourceType) };
            if (Array.Exists(myFields, f => missingFields.Contains(f)))
                throw new SourceConfigurationException($"'{nameof(SourceAdapterConfig)}' is missing values. {missingFields}");

            var filePath = ValidateConfigFileName(ConfigDirectory, ConfigFileName);
            CheckEncryption(this, filePath);

            var toLower = new Dictionary<string, string>();
            foreach (var key in IssueSeverityMap.Keys)
                toLower.Add(key.ToLower(), IssueSeverityMap[key]);
            IssueSeverityMap = toLower;

            DecryptProperties(this);
        }
    }
}
