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
    public class AttachmentContext : ContextBase
    {
        public AttachmentContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<AttachmentContext> logger) : base(config, dataRepository, factory, logger)
        { }

        public NoDataResponse DeleteAllEngagement(string id, bool engagementLevelOnly = false, bool isMarkdown = false)
        {
            var request = new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string> { { "Saltminer.Engagement.Id", id } }                
                }
            };

            if (isMarkdown)
            {
                request.Filter.FilterMatches.Add("Saltminer.IsMarkdown", "true");
            }
            else
            {
                request.Filter.FilterMatches.Add("Saltminer.IsMarkdown", "false");
            }

            if (engagementLevelOnly)
            {
                request.Filter.SubFilter = new Filter
                {
                    FilterMatches = new Dictionary<string, string> { { "Saltminer.Issue.Id", "!" } }
                };
            }

            return ElasticClient.DeleteByQuery<Attachment>(request, Attachment.GenerateIndex()).ToNoDataResponse();
        }

        public NoDataResponse DeleteAllIssue(string id, bool? isMarkdown)
        {
            var request = new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string> { { "Saltminer.Issue.Id", id } }
                }
            };

            if (isMarkdown.HasValue)
            {
                if (isMarkdown.Value)
                {
                    request.Filter.FilterMatches.Add("Saltminer.IsMarkdown", "true");
                }
                else
                {
                    request.Filter.FilterMatches.Add("Saltminer.IsMarkdown", "false");
                }
            }

            return ElasticClient.DeleteByQuery<Attachment>(request, Attachment.GenerateIndex()).ToNoDataResponse();
        }
    }
}
