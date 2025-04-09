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
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.SaltMiner.SourceAdapters.Core.Helpers;
using Saltworks.SaltMiner.SourceAdapters.Core.Interfaces;
using Saltworks.Utility.ApiHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.SourceAdapters.BlackDuck
{
    public class BlackDuckAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
        private ISourceAdapterCustom CustomAssembly;
        private BlackDuckConfig Config;
        private readonly string AssetType = "app";
        private readonly ApiClient BearerClient;
        private readonly ProgressHelper ProgressHelper;

        public BlackDuckAdapter(DataClientFactory<DataClient.DataClient> dataFactory, ApiClientFactory<SourceAdapter> clientFactory, IServiceProvider provider, ILogger<BlackDuckAdapter> logger) : base(dataFactory, clientFactory, provider, logger)
        {
            Logger.LogDebug("BlackDuckAdapter Initialization complete.");
            BearerClient = clientFactory.CreateApiClient();
            ProgressHelper = new ProgressHelper(Logger);
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            try
            {
                if (!(config is BlackDuckConfig))
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(BlackDuckConfig)}', but got '{config.GetType().Name}'");
                }

                Config = config as BlackDuckConfig;
                CancelToken = token;
                Config.Validate();

                ProgressHelper.StartTimer(GetProgressKey(Config.Instance, config.SourceType, AssetType, null, "RunAsync"));

                FirstLoadSyncUpdate(config);

                SetApiClientSslVerification(Config.VerifySsl);

                var client = new BlackDuckClient(ApiClient, BearerClient, Config, Logger);

                if (Config.HasCustomAssembly)
                {
                    CustomAssembly = AssemblyHelper.LoadClassAssembly<ISourceAdapterCustom>(Config.CustomAssemblyName, Config.CustomAssemblyType);
                    CustomAssembly.PreProcess();
                }

                StillLoading = true;

                await Task.WhenAll(SyncAsync(client), SendAsync(ProgressHelper, Config, AssetType));

                ResetFailures(Config);
                DeleteFailures(Config);

                await Task.Delay(5, CancellationToken.None);

                ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, null, "RunAsync"), 1);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex.InnerException?.Message ?? ex.Message, ex);

                throw;
            }
        }


        async Task SyncAsync(BlackDuckClient client)
        {
            CheckCancel();

            if (Config.SourceType != SourceType.BlackDuck.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{etype}' but was found to be '{atype}'", SourceType.BlackDuck.GetDescription(), Config.SourceType);
                throw new BlackDuckValidationException("Invalid configuration - source type");
            }

            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, "", "SyncAsync"));

            var exceptionCounter = 0;

            Logger.LogInformation($"Getting projects...");
            var projects = (await client.GetProjects()).ToList();
            Logger.LogInformation($"Received {projects.Count()} projects");

            Logger.LogInformation($"Getting SourceMetrics...");
            var sourceMetrics = (await client.SourceMetricsAsync(projects)).Results.ToList();
            Logger.LogInformation($"Received SourceMetrics");

            Logger.LogInformation($"Processing SourceMetrics (Total: {sourceMetrics.Count()})");

            var localMetrics = LocalData.GetSourceMetrics(Config.Instance, Config.SourceType).ToList();
            var newLocalIssues = 0;
            var newLocalScans = 0;
            var newLocalAssets = 0;

            for (var i = 0; i < sourceMetrics.Count; i++)
            {
                var metric = sourceMetrics[i];
                SyncRecord = LocalData.CheckSyncRecordSourceForFailure(Config.Instance, Config.SourceType);
                var RecoveryMode = SyncRecord != null;

                try
                {
                    ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"));

                    if (RecoveryMode)
                    {
                        if (SyncRecord.CurrentSourceId != metric.SourceId)
                        {
                            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"), 0, "Skipped due to recovery");
                            continue;
                        }
                        else
                        {
                            RecoveryMode = false;
                            ClearQueues();
                        }
                    }
                    else
                    {
                        SyncRecord = LocalData.GetSyncRecord(Config.Instance, Config.SourceType);
                    }

                    SyncRecord.CurrentSourceId = metric.SourceId;
                    SyncRecord.State = SyncState.InProgress;
                    LocalData.AddUpdate(SyncRecord);

                    var localMetric = localMetrics.FirstOrDefault(x => x.SourceId == metric.SourceId);
                    if (localMetric != null)
                    {
                        localMetric.IsProcessed = true;
                    }

                    var indexSplit = metric.SourceId.IndexOf("|");
                    var projectId = metric.SourceId.Substring(0, indexSplit);
                    var versionId = metric.SourceId.Substring(indexSplit + 1);

                    var appReport = await client.GetAppReportAsync(projectId, versionId);

                    metric.IssueCount = appReport.VulernabilityCount;
                    LoadIssueSeverityCounts(metric, appReport.Components);

                    if (Config.FullSyncMaintEnabled)
                    {
                        FullSyncBatchProcess(SyncRecord, Config.FullSyncBatchSize);
                        // End of metrics and full sync processing is still true,
                        // means last batch did not meet batch size... reset source ID to cycle through again
                        // End of metrics and batch count is zero,
                        // means nothing was found to process (ie. maybe a source metric doesn't exist anymore)...reset source ID
                        if (i + 1 == sourceMetrics.Count && (FullSyncProcessing || FullSyncBatchCount == 0))
                        {
                            SyncRecord.FullSyncSourceId = "";
                        }
                    }

                    if (NeedsUpdate(metric, localMetric, Config.LogNeedsUpdate) || RecoveryMode)
                    {
                        Logger.LogInformation($"[Sync] Updating Config.Instance '{Config.Instance}', Config.SourceType '{Config.SourceType}', SourceId '{metric.SourceId}', AssetType '{AssetType}'");
                        if (metric.LastScan == null)
                        {
                            QueueScan queueScan = MapScan(appReport);
                            newLocalScans++;

                            QueueAsset queueAsset = MapAsset(appReport, queueScan, localMetric == null && localMetrics.Count > 0 && !Config.DisableRetire);
                            newLocalAssets++;

                            if (queueScan.Entity.Saltminer.Internal.QueueStatus == QueueScanStatus.Cancel.ToString())
                            {
                                continue;
                            }

                            MapIssues(appReport, queueScan, queueAsset);

                            CheckCancel(false);
                            LocalData.AddUpdate(queueScan);

                            LocalData.AddUpdate(queueScan);

                            queueScan.Loading = false;

                            newLocalIssues = newLocalIssues + queueScan.Entity.Saltminer.Internal.IssueCount;
                            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"), queueScan.Entity.Saltminer.Internal.IssueCount, null);
                        }
                        else
                        {
                            MapAsset(appReport, null, localMetric == null && localMetrics.Count > 0 && !Config.DisableRetire);
                            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"), 0, null);
                        }

                        UpdateLocalMetric(metric, localMetric);
                        RecoveryMode = false;
                    }
                    else
                    {
                        ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"), 0, "Skipped due to not needing update");
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
                    exceptionCounter++;

                    Logger.LogWarning(ex, $"{Config.Instance} for {Config.SourceType} Sync Processing Error {exceptionCounter}: {ex.InnerException?.Message ?? ex.Message}");

                    if (exceptionCounter == Config.SourceAbortErrorCount)
                    {
                        Logger.LogCritical(ex, $"{Config.Instance} for {Config.SourceType} Exceeded {Config.SourceAbortErrorCount} Sync Processing Errors: {ex.InnerException?.Message ?? ex.Message}");

                        StillLoading = false;

                        break;
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

            SyncRecord.LastSync = DateTime.UtcNow;
            SyncRecord.CurrentSourceId = null;
            SyncRecord.State = SyncState.Completed;
            LocalData.AddUpdate(SyncRecord);

            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, "", "SyncAsync"), 0, $"Local QueueScans: {newLocalScans}; QueueAssets: {newLocalAssets}; QueueIssues: {newLocalIssues}");
        }

        private void LoadIssueSeverityCounts(SourceMetric metric, IEnumerable<AppComponents> components)
        {
            foreach (var component in components)
            {
                foreach (var vulnerability in component.Vulnerabilities)
                {
                    metric.IssueCountSev1 += GetIssueSeverityCount("high", vulnerability.UseCvss3 ? vulnerability.Cvss3.Severity : vulnerability.Cvss2.Severity);
                    metric.IssueCountSev2 += GetIssueSeverityCount("medium", vulnerability.UseCvss3 ? vulnerability.Cvss3.Severity : vulnerability.Cvss2.Severity);
                    metric.IssueCountSev3 += GetIssueSeverityCount("low", vulnerability.UseCvss3 ? vulnerability.Cvss3.Severity : vulnerability.Cvss2.Severity);
                }
            }
        }

        private long GetIssueSeverityCount(string severity, string vulSeverity)
        {
            if (vulSeverity.ToLower().Contains(severity))
            {
                return 1;
            }

            return 0;
        }

        private QueueScan MapScan(App appReport)
        {
            var sourceId = $"{appReport.ProjectId}|{appReport.VersionId}";

            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapScan"));
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
                            AssessmentType = AssessmentType.Open.ToString("g"),
                            Product = "BlackDuck",
                            ReportId = appReport.ProjectId,
                            ScanDate = appReport.EvaluationDate.Value.ToUniversalTime(),
                            ProductType = "Open",
                            Vendor = "Synopsis",
                            AssetType = AssetType,
                            IsSaltminerSource = BlackDuckConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = appReport.VulernabilityCount,
                            QueueStatus = QueueScanStatus.Loading.ToString("g"),
                        },
                    },
                    Timestamp = now
                },
                Timestamp = now
            };

            if (CustomAssembly != null)
            {
                CustomAssembly.CustomizeQueueScan(queueScan, appReport);
                if (CustomAssembly.CancelScan)
                {
                    queueScan.Entity.Saltminer.Internal.QueueStatus = QueueScanStatus.Cancel.ToString();
                    LocalData.DeleteQueueScan(queueScan.LocalId); 
                    ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapScan"), 0, "CustomAssembnly CancelScan Set");
                    return queueScan;
                }
            }

            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapScan"), 1);

            return LocalData.AddUpdate(queueScan);
        }

        private QueueAsset MapAsset(App appReport, QueueScan queueScan, bool isRetired = false)
        {
            var sourceId = $"{appReport.ProjectId}|{ appReport.VersionId}";

            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapAsset"));

            var queueAsset = new QueueAsset
            {
                LocalScanId = queueScan?.LocalId,
                Entity = new()
                {
                    Timestamp = DateTime.UtcNow,
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueAssetInfo
                    {
                        Asset = new SaltMiner.Core.Entities.AssetInfoPolicy
                        {
                            Description = appReport.Name,
                            Name = appReport.Name,
                            VersionId = appReport.VersionId,
                            Attributes = new Dictionary<string, string>(),
                            Instance = Config.Instance,
                            IsSaltminerSource = BlackDuckConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = sourceId,
                            Version = appReport.Version,
                            AssetType = AssetType,
                            LastScanDaysPolicy = Config.LastScanDaysPolicy,
                            IsRetired = isRetired,
                            IsProduction = true
                        },
                        Internal = new SaltMiner.Core.Entities.QueueAssetInternal
                        {
                            QueueScanId = queueScan?.Id
                        },
                    }
                }
            };

            var result = LocalData.GetQueueAsset(Config.SourceType, sourceId) ?? LocalData.AddUpdate(queueAsset);

            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapAsset"), 1);

            return result;
        }

        private void MapIssues(App appReport, QueueScan queueScan, QueueAsset queueAsset)
        {
            var sourceId = $"{appReport.ProjectId}|{ appReport.VersionId}";

            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"));

            List<QueueIssue> queueIssues = new();

            var localIssues = LocalData.GetQueueIssues(queueScan.LocalId, queueAsset.LocalId); 

            if (queueScan.Entity.Saltminer.Internal.IssueCount == 0)
            {
                queueIssues.Add(new QueueIssue
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
                            FoundDate = appReport.EvaluationDate.Value.ToUniversalTime(), // check for orginal found or null
                            Name = "ZeroIssue",
                            ReportId = appReport.ProjectId,
                            Location = "N/A",
                            LocationFull = "N/A",
                            Scanner = new SaltMiner.Core.Entities.ScannerInfo
                            {
                                AssessmentType = AssessmentType.Open.ToString("g"),
                                Product = "BlackDuck",
                                Vendor = "Synopsis"
                            },
                            Severity = "0",
                        },
                        Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                        {
                            Attributes = new Dictionary<string, string>(),
                            QueueScanId = queueScan.Id,
                            QueueAssetId = queueAsset.Id,
                            Source = new SaltMiner.Core.Entities.SourceInfo
                            {
                                Analyzer = "BlackDuck",
                            },
                        },
                        Timestamp = DateTime.UtcNow
                    }
                });
            }
            else
            {
                foreach (var component in appReport.Components)
                {
                    foreach (var vulnerability in component.Vulnerabilities)
                    {
                        if (localIssues.Any(x => x.Entity.Id == vulnerability.Id))
                        {
                            continue;
                        }

                        queueIssues.Add(new QueueIssue
                        {
                            LocalScanId = queueScan.LocalId,
                            LocalAssetId = queueAsset.LocalId,
                            Entity = new()
                            {
                                Labels = new Dictionary<string, string>(),
                                Vulnerability = new SaltMiner.Core.Entities.VulnerabilityInfo
                                {
                                    SourceSeverity = vulnerability.UseCvss3 ? vulnerability.Cvss3.Severity : vulnerability.Cvss2.Severity,
                                    Audit = new SaltMiner.Core.Entities.AuditInfo
                                    {
                                        Audited = true,
                                    },
                                    Category = new string[1] { "Application" },
                                    Enumeration = vulnerability.Source,
                                    Description = vulnerability.Summary,
                                    FoundDate = appReport.EvaluationDate.Value.ToUniversalTime(),
                                    Id = vulnerability.Id,
                                    LocationFull = "N/A",
                                    Location = "N/A",
                                    Name = vulnerability.Id,
                                    Reference = vulnerability.Id,
                                    ReportId = appReport.ProjectId,
                                    Scanner = new SaltMiner.Core.Entities.ScannerInfo
                                    {
                                        Id = vulnerability.Id,
                                        AssessmentType = AssessmentType.Open.ToString("g"),
                                        Product = "BlackDuck",
                                        Vendor = "Synopsis"
                                    },
                                    Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, vulnerability.UseCvss3 ? vulnerability.Cvss3.Severity : vulnerability.Cvss2.Severity)
                                },
                                Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                                {
                                    Attributes = new Dictionary<string, string>(),
                                    QueueScanId = queueScan.Id,
                                    QueueAssetId = queueAsset.Id,
                                    Source = new SaltMiner.Core.Entities.SourceInfo
                                    {
                                        Analyzer = "BlackDuck",
                                    }
                                },
                                Tags = Array.Empty<string>(),
                                Timestamp = DateTime.UtcNow
                            }
                        });
                    }
                }
            }

            foreach (var queueIssue in queueIssues)
            {
                if (CustomAssembly != null)
                    CustomAssembly.CustomizeQueueIssue(queueIssue, appReport);
                LocalData.AddUpdate(queueIssue); 
            }
            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"), queueIssues.Count);
        }
    }
}



