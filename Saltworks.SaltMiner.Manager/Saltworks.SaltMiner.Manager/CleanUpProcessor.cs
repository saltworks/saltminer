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
using Saltworks.SaltMiner.DataClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.Manager;

public class CleanUpProcessor(ILogger<CleanUpProcessor> logger, DataClientFactory<Manager> dataClientFactory, ManagerConfig config)
{
    private readonly ILogger Logger = logger;
    private readonly DataClient.DataClient DataClient = dataClientFactory.GetClient();
    private readonly ManagerConfig Config = config;
    private CleanUpRuntimeConfig RunConfig;
    private readonly ConcurrentQueue<string> DeleteQueue = new();
    private readonly ConcurrentQueue<string> TaskQueue = [];

    /// <summary>
    /// Runs clean up processing for queue scans that have exceeded the maximum days as assigned
    /// to the setting variables Cleanup[Status]AfterHours in config settings. Removes orphaned history scans
    /// if status is 'pending' and parent scan status is 'complete' or 'error'.
    /// </summary>
    /// <remarks>
    /// Design:
    ///     1. Use async as much as possible and batch delete calls.
    ///     2. Queue up queue scan IDs to be deleted, even if we expect no queue scan to exist.  Back end deletes all queue scans/assets/issues from this ID.
    ///     3. Use queue deletion instead of index removal via aging policy because we aren't building date tagged indices.  Policy is based on status/age, not just age
    ///     4. Don't remove Pentest Error/Loading/Processing queues
    ///     5. Three phase run:
    ///        a. Remove by queue status & aging settings
    ///        b. Find and remove orphan queue assets and queue issues by queue asset search
    ///        c. Find and remove orphan queue issues by queue issue search - this one must only be run once a & b are complete successfully
    /// Future:
    ///     1. Add aggregate search to get distinct queue scan for issues, OR search for orphans ordered by oldest first and stop processing when no orphans found in x batches
    /// </remarks>
    public void Run(RuntimeConfig config)
    {

        if (config is not CleanUpRuntimeConfig crunConfig)
        {
            throw new ArgumentException($"Expected type '{nameof(CleanUpRuntimeConfig)}', but passed value is '{config.GetType().Name}'", nameof(config));
        }

        RunConfig = config.Validate() as CleanUpRuntimeConfig;

        Logger.LogInformation("Looking for queue scan(s) to clean up, configured for source '{Source}', limit {Limit}, and listOnly {ListOnly}", string.IsNullOrEmpty(RunConfig?.SourceType) ? "[all]" : RunConfig?.SourceType, RunConfig?.Limit, RunConfig?.ListOnly);

        var tasks = new List<Task>();
        List<string> skips = [];
        List<QueueScan.QueueScanStatus> statuses = [
            QueueScan.QueueScanStatus.Complete,
            QueueScan.QueueScanStatus.Error,
            QueueScan.QueueScanStatus.Processing,
            QueueScan.QueueScanStatus.Loading,
            QueueScan.QueueScanStatus.Cancel
        ];
        foreach (var s in statuses)
        {
            if (Config.CleanupProcessorDisableForStatus.Contains(s.ToString("g")))
                skips.Add(s.ToString("g"));
            else
                tasks.Add(GetByStatusAsync(QueueScan.QueueScanStatus.Complete));
        }
        
        if (skips.Count > 0)
            Logger.LogInformation("Per CleanupProcessorDisableForStatus setting, skipping cleanup for statuses: {Statuses}", string.Join(", ", skips));

        try
        {
            // Remove by status and aging settings
            if (tasks.Count == 0)
            {
                Logger.LogInformation("No queue scan statuses to clean up.");
            }
            else
            {
                tasks.Add(ProcessQueueAsync(crunConfig));
                Task.WaitAll([.. tasks]);
                Logger.LogInformation("Pausing for 30 sec to allow delete to complete before looking for orphans...");
                Task.Delay(TimeSpan.FromSeconds(30)).Wait(); // wait for the delete to finish before looking for orphans
            }

            // Remove orphan queue assets and queue issues by queue asset search
            Task.WaitAll(
                GetByAssetOrphanAsync(),
                ProcessQueueAsync(crunConfig)
            );

            // Remove orphan queue issues by queue issue search
            Task.WaitAll(
                GetByIssueOrphanAsync(),
                ProcessQueueAsync(crunConfig)
            );
        }
        catch (CancelTokenException)
        {
            // Already logged, so just do nothing but quit silently
        }
        catch (ManagerException ex)
        {
            Logger.LogError(ex, "Error in CleanUp processor: [{Type}] {Msg}", ex.GetType().Name, ex.Message);
            throw new ManagerException($"Cleanup processor error: [{ex.GetType().Name}] {ex.Message}");
        }
    }

