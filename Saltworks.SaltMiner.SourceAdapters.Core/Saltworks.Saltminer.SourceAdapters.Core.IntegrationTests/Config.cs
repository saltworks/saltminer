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

﻿using System.Collections.Generic;

namespace Saltworks.Saltminer.SourceAdapters.Core.IntegrationTests
{
    public class Config
    {
        public Dictionary<string, string> DefaultHeaders { get; set; }
        public string ApiBaseAddress { get; set; }
        public string ApiKey { get; set; }
        public string ApiKeyHeader { get; set; } = "Authorization";
        public int ApiTimeoutSec { get; set; } = 10;
        public bool ApiVerifySsl { get; set; } = true;
        public TestSourceConfig SourceConfig { get; set; }
    }
}
