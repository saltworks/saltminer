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

namespace Saltworks.SaltMiner.SourceAdapters.MendSca
{
    public class MendScaConfig : SourceAdapterConfig
    {
        public MendScaConfig()
        {
            SourceAbortErrorCount = 10;
        }

        public string BaseAddress { get; set; }
        public int Timeout { get; set; } = 30;
        public string UserKey { get; set; }
        public bool IncludeCountsInMetrics { get; set; } = false;
        public List<string> OrgTokens { get; set; }
        public List<string> VulnerabilityImportTypes { get; set; }
        public new static bool IsSaltminerSource { get => true; }
        public int ProductsPullThreshold { get; set; } = 50;
        public override string CurrentCompatibleApiVersion => "3.0.8";
        public override string MinimumCompatibleApiVersion => "3.0.8";

        public override string Serialize()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        public override void Validate()
        {
            base.Validate();
            
            var missingFields = Core.Helpers.Extensions.IsAnyNullOrEmpty(this);
            var myFields = new string[] { nameof(BaseAddress), nameof(Timeout), nameof(UserKey), nameof(OrgTokens), nameof(VulnerabilityImportTypes) };
            
            if (Array.Exists(myFields, f => missingFields.Contains(f)))
            {
                throw new SourceConfigurationException($"'{nameof(MendScaConfig)}' is missing values. {missingFields}");
            }
        }
    }
}
