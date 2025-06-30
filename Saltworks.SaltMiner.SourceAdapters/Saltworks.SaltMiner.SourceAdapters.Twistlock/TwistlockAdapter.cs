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

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.SaltMiner.SourceAdapters.Core.Helpers;
using Saltworks.SaltMiner.SourceAdapters.Core.Interfaces;
using Saltworks.Utility.ApiHelper;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.SourceAdapters.Twistlock
{
    public class TwistlockAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private TwistlockConfig Config;
        private readonly string AssetType = "ctr";
        private readonly ProgressHelper ProgressHelper;
        private readonly Dictionary<string, List<ScanDto>> HistoryScans;
        private readonly List<ScanDto> CurrentScans;

        public TwistlockAdapter(DataClientFactory<DataClient.DataClient> dataFactory, ApiClientFactory<SourceAdapter> clientFactory, IServiceProvider provider, ILogger<TwistlockAdapter> logger) : base(dataFactory, clientFactory, provider, logger)
        {
            Logger.LogDebug("TwistlockAdapter Initialization complete.");
            ProgressHelper = new ProgressHelper(Logger);
            HistoryScans = new Dictionary<string, List<ScanDto>>();
            CurrentScans = new List<ScanDto>();
        }

        private void StartTimer(string source, string sourceType, string assetType, string sourceId, string action)
        {
            try
            {
                ProgressHelper.StartTimer(GetProgressKey(source, sourceType, assetType, sourceId, action));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Progress helper timer error");
            }
        }

        private void CompleteTimer(string source, string sourceType, string assetType, string sourceId, string action, int count = 0, string message = null)
        {
            try
            {
                ProgressHelper.CompleteTimer(GetProgressKey(source, sourceType, assetType, sourceId, action), count, message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Progress helper timer error");
            }
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {

            try
            {
                #region Include: Get Config and Validate

                config = config ?? throw new ArgumentNullException(nameof(config));

                if (config is not TwistlockConfig)
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(TwistlockConfig)}', but got '{config.GetType().Name}'");
                }

                Config = config as TwistlockConfig;
                CancelToken = token;
                Config.Validate();

                #endregion

                //Write
                StartTimer(Config.Instance, Config.SourceType, AssetType, null, "RunAsync");

                //Include: Check local metrics to make sure there is data, if not run FirstLoad
                FirstLoadSyncUpdate(config);

                //Include
                SetApiClientSslVerification(Config.VerifySsl);

                //Write
                var client = new TwistlockClient(ApiClient, Config, Logger);

                //Write: Possiblilty that custom assembly has custom code
                if (Config.HasCustomAssembly)
                {
                    CustomAssembly = AssemblyHelper.LoadClassAssembly<ISourceAdapterCustom>(Config.CustomAssemblyName, Config.CustomAssemblyType);
                    CustomAssembly.PreProcess();
                }

                //Include: This will allow SendAsync to keep running until your done Syncing records
                StillLoading = true;

                //Include: This will run sync/send synchrononusly until both finish
                await Task.WhenAll(SyncAsync(client), SendAsync(ProgressHelper, Config, AssetType));

                //Include: This allows us to track the failure on trying to load any queuescan and reset to load agin until a configureable failure count is hit
                ResetFailures(Config);

                //Inlcude: This deletes any queuescans that hit that configurable failure count
                DeleteFailures(Config);

                //Include: This is needed to give the app a moment to finish before finishing
                await Task.Delay(5, CancellationToken.None);

                //Write: End Timer for overall Run
                CompleteTimer(Config.Instance, Config.SourceType, AssetType, null, "RunAsync", 1);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex.InnerException?.Message ?? ex.Message, ex);
                throw;
            }
        }

        private async Task GetCiDataSource(string source, TwistlockClient client, int offSet, int limit)
        {
            var scans = await client.GetCiScansAsync(offSet, limit);
            var filteredScans = scans.Where(x => x.EntityInfo.RepoTag != null).ToList();
            var scansRemoved = (scans.Count - filteredScans.Count);

            var groupedScans = filteredScans.GroupBy(x => new { x.EntityInfo.RepoTag.Repo, x.EntityInfo.RepoTag.Tag }).ToList();
            Logger.LogInformation("Received {count} {source} scans", groupedScans.Count, source);
            if (scansRemoved > 0)
            {
                Logger.LogWarning("{scansSkipped} {source} scan(s) will not be processed due to missing repo information", scansRemoved, source);
            }

            foreach (var group in groupedScans)
            {
                var first = true;
                var currentScanId = string.Empty;
                var historyScanList = new List<ScanDto>();
                foreach (var item in group.OrderByDescending(x => x.EntityInfo.ScanTime).ThenByDescending(x => x.EntityInfo.CreationTime).ToList())
                {
                    item.DataSource = source;
                    item.SourceId = $"{item.EntityInfo.RepoTag.Repo}:{item.EntityInfo.RepoTag.Tag}:{source.ToUpper()}";

                    if (first)
                    {
                        CurrentScans.Add(item);
                        currentScanId = item.SourceId;
                        first = false;
                    }
                    else
                    {
                        historyScanList.Add(item);
                    }
                }
                HistoryScans.Add(currentScanId, historyScanList);
            }
        }

        private async Task GetRegistryDataSource(string source, TwistlockClient client, int offSet, int limit)
        {
            var scans = await client.GetRegistryScansAsync(offSet, limit);
            var filteredScans = scans.Where(x => x.EntityInfo.RepoTag != null).ToList();
            var scansRemoved = (scans.Count - filteredScans.Count);

            var groupedScans = filteredScans.GroupBy(x => new { x.EntityInfo.RepoTag.Registry, x.EntityInfo.RepoTag.Repo, x.EntityInfo.RepoTag.Tag }).ToList();
            Logger.LogInformation("Received {count} {source} scans", groupedScans.Count, source);
            if (scansRemoved > 0)
            {
                Logger.LogWarning("{scansSkipped} {source} scan(s) will not be processed due to missing repo information", scansRemoved, source);
            }

            foreach (var group in groupedScans)
            {
                var first = true;
                var currentScanId = string.Empty;
                var historyScanList = new List<ScanDto>();
                foreach (var item in group.OrderByDescending(x => x.EntityInfo.ScanTime).ThenByDescending(x => x.EntityInfo.CreationTime).ToList())
                {
                    item.DataSource = source;
                    item.SourceId = $"{item.EntityInfo.RepoTag.Registry}:{item.EntityInfo.RepoTag.Repo}:{item.EntityInfo.RepoTag.Tag}:{source.ToUpper()}";

                    if (first)
                    {
                        CurrentScans.Add(item);
                        currentScanId = item.SourceId;
                        first = false;
                    }
                    else
                    {
                        historyScanList.Add(item);
                    }
                }
                HistoryScans.Add(currentScanId, historyScanList);
            }
        }

        private async Task GetDeployedDataSource(string source, TwistlockClient client, int offSet, int limit)
        {
            var scans = await client.GetDeployedScansAsync(offSet, limit);
            var filteredScans = scans.Where(x => x.EntityInfo.RepoTag != null).ToList();
            var scansRemoved = (scans.Count - filteredScans.Count);

            var groupedScans = filteredScans.GroupBy(x => new { x.EntityInfo.RepoTag.Registry, x.EntityInfo.RepoTag.Repo, x.EntityInfo.RepoTag.Tag }).ToList();
            Logger.LogInformation("Received {count} {source} scans", groupedScans.Count, source);
            if (scansRemoved > 0)
            {
                Logger.LogWarning("{scansSkipped} {source} scan(s) will not be processed due to missing repo information", scansRemoved, source);
            }

            foreach (var group in groupedScans)
            {
                if (string.IsNullOrEmpty(group.Key.Registry) && string.IsNullOrEmpty(group.Key.Repo) && string.IsNullOrEmpty(group.Key.Tag))
                {
                    Logger.LogWarning("{count} {source} scan(s) will not be processed due to missing key information (registry: '{registry}', repo: '{repo}', tag: '{tag}'", 
                        group.Count(), source, group.Key.Registry, group.Key.Repo, group.Key.Tag);
                    continue;
                }

                var first = true;
                var currentScanId = string.Empty;
                var historyScanList = new List<ScanDto>();
                foreach (var item in group.OrderByDescending(x => x.EntityInfo.ScanTime).ThenByDescending(x => x.EntityInfo.CreationTime).ToList())
                {
                    item.DataSource = source;
                    item.SourceId = $"{item.EntityInfo.RepoTag.Registry}:{item.EntityInfo.RepoTag.Repo}:{item.EntityInfo.RepoTag.Tag}:{source.ToUpper()}";

                    if (first)
                    {
                        CurrentScans.Add(item);
                        currentScanId = item.SourceId;
                        first = false;
                    }
                    else
                    {
                        historyScanList.Add(item);
                    }
                }
                HistoryScans.Add(currentScanId, historyScanList);
            }
        }

        private async Task SyncAsync(TwistlockClient client)
        {
            CheckCancel();

            if (Config.SourceType != SourceType.Twistlock.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{etype}' but was found to be '{atype}'", SourceType.Twistlock.GetDescription(), Config.SourceType);
                throw new TwistlockValidationException("Invalid configuration - source type");
            }

            // Write: Start Timer for Sync
            StartTimer(Config.Instance, Config.SourceType, AssetType, "", "SyncAsync");

            // Include: Clear any leftover queue data from previous run
            // If adding recovery, then only run this if NOT in recovery mode
            ClearQueues();

            // Write: Gather SourceMetrics from client
            var exceptionCounter = 0;
            int offSet = 0;
            // TL API data return limit is 50, so make sure it is 50 or less
            int limit = Config.DataSourceReturnLimit > 50 ? 50 : Config.DataSourceReturnLimit;

            foreach (var source in Config.DataSources)
            {
                var errorCount = 0;
                var retryCount = Config.DataApiRetryCount;

                while (errorCount <= retryCount)
                {
                    try
                    {
                        Logger.LogInformation("Getting {source} Scans...", source);

                        if (source.ToUpper() == "CI")
                        {
                            await GetCiDataSource(source, client, offSet, limit);
                        }

                        if (source.ToUpper() == "REGISTRIES")
                        {
                            await GetRegistryDataSource(source, client, offSet, limit);
                        }

                        if (source.ToUpper() == "DEPLOYED")
                        {
                            await GetDeployedDataSource(source, client, offSet, limit);
                        }

                        break;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;

                        if (errorCount > retryCount)
                        {
                            var msg = $"Exceeded maximum of ({retryCount}) allowed API retry errors.";

                            Logger.LogWarning("{msg}", msg);

                            errorCount = 0;
                            break;
                        }

                        Logger.LogError(ex, "API source '{source}' failed to process: [{exType}] {exMsg}. {errorCount} of {RetryCount} attempts", source, ex.GetType().Name, ex.Message, errorCount, retryCount);
                    }
                }
            }

            var sourceMetrics = new List<SourceMetric>();
            Logger.LogInformation($"Getting SourceMetrics...");
            var scanSourceMetrics = GetSourceMetrics(CurrentScans).Results.ToList();
            sourceMetrics.AddRange(scanSourceMetrics);
            Logger.LogInformation("{total} total source metrics to process", sourceMetrics.Count);

            // Write: Gather SourceMetrics from LocalData
            var localMetrics = LocalData.GetSourceMetrics(Config.Instance, Config.SourceType).ToList();
            var newLocalIssues = 0;
            var newLocalScans = 0;
            var newLocalAssets = 0;

            //Include: Check for existing Run that did not finish
            SyncRecord = LocalData.CheckSyncRecordSourceForFailure(Config.Instance, Config.SourceType);

            //Include: Set recoverymode based on whether not there is a prior syncrecod
            if (SyncRecord != null)
            {
                RecoveryMode = true;
            }
            else
            {
                //Include: If not create a new record for this run
                RecoveryMode = false;
                SyncRecord = LocalData.GetSyncRecord(Config.Instance, Config.SourceType);
            }

            var count = 0;
            foreach (var metric in sourceMetrics)
            {
                count++;
                if (count % 100 == 0)
                {
                    Logger.LogInformation("Processed {count} / {total} source metrics for source '{source}'", count, sourceMetrics.Count, Config.Instance);
                }

                try
                {
                    //Write: Start time for individual sourcemetric
                    StartTimer(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync");  // source ID O1

                    //If Recoverymode loop through until you are on that sourcemetric
                    if (RecoveryMode)
                    {
                        if (SyncRecord.CurrentSourceId != metric.SourceId)
                        {
                            //Write: End Timer for each skipped metric until recovery record hit
                            CompleteTimer(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync", 0, "Skipped due to recovery");
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
                    LocalData.AddUpdate(SyncRecord);

                    //Include: Get matching local metric to current metric
                    var localMetric = localMetrics.FirstOrDefault(x => x.SourceId == metric.SourceId);
                    if (localMetric != null)
                    {
                        //Include: If found set isProcessed to true for tracking and retiring records
                        localMetric.IsProcessed = true;
                    }
                    var scan = CurrentScans.First(x => x.SourceId == metric.SourceId);

                    if (Config.FullSyncMaintEnabled)
                    {
                        FullSyncBatchProcess(SyncRecord, Config.FullSyncBatchSize);
                        // End of metrics and full sync processing is still true,
                        // means last batch did not meet batch size... reset source ID to cycle through again
                        // End of metrics and batch count is zero,
                        // means nothing was found to process (ie. maybe a source metric doesn't exist anymore)...reset source ID
                        if (count == sourceMetrics.Count && (FullSyncProcessing || FullSyncBatchCount == 0))
                        {
                            SyncRecord.FullSyncSourceId = "";
                        }
                    }

                    //Include:  Get all data needed to determine if local metric and source metric match
                    if (NeedsUpdate(metric, localMetric, Config.LogNeedsUpdate, new() { NeedsUpdateEnum.Attributes }) || RecoveryMode)
                    {
                        Logger.LogInformation("[Sync] {current}/{total} Updating '{config}', DataSource '{datasource}', SourceId '{sourceId}'", count, sourceMetrics.Count, Config.Instance, scan.DataSource, metric.SourceId);

                        //If source has a master list of assets, then we can check if there is a new scan for each asset
                        //in this case if there is no scan for this asset, then just map a asset
                        if (metric.LastScan != null)
                        {
                            //Write: Map scan and add to localData, including if there are reports that are from the last scan
                            QueueScan queueScan = MapScan(scan);
                            newLocalScans++;

                            //Write: Map asset and add to localData, including data from the scan
                            QueueAsset queueAsset = MapAsset(scan, queueScan);
                            newLocalAssets++;

                            if (queueScan.Entity.Saltminer.Internal.QueueStatus == QueueScanStatus.Cancel.ToString())
                            {
                                continue;
                            }

                            MapIssues(scan, queueScan, queueAsset);

                            CheckCancel(false);

                            //Include: Update Scan with loading = false in localData
                            queueScan.Loading = false;
                            LocalData.AddUpdate(queueScan);

                            UpdateLocalMetric(metric, localMetric);

                            RecoveryMode = false;

                            newLocalIssues += queueScan.Entity.Saltminer.Internal.IssueCount;
                            //Write: End time for individual sourcemetric
                            CompleteTimer(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync", queueScan.Entity.Saltminer.Internal.IssueCount, null);
                        }
                        else
                        {
                            Logger.LogInformation("Source ID '{id}' has no scans.  Asset will still be created.", metric.SourceId);
                            MapAsset(scan, null);
                            newLocalAssets++;
                            //Write: End time for individual sourcemetric
                            CompleteTimer(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync", 0, null);
                        }

                        //Include: Update Local metric with source metric, so always current
                        UpdateLocalMetric(metric, localMetric);
                        //Include: Set recoverymode to false, for the next iteration
                        RecoveryMode = false;

                    }
                    else
                    {
                        CompleteTimer(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync", 0, "Skipped due to not needing update");
                    }
                }
                catch (LocalDataException ex)
                {
                    Logger.LogCritical(ex.InnerException?.Message ?? ex.Message, ex);

                    StillLoading = false;

                    throw;
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Not Found")
                    {
                        Logger.LogWarning(ex, $"[Sync] {Config.Instance} Sync Processing Error: {ex.InnerException?.Message ?? ex.Message}");
                    }
                    else
                    {
                        exceptionCounter++;

                        Logger.LogWarning(ex, $"[Sync] {Config.Instance} Sync Processing Error {exceptionCounter}: {ex.InnerException?.Message ?? ex.Message}");

                        if (exceptionCounter == Config.SourceAbortErrorCount)
                        {
                            Logger.LogCritical(ex, $"[Sync] {Config.Instance} Exceeded {Config.SourceAbortErrorCount} Sync Processing Errors: {ex.InnerException?.Message ?? ex.Message}");

                            StillLoading = false;

                            break;
                        }
                    }
                }

                CheckCancel();
            }


            // Set this to indicate syncing is complete
            StillLoading = false;
            if (!Config.DisableRetire)
            {
                try
                {
                    RetireLocalMetrics(localMetrics);
                    RetireQueueAssets(localMetrics, AssetType, Config);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "[Sync] Error occurred when processing retirees, see log for details.");
                }
            }
            else
            {
                Logger.LogInformation("[Sync] Asset retirement processing disabled by configuration, skipping.");
            }

            // Indicate source metric is complete
            SyncRecord.LastSync = (DateTime.UtcNow);
            SyncRecord.CurrentSourceId = null;
            SyncRecord.State = SyncState.Completed;
            LocalData.AddUpdate(SyncRecord);

            //Write: End Timer for entire sync process
            CompleteTimer(Config.Instance, Config.SourceType, AssetType, "", "SyncAsync", 0, $"Local QueueScans: {newLocalScans}; QueueAssets: {newLocalAssets}; QueueIssues: {newLocalIssues}");
            Logger.LogInformation("[Sync] Twistlock sync complete with {errcount} error(s).", exceptionCounter);
        }

        private QueueScan MapHistoryScan(ScanDto scan)
        {
            var now = DateTime.UtcNow;
            var queueScan = new QueueScan
            {
                Loading = false,
                Entity = new()
                {
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueScanInfo
                    {
                        Scan = new SaltMiner.Core.Entities.QueueScanInfo
                        {
                            AssessmentType = AssessmentType.Container.ToString("g"),
                            Product = "Twistlock",
                            ReportId = scan.Id,
                            ScanDate = scan.EntityInfo.ScanTime.ToUniversalTime(),
                            ProductType = "Container",
                            Vendor = "Palo Alto Networks",
                            AssetType = AssetType,
                            IsSaltminerSource = TwistlockConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = scan.EntityInfo?.VulnerabilitiesCount ?? 0,
                            QueueStatus = QueueScanStatus.Loading.ToString("g"),
                        },
                    },
                    Timestamp = now
                },
                Timestamp = now
            };
            return queueScan;
        }


        private QueueScan MapScan(ScanDto scan)
        {
            StartTimer(Config.Instance, Config.SourceType, AssetType, scan.SourceId, "MapScan");

            var scanHistoryList = new List<ScanDto>();
            if (HistoryScans.ContainsKey(scan.SourceId))
            {
                scanHistoryList = HistoryScans[scan.SourceId];
            }

            var now = DateTime.UtcNow;
            var queueScan = new QueueScan
            {
                History = scanHistoryList.Select(x => MapHistoryScan(x)).ToList(),
                Loading = true,
                Timestamp = now,
                Entity = new()
                {
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueScanInfo
                    {
                        Scan = new SaltMiner.Core.Entities.QueueScanInfo
                        {
                            AssessmentType = AssessmentType.Container.ToString("g"),
                            Product = "Twistlock",
                            ReportId = scan.Id,
                            ScanDate = scan.EntityInfo.ScanTime.ToUniversalTime(),
                            ProductType = "Container",
                            Vendor = "Palo Alto Networks",
                            AssetType = AssetType,
                            IsSaltminerSource = TwistlockConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = scan.EntityInfo?.VulnerabilitiesCount ?? 0,
                            QueueStatus = QueueScanStatus.Loading.ToString("g"),
                        },
                    },
                    Timestamp = now
                }
            };

            if (CustomAssembly != null)
            {
                CustomAssembly.CustomizeQueueScan<ScanDto>(queueScan, scan);
                if (CustomAssembly.CancelScan)
                {
                    queueScan.Entity.Saltminer.Internal.QueueStatus = QueueScanStatus.Cancel.ToString();
                    LocalData.DeleteQueueScan(queueScan.LocalId);
                    CompleteTimer(Config.Instance, Config.SourceType, AssetType, scan.SourceId, "MapScan", 0, "CustomAssembly requested to cancel the scan");
                    return queueScan;
                }
            }

            CompleteTimer(Config.Instance, Config.SourceType, AssetType, scan.SourceId, "MapScan", 1);
            return LocalData.AddUpdate(queueScan);
        }

        private QueueAsset MapAsset(ScanDto scan, QueueScan queueScan, bool isRetired = false)
        {
            var sourceId = scan.SourceId;
            var imageName = $"{scan.EntityInfo.RepoTag.Repo}:{scan.EntityInfo.RepoTag.Tag}";

            StartTimer(Config.Instance, Config.SourceType, AssetType, sourceId, "MapAsset");
            var attributes = new Dictionary<string, string>
            {
                { "ImageType", scan.DataSource },
                { "Registry", scan.EntityInfo.RepoTag.Registry },
                { "Image", imageName },
                { "Id", scan.EntityInfo.Id },
                { "OsDistribution", scan.EntityInfo.Distro },
                { "OsDistroRelease", scan.EntityInfo.OsDistroRelease },
                { "Digest", scan.EntityInfo.RepoDigests.Count > 0 ? scan.EntityInfo.RepoDigests[0] : string.Empty },
                { "Tags", scan.EntityInfo.Tags.Count > 0 ? scan.EntityInfo.Tags[0].Tag : string.Empty },
                { "Scanner", scan.EntityInfo.HostName }
            };

            //Write Mapper for QueueAsset
            var now = DateTime.UtcNow;
            var queueAsset = new QueueAsset
            {
                LocalScanId = queueScan?.LocalId,
                Entity = new()
                {
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueAssetInfo
                    {
                        Asset = new SaltMiner.Core.Entities.AssetInfoPolicy
                        {
                            Description = imageName,
                            Name = scan.EntityInfo.RepoTag.Repo,
                            Version = scan.EntityInfo.RepoTag.Tag,
                            Instance = Config.Instance,
                            IsProduction = true,
                            IsSaltminerSource = TwistlockConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = sourceId,
                            AssetType = AssetType,
                            LastScanDaysPolicy = Config.LastScanDaysPolicy,
                            Attributes = attributes,
                            IsRetired = isRetired
                        }
                    },
                    Timestamp = now
                }
            };

            var result = LocalData.AddUpdate(queueAsset); 
            CompleteTimer(Config.Instance, Config.SourceType, AssetType, sourceId, "MapAsset", 1);
            return result;
        }

        private void MapIssues(ScanDto scan, QueueScan queueScan, QueueAsset queueAsset)
        {
            var sourceId = scan.SourceId;
            StartTimer(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues");
            List<QueueIssue> queueIssues = new();

            if (queueScan.Entity.Saltminer.Internal.IssueCount == 0 && scan.EntityInfo.ComplianceIssuesCount == 0)
            {
                //Mapper for empty issue should only need these fields actually filled and the rest defaulted
                queueIssues.Add(new QueueIssue
                {
                    LocalScanId = queueScan.LocalId,
                    LocalAssetId = queueAsset.LocalId,
                    Entity = new()
                    {
                        Labels = new Dictionary<string, string>(),
                        Vulnerability = new SaltMiner.Core.Entities.VulnerabilityInfo
                        {
                            Id = Guid.NewGuid().ToString(),
                            Audit = new SaltMiner.Core.Entities.AuditInfo
                            {
                                Audited = true,
                            },
                            Category = new string[1] { "Container" },
                            Description = "keyword",
                            Name = "ZeroIssue",
                            Location = "N/A",
                            LocationFull = "N/A",
                            Scanner = new SaltMiner.Core.Entities.ScannerInfo
                            {
                                AssessmentType = AssessmentType.Container.ToString("g"),
                                Product = "Twistlock",
                                Vendor = "Palo Alto Networks",
                                Id = GetZeroScannerId(Config.SourceType, sourceId)
                            },
                            Severity = Severity.Zero.ToString("g"),
                            FoundDate = DateTime.UtcNow,
                            ReportId = queueScan.Entity.Saltminer.Scan.ReportId
                        },
                        Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                        {
                            Attributes = new Dictionary<string, string>(),
                            QueueScanId = queueScan.Id,
                            QueueAssetId = queueAsset.Id,
                            Source = new SaltMiner.Core.Entities.SourceInfo
                            {
                                Analyzer = "Twistlock",
                            }
                        },
                        Tags = Array.Empty<string>(),
                        Timestamp = DateTime.UtcNow
                    }
                });
            }
            else
            {
                if (scan.EntityInfo.Vulnerabilities != null)
                {
                    foreach (var vul in scan.EntityInfo.Vulnerabilities)
                    {
                        queueIssues.Add(MapIssue(queueScan, queueAsset, vul));
                    }
                }

                if (scan.EntityInfo.ComplianceIssues != null)
                {
                    foreach (var cis in scan.EntityInfo.ComplianceIssues)
                    {
                        queueIssues.Add(MapIssue(queueScan, queueAsset, cis, true));
                    }
                }
            }


            foreach (var queueIssue in queueIssues)
            {
                if (CustomAssembly != null)
                    CustomAssembly.CustomizeQueueIssue(queueIssue, scan);
                LocalData.AddUpdate(queueIssue);
            }

            CompleteTimer(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues", queueIssues.Count);
        }

        public SourceClientResult<SourceMetric> GetSourceMetrics(List<ScanDto> scans)
        {
            var metrics = new List<SourceMetric>();

            foreach (var scan in scans)
            {
                var attributes = new Dictionary<string, string>
                {
                    { "DataSource", scan.DataSource }
                };

                metrics.Add(new SourceMetric
                {
                    LastScan = scan.EntityInfo.ScanTime.ToUniversalTime(),
                    IssueCount = scan.EntityInfo.VulnerabilitiesCount,
                    IssueCountSev1 = scan.EntityInfo.VulnerabilityDistribution.High,
                    IssueCountSev2 = scan.EntityInfo.VulnerabilityDistribution.Medium,
                    IssueCountSev3 = scan.EntityInfo.VulnerabilityDistribution.Low,
                    Instance = Config.Instance,
                    IsSaltminerSource = TwistlockConfig.IsSaltminerSource,
                    SourceType = Config.SourceType,
                    SourceId = scan.SourceId,
                    Attributes = attributes
                });
            }

            return new SourceClientResult<SourceMetric>() { Results = metrics };
        }

        private static string CreateUniqueIssueId(ComplianceDto vul)
        {
            if (!string.IsNullOrEmpty(vul.Cve))
            {
                return $"{vul.Id}~{vul.Cve}~{vul.PackageName}~{vul.PackageVersion}~{vul.FixDate}";
            }
            else
            {
                return $"{vul.Id}~NoCveValue~{vul.PackageName}~{vul.PackageVersion}~{vul.FixDate}";
            }
        }

        private static string IssueName(ComplianceDto vul)
        {
            if (vul.Cve != string.Empty)
            {
                return $"{vul.Cve}";
            }
            else
            {
                return $"{vul.Title}";
            }
        }

        private QueueIssue MapIssue(QueueScan queueScan, QueueAsset queueAsset, ComplianceDto vul, bool isComplianceIssue = false)
        {
            if (vul == null)
            {
                Logger.LogError("An issue was missing information for report ID '{reportID}' and source ID '{sourceID}'.", queueScan.Entity.Saltminer.Scan.ReportId, queueAsset.Entity.Saltminer.Asset.SourceId);
                throw new ArgumentNullException(nameof(vul));
            }
            return new QueueIssue
            {
                LocalScanId = queueScan.LocalId,
                LocalAssetId = queueAsset.LocalId,
                Entity = new()
                {
                    Labels = new Dictionary<string, string>(),
                    Vulnerability = new SaltMiner.Core.Entities.VulnerabilityInfo
                    {
                        Audit = new SaltMiner.Core.Entities.AuditInfo
                        {
                            Audited = true,
                        },
                        Category = new string[1] { "Container" },
                        Description = vul.Description,
                        FoundDate = vul.Discovered.ToUniversalTime(),
                        Id = vul.Cve,
                        Classification = vul.Cvss.ToString(),
                        Enumeration = vul.Cve,
                        Name = IssueName(vul),
                        Location = "N/A",
                        LocationFull = "N/A",
                        ReportId = CreateUniqueIssueId(vul),
                        Scanner = new SaltMiner.Core.Entities.ScannerInfo
                        {
                            Id = CreateUniqueIssueId(vul),
                            AssessmentType = AssessmentType.Container.ToString("g"),
                            Product = "Twistlock",
                            Vendor = "Palo Alto Networks"
                        },
                        Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, isComplianceIssue ? "" : vul.Severity ?? ""),
                        SourceSeverity = vul.Severity ?? ""
                    },
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                    {
                        Attributes = new Dictionary<string, string>(),
                        QueueScanId = queueScan.Id,
                        QueueAssetId = queueAsset.Id,
                        Source = new SaltMiner.Core.Entities.SourceInfo
                        {
                            Analyzer = Config.Instance,
                        }
                    },
                    Tags = Array.Empty<string>(),
                    Timestamp = DateTime.UtcNow
                }
            };
        }
    }
}



