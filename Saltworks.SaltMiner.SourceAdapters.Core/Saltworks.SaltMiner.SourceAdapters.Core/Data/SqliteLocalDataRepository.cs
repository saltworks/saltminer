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

﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Saltworks.SaltMiner.SourceAdapters.Core.Data
{
    // TODO: review exception handling herein - (1) do we need it, (2), if so, method call on catch to keep it DRY
    public class SqliteLocalDataRepository : ILocalDataRepository
    {
        private bool DisposedValue;
        private readonly ILogger Logger;
        private readonly SqliteDbContext DataContext;
        private readonly object DataContextLock = new();  // in .net 9+, use System.Threading.Lock
        private readonly object AddUpdateQueueLock = new();  // in .net 9+, use System.Threading.Lock
        private readonly List<SqliteDbChangeEntry> AddUpdateQueue = [];
        private readonly int SaveBatchSize = 1000;
        private readonly int QueueScanBatchSize = 200;
        private readonly int QueueAssetBatchSize = 200;
        private readonly int QueueIssueBatchSize = 1000;
        private readonly int SourceMetricBatchSize = 1000;
        private bool DbConnectionSet = false;

        public SqliteLocalDataRepository(ILogger<SqliteLocalDataRepository> logger, IServiceProvider provider)
        {
            Logger = logger;
            DataContext = provider.CreateScope().ServiceProvider.GetRequiredService<SqliteDbContext>();
            DataContext.ChangeTracker.AutoDetectChangesEnabled = false;
            Logger.LogDebug("[Sqlite] SqlLiteLocalDataRepository initialized");
        }

        public void SetDbConnection(string connectionString)
        {
            if (DbConnectionSet)
                throw new SourceException("Cannot set db connection after it is already set.");
            DataContext.Database.SetConnectionString(connectionString);
            DataContext.Database.EnsureCreated();
            Logger.LogInformation("DB connection set - using {Db}", connectionString);
            DbConnectionSet = true;
        }

        public SyncRecord GetSyncRecord(string instance, string sourceType)
        {
            try
            {
                lock (DataContextLock)
                {
                    var syncRecord = DataContext.SyncRecords.FirstOrDefault(x => x.Instance == instance && x.SourceType == sourceType);
                    if (syncRecord != null)
                    {
                        return syncRecord;
                    }

                    return AddUpdate(new SyncRecord()
                    {
                        Instance = instance,
                        SourceType = sourceType,
                        State = SyncState.InProgress
                    }, true);
                }
            }
            catch (Exception ex)
            {
                throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
            }
        }

        public SyncRecord CheckSyncRecordSourceForFailure(string instance, string sourceType)
        {
            try
            {
                lock (DataContextLock)
                {
                    return DataContext.SyncRecords.FirstOrDefault(x => x.Instance == instance && x.SourceType == sourceType && x.State == SyncState.InProgress);
                }
            }
            catch (Exception ex)
            {
                throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
            }
        }

        public SourceMetric GetSourceMetric(string instance, string sourceType, string sourceId)
        {
            try
            {
                lock (DataContextLock)
                {
                    var sourceMetric = DataContext.SourceMetrics.FirstOrDefault(x => x.SourceId == sourceId && x.Instance == instance && x.SourceType == sourceType);
                    if (sourceMetric != null)
                        Logger.LogDebug("[Sqlite] GetSourceMetric succeeded for instance '{Instance}', sourceType '{SourceType}', and sourceId '{SourceId}'", instance, sourceType, sourceId);
                    else
                        Logger.LogDebug("[Sqlite] GetSourceMetric failed (not dataFound) for instance '{Instance}', sourceType '{SourceType}', and sourceId '{SourceId}'", instance, sourceType, sourceId);
                    return sourceMetric;
                }
            }
            catch (Exception ex)
            {
                throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
            }
        }

        /// <remarks>
        /// This method resets IsProcessed to false before enumerating results...
        /// </remarks>
        public IEnumerable<SourceMetric> GetSourceMetrics(string instance, string sourceType)
        {
            Logger.LogDebug("[Sqlite] GetSourceMetrics for instance '{Instance}' and sourceType '{Type}'", instance, sourceType);
            try
            {
                lock (DataContextLock)
                {
                    DataContext.Database.ExecuteSqlRaw($"update SourceMetrics set IsProcessed = 0 where Instance = '{instance}' and SourceType = '{sourceType}'");
                }
            }
            catch (Exception ex)
            {
                throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
            }
            string after = "";
            List<SourceMetric> metrics;
            while (true)
            {
                try
                {
                    lock (DataContextLock)
                    {
                        metrics = DataContext.SourceMetrics
                            .Where(x => x.Instance == instance && x.SourceType == sourceType && x.SourceId.CompareTo(after) > 0)
                            .OrderBy(x => x.SourceId)
                            .Take(SourceMetricBatchSize)
                            .ToList();
                    }

                }
                catch (Exception ex)
                {
                    throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
                }
                if (metrics.Count == 0)
                    break;
                after = metrics[^1].SourceId;
                foreach (var metric in metrics)
                    yield return metric;
            }
        }

        public void DeleteQueueScan(string id, bool immediate = false)
        {
            // Use cases:
            // 1. Immediate save to database: bypass save queues
            // 2. More issues to save than the SaveBatchSize: set immediate even if not set already
            // 3. Not immediate and not too many issues: add to queues but "pause" queue saves until done adding to not conflict with foreach queries
            // Try to delete assets/issues even if scan not dataFound
            try
            {
                Logger.LogDebug("[Sqlite] DeleteQueueScan for scan {Id}, immediate: {Yn}", id, immediate);
                bool didIt;
                lock (DataContextLock)
                {
                    didIt = Delete<QueueScan>(id, immediate);
                    immediate = immediate || DataContext.QueueIssues.Count(x => x.QueueScanId == id) >= SaveBatchSize;
                }
                if (!didIt)
                {
                    Logger.LogWarning("[Sqlite] DeleteQueueScan unable to delete queue scan with queueScanId {ScanId}, probably not dataFound.  Attempting to delete any related assets/issues anyway.", id);
                }
                if (immediate)
                {
                    lock (DataContextLock)
                    {
                        DataContext.QueueAssets.RemoveRange(DataContext.QueueAssets.Where(x => x.QueueScanId == id));
                        DataContext.QueueIssues.RemoveRange(DataContext.QueueIssues.Where(x => x.QueueScanId == id));
                        DataContext.ChangeTracker.DetectChanges();
                        DataContext.SaveChanges();
                    }
                }
                else
                {
                    lock (DataContextLock)
                    {
                        foreach (var e in DataContext.QueueAssets.Where(x => x.QueueScanId == id))
                            AddToSaveBatch(e, EntityState.Deleted, true);
                        foreach (var e in DataContext.QueueIssues.Where(x => x.QueueScanId == id))
                            AddToSaveBatch(e, EntityState.Deleted, true);
                    }
                    AddToSaveBatch<QueueAsset>(null, EntityState.Unchanged); // unpause
                }
            }
            catch (Exception ex)
            {
                throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
            }
        }

        public void DeleteAllQueues(string sourceType = "", bool includeMemoryQueues = true)
        {
            try
            {
                Logger.LogDebug($"[Sqlite] DeleteAllQueues start");
                if (includeMemoryQueues)
                {
                    lock (AddUpdateQueueLock)
                    {
                        AddUpdateQueue.Clear();
                    }
                }
                lock (DataContextLock)
                {
                    // ExecuteSql couldn't be made to work this way, and sourceType isn't a user-supplied value
                    #pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
                    if (!string.IsNullOrEmpty(sourceType))
                    {
                        DataContext.Database.ExecuteSqlRaw($"delete from {nameof(QueueIssue)}s where QueueScanId in (select Id from QueueScans where SourceType = '{sourceType}')");
                        DataContext.Database.ExecuteSqlRaw($"delete from {nameof(QueueAsset)}s where SourceType = '{sourceType}'");
                        DataContext.Database.ExecuteSqlRaw($"delete from {nameof(QueueScan)}s where SourceType = '{sourceType}'");
                    }
                    else
                    {
                        // False positive, not an SQL keyword
                        #pragma warning disable S2857 // SQL keywords should be delimited by whitespace
                        DataContext.Database.ExecuteSqlRaw($"delete from {nameof(QueueIssue)}s");
                        DataContext.Database.ExecuteSqlRaw($"delete from {nameof(QueueAsset)}s");
                        DataContext.Database.ExecuteSqlRaw($"delete from {nameof(QueueScan)}s");
                        #pragma warning restore S2857 // SQL keywords should be delimited by whitespace
                    }
                    #pragma warning restore EF1002 // Risk of vulnerability to SQL injection.
                }
                Logger.LogDebug($"[Sqlite] DeleteAllQueues complete");
            }
            catch (Exception ex)
            {
                throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
            }
        }

        public void SaveAllBatches()
        {
            SaveBatch<QueueAsset>();
            SaveBatch<QueueIssue>();
            SaveBatch<QueueScan>();  // scan should go last in case loading = false
            SaveBatch<SourceMetric>();
            SaveBatch<DataDict>();
            Logger.LogDebug("[LocalData] Save complete.");
        }


        private void AddToSaveBatch<T>(T entity, EntityState state, bool pauseFlush = false) where T : ILocalDataEntity
        {
            if (!object.Equals(entity, default(T)))
            {
                lock (AddUpdateQueueLock)
                {
                    var entry = AddUpdateQueue.Find(e => e.Entity.Id == entity.Id && e.Type == entity.GetType());
                    if (entry != null)
                    {
                        entry.Entity = entity;
                        if (entry.State != state)
                        {
                            // logic for state change based on current state and incoming state (if different)
                            // best guess handling here...
                            if (entry.State == EntityState.Added && state == EntityState.Deleted)
                                AddUpdateQueue.Remove(entry);  // just kidding, don't save this one
                            if (entry.State == EntityState.Modified && state == EntityState.Deleted)
                                entry.State = EntityState.Deleted;
                            if (entry.State == EntityState.Deleted)
                                entry.State = state;
                        }
                    }
                    else
                    {
                        AddUpdateQueue.Add(new()
                        {
                            Entity = entity,
                            State = state,
                            Type = typeof(T)
                        });
                    }
                }
            }

            int qCount;
            lock (AddUpdateQueueLock)
            {
                qCount = AddUpdateQueue.Count;
            }
            if (qCount >= SaveBatchSize && !pauseFlush)
                SaveAllBatches();
        }

        private void DataContextSave()
        {
            try
            {
                DataContext.ChangeTracker.DetectChanges();
                DataContext.SaveChanges(true);
                DataContext.ChangeTracker.Clear();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (ex.Message.StartsWith("The database operation was expected to affect"))
                {
                    // suppress the inner exception, avoid filling the log from one error message
                    #pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter, but we want to suppress the inner exception here.
                    Logger.LogError("During data context save operation, {ExName} was thrown.  Inner exception will be suppressed to protect logging overflow.", nameof(DbUpdateConcurrencyException));
                    #pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
                    throw new LocalDataConcurrencyException(ex.Message);
                }
            }
        }

        public void SaveBatch<T>() where T : class, ILocalDataEntity
        {
            var type = typeof(T);
            lock (AddUpdateQueueLock)
            {
                if (!AddUpdateQueue.Exists(ce => ce.Type == type))
                    return;  // nothing to do
                var batch = AddUpdateQueue.Where(ce => ce.Type == type);
                Logger.LogDebug("[LocalData] Saving changes for {Count} {Type} doc(s).", batch.Count(), type.Name);
                lock (DataContextLock)
                {
                    DataContext.Set<T>().AddRange(batch.Where(ce => ce.State == EntityState.Added).Select(ce => (T)ce.Entity));
                    DataContext.Set<T>().UpdateRange(batch.Where(ce => ce.State == EntityState.Modified).Select(ce => (T)ce.Entity));
                    DataContext.Set<T>().RemoveRange(batch.Where(ce => ce.State == EntityState.Deleted).Select(ce => (T)ce.Entity));
                    DataContextSave();
                }
                AddUpdateQueue.RemoveAll(ce => ce.Type == type);
            }
        }




        /// <param name="entity">Entity to add/update</param>
        /// <param name="immediate">If add and immediate set, will be immediately sent to DB instead of being queued for a batch add.</param>
        /// <remarks>
        /// Use immediate if you must, but performance may suffer.  
        /// Don't add ID or call entity.UpdateDtoFields, let me do it!
        /// All serialized entities should have IDs
        /// </remarks>
        public T AddUpdate<T>(T entity, bool immediate = false) where T : class, ILocalDataEntity
        {
            var isNew = string.IsNullOrEmpty(entity.Id);
            entity.Id = isNew ? Guid.NewGuid().ToString() : entity.Id;
            entity.UpdateDtoFields();
            try
            {
                if (!immediate)
                {
                    AddToSaveBatch(entity, isNew ? EntityState.Added : EntityState.Modified);
                }
                else
                {
                    // immediate save
                    //var perfId = PerfHelper.Start(nameof(AddUpdate) + etype.Name)
                    lock (DataContextLock)
                    {
                        DataContext.Entry(entity).State = isNew ? EntityState.Added : EntityState.Modified;
                        DataContextSave();
                    }
                    //PerfHelper.Complete(perfId)
                }
                return entity;
            }
            catch (Exception ex)
            {
                throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
            }
        }

        public bool Delete<T>(string id, bool immediate = false) where T : class, ILocalDataEntity
        {
            try
            {
                T entity;
                lock (DataContextLock)
                {
                    entity = DataContext.Set<T>().FirstOrDefault(x => x.Id == id);
                }
                if (entity == null)
                {
                    return false;
                }
                if (immediate)
                {
                    lock (DataContextLock)
                    {
                        DataContext.Set<T>().Remove(entity);
                        DataContext.ChangeTracker?.DetectChanges();
                        DataContext.SaveChanges();
                    }
                }
                else
                {
                    AddToSaveBatch(entity, EntityState.Deleted);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
            }
        }

        public T Get<T>(string id) where T : class, ILocalDataEntity
        {
            try
            {
                lock (AddUpdateQueueLock)
                {
                    var item = (T)AddUpdateQueue.Find(i => i.Entity.Id == id)?.Entity;
                    if (item != null)
                        return item;
                }
                lock (DataContextLock)
                {
                    return DataContext.Set<T>().FirstOrDefault(x => x.Id == id);
                }
            }
            catch (Exception ex)
            {
                throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
            }
        }

        public QueueAsset GetQueueAsset(string sourceType, string sourceId)
        {
            try
            {
                lock (DataContextLock)
                {
                    return DataContext.QueueAssets.FirstOrDefault(s => s.SourceType == sourceType && s.SourceId == sourceId);
                }
            }
            catch (Exception ex)
            {
                throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
            }
        }

        public QueueScan GetNextQueueScan(string instance, string sourceType, int failureCount) => GetQueueScans(instance, sourceType, failureCount).FirstOrDefault();

        public IEnumerable<QueueScan> GetQueueScans(string instance, string sourceType) => GetQueueScans(instance, sourceType, -1, DateTime.UtcNow, false);
        public IEnumerable<QueueScan> GetQueueScans(string instance, string sourceType, int failureCount) => GetQueueScans(instance, sourceType, failureCount, DateTime.UtcNow, false);
        public IEnumerable<QueueScan> GetQueueScans(string instance, string sourceType, int failureCount, DateTime date, bool loading)
        {
            if (failureCount < 0)
                failureCount = int.MaxValue;
            List<QueueScan> scans;
            string after = "";
            while (true)
            {
                try
                {
                    lock (DataContextLock)
                    {
                        scans = DataContext.QueueScans
                        .Where(s => s.Loading == loading &&
                            s.Instance == instance &&
                            s.SourceType == sourceType &&
                            s.FailureCount <= failureCount &&
                            s.Timestamp < date &&
                            s.Id.CompareTo(after) > 0)
                        .OrderBy(s => s.Id)
                        .Take(QueueScanBatchSize)
                        .ToList();
                    }
                }
                catch (Exception ex)
                {
                    throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
                }
                if (scans.Count == 0)
                    break;
                after = scans[^1].Id;
                foreach (var scan in scans)
                    yield return scan;
            }
        }

        public DataDict GetDataDictionary(string instance, string sourceType, string dataType, string key) =>
            GetDataDictionary(instance, sourceType, dataType, string.IsNullOrEmpty(key) ? null : [key]).First();
        public IEnumerable<DataDict> GetDataDictionary(string instance, string sourceType, string dataType, IEnumerable<string> keys)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(nameof(instance));
            ArgumentNullException.ThrowIfNullOrEmpty(nameof(sourceType));
            List<DataDict> dictionary;
            string after = "";
            while (true)
            {
                try
                {
                    lock (DataContextLock)
                    {
                        if (keys == null)
                        {
                            dictionary = DataContext.DataDicts
                            .Where(s => s.Instance == instance &&
                                s.SourceType == sourceType &&
                                s.DataType == (string.IsNullOrEmpty(dataType) ? s.DataType : dataType) &&
                                s.Id.CompareTo(after) > 0)
                            .OrderBy(s => s.Id)
                            .Take(1000)
                            .ToList();
                        }
                        else
                        {
                            dictionary = DataContext.DataDicts
                            .Where(s => s.Instance == instance &&
                                s.SourceType == sourceType &&
                                s.DataType == (string.IsNullOrEmpty(dataType) ? s.DataType : dataType) &&
                                keys.Contains(s.Key) &&  // this should become "in (x,y,z)" in sql
                                s.Id.CompareTo(after) > 0)
                            .OrderBy(s => s.Id)
                            .Take(1000)
                            .ToList();
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
                }
                if (dictionary.Count == 0)
                    break;
                after = dictionary[^1].Id;
                foreach (var dict in dictionary)
                    yield return dict;
            }
        }

        public long CountQueueScans(string instance, string sourceType) => CountQueueScans(instance, sourceType, -1, DateTime.UtcNow, false);
        public long CountQueueScans(string instance, string sourceType, int failureCount) => CountQueueScans(instance, sourceType, failureCount, DateTime.UtcNow, false);
        public long CountQueueScans(string instance, string sourceType, int failureCount, DateTime date, bool loading)
        {
            if (failureCount < 0)
                failureCount = int.MaxValue;
            try
            {
                lock (DataContextLock)
                {
                    var cmd = DataContext.Database.GetDbConnection().CreateCommand();
                    if (cmd.Connection.State != ConnectionState.Open)
                    {
                        cmd.Connection.Open();
                    }
                    cmd.CommandText = $"select count(1) from QueueScans where Loading = {(loading ? 1 : 0)} and Instance = '{instance}' and SourceType = '{sourceType}' and FailureCount < {failureCount} and Timestamp < '{date:O}'";
                    var result = (long)(cmd.ExecuteScalar() ?? 0);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
            }
        }


        public DateTime? GetLatestFoundDate(string queueScanId)
        {
            DateTime? dataFound;
            lock (DataContextLock)
            {
                dataFound = DataContext.QueueIssues.Where(qi => qi.QueueScanId == queueScanId).Max(qi => qi.FoundDate);
            }
            lock (AddUpdateQueueLock)
            {
                var memFound = AddUpdateQueue
                    .Where(e => e.Type == typeof(QueueIssue) && ((QueueIssue)e.Entity).QueueScanId == queueScanId)
                    .Max(e => ((QueueIssue)e.Entity).FoundDate);
                if ((dataFound ?? DateTime.MinValue) > (memFound ?? DateTime.MinValue))
                    return dataFound;
                else
                    return memFound;
            }
        }

        public int GetQueueIssuesCountByScanId(string queueScanId, bool includeBuffer=true)
        {
            try
            {
                int dataFound;
                lock (DataContextLock)
                {
                    dataFound = DataContext.QueueIssues.Count(x => x.QueueScanId == queueScanId);
                }
                if (includeBuffer)
                {
                    lock (AddUpdateQueueLock)
                    {
                        return dataFound + AddUpdateQueue.Count(e => e.Type == typeof(QueueIssue) && ((QueueIssue)e.Entity).QueueScanId == queueScanId);
                    }
                }
                else
                {
                    return dataFound;
                }
            }
            catch (Exception ex)
            {
                throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
            }
        }

        public IEnumerable<QueueIssue> GetQueueIssues(string queueScanId, string queueAssetId)
        {
            List<QueueIssue> issues;
            string after = "";
            while (true)
            {
                try
                {
                    lock (DataContextLock)
                    {
                        issues = DataContext.QueueIssues
                        .Where(x => x.QueueScanId == queueScanId && x.QueueAssetId == queueAssetId && x.Id.CompareTo(after) > 0)
                        .OrderBy(x => x.Id)
                        .Take(QueueIssueBatchSize)
                        .ToList();
                    }
                }
                catch (Exception ex)
                {
                    throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
                }
                if (issues.Count == 0)
                    break;
                after = issues[^1].Id;
                foreach (var issue in issues)
                    yield return issue;
            }
        }

        public IEnumerable<QueueAsset> GetQueueAssets(string queueScanId)
        {
            List<QueueAsset> assets;
            string after = "";
            while (true)
            {
                try
                {
                    lock (DataContextLock)
                    {
                        assets = DataContext.QueueAssets
                        .Where(x => x.QueueScanId == queueScanId && x.Id.CompareTo(after) > 0)
                        .OrderBy(x => x.Id)
                        .Take(QueueAssetBatchSize)
                        .ToList();
                    }
                }
                catch (Exception ex)
                {
                    throw new LocalDataException(ex.InnerException?.Message ?? ex.Message, ex);
                }
                if (assets.Count == 0)
                    break;
                after = assets[^1].Id;
                foreach (var asset in assets)
                    yield return asset;
            }
        }

        internal class SqliteDbChangeEntry
        {
            internal Type Type { get; set; }
            internal ILocalDataEntity Entity { get; set; }
            internal EntityState State { get; set; }
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!DisposedValue)
            {
                if (disposing)
                {
                    DataContext?.Dispose();
                }
                DisposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    public class SqliteDbContext : DbContext
    {
        public DbSet<SyncRecord> SyncRecords { get; set; }
        public DbSet<SourceMetric> SourceMetrics { get; set; }
        public DbSet<QueueAsset> QueueAssets { get; set; }
        public DbSet<QueueIssue> QueueIssues { get; set; }
        public DbSet<QueueScan> QueueScans { get; set; }
        public DbSet<DataDict> DataDicts { get; set; }

        public SqliteDbContext(DbContextOptions<SqliteDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QueueAsset>().ToTable("QueueAssets").HasIndex(i => i.QueueScanId);
            var qit = modelBuilder.Entity<QueueIssue>().ToTable("QueueIssues");
            qit.HasIndex(i => new { i.QueueScanId, i.QueueAssetId });
            qit.HasIndex(i => new { i.Id });
            modelBuilder.Entity<QueueScan>().ToTable("QueueScans").HasIndex(i => new { i.Loading, i.Instance, i.SourceType });
            modelBuilder.Entity<SourceMetric>().ToTable("SourceMetrics").HasIndex(i => new { i.Instance, i.SourceType, i.SourceId });
            modelBuilder.Entity<SyncRecord>().ToTable("SyncRecords");
            modelBuilder.Entity<DataDict>().ToTable("DataDicts").HasIndex(i => new { i.Instance, i.SourceType, i.DataType, i.Key });
        }
    }
}
