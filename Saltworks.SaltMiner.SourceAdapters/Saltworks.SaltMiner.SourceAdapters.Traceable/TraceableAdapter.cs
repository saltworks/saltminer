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
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.SaltMiner.SourceAdapters.Core.Helpers;
using Saltworks.SaltMiner.SourceAdapters.Core.Interfaces;
using System.Globalization;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.SourceAdapters.Traceable
{
    public class TraceableAdapter : SourceAdapter
    {
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private TraceableConfig Config;
        private readonly string AssetType = "app";
        private readonly string Vendor = "Traceable";
        private readonly string Product = "Traceable";
        private static readonly string[] item = ["Application"];

        public TraceableAdapter(IServiceProvider provider, ILogger<TraceableAdapter> logger) : base(provider, logger)
        {
            Logger.LogDebug("Traceable adapter initialization complete.");
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            await base.RunAsync(config, token);
            try
            {
                #region Include: Get Config and Validate

                config = config ?? throw new ArgumentNullException(nameof(config));

                if (!(config is TraceableConfig))
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(TraceableConfig)}', but got '{config.GetType().Name}'");
                }

                Config = config as TraceableConfig;

                Config.Validate();

                #endregion

                //Include: Check local metrics to make sure there is data, if not run FirstLoad
                // TODO: enable and test first load
                // FirstLoadSyncUpdate(config);
                
                //Include
                SetApiClientSslVerification(Config.VerifySsl);

                //Write
                var client = new TraceableClient(ApiClient, Config, Logger);

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
                await Task.WhenAll(SyncAsync(client), SendAsync(Config, AssetType));

                //Include: This allows us to track the failure on trying to load any queuescan and reset to load agin until a configureable failure count is hit
                ResetFailures(Config);

                //Inlcude: This deletes any queuescans that hit that configurable failure count
                ResetFailures(Config);

                //Include: This is needed to give the app a moment to finish before finishing
                await Task.Delay(5, CancellationToken.None);
            }
            catch (CancelTokenException ex)
            {
                StillLoading = false;
                Logger.LogInformation(ex, "Cancellation requested, cancelling processing.");

            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Source adapter failure: {Msg}", ex.InnerException?.Message ?? ex.Message);
                throw new TraceableException($"Source adapter failure: {ex.Message}", ex);
            }
        }

        private async Task SyncAsync(TraceableClient client)
        {
            CheckCancel();

            if (Config.SourceType != SourceType.Traceable.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{Etype}' but was found to be '{Atype}'", SourceType.Traceable.GetDescription(), Config.SourceType);
                throw new TraceableValidationException("Invalid configuration - source type");
            }

            try
            {
                Logger.LogInformation($"[Sync] Starting Traceable sync...");

                if (Config.TestingAssetLimit > 0)
                {
                    Logger.LogWarning("TestingAssetLimit of {Amt} is in effect.  Traceable loading will stop at this count.", Config.TestingAssetLimit);
                }

                var exceptionCounter = 0;

                //Include: Check for existing Run that did not finish
                var SyncRecord = LocalData.CheckSyncRecordSourceForFailure(Config.Instance, Config.SourceType);

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

                    //Include: Clear any leftover queue data from previous run
                    ClearQueues();
                }

                var totalAssetCount = 0;
                var totalIssueCount = 0;

                Logger.LogInformation($"[Sync] Begin loading projects data...");
                await foreach (var asset in GetAsync(client))
                {
                    CheckCancel(true);
                    try
                    {
                        // testing limit reached or testing 1 specific asset - break out of all processing
                        if ((Config.TestingAssetLimit > 0 && totalAssetCount >= Config.TestingAssetLimit))
                            break;

                        totalAssetCount++;
                        if (totalAssetCount % 100 == 0)
                            Logger.LogInformation("Total assets processed: {Total}", totalAssetCount);
                        else
                            Logger.LogDebug("Total assets processed: {Total}", totalAssetCount);

                        var lastScanDate = asset.LastScanTimestamp[0..3] == "1970" ? asset.LastCalledTime : asset.LastScanTimestamp;

                        var issueCount = (await client.GetAssetVulnerabilitiesAsync(asset.Id, 0, 0)).Data.VulnerabilitiesV3.Total;

                        SourceMetric metric = client.GetSourceMetric(asset, ConvertStringDate(lastScanDate), issueCount);
                        Logger.LogInformation("Processing source ID '{Id}', asset '{Name}'", metric.SourceId, asset.Name);

                        //If Recoverymode loop through until you are on that sourcemetric
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

                        //Include: Update sync record to reflect the metric currently processed
                        SyncRecord.CurrentSourceId = metric.SourceId;
                        SyncRecord.State = SyncState.InProgress;
                        LocalData.AddUpdate(SyncRecord, true);

                        //Include: Get matching local metric to current metric
                        var localMetric = LocalData.GetSourceMetric(Config.Instance, Config.SourceType, metric.SourceId);
                        if (localMetric != null)
                        {
                            //Include: If found set isProcessed to true for tracking and retiring records
                            localMetric.IsProcessed = true;
                        }

                        //Get all data needed to determine if local metric and source metric match
                        if (NeedsUpdate(metric, localMetric, [NeedsUpdateEnum.IsNotScanned, NeedsUpdateEnum.Attributes]) || RecoveryMode)
                        {
                            Logger.LogInformation("[Sync] Updating Source ID '{SourceId}' with name '{Name}' for instance '{Instance}'", metric.SourceId, asset.Name, Config.Instance);

                            // If source has a master list of assets, then we can check if there is a new scan for each asset
                            // in this case if there is no scan for this asset, then map a 'noscan' queueScan and queueAsset,
                            // but skip adding a queueIssue. That will be done when the manager processes
                            var noScan = metric.LastScan == null;
                            var queueScan = MapScan(asset, noScan);
                            var queueAsset = MapAsset(asset, queueScan);

                            if (queueScan.Entity.Saltminer.Internal.QueueStatus == QueueScanStatus.Cancel.ToString())
                            {
                                Logger.LogDebug("[Sync] Cancelling update, mapping set cancel. Source ID '{SourceId}' with name '{Name}' for instance '{Instance}'", metric.SourceId, asset.Name, Config.Instance);
                                continue;
                            }

                            if (!noScan)
                            {
                                Logger.LogDebug("[Sync] Begin loading asset {Asset} issue data", asset.Name);

                                try
                                {
                                    await foreach (var issue in GetIssuesAsync(client, asset.Id))
                                    {
                                        MapIssue(asset, issue, queueScan, queueAsset);
                                        totalIssueCount++;
                                        CheckCancel(false);
                                    }

                                    if (issueCount == 0)
                                    {
                                        MapIssue(asset, null, queueScan, queueAsset, true);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError(ex, "[Sync] Issue mapping error: {Err}", ex.Message);
                                }

                            }
                            queueScan.Entity.Saltminer.Internal.IssueCount = issueCount;

                            //Update Local metric with source metric, so always current
                            UpdateLocalMetric(metric, localMetric);

                            queueScan.Loading = false;
                            LocalData.AddUpdate(queueScan);
                            CheckCancel(true);
                            await LetSendCatchUpAsync(Config);

                            //Set recoverymode to false, for the next iteration
                            RecoveryMode = false;
                        }
                    }
                    catch (LocalDataException ex)
                    {
                        Logger.LogCritical(ex, "[Sync] Local data access error: {Msg}", ex.InnerException?.Message ?? ex.Message);
                        StillLoading = false;
                        throw new TraceableException($"Local data access error: {ex.InnerException?.Message ?? ex.Message}", ex);
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message == "Not Found")
                        {
                            Logger.LogWarning(ex, "[Sync] {Instance} instance processing error: {Msg}", Config.Instance, ex.InnerException?.Message ?? ex.Message);
                        }
                        else
                        {
                            exceptionCounter++;
                            Logger.LogWarning(ex, "[Sync] {Instance} instance processing error: {Msg}", Config.Instance, ex.InnerException?.Message ?? ex.Message);
                            if (exceptionCounter == Config.SourceAbortErrorCount)
                            {
                                Logger.LogCritical(ex, "[Sync] {Instance} instance exceeded {Count} sync processing errors: {Msg}", Config.Instance, Config.SourceAbortErrorCount, ex.InnerException?.Message ?? ex.Message);
                                StillLoading = false;
                                break;
                            }
                        }
                    }
                }

                //Include: indicate you are finished with this source metric
                SyncRecord.LastSync = (DateTime.UtcNow);
                SyncRecord.CurrentSourceId = null;
                SyncRecord.State = SyncState.Completed;
                LocalData.AddUpdate(SyncRecord, true);
                LocalData.SaveAllBatches();
                Logger.LogInformation("[Sync] Sync complete ({Ac} total assets, {Ic} total source issues), exiting phase in 5 sec...", totalAssetCount, totalIssueCount);
                await Task.Delay(5000);
                //Include: Set this to indicate you are finished syncing
                StillLoading = false;
            }
            catch (CancelTokenException cte)
            {
                Logger.LogWarning(cte, "[Sync] Cancellation requested, aborting processing: {Msg}.", cte.Message);
                StillLoading = false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[Sync] Aborting Traceable sync due to exception: [{Type}] {Msg}", ex.GetType().Name, ex.Message);
                StillLoading = false;
            }
        }

        internal async IAsyncEnumerable<EntityResultsDto> GetAsync(TraceableClient client)
        {
            var complete = false;
            var offset = 0;

            while (!complete)
            {
                var assetBatch = (await client.GetEntitiesAsync(offset, Config.BatchLimit)).Data.Entities.Results;
                complete = assetBatch.Count == 0;

                if (complete)
                {
                    Logger.LogInformation("[GetAsync] Complete.");
                }
                else
                {
                    offset += Config.BatchLimit;

                    foreach (var asset in assetBatch)
                    {
                        if (asset != null)
                        {
                            yield return asset;
                        }
                    }
                }
            }
        }

        internal async IAsyncEnumerable<VulnerabilityResultsDto> GetIssuesAsync(TraceableClient client, string assetId)
        {
            var complete = false;
            var offset = 0;

            while (!complete)
            {
                var issueBatch = (await client.GetAssetVulnerabilitiesAsync(assetId, offset, Config.BatchLimit)).Data.VulnerabilitiesV3.Results;
                complete = issueBatch.Count == 0;

                if (complete)
                {
                    Logger.LogInformation("[GetIssuesAsync] Complete.");
                }
                else
                {
                    offset += Config.BatchLimit;

                    foreach (var issue in issueBatch)
                    {
                        yield return issue;
                    }
                }
            }
        }


        private QueueScan MapScan(EntityResultsDto asset, bool noScan = false)
        {
            var sourceId = $"{asset.Id}|{asset.LastScanTimestamp}";
            var assessmentType = AssessmentType.DAST.ToString("g"); 
            var now = DateTime.UtcNow;
            DateTime scanDate = ConvertStringDate(asset.LastScanTimestamp) ?? now;

            var queueScan = new QueueScan
            {
                Loading = true,
                Entity = new()
                {
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueScanInfo
                    {
                        Scan = new SaltMiner.Core.Entities.QueueScanInfo
                        {
                            AssessmentType = assessmentType,
                            Product = Product,
                            ReportId = noScan ? GetNoScanReportId(assessmentType) : $"{sourceId}|{scanDate}",
                            ScanDate = scanDate,
                            ProductType = "Application",
                            Vendor = Vendor,
                            AssetType = AssetType,
                            IsSaltminerSource = TraceableConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            QueueStatus = QueueScanStatus.Loading.ToString("g")
                        }
                    },
                    Timestamp = now
                },
                Timestamp = now
            };

            if (CustomAssembly != null)
            {
                CustomAssembly.CustomizeQueueScan(queueScan, asset);
                if (CustomAssembly.CancelScan)
                {
                    queueScan.Entity.Saltminer.Internal.QueueStatus = QueueScanStatus.Cancel.ToString();
                    LocalData.DeleteQueueScan(queueScan.Id);
                    return queueScan;
                }
            }
            return LocalData.AddUpdate(queueScan);
        }

        private QueueAsset MapAsset(EntityResultsDto asset, QueueScan queueScan, bool isRetired = false)
        {
            var sourceId = $"{asset.Id}";
            var queueAsset = new QueueAsset
            {
                Entity = new()
                {
                    Timestamp = DateTime.UtcNow,
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueAssetInfo
                    {
                        Internal = new()
                        {
                            QueueScanId = queueScan.Entity.Id
                        },
                        Asset = new SaltMiner.Core.Entities.AssetInfoPolicy
                        {
                            Description = asset.Name,
                            Name = asset.Name,
                            Attributes = new()
                            {
                                { "labels", string.Join(", ", asset.Labels.Results.Select(x => x.Key)) },
                                { "is_external", asset.IsExternal.ToString().ToLower() },
                                { "environment", asset.Environment }
                            },
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = TraceableConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = sourceId,
                            Version = string.Empty,
                            AssetType = AssetType,
                            LastScanDaysPolicy = Config.LastScanDaysPolicy,
                            IsRetired = isRetired
                        }
                    }
                }
            };

            var result = LocalData.GetQueueAsset(Config.SourceType, sourceId) ?? LocalData.AddUpdate(queueAsset);
            return result;
        }


        private void MapIssue(EntityResultsDto asset, VulnerabilityResultsDto issue, QueueScan queueScan, QueueAsset queueAsset, bool zeroRecord = false)
        {
            if (zeroRecord)
            {
                LocalData.AddUpdate(GetZeroQueueIssue(queueScan, queueAsset));
                return;
            }
            var qIssue = new QueueIssue
            {
                Entity = new()
                {
                    Labels = [],
                    Vulnerability = new SaltMiner.Core.Entities.VulnerabilityInfo
                    {
                        Id = [issue.VulnerabilityId.Value],
                        Audit = new SaltMiner.Core.Entities.AuditInfo
                        {
                            Audited = true
                        },
                        Category = item,
                        FoundDate = ConvertMillisecondsDate(issue?.CreatedTimestampMillis.Value) ?? DateTime.Now,
                        LocationFull = issue?.DisplayName.Value,
                        Location = issue?.DisplayName.Value,
                        Name = issue?.DisplayName.Value,
                        Description = issue?.DisplayName.Value,
                        ReportId = issue.VulnerabilityId.Value,
                        Scanner = new SaltMiner.Core.Entities.ScannerInfo
                        {
                            Id = $"{issue.VulnerabilityId.Value}|{asset.Id}",
                            AssessmentType = queueScan.Entity.Saltminer.Scan.AssessmentType,
                            Product = Product,
                            Vendor = Vendor,
                            ApiUrl = "", // todo - where to get?
                            GuiUrl= "", // todo - where to get?
                        },
                        Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, issue.Severity.Value),
                        SourceSeverity = issue.Severity.Value,
                        IsSuppressed = false, // todo - how to get?
                        RemovedDate = ConvertMillisecondsDate(issue?.ClosedTimestampMillis.Value)
                    },
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                    {
                        IssueType = queueScan.Entity.Saltminer.Scan.AssessmentType,
                        Attributes = new Dictionary<string, string>
                        { 
                            { "status", issue?.Status.Value ?? string.Empty },
                            { "sources", issue?.Sources?.ToCsv() },
                            { "affected_domain_ids", issue?.AffectedDomainIds?.ToCsv() },
                            { "affected_domains", issue?.AffectedDomainNames?.ToCsv() }
                        },
                        QueueScanId = queueScan.Id,
                        QueueAssetId = queueAsset.Id,
                        Source = new SaltMiner.Core.Entities.SourceInfo
                        {
                            Analyzer = Product,
                        }
                    },
                    Tags = [],
                    Timestamp = DateTime.UtcNow
                }
            };
            CustomAssembly?.CustomizeQueueIssue(qIssue, asset);
            LocalData.AddUpdate(qIssue);
        }

        private static DateTime? ConvertMillisecondsDate(long? timestampMilliseconds)
        {
            if (timestampMilliseconds == null)
            {
                return null;
            }
            var milliseconds = (long)timestampMilliseconds;
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
            return dateTimeOffset.UtcDateTime;
        }

        private static DateTime? ConvertStringDate(string date)
        {
            if (string.IsNullOrEmpty(date))
            {
                return null;
            }
            return TryParseDateToUtc(date);
        }

        static DateTime? TryParseDateToUtc(string dateString)
        {
            string[] formats = [
                "yyyy-MM-ddTHH:mm:ss'Z'",
                "yyyy-MM-ddTHH:mm:ss.fff'Z'",
                "yyyy-MM-ddTHH:mm:ssZ",
                "yyyy-MM-ddTHH:mmzzz",
                "yyyy-MM-ddTHH:mm:sszzz"
            ];

            foreach (var format in formats)
            {
                if (DateTimeOffset.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dateTimeOffset))
                {
                    return dateTimeOffset.UtcDateTime;
                }
            }
            return null;
        }
    }
}



