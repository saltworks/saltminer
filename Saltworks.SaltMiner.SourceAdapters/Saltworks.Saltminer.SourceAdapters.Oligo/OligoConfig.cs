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
using System.Linq;
using System.Text.Json;

namespace Saltworks.SaltMiner.SourceAdapters.Oligo
{
    //This is the config for the specfic Source that will always use this validate method.
    //Along with any other source specific field needed
    public class OligoConfig : SourceAdapterConfig
    {
        public OligoConfig()
        {
            SourceAbortErrorCount = 3;
             
        }

        public string BaseAddress { get; set; }
        public int Timeout { get; set; }
        public string ClientSecret { get; set; }
        public string ClientId { get; set; }
        public new static bool IsSaltminerSource { get => true; }
        // Set these two based on current adapter compatibility.  Example versions used here.
        public override string CurrentCompatibleApiVersion => "3.1.0";
        public override string MinimumCompatibleApiVersion => "3.0.8";
        public int ClientMaxRetries = 3;
        public int ClientRetryDelay = 10000;
        public int ClientRateLimitDelay = 25000;
        public int ClientBatchSize = 1500;
        public bool EnableDifferentialUpdates = false; 

        public override string Serialize()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        // Write: override for validate to validate local properties
        public override void Validate()
        {
            base.Validate();
            var missingFields = Core.Helpers.Extensions.IsAnyNullOrEmpty(this);
            var myFields = new string[] { nameof(BaseAddress), nameof(Timeout), nameof(ClientId), nameof(ClientSecret) };
            if (myFields.Any(f => missingFields.Contains(f)))
                throw new SourceConfigurationException($"'{nameof(OligoConfig)}' is missing values. {missingFields}");
        }
    }
}
