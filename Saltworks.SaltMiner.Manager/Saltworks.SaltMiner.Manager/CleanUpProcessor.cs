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
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Saltworks.SaltMiner.Manager;

public class CleanUpProcessor(ILogger<CleanUpProcessor> logger, DataClientFactory<Manager> dataClientFactory, ManagerConfig config)
{
    private readonly ILogger Logger = logger;
    private readonly DataClient.DataClient DataClient = dataClientFactory.GetClient();
    private readonly ManagerConfig Config = config;
    private CleanUpRuntimeConfig RunConfig;

    /// <summary>
    /// Runs clean up processing for queue scans that have exceeded the maximum days as assigned
    /// to the setting variable CleanupQueueAfterDays in config settings. Removes orphaned history scans
    /// if status is 'pending' and parent scan status is 'complete' or 'error'.
    /// </summary>
    /// <remarks>
    /// Design decisions:
    ///     1. Use separate search results for each queue scan end status, because we don't have a way to search for multiple term values yet
    ///     2. Delete 1 at a time because we don't currently support batch deletes
    ///     3. Use queue deletion instead of index removal via aging policy because we aren't building date tagged indices
    /// If this utility ends up being too slow then we may need to add one or more search features or change the delete method
    /// </remarks>
    public void Run(RuntimeConfig config)
    {

        if (config is not CleanUpRuntimeConfig)
        {
            throw new ArgumentException($"Expected type '{nameof(CleanUpRuntimeConfig)}', but passed value is '{config.GetType().Name}'", nameof(config));
        }

        RunConfig = config.Validate() as CleanUpRuntimeConfig;

        Logger.LogInformation("Looking for queue scan(s) to clean up, configured for source '{Source}', limit {Limit}, and listOnly {ListOnly}", string.IsNullOrEmpty(RunConfig?.SourceType) ? "[all]" : RunConfig?.SourceType, RunConfig?.Limit, RunConfig?.ListOnly);

        try
        {
            CleanUpOrphanedHistoryScans();
            CleanUpScansByStatus(QueueScan.QueueScanStatus.Complete);
            CleanUpScansByStatus(QueueScan.QueueScanStatus.Error);
            CleanUpScansByStatus(QueueScan.QueueScanStatus.Cancel);
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

    private void CleanUpOrphanedHistoryScans()
    {
        var queueScanRequest = new SearchRequest()
        {
            Filter = new()
            {
                FilterMatches = new Dictionary<string, string>()
                {
                    { "Saltminer.Internal.QueueStatus", QueueScan.QueueScanStatus.Pending.ToString("g")},
                    { "Saltminer.Internal.CurrentQueueScanId", SaltMiner.DataClient.Helpers.BuildMustExistsFilterValue() }
                },
                AnyMatch = false
            },
            PitPagingInfo = new PitPagingInfo(Config.CleanupProcessorBatchSize, false)
        };
        if (!string.IsNullOrEmpty(RunConfig.SourceType))
        {
            queueScanRequest.Filter.FilterMatches.Add("Saltminer.Scan.SourceType", RunConfig.SourceType);
        }

        var count = 0;
        IEnumerable<QueueScan> pendingHistoryScans = DataClient.QueueScanSearch(queueScanRequest).Data;
        do
        {
            if (!pendingHistoryScans.Any())
            {
                break;
            }

            foreach (var historyScan in pendingHistoryScans)
            {
                CheckCancel();

                var removeOrphan = false;

                QueueScan parentScan = null;
                try
                {
                    parentScan = DataClient.QueueScanGet(historyScan.Saltminer.Internal.CurrentQueueScanId).Data;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Could not find scan {Scanid} for history scan {HistoryScanId}.", historyScan.Saltminer.Internal.CurrentQueueScanId, historyScan.Id);
                }
                
                if (parentScan == null)
                {
                    removeOrphan = true;
                }

                if (parentScan != null && 
                        (parentScan.Saltminer.Internal.QueueStatus == QueueScan.QueueScanStatus.Complete.ToString("g") ||
                        parentScan.Saltminer.Internal.QueueStatus == QueueScan.QueueScanStatus.Error.ToString("g"))
                )
                {
                    removeOrphan = true;

                    // shouldn't have a pending history scan and a parent with complete status - indicates manager bug - critical
                    if (parentScan.Saltminer.Internal.QueueStatus == QueueScan.QueueScanStatus.Complete.ToString("g"))
                    {
                        Logger.LogCritical("Scan {ScanId} has a status of 'complete' with a history scan of 'pending'. This is a critical bug in Manager.", parentScan.Id);
                    }
                }

                if (removeOrphan)
                {
                    count++;
                    Logger.LogInformation("Pending history scan {ScanId} will be removed.", historyScan.Id);
                    if (!RunConfig.ListOnly)
                    {
                        DataClient.QueueScanDeleteAll(historyScan.Id);
                    }
                }
            }

            DataClient.RefreshIndex(QueueScan.GenerateIndex());

            if (Config.CleanupProcessorBatchDelayMs > 0)
            {
                Thread.Sleep(Config.CleanupProcessorBatchDelayMs);
            }

            pendingHistoryScans = DataClient.QueueScanSearch(queueScanRequest).Data;

        } while (pendingHistoryScans.Any());

        Logger.LogInformation("{count} pending history scans removed", count);
    }

    private void CleanUpScansByStatus(QueueScan.QueueScanStatus queueStatus)
    {
        var queueScanRequest = new SearchRequest()
        {
            Filter = new()
            {
                FilterMatches = new Dictionary<string, string>()
                {
                    { "Saltminer.Internal.QueueStatus", queueStatus.ToString("g")},
                    { "Saltminer.Scan.ScanDate", SaltMiner.DataClient.Helpers.BuildLessThanFilterValue($"{DateTime.Now.AddDays(-1 * Config.CleanupQueueAfterDays).ToString("yyyy-MM-dd")}") }
                },
                AnyMatch = false
            },
            PitPagingInfo = new PitPagingInfo(Config.CleanupProcessorBatchSize, false)
        };
        if (!string.IsNullOrEmpty(RunConfig.SourceType))
        {
            queueScanRequest.Filter.FilterMatches.Add("Saltminer.Scan.SourceType", RunConfig.SourceType);
        }

        var count = 0;
        IEnumerable<QueueScan> outdatedQueueScans = DataClient.QueueScanSearch(queueScanRequest).Data;
        do
        {
            if (!outdatedQueueScans.Any())
            {
                break;
            }

            foreach (var queueScan in outdatedQueueScans)
            {
                CheckCancel();
                if (!RunConfig.ListOnly)
                {
                    DataClient.QueueScanDeleteAll(queueScan.Id);
                }
            }

            count += Config.CleanupProcessorBatchSize;
            
            Logger.LogInformation("{count} queue(s) with status '{status}' removed", count, queueStatus.ToString("g"));
            DataClient.RefreshIndex(QueueScan.GenerateIndex());
            
            if (Config.CleanupProcessorBatchDelayMs > 0)
            {
                Thread.Sleep(Config.CleanupProcessorBatchDelayMs);
            }

            outdatedQueueScans = DataClient.QueueScanSearch(queueScanRequest).Data;

        } while (outdatedQueueScans.Any());
    }

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
