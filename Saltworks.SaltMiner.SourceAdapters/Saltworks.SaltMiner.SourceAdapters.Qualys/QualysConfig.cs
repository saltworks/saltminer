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

﻿using Saltworks.SaltMiner.SourceAdapters.Core;
using System;
using System.Text.Json;

namespace Saltworks.SaltMiner.SourceAdapters.Qualys
{
    public class QualysConfig : SourceAdapterConfig
    {
        public QualysConfig()
        {
            SourceAbortErrorCount = 10;
        }

        public string BaseAddress { get; set; }
        public int Timeout { get; set; } = 300;
        public string Username { get; set; }
        public string Password { get; set; }
        public string SyncRecordDateFormat { get; set; } = "yyyy-MM-dd HH:mm";
        public int HostDetectionHostBatchSize { get; set; } = 100;
        public int KbIssueBatchSize { get; set; } = 2000;
        public int KbBatchSize { get; set; } = 200;
        public bool LogApiCalls { get; set; } = false;
        public bool EnableScanSync { get; set; } = true;
        public bool EnablePostApiCalls { get; set; } = false;
        public bool IncludeInfoIssues { get; set; } = false;
        public string GuiUrlTemplate { get; set; } = "https://qualysguard.qg4.apps.qualys.com/vm/#/vulndetails/{id}?detail=Vul&assetDetail=%2Fvulnerabilities%2Fasset%2F";
        public new static bool IsSaltminerSource { get => true; }
        public DateTime? OverrideStartDate { get; set; } = null;
        public override string CurrentCompatibleApiVersion => "3.0.8";
        public override string MinimumCompatibleApiVersion => "3.0.8";

        public override string Serialize()
        {
            var opts = JsonSerializerOptions.Default;
            var orgVal = opts.WriteIndented;
            opts.WriteIndented = true;
            var rsp = JsonSerializer.Serialize(this, opts);
            opts.WriteIndented = orgVal;
            return rsp;
        }

        public override void Validate()
        {
            base.Validate();
            var missingFields = Core.Helpers.Extensions.IsAnyNullOrEmpty(this);
            var myFields = new string[] { nameof(BaseAddress), nameof(Timeout), nameof(Username), nameof(Password) };
            if (Array.Exists(myFields, f => missingFields.Contains(f)))
                throw new SourceConfigurationException($"'{nameof(QualysConfig)}' is missing values. {missingFields}");
        }
    }
}
