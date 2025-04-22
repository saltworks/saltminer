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

﻿using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.DataApi.Data;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.ElasticClient;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System.Collections.Generic;
using System.Linq;
using System;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataApi.Extensions;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.Core.Extensions;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class EngagementContext : ContextBase
    {
        private AttachmentContext AttachmentContext;
        private string EngagementIndex = Engagement.GenerateIndex();

        public EngagementContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<EngagementContext> logger, AttachmentContext attachmentContext) : base(config, dataRepository, factory, logger)
        {
            AttachmentContext = attachmentContext;
        }

        public NoDataResponse Delete(string id)
        {
            CheckForEntity<Engagement>(id, EngagementIndex);

            var engagement = Get<Engagement>(id, EngagementIndex);
            var request = new ElasticDataFilter("Saltminer.Engagement.Id", id);
            if (engagement.Data.Saltminer.Engagement.PublishDate != null)
            {
                Logger.LogWarning("Published engagement with ID {Id} is being deleted.", id);
                
                try { ElasticClient.DeleteByQuery<Scan>(request, Scan.GenerateIndex()); }
                catch (Exception ex)
                { Logger.LogWarning(ex, "Exception when cleaning up engagement ID {Id} scan: {Msg}", id, ex.Message); }
                
                try { ElasticClient.DeleteByQuery<Issue>(request, Issue.GenerateIndex()); }
                catch (Exception ex)
                { Logger.LogWarning(ex, "Exception when cleaning up engagement ID {Id} issues: {Msg}", id, ex.Message); }
                
                try
                { ElasticClient.DeleteByQuery<Asset>(request, Asset.GenerateIndex()); }
                catch (Exception ex)
                { Logger.LogWarning(ex, "Exception when cleaning up engagement ID {Id} assets: {Msg}", id, ex.Message); }
            }
            else
            {
                try { ElasticClient.DeleteByQuery<QueueScan>(request, QueueScan.GenerateIndex()); }
                catch (Exception ex)
                { Logger.LogWarning(ex, "Exception when cleaning up engagement ID {Id} queue scan: {Msg}", id, ex.Message); }

                try { ElasticClient.DeleteByQuery<QueueIssue>(request, QueueIssue.GenerateIndex()); }
                catch (Exception ex)
                { Logger.LogWarning(ex, "Exception when cleaning up engagement ID {Id} queue issues: {Msg}", id, ex.Message); }

                try { ElasticClient.DeleteByQuery<QueueAsset>(request, QueueAsset.GenerateIndex()); }
                catch (Exception ex)
                { Logger.LogWarning(ex, "Exception when cleaning up engagement ID {Id} queue assets: {Msg}", id, ex.Message); }
            }

            try { ElasticClient.DeleteByQuery<Comment>(request, Comment.GenerateIndex()); }
            catch (Exception ex)
            { Logger.LogWarning(ex, "Exception when cleaning up engagement ID {Id} comments: {Msg}", id, ex.Message); }

            try { AttachmentContext.DeleteAllEngagement(id); }
            catch (Exception ex)
            { Logger.LogWarning(ex, "Exception when cleaning up engagement ID {Id} attachments: {Msg}", id, ex.Message); }

            return ElasticClient.Delete<Engagement>(id, EngagementIndex).ToNoDataResponse();
        }

        public NoDataResponse DeleteGroup(string id)
        {
            var engagements = Search<Engagement>(
                new SearchRequest 
                { 
                    Filter = new Filter
                    {
                        FilterMatches = new Dictionary<string, string>
                        {
                            { "Saltminer.Engagement.GroupId", id }
                        }
                    }
                }, EngagementIndex
            );

            foreach (var engagement in engagements.Data)
            {
                Delete(engagement.Id);
            }

            return new NoDataResponse(engagements.Data.Count());
        }

        public NoDataResponse SetHistoricalIssues(string id, string groupId)
        {
            var issueCount = 0;

            var engagements = Search<Engagement>(new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Engagement.GroupId", groupId },
                        { "Saltminer.Engagement.Status", EngagementStatus.Historical.ToString("g") }
                    }
                }
            }, EngagementIndex);

            foreach(var engagement in engagements?.Data?.Where(x => x.Id != id))
            {
                var response = ElasticClient.UpdateByQuery(new UpdateQueryRequest<Issue>
                {
                    Filter = new Filter
                    {
                        FilterMatches = new Dictionary<string, string>
                        {
                            { "Saltminer.Engagement.Id", engagement.Id }
                        }
                    },
                    ScriptUpdates = new Dictionary<string, object> { 
                        { "Saltminer.IsHistorical", true }, 
                        { "LastUpdated", DateTime.UtcNow }
                    }
                },
                Issue.GenerateIndex());

                issueCount = issueCount + (int) response.CountAffected;
            }

            return new NoDataResponse(issueCount);
        }

        public NoDataResponse UnSetHistoricalIssues(string id)
        {
            var issueCount = 0;

            var response = ElasticClient.UpdateByQuery(new UpdateQueryRequest<Issue>
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Engagement.Id", id }
                    }
                },
                ScriptUpdates = new Dictionary<string, object> {
                    { "Saltminer.IsHistorical", false },
                    { "LastUpdated", DateTime.UtcNow }
                }
            },
            Issue.GenerateIndex());

            issueCount = issueCount + (int)response.CountAffected;

            return new NoDataResponse(issueCount);
        }

        public DataItemResponse<Dictionary<string, long?>> IssueCounts(string engagemnetId, string assetType, string sourceType, string instance)
        {
            var sourceFields = new List<string>
            {
                "Vulnerability.Severity"
            };

            var sumAggFields = new List<string>();

            var aggList = sumAggFields.Select(x => ElasticClient.BuildRequestAggregate(x, x, ElasticAggregateType.Sum)).ToList();

            var engagement = Get<Engagement>(engagemnetId, EngagementIndex);

            if(engagement.Data.Saltminer.Engagement.Status == EnumExtensions.GetDescription(EngagementStatus.Published) ||
                engagement.Data.Saltminer.Engagement.Status == EnumExtensions.GetDescription(EngagementStatus.Historical))
            {
                var index = Issue.GenerateIndex(assetType, sourceType, instance);

                var response = DataRepo.EngagementIssueCountAggregates(engagemnetId, new PitPagingInfo(), sourceFields, aggList, index);
                var resultDict = new Dictionary<string, long?>();

                foreach (var composite in response.Results)
                {
                    resultDict.Add(composite.Key, composite.DocCount);
                }

                return new DataItemResponse<Dictionary<string, long?>>(resultDict);
            }
            else
            {
                var index = QueueIssue.GenerateIndex();

                var response = DataRepo.EngagementIssueCountAggregates(engagemnetId, new PitPagingInfo(), sourceFields, aggList, index);
                var resultDict = new Dictionary<string, long?>();

                foreach (var composite in response.Results)
                {
                    resultDict.Add(composite.Key, composite.DocCount);
                }

                return new DataItemResponse<Dictionary<string, long?>>(resultDict);
            }
        }

        public NoDataResponse UpdateStatus(string id, string status)
        {
            Logger.LogInformation("UpdateStatus for id '{id}' and status '{status}'", id, status);

            IsValidStatus(status);

            var tuple = DataRepo.GetWithLocking<Engagement>(id, EngagementIndex);
            if (tuple?.Item1 == null)
            {
                throw new ApiResourceNotFoundException($"Engagement with ID '{id}' not found.");
            }

            var engagement = tuple.Item1;
            var lockInfo = tuple.Item2;

            if (!IsOkToEditStatus(engagement.Saltminer.Engagement.Status, status))
            {
                throw new ApiValidationQueueStateException($"Cannot update engagement from '{engagement.Saltminer.Engagement.Status:g}' to '{status}' state, invalid transition.");
            }

            engagement.Saltminer.Engagement.Status = status;
            engagement.LastUpdated = DateTime.UtcNow;

            if(status.ToEngagementStatus() == EngagementStatus.Published) 
            {
                engagement.Saltminer.Engagement.PublishDate = DateTime.UtcNow;
            }

            DataRepo.UpdateWithLocking(engagement, EngagementIndex, lockInfo);

            return new NoDataResponse(1);
        }

        private EngagementStatus IsValidStatus(string status)
        {
            EngagementStatus engagementStatus;
            if (Enum.TryParse(status, out engagementStatus))
            {
                return engagementStatus;
            }
            throw new ApiValidationException($"{status} is not a valid Engagement Status.");
        }

        private bool IsOkToEditStatus(string curStatus, string nStatus)
        {
            var currentStatus = curStatus.ToEngagementStatus();
            var newStatus = nStatus.ToEngagementStatus();

            if (IsInRole(Role.Pentester))
            {
                // to reset failed engagements, allow changing error back to draft
                if (currentStatus == EngagementStatus.Error && newStatus == EngagementStatus.Draft)
                {
                    return true;
                }

                // current must be draft, new must be queued
                return currentStatus == EngagementStatus.Draft &&
                    newStatus == EngagementStatus.Queued;
            }

            // Current has to be at least be queued in the workflow, new must be higher than current, current can't be an end state
            return currentStatus >= EngagementStatus.Queued && newStatus > currentStatus && currentStatus < EngagementStatus.Error;
        }
    }
}
