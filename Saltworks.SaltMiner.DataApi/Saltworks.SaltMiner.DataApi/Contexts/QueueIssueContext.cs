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

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.ElasticClient;
using System;
using System.Linq;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class QueueIssueContext : ContextBase
    {
        private string QueueIssueIndex = QueueIssue.GenerateIndex();
        private string QueueScanIndex = QueueScan.GenerateIndex();

        public QueueIssueContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<QueueIssueContext> logger) : base(config, dataRepository, factory, logger)
        {
        }

        public DataItemResponse<QueueIssue> AddUpdate(DataItemRequest<QueueIssue> request)
        {
            if (request?.Entity == null)
            {
                throw new ApiValidationMissingArgumentException("Request document empty or missing");
            }

            CheckForEntity<QueueScan>(request.Entity.Saltminer.QueueScanId, QueueScanIndex);

            if (request.Entity?.Vulnerability?.Scanner?.Id == null || string.IsNullOrEmpty(request.Entity.Vulnerability.Scanner.Id))
            {
                throw new ApiValidationMissingArgumentException($"QueueIssue requires a unique Vulnerability Scanner Id, Id set to {request?.Entity?.Vulnerability?.Scanner?.Id}");
            }

            request.Entity.LastUpdated = DateTime.UtcNow;
            
            Logger.LogInformation("Add/Update id '{Id}' of type QueueIssue", request.Entity.Id ?? "[new]");

            return ElasticClient.AddUpdate(request.Entity, QueueIssueIndex).ToDataItemResponse();
        }

        internal static void Validate(QueueIssue qIssue)
        {
            if (qIssue == null)
                throw new ApiValidationException("Queue issue cannot be null.");
            if (string.IsNullOrEmpty(qIssue.Saltminer.QueueScanId) || string.IsNullOrEmpty(qIssue.Saltminer.QueueAssetId))
                throw new ApiValidationException("All queue issues must include a queue scan ID and queue asset ID (in Saltminer.Internal).");
        }

        // TODO: better validation for bulk queueIssue handling - this method will allow updating of existing issues not belonging to a particular agent.
        // If Agent or Pentest role:
        // 1. Get all the queue scan IDs from the request
        // 2. Search queue scans for all the IDs at once
        public BulkResponse AddUpdateBulk(DataRequest<QueueIssue> request)
        {
            if (!(request?.Documents?.Any() ?? false))
            {
                throw new ApiValidationMissingValueException("Request documents empty or missing");
            }

            Logger.LogInformation("AddUpdate: {Count} docs for scan id '{Id}'", request.Documents.Count(), request.Documents.First().Saltminer.QueueScanId);

            if (request.Documents.Any(d => d == null))
            {
                throw new ApiValidationException("At least one queue issue sent for add/update is null.");
            }

            if (request.Documents.Any(d => string.IsNullOrEmpty(d.Saltminer.QueueAssetId) || string.IsNullOrEmpty(d.Saltminer.QueueScanId)))
            {
                throw new ApiValidationException("All queue issues must include a queue scan ID and a queue asset ID (Saltminer.QueueScanId & Saltminer.QueueAssetId).");
            }

            // NOTE: removed validation of related entities so can support batch processing (scans and/or assets may not yet exist while loading...)

            // TODO: rethink index field & its logic, current design is complicated and not intuitive
            foreach (var queueIssue in request.Documents)
            {
                if (queueIssue?.Vulnerability?.Scanner?.Id == null || string.IsNullOrEmpty(queueIssue.Vulnerability.Scanner.Id))
                {
                    throw new ApiValidationMissingValueException($"All QueueIssues requires a unique Vulnerability Scanner Id, Id set to {queueIssue?.Vulnerability?.Scanner?.Id}");
                }

                queueIssue.LastUpdated = DateTime.UtcNow;
            }

            return ElasticClient.AddUpdateBulk(request.Documents, QueueIssueIndex).ToBulkResponse();
        }

        public NoDataResponse Delete(string id)
        {
            Logger.LogInformation("Delete id {id}", id);

            var queueIssue = CheckForEntity<QueueIssue>(id, QueueIssueIndex);
            var queueScan = CheckForEntity<QueueScan>(queueIssue.Data.Saltminer.QueueScanId, QueueScanIndex);

            if (CanDelete(queueScan.Data))
            {
                return ElasticClient.Delete<QueueIssue>(id, QueueIssueIndex).ToNoDataResponse();
            }
            else
            {
                if (IsInRole(Role.Agent) || IsInRole(Role.Pentester))
                {
                    var agent = IsInRole(Role.Agent) ? "Agent" : "Pentester";
                    Logger.LogWarning("{agent} not allowed to delete queue issues for queue scan '{id}'.", agent, queueScan.Data.Id);
                }
                else
                {
                    Logger.LogWarning("Manager cannot remove queue issues for queue scan '{id}' when it has status '{queueStatus}'.", QueueScanIndex, queueScan.Data.Saltminer.Internal.QueueStatus);
                }

                throw new ApiForbiddenException("Not allowed to remove issues.");
            }
        }

        public NoDataResponse CountByQueueScanId(string id)
        {
            Logger.LogInformation("Issue Count for queue scan '{id}'", id);

            CheckForEntity<QueueScan>(id, QueueScanIndex);

            return ElasticClient.Count<QueueIssue>(new ElasticDataFilter("Saltminer.QueueScanId", id), QueueIssueIndex).ToNoDataResponse();
        }

        public DataItemResponse<QueueIssue> GetAndLock(string id, string userName)
        {
            var issue = CheckForEntity<QueueIssue>(id, QueueIssueIndex);

            if (issue?.Data?.Lock?.User != null)
            {
                if (userName == issue.Data.Lock.User)
                {
                    issue.Data.Lock.Expires = DateTime.UtcNow.AddMinutes(Config.ConcurrencyLockTime);
                    ElasticClient.UpdateWithLocking(issue.Data, QueueIssueIndex, issue.Primary, issue.SeqNum);
                }
                else if (issue.Data.Lock.Expires < DateTime.UtcNow)
                {
                    issue.Data.Lock.User = userName;
                    issue.Data.Lock.Expires = DateTime.UtcNow.AddMinutes(Config.ConcurrencyLockTime);
                    ElasticClient.UpdateWithLocking(issue.Data, QueueIssueIndex, issue.Primary, issue.SeqNum);
                }
            }
            else
            {
                issue.Data.Lock = new LockInfo
                {
                    User = userName,
                    Expires = DateTime.UtcNow.AddMinutes(Config.ConcurrencyLockTime)
                };
                ElasticClient.UpdateWithLocking(issue.Data, QueueIssueIndex, issue.Primary, issue.SeqNum);
            }
            
            return issue;
        }

        public void RefreshLock(string id, string userName)
        {
            var issue = CheckForEntity<QueueIssue>(id, QueueIssueIndex);
            if (issue?.Data?.Lock?.User != null && userName == issue.Data.Lock.User)
            {
                issue.Data.Lock.Expires = DateTime.UtcNow.AddMinutes(Config.ConcurrencyLockTime);
                ElasticClient.UpdateWithLocking(issue.Data, QueueIssueIndex, issue.Primary, issue.SeqNum);
            }
        }

        private bool CanDelete(QueueScan scan)
        {
            var cstate = Enum.Parse<QueueScan.QueueScanStatus>(scan.Saltminer.Internal.QueueStatus);

            // Allow deletion if related scan is in an appropriate state and if agent, is owned by the agent
            bool ok = IsInRole(Role.Admin);

            ok = ok || (IsInRole(Role.Manager) && cstate > QueueScan.QueueScanStatus.Processing);
            ok = ok || (cstate == QueueScan.QueueScanStatus.Loading);

            return ok;
        }
    }
}