    #region Orphans

    private async Task<int> FindOrphansAsync(List<string> idList)
    {
        var counter = 0;
        if (idList.Count > 10000)
            throw new ArgumentOutOfRangeException(nameof(idList), "Must be less than 10000 IDs in list");
        var srch = new SearchRequest()
        {
            Filter = new()
            {
                FilterMatches = new()
                {
                    { "Id", SaltMiner.DataClient.Helpers.BuildTermsFilterValue(idList) }
                }
            },
            UIPagingInfo = new(10000)
        };
        var rsp = await DataClient.QueueScanSearchAsync(srch);
        if (!rsp.Success)
            throw new ManagerException("Failed to search for orphan queue assets (queue scan lookup): " + rsp.Message);
        foreach (var qs in rsp.Data)
            if (idList.Contains(qs.Id))
                idList.Remove(qs.Id);
        // remaining IDs are the orphans
        foreach (var id in idList)
        {
            DeleteQueue.Enqueue(id);
            counter++;
        }
        return counter;
    }

    private async Task GetByAssetOrphanAsync()
    {
        TaskQueue.Enqueue(Guid.NewGuid().ToString());
        try
        {
            var maxloops = 25000;
            var curloops = 0;
            var counter = 0;
            var acounter = 0;
            var size = 10000;
            var asrch = new SearchRequest()
            {
                Filter = new()
                {
                    FilterMatches = []
                },
                UIPagingInfo = new(size)
                {
                    SortFilters = new() { { "Saltminer.Internal.QueueScanId", true } }
                }
            };
            // Filter to passed source type if appropriate
            if (!string.IsNullOrEmpty(RunConfig.SourceType))
                asrch.Filter.FilterMatches.Add("Saltminer.Scan.SourceType", RunConfig.SourceType);

            while (curloops < maxloops)
            {
                curloops++;
                List<string> qsIds = [];
                var rsp = await DataClient.QueueAssetSearchAsync(asrch);
                if (!rsp.Success)
                    break;
                foreach (var qs in rsp.Data)
                {
                    if (qsIds.Contains(qs.Saltminer.Internal.QueueScanId))
                        continue;
                    qsIds.Add(qs.Saltminer.Internal.QueueScanId);
                }
                asrch.UIPagingInfo.Page++;
                asrch.AfterKeys = rsp.AfterKeys;
                counter += await FindOrphansAsync(qsIds);
                if (rsp.UIPagingInfo.TotalPages == 0 || asrch.UIPagingInfo.Page > rsp.UIPagingInfo.TotalPages)
                    break;
                if (acounter >= Config.CleanupProcessorMaxOrphanSearch)
                {
                    Logger.LogInformation("Max orphan search count of {Max} reached (CleanupProcessorMaxOrphanSearch), stopping search for queue asset orphans", Config.CleanupProcessorMaxOrphanSearch);
                    break;
                }
                Logger.LogInformation("Looking for orphans in assets, {Count}/{Total} so far...", acounter, rsp.UIPagingInfo.Total);
            }
            if (curloops >= maxloops)
                throw new ManagerException($"Max loops exceeded when searching for queue asset orphans - possible hang bug detected.");
            CheckCancel(true);
            Logger.LogInformation("[GetByOrphan] Found a total of {Count} orphaned queue asset scan IDs that can be removed", counter);
        }
        finally
        {
            if (!TaskQueue.TryDequeue(out _))
                Logger.LogError("Failed to de-queue GetByAssetOrphan task");
        }
    }

