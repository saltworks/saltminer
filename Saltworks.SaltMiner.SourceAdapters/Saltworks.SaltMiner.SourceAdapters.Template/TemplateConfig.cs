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
using System.Linq;
using System.Text.Json;

namespace Saltworks.SaltMiner.SourceAdapters.Template
{
    //This is the config for the specfic Source that will always use this validate method.
    //Along with any other source specific field needed
    public class TemplateConfig : SourceAdapterConfig
    {
        public TemplateConfig()
        {
            SourceAbortErrorCount = 10;
        }

        public string BaseAddress { get; set; }
        public int Timeout { get; set; }
        public new static bool IsSaltminerSource { get => true; }
        // Set these two based on current adapter compatibility.  Example versions used here.
        public override string CurrentCompatibleApiVersion => "3.0.8";
        public override string MinimumCompatibleApiVersion => "3.0.8";

        public override string Serialize()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        // Write: override for validate to validate local properties
        public override void Validate()
        {
            base.Validate();
            var missingFields = Core.Helpers.Extensions.IsAnyNullOrEmpty(this);
            var myFields = new string[] { nameof(BaseAddress), nameof(Timeout) };
            if (myFields.Any(f => missingFields.Contains(f)))
                throw new SourceConfigurationException($"'{nameof(TemplateConfig)}' is missing values. {missingFields}");
        }
    }
}
