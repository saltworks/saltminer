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
using Saltworks.SaltMiner.Core.Data;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Entities;
using System.Collections.Generic;
using Saltworks.SaltMiner.ElasticClient;
using System.Linq;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class ConfigContext : ContextBase
    {
        public ConfigContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<LookupContext> logger) : base(config, dataRepository, factory, logger)
        { }

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
            var result = DataRepo.Search<Config>(
                new SearchRequest
                {
                    Filter = new Filter
                    {
                        FilterMatches = new Dictionary<string, string> { { "Type", type } }
                    }
                },
                Core.Entities.Config.GenerateIndex()
           );

            return new DataItemResponse<Config>(result.Data.FirstOrDefault());
        }

        public DataResponse<Config> GetAll()
        {
            var result = DataRepo.Search<Config>(
                new SearchRequest(),
                Core.Entities.Config.GenerateIndex()
           );

            return new DataResponse<Config>(result.Data);
        }
    }
}
