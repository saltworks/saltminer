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

﻿using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.DataApi.Data;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.ElasticClient;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class FieldDefinitionContext : ContextBase
    {
        public FieldDefinitionContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<FieldDefinitionContext> logger) : base(config, dataRepository, factory, logger)
        { }

        public DataItemResponse<List<FieldDefinition>> GetFieldDefinitionsByType(string entity)
        {
            var search = new SearchRequest();
            search.Filter = new()
            {
                FilterMatches = new()
                {
                    {"Entity", entity }
                }
            };

            var result = DataRepo.Search<FieldDefinition>(search, FieldDefinition.GenerateIndex()
            );

            return new DataItemResponse<List<FieldDefinition>>(result.Data.ToList());
        }
    }
}
