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


namespace Saltworks.SaltMiner.SourceAdapters.Debricked
{
    public class DebrickedAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private DebrickedConfig Config;
        private readonly string AssetType = "app";
        private readonly ApiClient BearerClient;
        private readonly ProgressHelper ProgressHelper;

        public DebrickedAdapter(DataClientFactory<DataClient.DataClient> dataFactory, ApiClientFactory<SourceAdapter> clientFactory, IServiceProvider provider, ILogger<DebrickedAdapter> logger) : base(dataFactory, clientFactory, provider, logger)
        {
            Logger.LogDebug("DebrickedAdapter Initialization complete.");
            BearerClient = clientFactory.CreateApiClient();
            ProgressHelper = new ProgressHelper(Logger);
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {

            try
            {
                #region Include: Get Config and Validate

                config = config ?? throw new ArgumentNullException(nameof(config));

                if (!(config is DebrickedConfig))
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(DebrickedConfig)}', but got '{config.GetType().Name}'");
                }

                Config = config as DebrickedConfig;
                CancelToken = token;
                Config.Validate();

                #endregion

                ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, null, "RunAsync"));

                //Include: Check local metrics to make sure there is data, if not run FirstLoad
                FirstLoadSyncUpdate(config);
                SetApiClientSslVerification(Config.VerifySsl);

                ApiClient.Options.CamelCaseJsonOutput = true;
                var client = new DebrickedClient(ApiClient, BearerClient, Config, Logger);

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
                ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, null, "RunAsync"), 1);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Unexpected failure in Debricked source adapter: [{type}] {err}", ex.GetType().Name, ex.InnerException?.Message ?? ex.Message);
                throw;
            }
        }

        private async Task SyncAsync(DebrickedClient client)
        {
            CheckCancel();

            if (Config.SourceType != SourceType.Debricked.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{etype}' but was found to be '{atype}'", SourceType.Debricked.GetDescription(), Config.SourceType);
                throw new Exception("Invalid configuration - source type");
            }

            //Write: Start Timer for Sync
            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, "", "SyncAsync"));

            var exceptionCounter = 0;

            var request = new SbomRequest
            {
                Vulnerabilities = true,
                SendEmail = false,
                Locale = "en"
            };

            Logger.LogInformation($"Getting Components...");
            var sbom = new SbomReportDto();
            int reportRetry = 0;
            var report = await client.GenerateSbom(request);
            if (report.ReportUuid != null)
            {
                do
                {
                    reportRetry++;
                    Logger.LogInformation("[Sync] Generating SBOM report. {reportRetry} attempt(s).", reportRetry);
                    // delay for sbom to be generated
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                    sbom = client.GetSbomReport(report.ReportUuid);
                } while (sbom.BomFormat == null && reportRetry < 3);
            }

            if (sbom.BomFormat == null)
            {
                Logger.LogInformation("[Sync] Could not generate the SBOM report after 3 attempts.");
                return;
            }

            Logger.LogInformation("[Sync] Received {componentCount} total components", sbom?.Components.Count ?? 0);
            Logger.LogInformation("[Sync] Received {vulCount} total vulnerabilities", sbom?.Vulnerabilities.Count ?? 0);
            Logger.LogInformation("[Sync] Getting SourceMetrics...");

            // Write: Gather SourceMetrics from client
            //Use Config.TestingAssetLimit to limit the assets if > 0(set)
            //Design decision: expectations to handle 10k assets
            var sourceMetrics = client.GetSourceMetrics(sbom ?? new SbomReportDto(), Config).Results.ToList();
  
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

                //Include: Clear any leftover queue data from previous run
                ClearQueues();
            }

            for (var i = 0; i < sourceMetrics.Count; i++)
            {
                var metric = sourceMetrics[i];

                try
                {
                    //Write: Start time for individual sourcemetric
                    ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"));

                    //If Recoverymode loop through until you are on that sourcemetric
                    if (RecoveryMode)
                    {
                        if (SyncRecord.CurrentSourceId != metric.SourceId)
                        {
                            //Write: End Timer for each skipped metric until recovery record hit
                            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"), 0, "Skipped due to recovery");
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

                    var component = sbom?.Components.First(x => x.BomRef == metric.SourceId);
                    var issues = sbom?.Vulnerabilities.Where(x => x.Affects.Any(y => y.Ref == component?.BomRef)).ToList();

                    LoadIssueSeverityCounts(metric, issues);

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

                    // no scan dates for this source - skipping?
                    var skipUpdateChecks = new List<NeedsUpdateEnum> { NeedsUpdateEnum.LastScan };

                    //Include:  Get all data needed to determine if local metric and source metric match
                    if (NeedsUpdate(metric, localMetric, Config.LogNeedsUpdate, skipUpdateChecks) || RecoveryMode)
                    {
                        Logger.LogInformation($"[Sync] Updating Config.Instance '{Config.Instance}', SourceType {Config.SourceType}, SourceId '{metric.SourceId}', AssetType '{AssetType}'");

                        //If source has a master list of assets, then we can check if there is a new scan for each asset
                        //in this case if there is no scan for this asset, then just map a asset
                        if (metric.LastScan != null)
                        {
                            //Write: Map scan and add to localData, including if there are reports that are from the last scan
                            var queueScan = MapScan(component, issues);
                            newLocalScans++;
                            //Write: Map asset and add to localData, including data from the scan
                            QueueAsset queueAsset = MapAsset(component, queueScan);
                            newLocalAssets++;
                            //Write: Map issues and add to localData, including data from the scan and asset
                            MapIssues(component, issues, queueScan, queueAsset);
                            CheckCancel(false);

                            //Include: Update Scan with loading = false in localData
                            queueScan.Loading = false;
                            LocalData.AddUpdate(queueScan);

                            //Write: End time for individual sourcemetric
                            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"), queueScan.Entity.Saltminer.Internal.IssueCount, null);
                        }
                        else
                        {
                            //Write:Map asset and add to localData, without data from scan

                            //Write: End time for individual sourcemetric
                            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"), 0, null);

                        }

                        //Include: Update Local metric with source metric, so always current
                        UpdateLocalMetric(metric, localMetric);
                        //Include: Set recoverymode to false, for the next iteration
                        RecoveryMode = false;
                       
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

                        Logger.LogWarning(ex, $"[Sync] {Config.Instance} for {Config.SourceType} Sync Processing Error {exceptionCounter}: {ex.InnerException?.Message ?? ex.Message}");

                        if (exceptionCounter == Config.SourceAbortErrorCount)
                        {
                            Logger.LogCritical(ex, $"[Sync] {Config.Instance} for {Config.SourceType} Exceeded {Config.SourceAbortErrorCount} Sync Processing Errors: {ex.InnerException?.Message ?? ex.Message}");

                            StillLoading = false;

                            break;
                        }
                    }
                }
            }

            CheckCancel();

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

            //Include: indicate you are done with this source metric
            SyncRecord.LastSync = (DateTime.UtcNow);
            SyncRecord.CurrentSourceId = null;
            SyncRecord.State = SyncState.Completed;
            LocalData.AddUpdate(SyncRecord);

            //Write: End Timer for entrie sync process
            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, "", "SyncAsync"));
        }


        //Not all sources allow for history reports
        private QueueScan MapScan(ReportComponentDto component, List<ReportVulnerabilityDto> issues)
        {
            var sourceId = component.BomRef;

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
                            Product = "Debricked",
                            ReportId = sourceId,
                            ScanDate = now.ToUniversalTime(),
                            ProductType = "Open",
                            Vendor = "Debricked",
                            AssetType = AssetType,
                            IsSaltminerSource = DebrickedConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = issues.Count,
                            QueueStatus = QueueScanStatus.Loading.ToString("g"),
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
                    queueScan.Entity.Saltminer.Internal.QueueStatus = QueueScanStatus.Cancel.ToString("g");
                    LocalData.DeleteQueueScan(queueScan.LocalId);
                    ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapScan"), 0, "CustomAssembnly CancelScan Set");
                    return queueScan;
                }
            }

            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapScan"), 1);

            return LocalData.AddUpdate(queueScan);
        }

        private QueueAsset MapAsset(ReportComponentDto component, QueueScan queueScan, bool isRetired = false)
        {
            var sourceId = component.BomRef;

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
                            Description = component.Name,
                            Name = $"{component.Name}",
                            VersionId = component.BomRef,
                            Version = component.Version,
                            Attributes = new Dictionary<string, string>(),
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = DebrickedConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = sourceId,
                            AssetType = AssetType,
                            LastScanDaysPolicy = Config.LastScanDaysPolicy,
                            IsRetired = isRetired
                        }
                    }
                }
            };

            var result = LocalData.AddUpdate(queueAsset);
            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapAsset"), 1);
            return result;
        }

        private void MapIssues(ReportComponentDto component, List<ReportVulnerabilityDto> issues, QueueScan queueScan, QueueAsset queueAsset)
        {
            var sourceId = component.BomRef;

            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"));

            List<QueueIssue> queueIssues = new();

            var localIssues = LocalData.GetQueueIssues(queueScan.LocalId, queueAsset.LocalId);

            // Design decision: if zero issues, send in a zero record, even if one may already exist.  Trust the Manager to prevent duplicates.
            // Use project alerts to determine if issues exist, not queueScan
            if (!issues.Any())
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
                            Description = "",
                            Name = "ZeroIssue",
                            Location = "N/A",
                            LocationFull = "N/A",
                            Scanner = new SaltMiner.Core.Entities.ScannerInfo
                            {
                                AssessmentType = AssessmentType.Open.ToString("g"),
                                Product = SourceType.Debricked.ToString("g"),
                                Vendor = "Debricked",
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
                                Analyzer = "Debricked"
                            }
                        },
                        Tags = Array.Empty<string>(),
                        Timestamp = DateTime.UtcNow
                    }
                });
            }
            else
            {

                foreach (var issue in issues)
                {
                    if (localIssues.Any(x => x.Id == $"{component.BomRef}-{issue.Id}"))
                    {
                        // Skip if already exists
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
                                Audit = new SaltMiner.Core.Entities.AuditInfo
                                {
                                    Audited = true,
                                },
                                Category = new string[1] { "Application" },
                                Description = issue.Description,
                                FoundDate = issue.Published.ToUniversalTime(),
                                Id = $"{component.BomRef}-{issue.Id}",
                                LocationFull = issue.Source.Url,
                                Location = issue.Source.Url,
                                Name = issue.Id,
                                ReportId = queueScan.Entity.Saltminer.Scan.ReportId,
                                Scanner = new SaltMiner.Core.Entities.ScannerInfo
                                {
                                    Id = $"{component.BomRef}-{issue.Id}",
                                    AssessmentType = AssessmentType.Open.ToString("g"),
                                    Product = SourceType.Debricked.ToString("g"),
                                    Vendor = "Debricked"
                                },
                                Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, issue.Ratings.Where(x => x.Method == "CVSSv3").Select(z => z.Severity).ToString() ?? ""),
                                SourceSeverity = issue.Ratings.Where(x => x.Method == "CVSSv3").Select(z => z.Severity).ToString() ?? ""
                            },
                            Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                            {
                                Attributes = new Dictionary<string, string>(),
                                QueueScanId = queueScan.Id,
                                QueueAssetId = queueAsset.Id,
                                Source = new SaltMiner.Core.Entities.SourceInfo
                                {
                                    Analyzer = "Debricked"
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
                LocalData.AddUpdate(queueIssue);
            }
            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"), queueIssues.Count);
        }

        private void LoadIssueSeverityCounts(SourceMetric metric, List<ReportVulnerabilityDto> issues)
        {
            foreach (var issue in issues)
            {
                // CVSSv3 is the latest measurement of vul threats and is recommended over CVSSv2
                // It appears that Debricked reporting does have CVSSv2 as well. Not sure which to use?
                foreach (var rating in issue.Ratings.Where(x => x.Method == "CVSSv3"))
                {
                    metric.IssueCountSev1 += GetIssueSeverityCount("critical", rating.Severity ?? "");
                    metric.IssueCountSev1 += GetIssueSeverityCount("high", rating.Severity ?? "");
                    metric.IssueCountSev2 += GetIssueSeverityCount("medium", rating.Severity ?? "");
                    metric.IssueCountSev3 += GetIssueSeverityCount("low", rating.Severity ?? "");
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
    }
}



