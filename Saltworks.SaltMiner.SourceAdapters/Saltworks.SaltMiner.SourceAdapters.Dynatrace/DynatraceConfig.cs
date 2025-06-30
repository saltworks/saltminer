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
using System.Text.Json;

namespace Saltworks.SaltMiner.SourceAdapters.Dynatrace
{
    //This is the config for the specfic Source that will always use this validate method.
    //Along with any other source specific field needed
    public class DynatraceConfig : SourceAdapterConfig
    {
        public DynatraceConfig()
        {
            SourceAbortErrorCount = 10;
        }

        public string BaseAddress { get; set; }
        public int Timeout { get; set; }
        public int ApiRetryCount { get; set; } = 2;
        public int BatchLimit { get; set; } = 100;
        public string AuthEndpointAddress { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string EntityQueryOverride { get; set; }
        public string VulnQueryOverride { get; set; }
        public new static bool IsSaltminerSource { get => true; }
        public override string CurrentCompatibleApiVersion => "3.1.0";
        public override string MinimumCompatibleApiVersion => "3.0.8";
        private readonly JsonSerializerOptions SerializationOptions = new() { WriteIndented = true };

        public override string Serialize()
        {
            return JsonSerializer.Serialize(this, SerializationOptions);
        }

        public override void Validate()
        {
            base.Validate();
            var missingFields = Core.Helpers.Extensions.IsAnyNullOrEmpty(this);
            var myFields = new string[] { nameof(BaseAddress), nameof(Timeout) };
            if (myFields.Any(f => missingFields.Contains(f)))
                throw new SourceConfigurationException($"'{nameof(DynatraceConfig)}' is missing values. {missingFields}");
        }

    }
}