    private async Task GetByIssueOrphanAsync()
    {
        TaskQueue.Enqueue(Guid.NewGuid().ToString());
        try
        {
            var maxloops = 25000;
            var issCounter = 0;
            var size = 10000;
            var curloops = 0;
            var counter = 0;
            var asrch = new SearchRequest()
            {
                Filter = new()
                {
                    FilterMatches = []
                },
                UIPagingInfo = new(size)
                {
                    SortFilters = new() { { "Saltminer.QueueScanId", true } }
                }
            };
            // Filter to passed source type if appropriate - this will cause issue orphan search to find no matches, which is fine
            if (!string.IsNullOrEmpty(RunConfig.SourceType))
                asrch.Filter.FilterMatches.Add("Saltminer.Scan.SourceType", RunConfig.SourceType);

            while (curloops < maxloops)
            {
                curloops++;
                List<string> qsIds = [];
                var rsp = await DataClient.QueueIssueSearchAsync(asrch);
                issCounter += size;
                if (!rsp.Success || !rsp.Data.Any())
                    break;
                foreach (var qs in rsp.Data)
                {
                    if (qsIds.Contains(qs.Saltminer.QueueScanId))
                        continue;
                    qsIds.Add(qs.Saltminer.QueueScanId);
                }
                asrch.UIPagingInfo.Page++;
                asrch.AfterKeys = rsp.AfterKeys;
                counter += await FindOrphansAsync(qsIds);
                if (rsp.UIPagingInfo.TotalPages == 0 || asrch.UIPagingInfo.Page > rsp.UIPagingInfo.TotalPages)
                    break;
                if (counter >= Config.CleanupProcessorMaxOrphanSearch)
                {
                    Logger.LogInformation("Max orphan search count of {Max} reached (CleanupProcessorMaxOrphanSearch), stopping search for queue issue orphans", Config.CleanupProcessorMaxOrphanSearch);
                    break;
                }
                Logger.LogInformation("Looking for orphans in issues, {Count}/{Total} so far...", issCounter, rsp.UIPagingInfo.Total);
            }
            if (curloops >= maxloops)
                throw new ManagerException($"Max loops exceeded when searching for queue issue orphans - possible hang bug detected.");
            CheckCancel(true);
            Logger.LogInformation("[GetByOrphan] Found a total of {Count} orphaned queue issue scan IDs that can be removed", counter);
        }
        finally
        {
            if (!TaskQueue.TryDequeue(out _))
                Logger.LogError($"Failed to de-queue GetByAssetOrphan task)");
        }
    }

    #endregion

    #region Status

    private async Task GetByStatusAsync(QueueScan.QueueScanStatus status)
    {
        TaskQueue.Enqueue(Guid.NewGuid().ToString());
        var maxloops = 25000;
        var curloops = 0;
        var counter = 0;
        var now = DateTime.UtcNow;
        var hrs = status switch
        {
            QueueScan.QueueScanStatus.Complete => Config.CleanupCompleteAfterHours,
            QueueScan.QueueScanStatus.Cancel => Config.CleanupCompleteAfterHours,
            QueueScan.QueueScanStatus.Processing => Config.CleanupProcessingAfterHours,
            QueueScan.QueueScanStatus.Loading => Config.CleanupLoadingAfterHours,
            QueueScan.QueueScanStatus.Error => Config.CleanupErrorAfterHours,
            _ => throw new NotImplementedException($"Status {status:g} not supported"),
        };
        // Left out Saltminer.Internal.CurrentQueueScanId on purpose - include history scans in results
        var srch = new SearchRequest()
        {
            Filter = new()
            {
                FilterMatches = new()
                {
                    { "Saltminer.Internal.QueueStatus", status.ToString("g") },
                    { "LastUpdated", SaltMiner.DataClient.Helpers.BuildLessThanFilterValue(now.AddHours(-hrs).ToString("o")) }
                },
                AnyMatch = false
            },
            UIPagingInfo = new(Config.CleanupProcessorBatchSize)
        };
        // Don't remove PenTest unless complete or cancel
        if (status != QueueScan.QueueScanStatus.Complete && status != QueueScan.QueueScanStatus.Cancel)
        {
            srch.Filter.FilterMatches.Add("Saltminer.Engagment.Id", SaltMiner.DataClient.Helpers.BuildMustNotExistsFilterValue());
            srch.Filter.FilterMatches.Add("Saltminer.Scan.SourceType", SaltMiner.DataClient.Helpers.BuildExcludeTermsFilterValue(["Saltworks.PenTest"]));
        }
        // Filter to passed source type if appropriate
        if (!string.IsNullOrEmpty(RunConfig.SourceType))
            srch.Filter.FilterMatches.Add("Saltminer.Scan.SourceType", RunConfig.SourceType);

        while (curloops < maxloops)
        {
            curloops++;
            var rsp = await DataClient.QueueScanSearchAsync(srch);
            if (!rsp.Success)
                break;
            foreach (var qs in rsp.Data)
            {
                // Redundant check for draft pentest queue data
                if (!string.IsNullOrEmpty(qs.Saltminer.Engagement?.Id) && status != QueueScan.QueueScanStatus.Complete && status != QueueScan.QueueScanStatus.Cancel)
                {
                    Logger.LogWarning("[GetByStatus] Pentest queue scan {Id} with status '{Status}' was found for removal but will not be removed.", qs.Id, status.ToString("g"));
                    continue;
                }
                DeleteQueue.Enqueue(qs.Id);
                counter++;
            }
            srch.AfterKeys = rsp.AfterKeys;
            srch.UIPagingInfo.Page++;
            if (rsp.UIPagingInfo.TotalPages == 0 || srch.UIPagingInfo.Page > rsp.UIPagingInfo.TotalPages)
                break;
        }
        if (curloops >= maxloops)
            throw new ManagerException($"Max loops exceeded when searching for queue scans by status '{status:g}' - possible hang bug detected.");
        CheckCancel(true);
        Logger.LogInformation("[GetByStatus] Found a total of {Count} queue scans with status '{Status}' that can be removed", counter, status.ToString("g"));
        if (!TaskQueue.TryDequeue(out _))
            throw new ManagerException($"Failed to de-queue GetByStatus task ({status:g})");
    }

