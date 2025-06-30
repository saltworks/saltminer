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

﻿using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.DataApi.Data;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using System.Collections.Generic;
using Saltworks.SaltMiner.ElasticClient;
using System;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class QueueLogContext : ContextBase
    {
        public QueueLogContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<QueueLogContext> logger) : base(config, dataRepository, factory, logger)
        { }

        public DataResponse<QueueLog> Read(bool leaveUnread = false)
        {
            var searchRequest = new SearchRequest() { Filter = new() { FilterMatches = new Dictionary<string, string>() } };
            
            searchRequest.Filter.FilterMatches.Add("Read", "false");

            var response = DataRepo.Search<QueueLog>(searchRequest, QueueLog.GenerateIndex());

            foreach (var log in response.Data)
            {
                if (leaveUnread)
                {
                    break;
                }

                log.Read = true;
                log.LastUpdated = DateTime.UtcNow;

                ElasticClient.AddUpdate(log, QueueLog.GenerateIndex());
            }

            return response;
        }
    }
}
