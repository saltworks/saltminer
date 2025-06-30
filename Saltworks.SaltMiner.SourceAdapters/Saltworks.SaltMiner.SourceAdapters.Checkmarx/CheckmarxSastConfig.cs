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
using System.Threading;

namespace Saltworks.SaltMiner.SourceAdapters.CheckmarxSast
{
    public class CheckmarxSastConfig : SourceAdapterConfig
    {
        public CheckmarxSastConfig()
        {
            FilePaths = new List<string>();
            SourceAbortErrorCount = 10;
            DataApiRetryCount = 3;
        }

        public bool DeleteFileWhenDone { get; set; } = true;
        public List<string> FilePaths { get; set; }
        public string CxFlowFolder { get; set; }
        public new static bool IsSaltminerSource { get => true; }

        public override string Serialize()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        public override void Validate()
        {
            base.Validate();
            var missingFields = Core.Helpers.Extensions.IsAnyNullOrEmpty(this);
            var myFields = new string[] { nameof(FilePaths), nameof(CxFlowFolder) };
            if (myFields.Any(f => missingFields.Contains(f)))
                throw new SourceConfigurationException($"'{nameof(CheckmarxSastConfig)}' is missing values. {missingFields}");
        }

    }
}
