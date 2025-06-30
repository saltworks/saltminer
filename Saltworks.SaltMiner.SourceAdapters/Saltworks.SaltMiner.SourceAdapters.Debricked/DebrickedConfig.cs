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

namespace Saltworks.SaltMiner.SourceAdapters.Debricked
{
    //This is the config for the specfic Source that will always use this validate method.
    //Along with any other source specific field needed
    public class DebrickedConfig : SourceAdapterConfig
    {
        public DebrickedConfig()
        {
            SourceAbortErrorCount = 10;
            DataApiRetryCount = 3;
        }

        public string BaseAddress { get; set; }
        public int Timeout { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
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
            var myFields = new string[] { nameof(BaseAddress), nameof(Timeout) };
            if (myFields.Any(f => missingFields.Contains(f)))
                throw new SourceConfigurationException($"'{nameof(DebrickedConfig)}' is missing values. {missingFields}");
        }
    }
}
