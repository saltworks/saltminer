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
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.ElasticClient;
using System;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class ActionDefinitionContext : ContextBase
    {
        public ActionDefinitionContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<ActionDefinitionContext> logger) : base(config, dataRepository, factory, logger)
        {}
    }
}
