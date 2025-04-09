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

namespace Saltworks.SaltMiner.SourceAdapters.Contrast
{
    public class ContrastConfig : SourceAdapterConfig
    {
        public ContrastConfig()
        {
            SourceAbortErrorCount = 10;
            DataApiRetryCount = 3;
        }

        public string BaseAddress { get; set; }
        public int Timeout { get; set; }
        public string ApiKey { get; set; }
        public List<string> OrgIds { get; set; }
        public int ApplicationBatchSize { get; set; }
        public string AuthorizationHeader { get; set; }
        public new static bool IsSaltminerSource { get => true; }
        public List<string> VulnerabilityImportStatus { get; set; } = new List<string>();

        public override string Serialize()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        public override void Validate()
        {
            base.Validate();
            var missingFields = Core.Helpers.Extensions.IsAnyNullOrEmpty(this);
            var myFields = new string[] { nameof(BaseAddress), nameof(Timeout), nameof(ApiKey), nameof(OrgIds), nameof(ApplicationBatchSize), nameof(AuthorizationHeader) };
            if (myFields.Any(f => missingFields.Contains(f)))
                throw new SourceConfigurationException($"'{nameof(ContrastConfig)}' is missing values. {missingFields}");
        }

    }
}
