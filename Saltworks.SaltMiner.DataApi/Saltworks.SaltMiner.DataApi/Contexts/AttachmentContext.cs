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
