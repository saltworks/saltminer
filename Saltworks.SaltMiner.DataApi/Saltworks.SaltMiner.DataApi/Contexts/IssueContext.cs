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

﻿using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.Core.Data;
using Microsoft.Extensions.Logging;
using System.Linq;
using Saltworks.SaltMiner.ElasticClient;
using Saltworks.SaltMiner.DataApi.Authentication;
using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class IssueContext : ContextBase
    {
        public IssueContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<IssueContext> logger) : base(config, dataRepository, factory, logger)
        { }

        public BulkResponse AddUpdateBulk(DataRequest<Issue> request)
        {
            if (!(request?.Documents?.Any() ?? false) || request.Documents.Any(d => d == null))
            {
                var msg = "";
                if (request.Documents.Any(d => d == null))
                {
                    msg = $" ({request.Documents.Count(d => d == null)} document(s) found to be null)";
                } 
                else
                {
                    msg = "Request documents empty or missing";
                }
                throw new ApiValidationMissingArgumentException(msg);
            }

            var firstIssue = request.Documents.First();
            Logger.LogInformation("AddUpdate: {Count} docs for scan id '{Id}'", request.Documents.Count(), firstIssue.Saltminer.Scan.Id);

            if (request.Documents.Any(d => string.IsNullOrEmpty(d.Saltminer.Scan.Id) && !d.Saltminer.Scan.ReportId.ToLower().StartsWith("noscan")))
            {
                throw new ApiValidationException("All issues must include a scan id");
            }

            if (!string.IsNullOrEmpty(firstIssue.Saltminer.Scan.Id) && !firstIssue.Saltminer.Scan.ReportId.ToLower().StartsWith("noscan"))
            {
                CheckForEntity<Scan>(firstIssue.Saltminer.Scan.Id, Scan.GenerateIndex(firstIssue.Saltminer.Asset.AssetType, firstIssue.Saltminer.Asset.SourceType, firstIssue.Saltminer.Asset.Instance));
            }

            // TODO: add validation for issues
            var issueIndex = Issue.GenerateIndex(request.Documents.First().Saltminer.Asset.AssetType, request.Documents.First().Saltminer.Asset.SourceType, request.Documents.First().Saltminer.Asset.Instance);

            foreach(var doc in request.Documents)
            {
                doc.LastUpdated = DateTime.UtcNow;
            }

            //Ensure Index Exists in IndexMeta Index
            CheckForIndexMeta<Issue>(issueIndex);

            return ElasticClient.AddUpdateBulk(request.Documents, issueIndex).ToBulkResponse();
        }

        public NoDataResponse DeleteByScan(string scanId, string assetType, string sourceType, string instance)
        {
            Logger.LogInformation("Delete scan id '{ScanId}', type '{AssetType}', and source type '{SourceType}', and instance '{Instance}'", scanId, assetType, sourceType, instance);

            CheckForEntity<Scan>(scanId, Scan.GenerateIndex(assetType, sourceType, instance));

            if (IsInRole(Role.Admin) || IsInRole(Role.Manager))
            {
                return ElasticClient.DeleteByQuery<Issue>(new ElasticDataFilter("Saltminer.Scan.Id", scanId), Issue.GenerateIndex(assetType, sourceType, instance)).ToNoDataResponse();
            }
            else
            {
                throw new ApiForbiddenException();
            }
        }

        public NoDataResponse DeleteAllBySourceId(string sourceId, string assetType, string sourceType, string instance, string assessmentType)
        {
            var search = new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>()
                    {
                        { "Saltminer.Asset.SourceId", sourceId },
                        { "Saltminer.Asset.SourceType", sourceType },
                        { "Saltminer.Asset.Instance", instance },
                        { "Saltminer.Scan.AssessmentType", assessmentType }
                    }
                }
            };

            return ElasticClient.DeleteByQuery<Issue>(search, Issue.GenerateIndex(assetType, sourceType, instance), true, false).ToNoDataResponse();
        }

        public NoDataResponse DeleteAllByEngagementId(string engagementId, string assetType, string sourceType, string instance)
        {
            var search = new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>()
                    {
                        { "Saltminer.Engagement.Id", engagementId },
                        { "Saltminer.Asset.SourceType", sourceType },
                        { "Saltminer.Asset.Instance", instance },
                        { "Saltminer.Asset.AssetType", assetType }
                    }
                }
            };

            ElasticClient.DeleteByQuery<Scan>(search, Scan.GenerateIndex(assetType, sourceType, instance));
            ElasticClient.DeleteByQuery<Asset>(search, Asset.GenerateIndex(assetType, sourceType, instance));
            return ElasticClient.DeleteByQuery<Issue>(search, Issue.GenerateIndex(assetType, sourceType, instance), true, false).ToNoDataResponse();
        }
    }
}