    #endregion

    #region Process Em

    private async Task ProcessQueueAsync(CleanUpRuntimeConfig runConfig)
    {
        var startingTaskCount = await GetTaskCountAsync();
        var counter = 0L;
        await Task.Delay(5000); // make sure other tasks get started first
        while (!TaskQueue.IsEmpty)
        {
            CheckCancel(true);
            // Wait for empty queue in GetDeleteListBatchAsync()
            var lst = await GetDeleteListBatchAsync(Config.CleanupProcessorBatchSize);
            if (lst.Any())
            {
                if ((runConfig?.Limit ?? 0) > 0 && counter >= runConfig.Limit)
                {
                    Logger.LogInformation("[ProcessQueue] Limit of {Limit} reached or exceeded, stopping execution.", runConfig.Limit);
                    throw new CancelTokenException("Configured limit reached.");
                }
                var taskWaitCycles = 0;
                while ((await GetTaskCountAsync()) > startingTaskCount + Config.CleanupProcessorMaxTaskCount && taskWaitCycles < 360)
                {
                    Logger.LogInformation("[ProcessQueue] Waiting for Elasticsearch tasks to complete (10 sec delay)...");
                    await Task.Delay(10000);
                    taskWaitCycles++;
                    if (taskWaitCycles == 360)
                        throw new ManagerException("Processing failure, Elasticsearch task count remained above threshold (Config.CleanupProcessorMaxTaskCount) for too long.");
                }
                if (runConfig.ListOnly)
                {
                    Logger.LogInformation("[ProcessQueue] ListOnly set, {Count} queue scan(s) would be deleted...", lst.Count());
                    continue;
                }
                var rsp = await DataClient.QueueScanDeleteAllAsync([.. lst]);
                if (rsp.Success)
                {
                    Logger.LogInformation("[ProcessQueue] removed {Count} queue scan(s).", rsp.Affected);
                    counter += rsp.Affected;
                }
                else
                {
                    Logger.LogError("[ProcessQueue] failed to remove queue scan(s), see logs for more detail.");
                }
            }
        }
    }

    private async Task<IEnumerable<string>> GetDeleteListBatchAsync(int size)
    {
        var lst = new List<string>();
        while (lst.Count < size && !TaskQueue.IsEmpty)
        {
            while (DeleteQueue.IsEmpty && !TaskQueue.IsEmpty)
            {
                Logger.LogDebug("[ProcessQueue] GetDeleteListBatch - waiting 2 sec, empty queue...");
                await Task.Delay(2000);
            }
            if (DeleteQueue.TryDequeue(out var id))
                lst.Add(id);
            CheckCancel(true);
        }
        return lst;
    }

    private async Task<long> GetTaskCountAsync()
    {
        var retries = 0;
        while (retries < 3)
        {
            var rsp = await DataClient.GetClusterTaskCountAsync();
            if (rsp.Success)
                return rsp.Affected;
            retries++;
            await Task.Delay(1000); // if failed, wait a sec before trying again
        }
        throw new ManagerException("Failed to get count of running tasks from Elasticsearch");
    }

    #endregion

    private void CheckCancel(bool readyToAbort = true)
    {
        if (RunConfig.CancelToken.IsCancellationRequested)
        {
            if (readyToAbort)
            {
                Logger.LogInformation("Cancellation requested, aborting clean up process.");
                throw new CancelTokenException();
            }
            if (!RunConfig.CancelRequestedReported)
            {
                Logger.LogInformation("Cancellation requested, finishing current clean up process");
                RunConfig.CancelRequestedReported = true;
            }
        }
    }
}
