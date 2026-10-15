/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
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
        public override string CurrentCompatibleApiVersion => "3.2.0";
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
