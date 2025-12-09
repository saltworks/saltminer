/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
*/

﻿using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.SourceAdapters.Core.Data
{
    public interface ILocalDataRepository : IDisposable
    {

        SyncRecord GetSyncRecord(string instance, string sourceType);
        SyncRecord CheckSyncRecordSourceForFailure(string instance, string sourceType);
        /// <summary>
        /// Returns source metric that matches the passed criteria or null if not found.
        /// </summary>
        /// <param name="instance">Instance from config</param>
        /// <param name="sourceType">Source type from config</param>
        /// <param name="sourceId">Source ID</param>
        SourceMetric GetSourceMetric(string instance, string sourceType, string sourceId);
        IEnumerable<SourceMetric> GetSourceMetrics(string instance, string sourceType);
        IEnumerable<QueueIssue> GetQueueIssues(string queueScanId, string queueAssetId);
        IEnumerable<QueueAsset> GetQueueAssets(string queueScanId);
        QueueAsset GetQueueAsset(string sourceType, string sourceId);
        int GetQueueIssuesCountByScanId(string queueScanId, bool includeBuffer = true);
        DateTime? GetLatestFoundDate(string queueScanId);
        /// <summary>
        /// Delete queue scan and all related queue issues and queue assets
        /// </summary>
        void DeleteQueueScan(string id, bool immediate=false);
        void DeleteAllQueues(string sourceType="", bool includeMemoryQueues = true);
        QueueScan GetNextQueueScan(string instance, string sourceType, int failureCount);
        IEnumerable<DataDict> GetDataDictionary(string instance, string sourceType, string dataType, IEnumerable<string> keys);
        DataDict GetDataDictionary(string instance, string sourceType, string dataType, string key);
        IEnumerable<QueueScan> GetQueueScans(string instance, string sourceType);
        IEnumerable<QueueScan> GetQueueScans(string instance, string sourceType, int failureCount);
        IEnumerable<QueueScan> GetQueueScans(string instance, string sourceType, int failureCount, DateTime date, bool loading);
        long CountQueueScans(string instance, string sourceType);
        long CountQueueScans(string instance, string sourceType, int failureCount);
        long CountQueueScans(string instance, string sourceType, int failureCount, DateTime date, bool loading);
        T Get<T>(string id) where T : class, ILocalDataEntity;
        /// <summary>Adds or updates passed entity using buffered operations to improve performance</summary>
        /// <param name="entity">Entity to add/update</param>
        /// <param name="immediate">If add and immediate set, will be immediately sent to DB instead of being queued for a batch add.</param>
        /// <remarks>
        /// Use immediate if you must (sync records may need this for example), but performance may suffer.  
        /// Don't include Id or call entity.UpdateDtoFields, these will be handled automatically
        /// All serialized entities should have IDs
        /// </remarks>
        T AddUpdate<T>(T entity, bool immediate=false) where T : class, ILocalDataEntity;
        bool Delete<T>(string id, bool immediate=false) where T: class, ILocalDataEntity;
        /// <summary>
        /// Flushes any remaining buffered updates to the database.  Call this once at the end of processing.
        /// </summary>
        void SaveAllBatches();
        /// <summary>
        /// Flushes any buffered updates of type T to the database.  Call this sparingly or performance may suffer.
        /// </summary>
        /// <typeparam name="T">Type of entity to flush to database</typeparam>
        void SaveBatch<T>() where T : class, ILocalDataEntity;
        void SetDbConnection(string connectionString);
    }
}
