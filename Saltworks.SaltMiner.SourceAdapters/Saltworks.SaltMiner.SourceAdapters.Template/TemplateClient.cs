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

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.Utility.ApiHelper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.SourceAdapters.Template
{
    public class TemplateClient : SourceClient
    {
        private readonly TemplateConfig Config;
        public TemplateClient(ApiClient client, TemplateConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            SetApiClientDefaults(config.BaseAddress, config.Timeout);
        }

        //Here are all the calls to the Source API to get data and massage it into DTOs
        public async Task<IEnumerable<SourceMetric>> GetSourceMetricsAsync()
        {
            // Example, you would need an API to call to get real data
            // Might also need to return a DTO type instead of a direct source metric

            // Call API
            List<SourceMetric> metrics = [new(), new(), new()];
            await Task.Delay(1); // don't copy this, artificial async
            return metrics;
        }
    }
}
