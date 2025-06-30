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

﻿using Saltworks.SaltMiner.SourceAdapters.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Saltworks.SaltMiner.SourceAdapters.Sonatype
{
    public class SonatypeConfig : SourceAdapterConfig
    {
        public SonatypeConfig()
        {
            SourceAbortErrorCount = 10;
        }
        public string BaseAddress { get; set; }
        public int Timeout { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public List<string> VulnerabilityImportTypes { get; set; } = [];
        public new static bool IsSaltminerSource { get => true; }
        public string AppReportBaseUrl { get; set; }
        public override string CurrentCompatibleApiVersion => "3.1.0";
        public override string MinimumCompatibleApiVersion => "3.0.8";

        public override string Serialize()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }
        public override void Validate()
        {
            base.Validate();
            var missingFields = Core.Helpers.Extensions.IsAnyNullOrEmpty(this);
            var myFields = new string[] { nameof(BaseAddress), nameof(Timeout), nameof(UserName), nameof(Password), nameof(VulnerabilityImportTypes), nameof(AppReportBaseUrl) };
            if (myFields.Any(f => missingFields.Contains(f)))
                throw new SourceConfigurationException($"'{nameof(SonatypeConfig)}' is missing values. {missingFields}");
        }
    }
}
