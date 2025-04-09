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
using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.SaltMiner.SourceAdapters.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.SourceAdapters.Template
{
    public class TemplateAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private TemplateConfig Config;
        private TemplateClient Client;
        private readonly string AssetType = "app";

        public TemplateAdapter(IServiceProvider provider, ILogger<TemplateAdapter> logger) : base(provider, logger)
        {
            Logger.LogDebug("Adapter initialization complete.");
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            await base.RunAsync(config, token);
            try
            {
                // Include (modify config class name)
                config = config ?? throw new ArgumentNullException(nameof(config));
                if (config is not TemplateConfig)
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(TemplateConfig)}', but got '{config.GetType().Name}'");
                Config = config as TemplateConfig;
                Config.Validate();

                //Include: Check local metrics to make sure there is data, if not run FirstLoad
                FirstLoadSyncUpdate(config);
                
                //Include
                SetApiClientSslVerification(Config.VerifySsl);

                //Write
                Client = new TemplateClient(ApiClient, Config, Logger);

                CancelToken = token;

                //Write: Possiblilty that custom assembly has custom code
                if (Config.HasCustomAssembly)
                {
                    CustomAssembly = AssemblyHelper.LoadClassAssembly<ISourceAdapterCustom>(Config.CustomAssemblyName, Config.CustomAssemblyType);
                    CustomAssembly.PreProcess();
                }

                //Include: This will allow SendAsync to keep running until your done Syncing records
                StillLoading = true;

                //Include: This will run sync/send synchrononusly until both finish
                await Task.WhenAll(SyncAsync(), SendAsync(Config, AssetType));

                //Include: This allows us to track the failure on trying to load any queuescan and reset to load agin until a configureable failure count is hit
                ResetFailures(Config);

                //Inlcude: This deletes any queuescans that hit that configurable failure count
                DeleteFailures(Config);

                //Include: This is needed to give the app a moment to finish before finishing
                await Task.Delay(5, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Source adapter unexpected error: [{Type}] {Msg}", ex.GetType().Name, ex.InnerException?.Message ?? ex.Message);
                throw new TemplateException($"Source adapter unexpected error: [{ex.GetType().Name}] {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // Recommended: use a generator to get batches of metrics so only a batch is in memory at any given time.
        private async IAsyncEnumerable<SourceMetric> GetAsync()
        {
            // Write: Modify this generator to use the client class to get a list of source metrics to run through
            var loopCount = 0;
            var count = 0;
            while (loopCount < 3) // Real adapter would use something like results != null instead of a fixed loop
            {
                // Get one batch at a time, then yield one of each batch at a time until batch is all used up, then get next batch.
                foreach (var metric in await Client.GetSourceMetricsAsync())
                {
                    if (count % 50 == 0)
                    {
                        // Every so often write updates to SyncRecord so can resume if previous run dies
                        SyncRecord.CurrentSourceId = metric.SourceId;
                    }
                    yield return metric;
                }
                loopCount++;
            }
            // Full sync maintenance - refresh periodically even if not needed based on last scan, counts, etc.
            if (Config.FullSyncMaintEnabled)
            {
                // This code is more murky, leave this until last and possibly plan to do some research to get it done.
                FullSyncBatchProcess(SyncRecord, Config.FullSyncBatchSize);
                // End of metrics and full sync processing is true, then get the generator to start from the beginning and run some more.
                // Also do some kind of sync record updates to keep track of where we are in full maint updates
                if (FullSyncProcessing || FullSyncBatchCount == 0)
                {
                    SyncRecord.FullSyncSourceId = "";
                }
            }
        }

        private async Task SyncAsync()
        {
            CheckCancel();

            // Write: Change Sonatype source type checking to your own.
            if (Config.SourceType != SourceType.Sonatype.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{Etype}' but was found to be '{Atype}'", SourceType.Sonatype.GetDescription(), Config.SourceType);
                // Throw configuration exception when issue deals with config
                throw new TemplateConfigurationException("Invalid configuration - source type");
            }

            var exceptionCounter = 0;

            // Include: Check for existing Run that did not finish
            SyncRecord = LocalData.CheckSyncRecordSourceForFailure(Config.Instance, Config.SourceType);

            // Include: Set recoverymode based on whether not there is a prior syncrecod
            if (SyncRecord != null)
            {
                RecoveryMode = true;
            }
            else
            {
                // Include: If not create a new record for this run
                RecoveryMode = false;
                SyncRecord = LocalData.GetSyncRecord(Config.Instance, Config.SourceType);

                // Include: Clear any leftover queue data from previous run
                ClearQueues();
            }

            // Main loop, get source metrics using generator or in batches, then process each one
            await foreach (var metric in GetAsync())
            {
                try
                {
                    // If Recoverymode loop through until you are on that sourcemetric
                    if (RecoveryMode)
                    {
                        if (SyncRecord.CurrentSourceId != metric.SourceId)
                        {
                            continue;
                        }
                        else
                        {
                            RecoveryMode = false;
                        }
                    }

                    //Include: Update Syncrecord to reflect the metric currently processed
                    SyncRecord.CurrentSourceId = metric.SourceId;
                    SyncRecord.State = SyncState.InProgress;
                    LocalData.AddUpdate(SyncRecord, true); // use true for second paramter to write this update immediately (no queuing)

                    //Include: Get matching local metric to current metric (all metrics are generally called "SourceMetric", which is possibly unfortunate)
                    var localMetric = LocalData.GetSourceMetric(Config.Instance, Config.SourceType, metric.SourceId);
                    if (localMetric != null)
                    {
                        //Include: If found set isProcessed to true for tracking and retiring records
                        localMetric.IsProcessed = true;
                    }

                    //Include:  Get all data needed to determine if local metric and source metric match
                    if (NeedsUpdate(metric, localMetric) || RecoveryMode)
                    {
                        Logger.LogInformation("[Sync] Updating Config.Instance '{Instance}', SourceType {SourceType}, SourceId '{SourceId}', AssetType '{AssetType}'", Config.Instance, Config.SourceType, metric.SourceId, AssetType);

                        // If source has a master list of assets, then we can check if there is a new scan for each asset
                        // in this case if there is no scan for this asset, then map a 'noscan' queueScan and queueAsset,
                        // but skip adding a queueIssue. That will be done when the manager processes
                        var noScan = metric.LastScan == null;

                        //Write: Map scan and add to localData, including if there are reports that are from the last scan. Pass noScan flag if necessary
                        //Example: var queueScan = MapScan(appReport, historyReports, noScan);
                        var queueScan = new QueueScan();
                        //Write: Map asset and add to localData, including data from the scan.
                        //Write: Map issues and add to localData, including data from the scan and asset. Skip if 'noscan'. Manager will create a 'noscan' issue.
                        //if (!noScan)
                            //Example: var queueIssue = MapIssue(?, ?, ?);

                        CheckCancel(false);

                        //Include: Update Scan with loading = false in localData
                        queueScan.Loading = false;
                        LocalData.AddUpdate(queueScan);  // don't use immediate=true here (2nd param), it's best to allow the queueing to happen

                        //Include: Update Local metric with source metric, so always current
                        UpdateLocalMetric(metric, localMetric);
                        //Include: Set recoverymode to false, for the next iteration
                        RecoveryMode = false;

                        // This allows for Sync to wait for Send if it gets too far ahead (performance boost, keeps local db smaller)
                        await LetSendCatchUpAsync(Config);
                       
                    }
                }
                catch (LocalDataException ex)
                {
                    Logger.LogCritical(ex, "[Sync] Local data exception while processing {SrcType}, instance {Instance}: [{Type}] {Msg}", Config.SourceType, Config.Instance, ex.GetType().Name, ex.InnerException?.Message ?? ex.Message);
                    StillLoading = false;
                    throw new TemplateException($"Local data exception while processing {Config.SourceType}, instance {Config.Instance}: [{ex.GetType().Name}] {ex.InnerException?.Message ?? ex.Message}");
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Not Found")
                    {
                        Logger.LogWarning(ex, "[Sync] {Instance} for {SourceType} sync processing error: {Message}", Config.Instance, Config.SourceType, ex.InnerException?.Message ?? ex.Message);
                    }
                    else
                    {
                        exceptionCounter++;
                        Logger.LogWarning(ex, "[Sync] {Instance} for {SourceType} sync processing error {Count}: {Message}", Config.Instance, Config.SourceType, exceptionCounter, ex.InnerException?.Message ?? ex.Message);
                        if (exceptionCounter == Config.SourceAbortErrorCount)
                        {
                            Logger.LogCritical(ex, "[Sync] {Instance} for {SourceType} exceeded {Count} sync processing Errors: {Message}", Config.Instance, Config.SourceType, Config.SourceAbortErrorCount, ex.InnerException?.Message ?? ex.Message);
                            StillLoading = false;
                            break;
                        }
                    }
                }
            }

            CheckCancel();

            // Include: Set this to indicate sync completion
            StillLoading = false;
            if (!Config.DisableRetire)
            {
                try
                {
                    //RetireLocalMetrics(localMetrics);
                    //RetireQueueAssets(localMetrics, AssetType, Config);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error occurred when processing retirees, see log for details.");
                }
            }
            else
            {
                Logger.LogInformation("Asset retirement processing disabled by configuration, skipping.");
            }

            //Include: indicate you are done with this source metric
            SyncRecord.LastSync = (DateTime.UtcNow);
            SyncRecord.CurrentSourceId = null;
            SyncRecord.State = SyncState.Completed;
            LocalData.AddUpdate(SyncRecord);
            LocalData.SaveAllBatches(); // use this to flush all remaining queued database updates after completing sync processing
        }


        // Example of how to create a mapped queue scan
        //private QueueScan MapScan(?? dto, AssessmentType assessmentType, bool noScan = false)
        //{
        //    if (noScan)
        //    {
        //        return GetNoScanScan(assessmentType.ToString("g"), Config.SourceType);
        //    }

        //    var sourceId = ?;
        //    var queueScan = new QueueScan
        //    {
        //        Loading = true,
        //        Index = QueueScan.GenerateIndex(),
        //        Instance = Config.Instance,
        //        IsSaltminerSource = true,
        //        SourceType = Config.SourceType,
        //        Saltminer = new()
        //        {
        //            AssessmentType = assessmentType.ToString("g"),
        //            Product = ?,
        //            ReportId = ?, // CANNOT be just SourceId, that causes duplicates.
        //            ScanDate = ?,
        //            Type = ?,
        //            Vendor = ?,
        //        },
        //        IssueCount = ?,
        //        Timestamp = DateTime.UtcNow,
        //        QueueStatus = SaltMiner.Core.Entities.QueueScan.QueueScanStatus.Loading.ToString("g"),
        //        AssetType = AssetType
        //    };

        //    if (CustomAssembly != null && !noScan)
        //    {
        //        CustomAssembly.CustomizeQueueScan(queueScan, dto);
        //        if (CustomAssembly.CancelScan)
        //        {
        //            queueScan.Entity.Saltminer.Internal.QueueStatus = SaltMiner.Core.Entities.QueueScan.QueueScanStatus.Cancel.ToString();
        //            LocalData.DeleteQueueScan(queueScan.Id);
        //            return null;
        //        }
        //        return queueScan;
        //    }
        //    return LocalData.AddUpdate(queueScan); });
        //}

        //private QueueScan MapHistoryScan(?? appReport)
        //{
        //    //Write Mapper for QueueAsset
        //    var queueScan = new QueueScan();

        //    if (CustomAssembly != null)
        //    {
        //        CustomAssembly.CustomizeQueueScan(queueScan, appReport);
        //        if (CustomAssembly.CancelScan)
        //        {
        //            queueScan.Entity.Saltminer.Internal.QueueStatus = QueueScanStatus.Cancel.ToString();
        //            return queueScan;
        //        }
        //    }

        //    return queueScan;
        //}

        //private QueueAsset MapAsset(?? appReport, QueueScan queueScan, bool isRetired = false)
        //{
        //    var sourceId = ?;

        //    ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapAsset"));

        //    //Write Mapper for QueueAsset
        //    var queueAsset = new QueueAsset();


        //    var result = LocalData.GetAll<QueueAsset>().FirstOrDefault(s => s.Saltminer.Asset.SourceType == Config.SourceType && s.Saltminer.Asset.SourceId == sourceId) ?? LocalData.AddUpdate(queueAsset); });

        //    ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapAsset"), 1);

        //    return result;
        //}

        //private void MapIssues(?? appReport, QueueScan queueScan, QueueAsset queueAsset)
        //{
        //    var sourceId = ??;

        //    ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"));

        //    List<QueueIssue> queueIssues = new();

        //    var localIssues = LocalData.GetIssues(queueScan.LocalId, queueAsset.LocalId); });
        //    // Use GetZeroQueueIssue() for zero records (queue scan found with no issues)


        //    if (queueScan.Entity.Saltminer.Internal.IssueCount == 0)
        //    {    
        //        //Mapper for empty issue should only need these fields actually filled and the rest defaulted
        //        queueIssues.Add(new QueueIssue
        //        {
        //            Vulnerability = new SaltMiner.Core.Entities.VulnerabilityInfo
        //            {
        //                Audit = new SaltMiner.Core.Entities.AuditInfo
        //                {
        //                    Audited = true,
        //                    Auditor = "",
        //                    LastAudit = DateTime.UtcNow
        //                },
        //                Category = new string[1] { ? },
        //                FoundDate = ?, // check for orginal found or null
        //                ReportId = ?,
        //                Scanner = new SaltMiner.Core.Entities.ScannerInfo
        //                {
        //                    AssessmentType = ?,
        //                    Product = ?,
        //                    Vendor = ?
        //                },
        //                Severity = Severity.Zero.ToString("g")
        //            },
        //            Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
        //            {
        //                QueueScanId = queueScan.Id,
        //                QueueAssetId = queueAsset.Id,
        //                Source = new SaltMiner.Core.Entities.SourceInfo
        //                {
        //                    Analyzer = ?,
        //                }
        //            },
        //            Tags = new string[1] { ? },
        //            Timestamp = DateTime.UtcNow
        //        });
        //    }
        //    else
        //    {
        //        foreach (var issue in ??)
        //        {
        //            //If this property is needed, it will need to come from SecurityIssues in raw data from Report API
        //            //if (localIssues.Any(x => x.Id == violation.Reference))
        //            //{
        //            //    continue;
        //            //}
        //
        //            //Write mapper for queue issue
        //            queueIssues.Add(new QueueIssue());;
        //        }
        //    }


        //    foreach (var queueIssue in queueIssues)
        //    {
        //        if (CustomAssembly != null)
        //        {
        //            CustomAssembly.CustomizeQueueIssue(queueIssue, appReport);
        //        }

        //        LocalData.AddUpdate(queueIssue); }); 
        //    }

        //    ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"), queueIssues.Count());
        //}
    }
}



