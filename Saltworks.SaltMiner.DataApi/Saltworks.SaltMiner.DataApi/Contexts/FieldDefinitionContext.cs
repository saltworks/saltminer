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

﻿using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.DataApi.Data;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.ElasticClient;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System.Linq;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.DataApi.Contexts;

public class FieldDefinitionContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<FieldDefinitionContext> logger) : ContextBase(config, dataRepository, factory, logger)
{
    public DataItemResponse<List<FieldDefinition>> GetFieldDefinitionsByType(string entity)
    {
        var search = new SearchRequest
        {
            Filter = new()
            {
                FilterMatches = new()
                {
                    {"Entity", entity }
                }
            }
        };

        var result = Search<FieldDefinition>(FieldDefinition.GenerateIndex(), search);

        return new DataItemResponse<List<FieldDefinition>>(result.Data.ToList());
    }
}
