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
