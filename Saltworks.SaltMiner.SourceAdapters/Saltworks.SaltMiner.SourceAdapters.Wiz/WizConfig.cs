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
using System.Text.Json;
using Saltworks.SaltMiner.Core.Util;

namespace Saltworks.SaltMiner.SourceAdapters.Wiz
{
    public class WizConfig : SourceAdapterConfig
    {
        public WizConfig()
        {
            SourceAbortErrorCount = 10;
            if (IssueSeverityMap.Count == 0)
            {
                IssueSeverityMap.Add("INFORMATIONAL", Severity.Info.ToString("g"));
                IssueSeverityMap.Add("CRITICAL", Severity.Critical.ToString("g"));
                IssueSeverityMap.Add("HIGH", Severity.High.ToString("g"));
                IssueSeverityMap.Add("MEDIUM", Severity.Medium.ToString("g"));
                IssueSeverityMap.Add("LOW", Severity.Low.ToString("g"));
            }
        }

        public string BaseAddress { get; set; } = "https://api.us17.app.wiz.io/graphql";
        public string SourceTypeIssues { get; } = "Saltworks.WizIssue";
        public string SourceTypeVulns { get; } = "Saltworks.WizVuln";
        public int Timeout { get; set; } = 30;
        public int ReportTimeoutMinutes { get; set; } = 30;
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public new static bool IsSaltminerSource { get => true; }
        public string AuthEndpointAddress { get; set; } = "https://auth.app.wiz.io/oauth/token";
        public string WorkPath { get; set; } = Path.Join("Work", "Wiz");
        public int WizApiBatchSize { get; set; } = 5000;
        public string WizUiIssueUriLeft { get; set; } = "https://app.wiz.io/issues#~(filters~(status~(equals~(~'OPEN~'IN_PROGRESS)))~issue~'";
        public string WizUiIssueUriRight { get; set; } = ")";
        public string WizUiVulnUriLeft { get; set; } = "https://app.wiz.io/explorer/vulnerability-findings#~(entity~(~'";
        public string WizUiVulnUriRight { get; set; } = "*2cSECURITY_TOOL_FINDING))";
        public int MaxIssueOrVulnExceptions { get; set; } = 5;
        public int MaxAssetExceptions { get; set; } = 5;
        public int ApiRetryCount { get; set; } = 2;
        public bool UseExistingReportFile { get; set; } = false;
        public int MaxApiDaysToPull { get; set; } = 14;
        public DateTime? OverrideFromDate { get; set; } = null;
        public string OverrideWizType { get; set; } = null;
        public bool SkipVulns { get; set; } = false;
        public bool SkipIssues { get; set; } = false;
        public override string CurrentCompatibleApiVersion => "3.1.0";
        public override string MinimumCompatibleApiVersion => "3.0.8";

        public override string Serialize()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        public override void Validate()
        {
            base.Validate();
            DisableFirstLoad = true;  // not applicable for this source
            var missingFields = Core.Helpers.Extensions.IsAnyNullOrEmpty(this);
            var myFields = new string[] { nameof(BaseAddress), nameof(Timeout), nameof(ClientId), nameof(ClientSecret), nameof(AuthEndpointAddress) };
            
            if (Array.Exists(myFields, f => missingFields.Contains(f)))
            {
                throw new SourceConfigurationException($"'{nameof(WizConfig)}' is missing values. {missingFields}");
            }
            if (!Directory.Exists(WorkPath))
            {
                throw new SourceConfigurationException($"Invalid {nameof(WorkPath)} setting: directory '{WorkPath}' does not exist.");
            }
            if (SyncHoldForSendThreshold < 200 || SyncResumeWhenSendThreshold < 50 || SyncHoldForSendThreshold - SyncResumeWhenSendThreshold <= 50)
            {
                throw new SourceConfigurationException($"Invalid {nameof(SyncHoldForSendThreshold)} and/or {nameof(SyncResumeWhenSendThreshold)}.  Minimums are 200 and 50 and they must be at least 50 apart.");
            }
            if (SkipVulns && SkipIssues)
            {
                throw new SourceConfigurationException("Both vulnerabilities and issues are set to be skipped - this is invalid.  Please check Wiz source configuration.");
            }
        }
    }
}
