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
using Saltworks.SaltMiner.Core.Data;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Entities;
using System.Collections.Generic;
using Saltworks.SaltMiner.ElasticClient;
using System.Linq;

namespace Saltworks.SaltMiner.DataApi.Contexts;

public class ConfigContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<LookupContext> logger) : ContextBase(config, dataRepository, factory, logger)
{
    public NoDataResponse DeleteByType(string type)
    {
        var request = new SearchRequest
        {
            Filter = new Filter
            {
                FilterMatches = new Dictionary<string, string> { { "Type", type } }
            }
        };

        return ElasticClient.DeleteByQuery<Config>(request, Core.Entities.Config.GenerateIndex()).ToNoDataResponse();
    }

    public DataItemResponse<Config> GetByType(string type)
    {
        var result = Search<Config>(Core.Entities.Config.GenerateIndex(),
            new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string> { { "Type", type } }
                }
            }
       );

        return new DataItemResponse<Config>(result.Data.FirstOrDefault());
    }

    public DataResponse<Config> GetAll()
    {
        var result = Search<Config>(Core.Entities.Config.GenerateIndex(), new SearchRequest());
        return new DataResponse<Config>(result.Data, result.PagingInfo);
    }
}
