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

namespace Saltworks.Saltminer.SourceAdapters.Core.IntegrationTests
{
    public class TestSourceConfig: SourceAdapterConfig
    {
        public TestSourceConfig()
        {
        }

        public override string MinimumCompatibleApiVersion => "3.0.3";
        public override string CurrentCompatibleApiVersion => "3.0.6";
        public override string Serialize()
        {
            // don't do anything
            return "";
        }
    }
}
