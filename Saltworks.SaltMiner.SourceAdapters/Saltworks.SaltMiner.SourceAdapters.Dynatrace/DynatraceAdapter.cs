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
using Saltworks.SaltMiner.SourceAdapters.Core.Helpers;
using Saltworks.SaltMiner.SourceAdapters.Core.Interfaces;
using System.Globalization;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.SourceAdapters.Dynatrace
{
    public class DynatraceAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private DynatraceConfig Config;
        private readonly string AssetType = "app";
        private readonly string Vendor = "Dynatrace, LLC";
        private readonly string Product = "Dynatrace";
                
        public DynatraceAdapter(IServiceProvider provider, ILogger<DynatraceAdapter> logger) : base(provider, logger)
        {
            Logger.LogDebug("SonatypeAdapter Initialization complete.");
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            await base.RunAsync(config, token);
            try
            {
                #region Include: Get Config and Validate

                config = config ?? throw new ArgumentNullException(nameof(config));
                if (config is not DynatraceConfig)
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(DynatraceConfig)}', but got '{config.GetType().Name}'");
                }
                Config = config as DynatraceConfig;
                Config.Validate();

                #endregion

                //Include: Check local metrics to make sure there is data, if not run FirstLoad
                FirstLoadSyncUpdate(config);
                
                //Include
                SetApiClientSslVerification(Config.VerifySsl);

                //Write
                var client = new DynatraceClient(ApiClient, Config, Logger);

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
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "{Msg}", ex.InnerException?.Message ?? ex.Message);
                throw new DynatraceException("Run error", ex);
            }
        }

        private async Task SyncAsync(DynatraceClient client)
        {
            CheckCancel();

            if (Config.SourceType != SourceType.Dynatrace.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{Etype}' but was found to be '{Atype}'", SourceType.Dynatrace.GetDescription(), Config.SourceType);
                throw new DynatraceValidationException("Invalid configuration - source type");
            }

            try
            {
                Logger.LogInformation($"[Sync] Starting Dynatrace sync...");

                if (Config.TestingAssetLimit > 0)
                {
                    Logger.LogWarning("TestingAssetLimit of {Amt} is in effect.  Dynatrace loading will stop at this count.", Config.TestingAssetLimit);
                }

                var exceptionCounter = 0;

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

                    //Include: Clear any leftover queue data from previous run
                    ClearQueues();
                }

                var totalAssetCount = 0;

                Logger.LogInformation($"[Sync] Begin loading asset data...");

                await foreach (var asset in GetAssetAsync(client))
                {
                    CheckCancel();

                    try
                    {
                        // testing limit reached or testing 1 specific asset - break out of all processing
                        if ((Config.TestingAssetLimit > 0 && totalAssetCount >= Config.TestingAssetLimit))
                        {
                            break;
                        }

                        totalAssetCount++;
                        Logger.LogInformation("Total assets processed: {TotalAssetCount}", totalAssetCount);

                        Logger.LogInformation("Getting SourceMetric for Asset {AssetId}", asset.Id);                        
                        SourceMetric metric = client.GetSourceMetric(asset);

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
                        if (NeedsUpdate(metric, localMetric) || RecoveryMode)
                        {
                            Logger.LogInformation("[Sync] Updating Config.Instance '{ConfigInstance}', SourceType {ConfigSourceType}, SourceId '{MetricSourceId}', AssetType '{AssetType}'", Config.Instance, Config.SourceType, metric.SourceId, AssetType);


                            QueueScan queueScan;

                            queueScan = MapScan(asset);

                            QueueAsset queueAsset = MapAsset(asset, queueScan);

                            if (queueScan.Entity.Saltminer.Internal.QueueStatus == QueueScanStatus.Cancel.ToString())
                            {
                                continue;
                            }

                            var issueCount = 0;
                            Logger.LogInformation("[NeedsUpdate] Begin loading asset {Asset} issue data", asset.Id);

                            try
                            {
                                await foreach (var issue in GetIssueByAssetAsync(client, asset.Id))
                                {
                                    issueCount++;
                                    MapIssue(asset, issue, queueScan, queueAsset);
                                }

                                if (issueCount == 0)
                                {
                                    MapIssue(asset, null, queueScan, queueAsset, true);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "[Sync] Exception: {ErrorMsg}", ex.InnerException?.Message ?? ex.Message);
                            }

                            queueScan.Entity.Saltminer.Internal.IssueCount = issueCount;

                            CheckCancel(false);
                            queueScan.Loading = false;
                            LocalData.AddUpdate(queueScan);
                            await LetSendCatchUpAsync(Config);

                            //Update Local metric with source metric, so always current
                            UpdateLocalMetric(metric, localMetric);
                            //Set recoverymode to false, for the next iteration
                            RecoveryMode = false;
                        }
                    }
                    catch (LocalDataException ex)
                    {
                        Logger.LogCritical(ex, "[Sync] Local data exception: {ErrorMsg}", ex.InnerException?.Message ?? ex.Message);

                        StillLoading = false;

                        throw;
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message == "Not Found")
                        {
                            Logger.LogWarning(ex, "[Sync] {ConfigInstance} for {ConfigSourceType} Sync Processing Error: {ErrorMsg}", Config.Instance, Config.SourceType, ex.InnerException?.Message ?? ex.Message);
                        }
                        else
                        {
                            exceptionCounter++;

                            Logger.LogWarning(ex, "[Sync] {ConfigInstance} for {ConfigSourceType} Sync Processing Error {ExceptionCounter}: {ErrorMsg}", Config.Instance, Config.SourceType, exceptionCounter, ex.InnerException?.Message ?? ex.Message);

                            if (exceptionCounter == Config.SourceAbortErrorCount)
                            {
                                Logger.LogCritical(ex, "[Sync] {ConfigInstance} for {ConfigSourceType} Exceeded {ConfigSourceAbortErrorCount} Sync Processing Errors: {ErrorMsg}", Config.Instance, Config.SourceType, Config.SourceAbortErrorCount, ex.InnerException?.Message ?? ex.Message);

                                StillLoading = false;

                                break;
                            }
                        }
                    }
                }

                // todo: RETIRE?
                //if (!Config.DisableRetire)
                //{
                //    try
                //    {
                //        RetireLocalMetrics(localMetrics);
                //        RetireQueueAssets(localMetrics, AssetType, Config);
                //    }
                //    catch (Exception ex)
                //    {
                //        Logger.LogError(ex, "Error occurred when processing retirees, see log for details.");
                //    }
                //}
                //else
                //{
                //    Logger.LogInformation("Asset retirement processing disabled by configuration, skipping.");
                //}

                //Include: indicate your done with this source metric
                SyncRecord.LastSync = (DateTime.UtcNow);
                SyncRecord.CurrentSourceId = null;
                SyncRecord.State = SyncState.Completed;
                LocalData.AddUpdate(SyncRecord, true);
                LocalData.SaveAllBatches();
                Logger.LogInformation("[Sync] Exiting sync loading phase in 5 sec...");
                await Task.Delay(5000);
                //Include: Set this to indiciate your done syncing
                StillLoading = false;
            }
            catch (CancelTokenException cte)
            {
                Logger.LogWarning(cte, "[Sync] Cancellation requested, aborting processing.");
                StillLoading = false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[Sync] Aborting Dynatrace sync due to exception: [{Type}] {Msg}", ex.GetType().Name, ex.Message);
                StillLoading = false;
            }
        }

        internal async IAsyncEnumerable<EntityRecord> GetAssetAsync(DynatraceClient client)
        {
            var complete = false;
            string afterToken = string.Empty;
            while (!complete)
            {
                var assetBatch = (await client.GetEntitiesAsync(afterToken, Config.BatchLimit))?.Content?.Result;

                if (assetBatch == null) break;
                    
                // afterToken didn't produce any more data - done
                complete = assetBatch.Records.Count == 0;
                if (complete)
                {
                    Logger.LogInformation("[GetAssetAsync] Complete.");
                }
                else
                {
                    afterToken = assetBatch.Records.Last().Id;

                    foreach (var asset in assetBatch.Records)
                    {
                        if (asset != null)
                        {
                            yield return asset;
                        }
                    }
                }
            }
        }

        internal async IAsyncEnumerable<SecurityEventRecord> GetIssueByAssetAsync(DynatraceClient client, string assetId)
        {
            var complete = false;
            string afterToken = string.Empty;
            while (!complete)
            {
                var issueBatch = (await client.GetIssuesByIdAsync(assetId, afterToken, Config.BatchLimit))?.Content?.Result;

                if (issueBatch == null) break;

                // afterToken didn't produce any more data - done
                complete = issueBatch.Records.Count == 0;
                if (complete)
                {
                    Logger.LogInformation("[GetIssueByAssetAsync] Complete.");
                }
                else
                {
                    afterToken = issueBatch.Records.Last().Id;

                    foreach (var issue in issueBatch.Records)
                    {
                        yield return issue;
                    }
                }
            }
        }

        private QueueScan MapScan(EntityRecord asset, bool noScan = false)
        {
            var sourceId = asset.Id;
            var assessmentType = AssessmentType.Open.ToString("g");
            var now = DateTime.UtcNow;

            DateTime scanDate = now;

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
                            ReportId = noScan ? GetNoScanReportId(assessmentType) : $"{sourceId}|{scanDate:yyyy-MM-dd}",
                            ScanDate = noScan ? now : scanDate,
                            ProductType = "Application",
                            Vendor = Vendor,
                            AssetType = AssetType,
                            IsSaltminerSource = DynatraceConfig.IsSaltminerSource,
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

        private QueueAsset MapAsset(EntityRecord asset, QueueScan queueScan, bool isRetired = false)
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
                            Attributes = [],
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = DynatraceConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = sourceId,
                            Version = "", // todo - get default branch name
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


        private void MapIssue(EntityRecord asset, SecurityEventRecord issue, QueueScan queueScan, QueueAsset queueAsset, bool zeroRecord = false)
        {
            List<QueueIssue> queueIssues = [];

            if (zeroRecord)
            {
                LocalData.AddUpdate(GetZeroQueueIssue(queueScan, queueAsset));
            }
            else
            {
                queueIssues.Add(new QueueIssue
                {
                    Entity = new()
                    {
                        Labels = [],
                        Vulnerability = new SaltMiner.Core.Entities.VulnerabilityInfo
                        {
                            Id = issue?.Cve?.ToArray(),
                            Audit = new SaltMiner.Core.Entities.AuditInfo
                            {
                                Audited = true
                            },
                            Category = [ "Application" ],
                            FoundDate = ConvertStringDate(issue.FirstSeen) ?? DateTime.Now,
                            LocationFull = issue.LocationFull,
                            Location = issue.Location,
                            Name = issue.Title,
                            Description = issue.Description,
                            ReportId = issue.Id,
                            Scanner = new SaltMiner.Core.Entities.ScannerInfo
                            {
                                Id = issue.Id,
                                AssessmentType = queueScan.Entity.Saltminer.Scan.AssessmentType,
                                Product = Product,
                                Vendor = Vendor,
                                ApiUrl = issue.ApiUrl,
                                GuiUrl= issue.GuiUrl
                            },
                            Score = new()
                            {
                                Base = issue.Score ?? 0
                            },
                            Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, issue.Level),
                            SourceSeverity = issue.Level,
                            IsSuppressed = issue.MuteStatus == "MUTED",
                            RemovedDate = null
                        },
                        Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                        {
                            IssueType = queueScan.Entity.Saltminer.Scan.AssessmentType,
                            Attributes = new Dictionary<string, string> 
                            {
                                {"remediation", issue.Remediation },
                                {"tracking_link_text", issue.TrackingLinkText },
                                {"tracking_link_url", issue.TrackingLinkUrl },
                                {"internet_exposure", issue.InternetExposure },
                                {"data_exposure", issue.DataExposure },
                                {"vulnerable_function", issue.VulnerableFunction },
                                {"public_exploit", issue.PublicExploit },
                                {"affected_hosts", string.Join(',', issue.AffectedHostNames ?? []) }
                            },
                            QueueScanId = queueScan.Id,
                            QueueAssetId = queueAsset.Id,
                            Source = new SaltMiner.Core.Entities.SourceInfo
                            {
                                Analyzer = Product,
                            },
                            CustomData = new { 
                                AffectedHosts = issue.AffectedHostNames,
                                issue.LoadOrigins
                            }
                        },
                        Tags = [],
                        Timestamp = DateTime.UtcNow
                    }
                });
            }

            foreach (var queueIssue in queueIssues)
            {
                CustomAssembly?.CustomizeQueueIssue(queueIssue, asset);
                LocalData.AddUpdate(queueIssue);
            }
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
            string[] formats = {
                "yyyy-MM-ddTHH:mm:ss'Z'",
                "yyyy-MM-ddTHH:mm:ssZ",
                "yyyy-MM-ddTHH:mmzzz",
                "yyyy-MM-ddTHH:mm:sszzz"
            };

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



