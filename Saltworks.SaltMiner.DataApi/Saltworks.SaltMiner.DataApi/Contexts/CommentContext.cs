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

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class CommentContext : ContextBase
    {
        public CommentContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<CommentContext> logger) : base(config, dataRepository, factory, logger)
        { }

        public NoDataResponse DeleteAllEngagement<T>(string id)
        {
            CheckForEntity<Engagement>(id, Engagement.GenerateIndex());

            var request = new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string> { { "Saltminer.Engagement.Id", id } }                
                }
            };

            return ElasticClient.DeleteByQuery<Comment>(request, Comment.GenerateIndex()).ToNoDataResponse();
        }
    }
}
