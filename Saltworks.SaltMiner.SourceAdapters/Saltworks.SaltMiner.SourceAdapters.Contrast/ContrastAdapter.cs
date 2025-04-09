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
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.SourceAdapters.Contrast
{
    public class ContrastAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private ContrastConfig Config;
        private readonly string AssetType = "app";
        private readonly ProgressHelper ProgressHelper;

        public ContrastAdapter(DataClientFactory<DataClient.DataClient> dataFactory, ApiClientFactory<SourceAdapter> clientFactory, IServiceProvider provider, ILogger<ContrastAdapter> logger) : base(dataFactory, clientFactory, provider, logger)
        {
            Logger.LogDebug("ContrastAdapter Initialization complete.");
            ProgressHelper = new ProgressHelper(Logger);
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            try
            {
                config = config ?? throw new ArgumentNullException(nameof(config));

                if (!(config is ContrastConfig))
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(ContrastConfig)}', but got '{config.GetType().Name}'");
                }

                Config = config as ContrastConfig;
                CancelToken = token;
                Config.Validate();

                ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, null, "RunAsync"));

                FirstLoadSyncUpdate(config);
               
                SetApiClientSslVerification(Config.VerifySsl);

                var client = new ContrastClient(ApiClient, Config, Logger);

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

        private async Task SyncAsync(ContrastClient client)
        {
            CheckCancel();

            if (Config.SourceType != SourceType.Contrast.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{etype}' but was found to be '{atype}'", SourceType.Contrast.GetDescription(), Config.SourceType);
                throw new ContrastValidationException("Invalid configuration - source type");
            }

            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, "", "SyncAsync"));

            var exceptionCounter = 0;
            //Design decision: expectations to handle 10k applications
            Logger.LogInformation($"[Sync] Getting Organizations...");
            var orgs = (await client.GetOrgsAsync());
            Logger.LogInformation($"[Sync] Received {orgs.Count()} organizations");

            Logger.LogInformation($"Getting LocalMetrics...");
            var localMetrics = LocalData.GetSourceMetrics(Config.Instance, Config.SourceType).ToList();

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

            var localScans = 0;
            var localAssets = 0;
            var localIssues = 0;

            foreach (var org in orgs)
            {
                Logger.LogInformation($"[Sync] Getting Applications for Organization ID {org.OrganizationUuid}...");
                var appCount = 0;
                var processedApps = 0;
                var date = DateTime.UtcNow;
                var apps = (await client.GetApplicationsAsync(org.OrganizationUuid, appCount));
                Logger.LogInformation($"[Sync] Received {apps.Count()} applications ");
                
                while (apps.Any()) 
                {
                    foreach (var app in apps.Where(x => x.IsPrimary))
                    {
                        if (Config.SkipSourceIds.Count > 0 && Config.SkipSourceIds.Contains(app.AppId))
                        {
                            Logger.LogInformation("Source Id {sid} is being skipped and will not process.", app.AppId);
                            continue;
                        }

                        ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, app.AppId, "SyncAsync"));

                        Logger.LogInformation($"Getting SourceMetric for Application ID {app.AppId}...");
                        var issueCount = await client.GetTraceCountsAsync(org.OrganizationUuid, app.AppId);
                        var metric = GetSourceMetric(app, issueCount);

                        try
                        {

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

                            //var indexSplit = metric.SourceId.IndexOf("|");
                            //var appId = metric.SourceId.Substring(0, indexSplit);
                            //var stage = metric.SourceId.Substring(indexSplit + 1);

                            CheckCancel(false);

                            if (NeedsUpdate(metric, localMetric, Config.LogNeedsUpdate, new List<NeedsUpdateEnum> { NeedsUpdateEnum.LastScan }) || RecoveryMode)
                            {
                                Logger.LogInformation($"[Sync] Updating Config.Instance '{Config.Instance}', SourceType {Config.SourceType}, SourceId '{metric.SourceId}', AssetType '{AssetType}'");

                                var traceCount = 0;
                                var appTraces = await client.GetTracesAsync(org.OrganizationUuid, app.AppId, traceCount);
                                var traces = new List<TraceResourceDTO>();

                                while (appTraces.Any())
                                {
                                    traces.AddRange(appTraces);
                                    Logger.LogInformation($"Traces running count {traces.Count} for app {app.Name}");
                                    traceCount = traceCount + appTraces.Count();
                                    try
                                    {
                                        
                                        appTraces = await client.GetTracesAsync(org.OrganizationUuid, app.AppId, traceCount);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.LogError($"Error getting traces: {ex.Message}", ex);
                                    }
                                }

                                if (metric.LastScan != null)
                                {
                                    QueueScan queueScan = MapScan(app, traces);
                                    localScans++;

                                    QueueAsset queueAsset = MapAsset(app, queueScan, localMetric == null && localMetrics.Count > 0 && !Config.DisableRetire);
                                    localAssets++;

                                    if (queueScan.Entity.Saltminer.Internal.QueueStatus == QueueScanStatus.Cancel.ToString())
                                    {
                                        continue;
                                    }

                                    MapIssues(app, traces, queueScan, queueAsset);

                                    CheckCancel(false);

                                    localIssues = localIssues + queueScan.Entity.Saltminer.Internal.IssueCount;

                                    queueScan.Loading = false;
                                    LocalData.AddUpdate(queueScan);

                                    ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"), queueScan.Entity.Saltminer.Internal.IssueCount, null);
                                }
                                else
                                {
                                    MapAsset(app, null, localMetric == null && localMetrics.Count > 0 && !Config.DisableRetire);
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
                            if (ex.Message == "Not Found")
                            {
                                Logger.LogWarning(ex, $"[Sync] {Config.Instance} for {Config.SourceType} Sync Processing Error: {ex.InnerException?.Message ?? ex.Message}");
                            }
                            else
                            {
                                exceptionCounter++;

                                Logger.LogError(ex, $"[Sync] {Config.Instance} for {Config.SourceType} Sync Processing Error {exceptionCounter}: {ex.InnerException?.Message ?? ex.Message}");

                                if (exceptionCounter == Config.SourceAbortErrorCount)
                                {
                                    Logger.LogCritical(ex, $"[Sync] {Config.Instance} for {Config.SourceType} Exceeded {Config.SourceAbortErrorCount} Sync Processing Errors: {ex.InnerException?.Message ?? ex.Message}");

                                    StillLoading = false;

                                    break;
                                }
                            }
                        }

                        processedApps++;
                    }
                    appCount = appCount + apps.Count();
                    apps = (await client.GetApplicationsAsync(org.OrganizationUuid, appCount));
                    if (apps.Any())
                    {
                        Logger.LogInformation($"[Sync] Received {apps.Count()} more applications ");
                    }
                    CheckCancel();
                }

                Logger.LogInformation($"[Sync] Processed { processedApps } applications");
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

            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, "", "SyncAsync"), 0, $"Local QueueScans: {localScans}; QueueAssets: {localAssets}; QueueIssues: {localIssues}");
        }

        private QueueScan MapScan(ApplicationResourceDTO app, List<TraceResourceDTO> traces)
        {
            var sourceId = app.AppId;

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
                            AssessmentType = AssessmentType.DAST.ToString("g"),
                            Product = "Contrast",
                            ReportId = sourceId,
                            ScanDate = ConvertDateTime(app.LastSeen)?.ToUniversalTime() ?? DateTime.UtcNow,
                            ProductType = "DAST",
                            Vendor = "Contrast Security",
                            AssetType = AssetType,
                            IsSaltminerSource = ContrastConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = traces.Count,
                            QueueStatus = QueueScanStatus.Loading.ToString("g"),
                        },
                    },
                    Timestamp = now
                },
                Timestamp = now
            };

            if (CustomAssembly != null)
            {
                CustomAssembly.CustomizeQueueScan(queueScan, app);
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

        private QueueAsset MapAsset(ApplicationResourceDTO app, QueueScan queueScan, bool isRetired = false)
        {
            var sourceId = app.AppId;
            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapAsset"));

            var queueAsset = new QueueAsset
            {
                LocalScanId = queueScan?.LocalId,
                Entity = new()
                {
                    Timestamp = DateTime.UtcNow,
                    //ExtraData = JsonConvert.SerializeObject(new ExtraAssetData
                    //{
                    //    ApplicationTags = application.ApplicationTags
                    //}),
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueAssetInfo
                    {
                        Asset = new SaltMiner.Core.Entities.AssetInfoPolicy
                        {
                            Description = app.Name,
                            Name = app.Name,
                            //todo: Add is Primary
                            Attributes = new Dictionary<string, string>(),
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = ContrastConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = sourceId,
                            AssetType = AssetType,
                            LastScanDaysPolicy = Config.LastScanDaysPolicy,
                            IsRetired = isRetired
                        }
                    }
                }
            };

            var result = LocalData.GetQueueAsset(Config.SourceType, sourceId) ?? LocalData.AddUpdate(queueAsset);
            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapAsset"), 1);
            return result;
        }

        private void MapIssues(ApplicationResourceDTO app, List<TraceResourceDTO> traces, QueueScan queueScan, QueueAsset queueAsset)
        {
            var sourceId = app.AppId;
            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"));
            List<QueueIssue> queueIssues = new();

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
                            Category = new string[1] { "Application" },
                            FoundDate = ConvertDateTime(app.LastSeen)?.ToUniversalTime() ?? DateTime.UtcNow, // check for orginal found or null
                            Name = "ZeroIssue",
                            ReportId = sourceId,
                            Location = "N/A",
                            LocationFull = "N/A",
                            Scanner = new SaltMiner.Core.Entities.ScannerInfo
                            {
                                Id = GetZeroScannerId(Config.SourceType, sourceId),
                                AssessmentType = AssessmentType.DAST.ToString("g"),
                                Product = "Contrast",
                                Vendor = "Contrast Security"
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
                                Analyzer = Config.Instance,
                            }
                        },
                        Tags = Array.Empty<string>(),
                        Timestamp = DateTime.UtcNow
                    }
                });
            }
            else
            {
               foreach (var trace in traces)
                {
                    if (Config.VulnerabilityImportStatus.Count() > 0 && !Config.VulnerabilityImportStatus.Contains(trace.Status))
                    {
                        //Skip if status is not configured for import
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
                                IsSuppressed = IsSuppressed(trace.Status),
                                RemovedDate = isRemoved(trace.Status, app),
                                Audit = new SaltMiner.Core.Entities.AuditInfo
                                {
                                    Audited = true,
                                },
                                Category = new string[1] { "Application" },
                                Description = trace.SubTitle,
                                FoundDate = ConvertDateTime(trace.FirstTimeSeen)?.ToUniversalTime() ?? DateTime.UtcNow,
                                Name = trace.Title,
                                Location = "N/A",
                                LocationFull = "N/A",
                                ReportId = sourceId,
                                Scanner = new SaltMiner.Core.Entities.ScannerInfo
                                {
                                    Id = trace.Uuid,
                                    AssessmentType = AssessmentType.DAST.ToString("g"),
                                    Product = "Contrast",
                                    Vendor = "Contrast Security"
                                },
                                Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, trace.Severity),
                                SourceSeverity = trace.Severity
                            },
                            Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                            {
                                //todo: Add sub application
                                Attributes = new Dictionary<string, string> { { "status", trace.Status } },
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
                    });
                }
            }


            foreach (var queueIssue in queueIssues)
            {
                if (CustomAssembly != null)
                    CustomAssembly.CustomizeQueueIssue(queueIssue, traces);
                LocalData.AddUpdate(queueIssue); 
            }
            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"), queueIssues.Count);
        }

        private static DateTime? ConvertDateTime(long? datetime)
        {
            if(datetime == null)
                return null;
            var start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return start.AddMilliseconds(datetime.Value).ToLocalTime();
        }

        private bool IsSuppressed(string status)
        {
            return status.ToLower().Contains("not a problem");
        }

        private DateTime? isRemoved(string status, ApplicationResourceDTO app)
        {
            var statusLower = status.ToLower();
            if (statusLower.Contains("remediated") || statusLower.Contains("fixed"))
            {
                // calculated scan date ?? not sure if this is correct
                return ConvertDateTime(app.LastSeen)?.ToUniversalTime() ?? DateTime.UtcNow;
            }

            return null;
        }

        private SourceMetric GetSourceMetric(ApplicationResourceDTO app, TraceBreakdownResourceDTO traceCount)
        {
            return new SourceMetric
            {
                LastScan = ConvertDateTime(app.LastSeen)?.ToUniversalTime() ?? DateTime.UtcNow,
                Instance = Config.Instance,
                IsSaltminerSource = ContrastConfig.IsSaltminerSource,
                SourceType = Config.SourceType,
                SourceId = app.AppId,
                VersionId = "",
                Attributes = new Dictionary<string, string>(),
                IssueCount = traceCount.Criticals + traceCount.Highs + traceCount.Meds + traceCount.Lows + traceCount.Notes,
                IssueCountSev1 = traceCount.Highs,
                IssueCountSev2 = traceCount.Meds,
                IssueCountSev3 = traceCount.Lows
            };
        }
    }
}



