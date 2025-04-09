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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.SourceAdapters.SonarQube
{
    public class SonarQubeAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private readonly string AssetType = "app";
        public SonarQubeConfig Config;

        public SonarQubeAdapter(IServiceProvider provider, ILogger<SonarQubeAdapter> logger) : base(provider, logger)
        {
            Logger.LogDebug("SonarQubeAdapter Initialization complete.");
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            await base.RunAsync(config, token);
            try
            {
                config = config ?? throw new ArgumentNullException(nameof(config));

                if (!(config is SonarQubeConfig))
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(SonarQubeConfig)}', but got '{config.GetType().Name}'");
                }

                Config = config as SonarQubeConfig;
                CancelToken = token;
                Config.Validate();
                FirstLoadSyncUpdate(config);
                SetApiClientSslVerification(Config.VerifySsl);

                var client = new SonarQubeClient(ApiClient, Config, Logger);

                if (Config.HasCustomAssembly)
                {
                    CustomAssembly = AssemblyHelper.LoadClassAssembly<ISourceAdapterCustom>(Config.CustomAssemblyName, Config.CustomAssemblyType);
                    CustomAssembly.PreProcess();
                }
                StillLoading = true;

                //await foreach (var component in client.GetAllComponentsAsync())
                //{
                //    var value = component; 
                //}
                await Task.WhenAll(SyncAsync(client), SendAsync(Config, AssetType));

                ResetFailures(Config);
                DeleteFailures(Config);

                await Task.Delay(5, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex.InnerException?.Message ?? ex.Message, ex);
                throw;
            }
        }

        private async Task SyncAsync(SonarQubeClient client)
        {
            CheckCancel();

            if (Config.SourceType != SourceType.SonarQube.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{etype}' but was found to be '{atype}'", SourceType.SonarQube.GetDescription(), Config.SourceType);
                throw new SonarQubeValidationException("Invalid configuration - source type");
            }

            var exceptionCounter = 0;

            //var components = await client.GetAllComponentsAsync();
            //Logger.LogInformation($"[Sync] Received {components.Count()} Components");

            //Get source metrics from client
            //var sourceMetrics = (await client.SourceMetricsAsync(components, Config)).Results.ToList();
            //Get local metrics from LocalData
            var localMetrics = LocalData.GetSourceMetrics(Config.Instance, Config.SourceType).ToList();
            var newLocalIssues = 0;
            var newLocalScans = 0;
            var newLocalAssets = 0;

            SyncRecord = LocalData.CheckSyncRecordSourceForFailure(Config.Instance, Config.SourceType);

            if (SyncRecord != null)
            {
                RecoveryMode = true;
            }
            else
            {
                RecoveryMode = false;
                SyncRecord = LocalData.GetSyncRecord(Config.Instance, Config.SourceType);
                ClearQueues();
            }

            await foreach (var component in client.GetAllComponentsAsync())
            //foreach (var metric in sourceMetrics)
            {
                var metric = await client.SourceMetricAsync(component, Config);
                try
                {
                    Logger.LogInformation($"[Sync] Starting: SourceId '{metric.SourceId}', AssetType '{AssetType}'");

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

                    SyncRecord.CurrentSourceId = metric.SourceId;
                    SyncRecord.State = SyncState.InProgress;
                    LocalData.AddUpdate(SyncRecord);

                    var localMetric = localMetrics.FirstOrDefault(x => x.SourceId == metric.SourceId);
                    if (localMetric != null)
                    {
                        localMetric.IsProcessed = true;
                    }

                    //var component = components.First(x => x.SourceId == metric.SourceId);
                    var issues = client.GetIssuesByComponentAsync(component.Key, component.LastAnalysisDate).Result;

                    metric.IssueCount = issues.Count();
                    metric.IssueCountSev1 = issues.Count(x => x.Severity.ToLower().Contains("critical"));
                    metric.IssueCountSev2 = issues.Count(x => x.Severity.ToLower().Contains("major"));
                    metric.IssueCountSev3 = issues.Count(x => x.Severity.ToLower().Contains("minor"));

                    if (Config.FullSyncMaintEnabled)
                    {
                        FullSyncBatchProcess(SyncRecord, Config.FullSyncBatchSize);
                        // End of metrics and full sync processing is still true,
                        // means last batch did not meet batch size... reset source ID to cycle through again
                        // End of metrics and batch count is zero,
                        // means nothing was found to process (ie. maybe a source metric doesn't exist anymore)...reset source ID

                        //TODO: FIND COUNTS ENDPOINT AND MAKE THIS WORK
                        //if (i + 1 == sourceMetrics.Count && (FullSyncProcessing || FullSyncBatchCount == 0))
                        //{
                        //    SyncRecord.FullSyncSourceId = "";
                        //}
                    }

                    if (NeedsUpdate(metric, localMetric) || RecoveryMode)
                    {
                        Logger.LogInformation($"[Sync] Updating: SourceId '{metric.SourceId}', AssetType '{AssetType}'");

                        if (metric.LastScan != null)
                        {
                            var queueScan = MapScan(component, issues.Count());
                            newLocalScans++;

                            QueueAsset queueAsset = MapAsset(component, queueScan, localMetric == null && localMetrics.Count > 0 && !Config.DisableRetire);
                            newLocalAssets++;

                            if (queueScan.Entity.Saltminer.Internal.QueueStatus == SaltMiner.Core.Entities.QueueScan.QueueScanStatus.Cancel.ToString())
                            {
                                continue;
                            }

                            foreach(var issue in issues)
                            {
                                QueueIssue queueIssue = MapIssue(component, issue, queueScan, queueAsset);
                            }
                            

                            CheckCancel(false);

                            queueScan.Loading = false;
                            LocalData.AddUpdate(queueScan);

                            newLocalIssues = newLocalIssues + queueScan.Entity.Saltminer.Internal.IssueCount;
                        }
                        else
                        {
                            MapAsset(component, null, localMetric == null && localMetrics.Count > 0 && !Config.DisableRetire);
                        }

                        UpdateLocalMetric(metric, localMetric);
                        RecoveryMode = false;
                    }

                    Logger.LogInformation($"[Sync] Finished: SourceId '{metric.SourceId}', AssetType '{AssetType}'");
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
                        Logger.LogWarning(ex, $"[Sync] {Config.Instance} for {Config.SourceType} Sync Processing Error: {ex.InnerException?.Message ?? ex.Message}");
                    }
                    else
                    {
                        exceptionCounter++;
                        var innerEx = ex.InnerException; 
                        Logger.LogError(ex, "[Sync] {Instance} for {SourceType} Sync Processing Error {ErrorCount}: {ErrorMessage}",Config.Instance,Config.SourceType, exceptionCounter, innerEx ?.Message ?? ex.Message);

                        if (exceptionCounter == Config.SourceAbortErrorCount)
                        {
                            Logger.LogCritical(ex, $"[Sync] {Config.Instance} for {Config.SourceType} Exceeded {Config.SourceAbortErrorCount} Sync Processing Errors: {ex.InnerException?.Message ?? ex.Message}");

                            StillLoading = false;

                            break;
                        }
                    }
                }

                CheckCancel();
            }

            //Include: Set this to indiciate your done syncing
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
                    Logger.LogError(ex, "Error occurred when processing retirees, see log for details.");
                }
            }
            else
            {
                Logger.LogInformation("Asset retirement processing disabled by configuration, skipping.");
            }

            SyncRecord.LastSync = (DateTime.UtcNow);
            SyncRecord.CurrentSourceId = null;
            SyncRecord.State = SyncState.Completed;
            LocalData.AddUpdate(SyncRecord);
            LocalData.SaveAllBatches();
        }

        private QueueScan MapScan(ComponentDto component, int issueCount)
        {
            var now = DateTime.UtcNow;
            var queueScan = new QueueScan
            {
                Loading = true,
                Entity = new()
                {
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueScanInfo
                    {
                        Scan = new SaltMiner.Core.Entities.QueueScanInfo
                        {
                            AssessmentType = AssessmentType.SAST.ToString("g"),
                            Product = "SonarQube",
                            ReportId = component.SourceId,
                            ScanDate = component.LastAnalysisDate,
                            ProductType = "SAST",
                            Vendor = "SonarQube",
                            AssetType = AssetType,
                            IsSaltminerSource = SonarQubeConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = issueCount,
                            QueueStatus = SaltMiner.Core.Entities.QueueScan.QueueScanStatus.Loading.ToString("g"),
                        },
                    },
                    Timestamp = now
                },
                Timestamp = now
            };

            if (CustomAssembly != null)
            {
                CustomAssembly.CustomizeQueueScan(queueScan, component);
                if (CustomAssembly.CancelScan)
                {
                    queueScan.Entity.Saltminer.Internal.QueueStatus = SaltMiner.Core.Entities.QueueScan.QueueScanStatus.Cancel.ToString();
                    LocalData.DeleteQueueScan(queueScan.Id);
                    return queueScan;
                }
            }
            return LocalData.AddUpdate(queueScan);
        }

        private QueueAsset MapAsset(ComponentDto component, QueueScan queueScan, bool isRetired = false)
        {
            var queueAsset = new QueueAsset
            {
                Entity = new()
                {
                    Timestamp = DateTime.UtcNow,
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueAssetInfo
                    {
                        Internal = new()
                        {
                            QueueScanId = queueScan.Id,
                        },
                        Asset = new SaltMiner.Core.Entities.AssetInfoPolicy
                        {
                            Description = component.Name,
                            Name = component.Name,
                            Attributes = new Dictionary<string, string>(),
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = SonarQubeConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = component.SourceId,
                            Version = component.Revision,
                            AssetType = AssetType,
                            LastScanDaysPolicy = Config.LastScanDaysPolicy,
                            IsRetired = isRetired
                        }
                    }
                }
            };

            var result = LocalData.GetQueueAsset(Config.SourceType, component.SourceId) ?? LocalData.AddUpdate(queueAsset);
            return result;
        }


        private QueueIssue MapIssue(ComponentDto component, IssueDto issue, QueueScan queueScan, QueueAsset queueAsset)
        {
            if (queueScan.Entity.Saltminer.Internal.IssueCount == 0)
            {
                var result = LocalData.AddUpdate(GetZeroQueueIssue(queueScan, queueAsset));
                return result;
            }
            else
            {
                var queueIssue = new QueueIssue
                {
                    Entity = new()
                    {
                        Labels = [],
                        Vulnerability = new SaltMiner.Core.Entities.VulnerabilityInfo
                        {
                            Audit = new SaltMiner.Core.Entities.AuditInfo
                            {
                                Audited = true,
                            },
                            Category = [ "Application" ],
                            Description = issue.Message,
                            Classification = issue.Tags?.Count > 0 ? $"{issue.Type}|{issue.Tags[0]}" : issue.Type,
                            FoundDate = issue.CreationDate,
                            Id = [issue.Key],
                            LocationFull = issue.Line ?? "N/A",
                            Location = issue.Component ?? "N/A",
                            Name = issue.Rule,
                            ReportId = component.SourceId,
                            Scanner = new SaltMiner.Core.Entities.ScannerInfo
                            {
                                Id = issue.Key,
                                AssessmentType = AssessmentType.SAST.ToString("g"),
                                Product = "SonarQube",
                                Vendor = "SonarQube"
                            },
                            Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, issue.Severity),
                            SourceSeverity = issue.Severity
                        },
                        Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                        {
                            Attributes = [],
                            QueueScanId = queueScan.Id,
                            QueueAssetId = queueAsset.Id,
                            Source = new SaltMiner.Core.Entities.SourceInfo
                            {
                                Analyzer = "SonarQube",
                            }
                        },
                        Tags = [],
                        Timestamp = DateTime.UtcNow
                    }
                };

                CustomAssembly?.CustomizeQueueIssue(queueIssue, component);

                var result = LocalData.AddUpdate(queueIssue);
                return result;
            }

        }
    }
}
