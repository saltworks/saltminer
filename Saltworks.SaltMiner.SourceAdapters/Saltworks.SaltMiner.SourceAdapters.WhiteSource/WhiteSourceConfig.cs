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
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Saltworks.SaltMiner.SourceAdapters.WhiteSource
{
    public class WhiteSourceConfig : SourceAdapterConfig
    {
        public WhiteSourceConfig()
        {
            SourceAbortErrorCount = 10;
            DataApiRetryCount = 3;
        }

        public string BaseAddress { get; set; }
        public int Timeout { get; set; }
        public string WsUserKey { get; set; }
        public bool IncludeCountsInMetrics { get; set; } = false;
        public List<string> WsOrgTokens { get; set; }
        public List<string> VulnerabilityImportTypes { get; set; }
        public new static bool IsSaltminerSource { get => true; }

        public override string Serialize()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        public override void Validate()
        {
            base.Validate();
            var missingFields = Core.Helpers.Extensions.IsAnyNullOrEmpty(this);
            var myFields = new string[] { nameof(BaseAddress), nameof(Timeout), nameof(WsUserKey), nameof(WsOrgTokens), nameof(VulnerabilityImportTypes) };
            if (myFields.Any(f => missingFields.Contains(f)))
                throw new SourceConfigurationException($"'{nameof(WhiteSourceConfig)}' is missing values. {missingFields}");
        }
    }
}
