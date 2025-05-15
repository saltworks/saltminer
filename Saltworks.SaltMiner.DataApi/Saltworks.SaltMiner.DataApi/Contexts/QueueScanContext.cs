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
using Saltworks.SaltMiner.DataApi.Extensions;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.ElasticClient;
using System;
using System.Collections.Generic;
using System.Linq;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class QueueScanContext : ContextBase
    {
        private readonly string QueueScanIndex = GenerateIndex();
        private readonly string QueueIssueIndex = QueueIssue.GenerateIndex();
        private readonly string QueueAssetIndex = QueueAsset.GenerateIndex();

        public QueueScanContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<QueueScanContext> logger) : base(config, dataRepository, factory, logger)
        {
        }

        public DataItemResponse<QueueScan> AddUpdate(DataItemRequest<QueueScan> request)
        {
            if (request?.Entity == null)
            {
                throw new ApiValidationMissingArgumentException("Missing/invalid entity");
            }

            Logger.LogInformation("AddUpdate: queue scan id '{Id}'", request.Entity.Id ?? "[new]");

            request.Entity.LastUpdated = DateTime.UtcNow;

            var r = ElasticClient.AddUpdate(Validate(request.Entity), QueueScanIndex);
            return r.ToDataItemResponse();
        }

        public BulkResponse AddUpdateBulk(DataRequest<QueueScan> request)
        {
            if (!(request?.Documents?.Any() ?? false) || request.Documents.Any(d => d == null))
            {
                throw new ApiValidationException("At least one null queue scan sent in bulk request");
            }

            Logger.LogInformation("AddUpdateBulk: queue scan batch of {Count}", request.Documents.Count());
            foreach (var item in request.Documents)
            {
                Validate(item, true);
                item.LastUpdated = DateTime.UtcNow;
            }

            return ElasticClient.AddUpdateBulk(request.Documents, QueueScanIndex).ToBulkResponse();
        }

        public BulkResponse AddUpdateQueueBulk(QueueDataRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            int[] counts = [(request.QueueScans?.Count() ?? 0), (request.QueueAssets?.Count() ?? 0), (request.QueueIssues?.Count() ?? 0)];
            if (counts[0] + counts[1] + counts[2] == 0)
                throw new ApiValidationException("Empty request, cannot process.");

            Logger.LogInformation("AddUpdateQueueBulk: {Qs} queue scan(s), {Qa} queue asset(s), {Qi} queue issue(s)", counts[0], counts[1], counts[2]);

            foreach (var qs in request.QueueScans)
            {
                Validate(qs, true);
                qs.LastUpdated = DateTime.UtcNow;
            }
            foreach(var qa in request.QueueAssets)
            {
                QueueAssetContext.Validate(qa);
                qa.LastUpdated = DateTime.UtcNow;
            }
            foreach (var qi in request.QueueIssues)
            {
                QueueIssueContext.Validate(qi);
                qi.LastUpdated = DateTime.UtcNow;
            }

            List<SaltMinerEntity> all = [];
            return ElasticClient.AddUpdateBulkQueue(all
                .Concat(request.QueueScans)
                .Concat(request.QueueAssets)
                .Concat(request.QueueIssues)).ToBulkResponse();
        }

        public NoDataResponse DeleteBulk(SearchRequest request)
        {
            ElasticClient.DeleteByQuery<QueueIssue>(request, QueueIssueIndex, false, false);
            ElasticClient.DeleteByQuery<QueueAsset>(request, QueueAssetIndex, false, false);
            ElasticClient.DeleteByQuery<QueueScan>(request, QueueScanIndex, false, false);
            return new(-1, "Delete operations submitted.");
        }

        public NoDataResponse Delete(string id)
        {
            CheckForEntity<QueueScan>(id, QueueScanIndex);

            if (VerifyScanDelete(id))
            {
                return ElasticClient.Delete<QueueScan>(id, QueueScanIndex).ToNoDataResponse();
            }
            else
            {
                throw new ApiForbiddenException();
            }
        }

        public DataResponse<QueueScan> Search(SearchRequest request)
        {
            Logger.LogInformation(Extensions.LoggerExtensions.SearchPagingLoggerMessage("Search", request));

            if ((IsInRole(Role.Agent) || IsInRole(Role.Pentester) || IsInRole(Role.PentesterViewer)) && (request.Filter == null || request.Filter.FilterMatches == null))
            {
                if (request.Filter == null)
                {
                    request.Filter = new Filter()
                    {
                        FilterMatches = []
                    };
                }
                else if (request.Filter.FilterMatches == null)
                {
                    request.Filter.FilterMatches = [];
                }
            }

            return DataRepo.Search<QueueScan>(request, QueueScanIndex);
        }

        // Aggregates don't seem to support pagination.  Once they do, finish this and wire up for use with Manager Cleanup
        public DataResponse<string> SearchForIdsByAggregate(bool assetNotIssue = true)
        {
            if (assetNotIssue)
                return new(DataRepo.SingleGroupAggregation("Saltminer.Internal.QueueScanId", QueueAssetIndex, []).Select(r => r.Result.Key));
            else
                return new(DataRepo.SingleGroupAggregation("Saltminer.QueueScanId", QueueIssueIndex, []).Select(r => r.Result.Key));
        }

        public DataItemResponse<QueueScan> Get(string id)
        {
            Logger.LogInformation("Get for id '{Id}'", id);

            return CheckForEntity<QueueScan>(id, QueueScanIndex);
        }

        public DataItemResponse<QueueScan> GetByEngagement(string id)
        {
            Logger.LogInformation("GetByEngagement for id '{Id}'", id);

            var response = Search<QueueScan>(new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Engagement.Id", id }
                    }
                }
            }, GenerateIndex());

            return new DataItemResponse<QueueScan>(response.Data.FirstOrDefault());
        }

        private static Filter GetListFilter(string field, IEnumerable<string> ids) => new() { FilterMatches = new() { { field, string.Join("||+", ids) } } };

        public NoDataResponse DeleteAllQueueByQueueScan(IEnumerable<string> idList)
        {
            if (!IsInRole(Role.Manager) && !IsInRole(Role.Admin))
                throw new ApiForbiddenException();
            ElasticClient.DeleteByQuery<QueueIssue>(new() { Filter = GetListFilter("Saltminer.QueueScanId", idList) }, QueueIssueIndex, true, false);
            ElasticClient.DeleteByQuery<QueueAsset>(new() { Filter = GetListFilter("Saltminer.Internal.QueueScanId", idList) }, QueueAssetIndex, true, false);
            ElasticClient.DeleteByQuery<QueueScan>(new() { Filter = GetListFilter("Id", idList) }, QueueScanIndex, true, false);
            return new(idList.Count());
        }

        public NoDataResponse DeleteAllQueueByQueueScan(string id)
        {
            CheckForEntity<QueueScan>(id, QueueScanIndex);

            if (VerifyScanDeleteAll(id))
            {
                ElasticClient.DeleteByQuery<QueueIssue>(new ElasticDataFilter("Saltminer.QueueScanId", id), QueueIssueIndex);
                ElasticClient.DeleteByQuery<QueueAsset>(new ElasticDataFilter("Saltminer.Internal.QueueScanId", id), QueueAssetIndex);

                return ElasticClient.Delete<QueueScan>(id, QueueScanIndex).ToNoDataResponse();
            }
            else
            {
                throw new ApiForbiddenException();
            }
        }

        public NoDataResponse UpdateStatus(string id, string status, string lockId = "")
        {
            Logger.LogInformation("UpdateStatus for id '{Id}' and status '{Status}'", id, status);

            ValidateStatus(status);

            var tuple = DataRepo.GetWithLocking<QueueScan>(id, QueueScanIndex);
            if (tuple?.Item1 == null)
            {
                throw new ApiResourceNotFoundException($"Queue scan with ID '{id}' not found.");
            }

            var scan = tuple.Item1;
            var lockInfo = tuple.Item2;
            if (!string.IsNullOrEmpty(scan.Saltminer.Internal.LockId) && (scan.Saltminer.Internal.LockId != lockId || string.IsNullOrEmpty(lockId)))
                throw new ApiValidationQueueStateException($"[Locked] Cannot update queue scan status, already locked to another process.");
            if (!string.IsNullOrEmpty(lockId))
                scan.Saltminer.Internal.LockId = lockId;

            if (!IsOkToEditStatus(scan.Saltminer.Internal.QueueStatus, status))
            {
                throw new ApiValidationQueueStateException($"[Invalid] Cannot update queue scan from '{scan.Saltminer.Internal.QueueStatus:g}' to '{status}' state, invalid transition.");
            }

            ValidatePendingStatus(status.ToQueueScanStatus(), scan);

            scan.Saltminer.Internal.QueueStatus = status;
            scan.LastUpdated = DateTime.UtcNow;
            DataRepo.UpdateWithLocking(scan, QueueScanIndex, lockInfo);

            return new NoDataResponse(1);
        }

        public NoDataResponse Unlock(string lockId)
        {
            Logger.LogInformation("Unlock for lock ID '{Id}'", lockId);
            var request = new ElasticDataFilter("Saltminer.Internal.LockId", lockId, new UIPagingInfo(1000));
            var counter = 0;
            List<QueueScan> scans = [];
            while (true)
            {
                var response = DataRepo.Search<QueueScan>(request, QueueScanIndex);
                if (!response.Data.Any())
                    break;
                foreach (var item in response.Data)
                {
                    item.Saltminer.Internal.LockId = null;
                    item.LastUpdated = DateTime.UtcNow;
                    scans.Add(item);
                    counter++;
                }
                DataRepo.AddUpdateBulk(scans, QueueScanIndex);
                request.UIPagingInfo.Page += 1;
                request.AfterKeys = response.AfterKeys;
            }
            return new NoDataResponse(counter);
        }

        #region Helpers

        private bool VerifyScanDelete(string id)
        {
            if (RelatedIssuesExist(id))
            {
                throw new ApiValidationReferentialIntegrityException("One or more queue issues still exist with this queue scan id");
            }

            return VerifyScanDeleteAll(id);
        }

        private bool VerifyScanDeleteAll (string id)
        {
            var scan = Get(id).Data;

            if (!IsOkToEditStatus(scan.Saltminer.Internal.QueueStatus))
            {
                throw new ApiValidationQueueStateException($"Cannot delete queue scan in '{scan.Saltminer.Internal.QueueStatus}' state");
            }

            bool ok = IsInRole(Role.Admin) || IsInRole(Role.Manager) || IsInRole(Role.Pentester);

            ok = ok || IsInRole(Role.Agent);
            ok = ok && IsOkToEditStatus(scan.Saltminer.Internal.QueueStatus); // allowable statuses to delete this queue scan

            if (ok)
            {
                return true;
            }
            else
            {
                if (IsInRole(Role.Agent) || IsInRole(Role.Pentester))
                {
                    Logger.LogWarning("Agent is not allowed to delete queue scan ID '{Id}'.", scan.Id);
                }
                else
                {
                    Logger.LogWarning("Not allowed to delete queue scan ID '{Id}' while in status '{Status}'.", scan.Id, scan.Saltminer.Internal.QueueStatus);
                }

                return false;
            }
        }

        private QueueScan Validate(QueueScan queueScan, bool isBulk = false)
        {
            // TODO: add more basic+ validation for queuescan

            ValidateStatus(queueScan.Saltminer.Internal.QueueStatus);

            // Update
            if (!string.IsNullOrEmpty(queueScan.Id))
            {
                var existing = ElasticClient.Get<QueueScan>(queueScan.Id, QueueScanIndex).ToDataItemResponse().Data;

                if (existing == null && !isBulk)
                {
                    throw new ApiResourceNotFoundException($"Could not add queue scan with {queueScan.Id}");
                }

                if (IsInRole(Role.Agent) || IsInRole(Role.Pentester))
                {
                    if (existing != null)
                    {
                        if (!IsOkToEditStatus(existing.Saltminer.Internal.QueueStatus))
                        {
                            throw new ApiValidationQueueStateException($"Cannot update queue scan in '{existing.Saltminer.Internal.QueueStatus}' state");
                        }

                        if (existing.Saltminer.Internal.QueueStatus != queueScan.Saltminer.Internal.QueueStatus && !IsOkToEditStatus(existing.Saltminer.Internal.QueueStatus, queueScan.Saltminer.Internal.QueueStatus))
                        {
                            throw new ApiValidationQueueStateException($"Agent cannot update queue scan status from '{existing.Saltminer.Internal.QueueStatus}' to '{queueScan.Saltminer.Internal.QueueStatus}'");
                        }

                        ValidatePendingStatus(queueScan.Saltminer.Internal.QueueStatus.ToQueueScanStatus(), existing);
                    }
                    else
                    {
                        if (queueScan.Saltminer.Internal.QueueStatus != QueueScanStatus.Loading.ToString("g"))
                        {
                            throw new ApiValidationException($"QueueStatus should be {QueueScanStatus.Loading:g}.");
                        }
                    }
                }
            }

            // TODO: rethink index field / logic, current design is complicated and not intuitive
            return queueScan;
        }

        private bool RelatedIssuesExist(string queueScanId)
        {
            var request = new ElasticDataFilter("Saltminer.QueueScanId", queueScanId, new PitPagingInfo(10));

            return DataRepo.Search<QueueIssue>(request, QueueScanIndex).Data.Any();
        }


        private void ValidatePendingStatus(QueueScanStatus newStatus, QueueScan qScan)
        {
            // Loading to Pending, are counts right for issues
            if (newStatus == QueueScanStatus.Pending && qScan.Saltminer.Internal.QueueStatus.ToQueueScanStatus() == QueueScanStatus.Loading)
            {
                // Agent is moving from Loading -> Pending
                var count = ElasticClient.Count<QueueIssue>(new ElasticDataFilter("Saltminer.QueueScanId", qScan.Id), QueueIssueIndex).ToNoDataResponse().Affected;
                if (count != qScan.Saltminer.Internal.IssueCount && qScan.Saltminer.Internal.IssueCount != -1)
                {
                    throw new ApiValidationException($"Counted {count} issues for scan Id '{qScan.Id}', but scan IssueCount was {qScan.Saltminer.Internal.IssueCount}.");
                }
            }
        }

        /// <summary>
        /// Whether it's ok to edit this queue scan while in this status (i.e. update / delete, not a status move)
        /// </summary>
        /// <param name="status">Current status</param>
        private bool IsOkToEditStatus(string status)
        {
            var qstatus = status.ToQueueScanStatus();

            if (IsInRole(Role.Admin))
            {
                return true;
            }

            if (IsInRole(Role.Agent) || IsInRole(Role.Pentester))
            {
                return qstatus == QueueScanStatus.Loading || qstatus == QueueScanStatus.Pending;
            }

            return status.ToQueueScanStatus() > QueueScanStatus.Loading;
        }

        /// <summary>
        /// Whether it's ok to move from curStatus to newStatus
        /// </summary>
        /// <param name="curStatus">Current status</param>
        /// <param name="newStatus">New status</param>
        private bool IsOkToEditStatus(string curStatus, string newStatus)
        {
            if (IsInRole(Role.Admin))
            {
                return true;
            }

            var currentScanStatus = curStatus.ToQueueScanStatus();
            var newScanStatus = newStatus.ToQueueScanStatus();

            if (IsInRole(Role.Agent) || IsInRole(Role.Pentester))
            {
                // Allow a pentest reset of queue scan if in error or processing
                if (IsInRole(Role.Pentester) && (currentScanStatus == QueueScanStatus.Error || currentScanStatus == QueueScanStatus.Processing) && newScanStatus == QueueScanStatus.Loading)
                {
                    return true;
                }

                // current must be loading/pending, new must be loading/pending/cancel, current and new can't be equal
                return (currentScanStatus == QueueScanStatus.Loading || currentScanStatus == QueueScanStatus.Pending) &&
                    (newScanStatus == QueueScanStatus.Loading || newScanStatus == QueueScanStatus.Pending || newScanStatus == QueueScanStatus.Cancel) &&
                    (newScanStatus != currentScanStatus);
            }

            // Current has to be at least pending in the workflow, new must be higher than current, current can't be an end state
           
            return currentScanStatus >= QueueScanStatus.Pending && newScanStatus > currentScanStatus && currentScanStatus < QueueScanStatus.Cancel;
        }

        private static void ValidateStatus(string status)
        {
            if (!Enum.TryParse<QueueScanStatus>(status, out _))
                throw new ApiValidationException($"{status} is not a valid Queue Scan Status.");
        }

        #endregion
    }
}