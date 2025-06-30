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
using System.Linq;
using System.Text.Json;

namespace Saltworks.SaltMiner.SourceAdapters.CheckmarxOne
{
    //This is the config for the specfic Source that will always use this validate method.
    //Along with any other source specific field needed
    public class CheckmarxOneConfig : SourceAdapterConfig
    {
        public CheckmarxOneConfig()
        {
            SourceAbortErrorCount = 10;
        }

        public string BaseAddress { get; set; }
        public int Timeout { get; set; }
        public string AuthBaseAddress { get; set; }
        public string TenantName { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string ApiKey { get; set; }
        public bool IsOAuth2 { get; set; }
        public override string CurrentCompatibleApiVersion => "3.1.0";
        public override string MinimumCompatibleApiVersion => "3.0.8";
        public string GuiAddress { get; set; }

        public new static bool IsSaltminerSource { get => true; }

        public override string Serialize()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        // Write: override for validate to validate local properties
        public override void Validate()
        {
            base.Validate();
            var missingFields = Core.Helpers.Extensions.IsAnyNullOrEmpty(this);
            var myFields = new string[] { nameof(BaseAddress), nameof(Timeout), nameof(TenantName), nameof(IsOAuth2), nameof(ApiKey) };
            if (myFields.Any(f => missingFields.Contains(f)))
                throw new SourceConfigurationException($"'{nameof(CheckmarxOneConfig)}' is missing values. {missingFields}");
        }

    }
}
