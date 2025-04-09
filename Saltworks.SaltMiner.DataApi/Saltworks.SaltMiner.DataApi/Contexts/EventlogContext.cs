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

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.ElasticClient;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class EventlogContext : ContextBase
    {
        public EventlogContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<EventlogContext> logger) : base(config, dataRepository, factory, logger)
        {
        }
    }
}
