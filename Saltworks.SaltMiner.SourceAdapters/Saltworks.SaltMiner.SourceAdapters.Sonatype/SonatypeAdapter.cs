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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.SourceAdapters.Sonatype
{
    public class SonatypeAdapter : SourceAdapter
    {
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private SonatypeConfig Config;
        private readonly string AssetType = "app";

        public SonatypeAdapter(IServiceProvider provider, ILogger<SonatypeAdapter> logger) : base(provider, logger)
        {
            Logger.LogDebug("SonatypeAdapter Initialization complete.");
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            await base.RunAsync(config, token);
            try
            {
                config = config ?? throw new ArgumentNullException(nameof(config));

                if (config is not SonatypeConfig)
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(SonatypeConfig)}', but got '{config.GetType().Name}'");
                }

                Config = config as SonatypeConfig;
                CancelToken = token;
                Config.Validate();

                FirstLoadSyncUpdate(config);

                SetApiClientSslVerification(Config.VerifySsl);

                var client = new SonatypeClient(ApiClient, Config, Logger);

                if (Config.HasCustomAssembly)
                {
                    CustomAssembly = AssemblyHelper.LoadClassAssembly<ISourceAdapterCustom>(Config.CustomAssemblyName, Config.CustomAssemblyType);
                    CustomAssembly.PreProcess();
                }
                StillLoading = true;

                await Task.WhenAll(SyncAsync(client), SendAsync(Config, AssetType));

                ResetFailures(Config);
                DeleteFailures(Config);

                await Task.Delay(5, CancellationToken.None);
            }
            catch (Exception ex)
            {
                SetCancelToken();
                Logger.LogCritical(ex, "Error in RunAsync: {Error}", ex.InnerException?.Message ?? ex.Message);
                throw new SonatypeException($"Sonatype adapter failed: [{ex.GetType().Name}] {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async IAsyncEnumerable<SonatypeWorkItem> GetAsync(SonatypeClient client, IEnumerable<SourceMetric> localMetrics)
        {
            string[] sourceFilters = [];
            string fileName = "debugSourceFilters.txt";
            if (File.Exists(fileName))
            {
                sourceFilters = await File.ReadAllLinesAsync(fileName);
                Logger.LogWarning("Using {FileName} to process specific source applications only. {Count} applications found in file.", fileName, sourceFilters.Length);
            }

            // API doesn't currently support paging, so have to get all applications in one call
            Logger.LogInformation($"[Get] Getting Applications...");
            var assets = (await client.GetAppsAsync());

            if (sourceFilters.Length > 0)
            {
                var filters = new HashSet<string>(sourceFilters);
                assets.Applications = assets.Applications.Where(x => filters.Contains(x.Id) || filters.Contains(x.PublicId) || filters.Contains(x.Name)).ToList();
                Logger.LogWarning("[Get] Filter file will limit the processing to only {Count} apps", assets.Applications.Count);
            }
            if (Config.TestingAssetLimit > 0)
            {
                assets.Applications = assets.Applications.Take(Config.TestingAssetLimit).ToList();
            }

            var appTotal = assets.Applications.Count;
            Logger.LogInformation("[Get] {AppTotal} applications", appTotal);
            var counter = 0;

            // We define asset as being application + stage.  Stage must be obtained from a report, so it's possible
            // reports returned for a Sonatype application will end up being multiple assets.  Group by stage and process accordingly.
            foreach (var asset in assets.Applications)
            {
                var reportGroups = (await client.GetReportsAsync(asset))
                    .OrderByDescending(x => x.EvaluationDate.ToUniversalTime())
                    .GroupBy(x => x.Stage);

                if (reportGroups == null || !reportGroups.Any())
                    Logger.LogInformation("No reports found for application name '{Name}'.", asset.Name);

                foreach (var grp in reportGroups ?? [])
                {
                    var sourceId = MapSourceId(asset, grp.Key);
                    var localMetric = localMetrics.FirstOrDefault(x => x.SourceId == sourceId) ?? new()
                    {
                        SourceId = sourceId,
                        SourceType = Config.SourceType,
                        LastScan = null,
                        IsSaltminerSource = SonatypeConfig.IsSaltminerSource,
                        VersionId = grp.Key, // Sonatype stage
                        Attributes = []
                    };
                    yield return new SonatypeWorkItem(asset, grp.Where(x => MapScanDate(x, false) > (localMetric.LastScan ?? DateTime.MinValue)).ToList(), localMetric);
                }
                counter++;
                if (counter % 100 == 0)
                    Logger.LogInformation("[Get] {Current}/{Total} applications processed", counter, appTotal);
            }
        }

        private async Task SyncAsync(SonatypeClient client)
        {
            IEnumerable<SourceMetric> localMetrics;
            try
            {
                CheckCancel();

                if (Config.SourceType != SourceType.Sonatype.GetDescription())
                {
                    Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{Etype}' but was found to be '{Atype}'", SourceType.Sonatype.GetDescription(), Config.SourceType);
                    throw new SonatypeValidationException("Invalid configuration - source type");
                }

                var exceptionCounter = 0;
                var syncRecord = LocalData.CheckSyncRecordSourceForFailure(Config.Instance, Config.SourceType);
                if (syncRecord != null)
                {
                    RecoveryMode = true;
                }
                else
                {
                    RecoveryMode = false;
                    syncRecord = LocalData.GetSyncRecord(Config.Instance, Config.SourceType);
                    ClearQueues();
                }

                var totalSourceMetricsCount = 0;
                var newLocalIssues = 0;
                var newLocalScans = 0;
                var newLocalAssets = 0;
                Logger.LogInformation("Getting local source metrics...");
                var sw = Stopwatch.StartNew();
                localMetrics = LocalData.GetSourceMetrics(Config.Instance, Config.SourceType);
                sw.Stop();
                Logger.LogInformation("Loaded {Count} local source metrics in {Sec} ms", localMetrics.Count(), sw.ElapsedMilliseconds);


                await foreach (var wrk in GetAsync(client, localMetrics))
                {
                    try
                    {
                        CheckCancel(true);
                        await LetSendCatchUpAsync(Config);
                        var organization = await client.GetOrganizationByOrgIdAsync(wrk.Application.OrganizationId);
                        var latestReport = wrk.NewReports.OrderByDescending(x => x.EvaluationDate).FirstOrDefault();
                        totalSourceMetricsCount++;

                        var sourceMetric = latestReport?.ToSourceMetric(wrk.Application, Config);

                        if (RecoveryMode)
                        {
                            if (syncRecord.CurrentSourceId != sourceMetric.SourceId)
                            {
                                Logger.LogDebug("[Sync] Skipping source ID {Id} due to recovery mode", sourceMetric.SourceId);
                                continue;
                            }
                            else
                            {
                                RecoveryMode = false;
                            }
                        }


                        var localMetric = wrk.LocalMetric;  // created new if not found as part of GetAsync
                        localMetric.IsProcessed = true;

                        syncRecord.CurrentSourceId = localMetric.SourceId;
                        syncRecord.State = SyncState.InProgress;
                        LocalData.AddUpdate(syncRecord, true);

                        var appReports = wrk.NewReports;
                        var historyReports = appReports.Where(x => x != latestReport).ToList();
                        var application = wrk.Application;
                        List<ComponentDto> components = null;
                        components = (await client.GetAppReportComponentsAsync(application.PublicId, latestReport.ReportId)).ToList();

                        sourceMetric.IssueCount = GetTotalIssueCount(components);
                        sourceMetric.IssueCountSev1 = GetIssueSeverityCount("high", components);
                        sourceMetric.IssueCountSev2 = GetIssueSeverityCount("medium", components);
                        sourceMetric.IssueCountSev3 = GetIssueSeverityCount("low", components);

                        // Removed full sync maintenance for now, may revisit later.  Currently no benefit due to diff sync taking as long as full sync

                        // Bail out on this one if doesn't need update (and not force flag set)
                        if (!NeedsUpdate(sourceMetric, localMetric) && !ForceUpdate)
                        {
                            Logger.LogInformation("[Sync] Source ID {Id} - no update needed, {Count} application/stages processed so far.", sourceMetric.SourceId, totalSourceMetricsCount);
                            continue;
                        }
                        Logger.LogInformation("[Sync] Processing Source ID {SourceId}, {RptCount} new report(s), {Count} application/stages processed so far.", sourceMetric.SourceId, historyReports.Count, totalSourceMetricsCount);

                        var noScan = sourceMetric.LastScan == null;  // This shouldn't be possible since we require a report to build a source metric, but handle it anyway
                        QueueScan queueScan = MapScan(latestReport, components, noScan);
                        newLocalScans++;
                        QueueAsset queueAsset = MapAsset(application, organization, latestReport, queueScan, localMetric == null && !Config.DisableRetire);
                        newLocalAssets++;

                        if (queueScan.Entity.Saltminer.Internal.QueueStatus == QueueScanStatus.Cancel.ToString())
                        {
                            continue;
                        }

                        if (!noScan)
                        {
                            MapIssues(application, latestReport, components, queueScan, queueAsset);
                        }

                        newLocalIssues += queueScan.Entity.Saltminer.Internal.IssueCount;
                        UpdateLocalMetric(sourceMetric, localMetric);
                        queueScan.Loading = false;
                        LocalData.AddUpdate(queueScan);
                        RecoveryMode = false;
                    }
                    catch (LocalDataException ex)
                    {
                        var msg = ex.InnerException?.Message ?? ex.Message;
                        Logger.LogCritical(ex, "{Msg}", msg);
                        SetCancelToken();
                        throw new SonatypeException($"Local data exception: {msg}");
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message == "Not Found")
                        {
                            Logger.LogWarning(ex, "[Sync] {Instance} for {SourceType} Sync Processing Error: {ErrorMessage}", Config.Instance, Config.SourceType, ex.InnerException?.Message ?? ex.Message);
                        }
                        else
                        {
                            exceptionCounter++;
                            Logger.LogError(ex, "[Sync] {Instance} for {SourceType} Sync Processing Error {ExceptionCounter}: {ErrorMessage}", Config.Instance, Config.SourceType, exceptionCounter, ex.InnerException?.Message ?? ex.Message);
                            if (exceptionCounter == Config.SourceAbortErrorCount)
                            {
                                Logger.LogCritical(ex, "[Sync] {Instance} for {SourceType} Exceeded {SourceAbortErrorCount} Sync Processing Errors: {ErrorMessage}", Config.Instance, Config.SourceType, Config.SourceAbortErrorCount, ex.InnerException?.Message ?? ex.Message);
                                SetCancelToken();
                                break;
                            }
                        }
                    }
                    finally
                    {
                        StillLoading = false;
                    }

                    CheckCancel();
                }

                if (!Config.DisableRetire)
                {
                    try
                    {
                        RetireLocalMetrics(localMetrics.Where(x => !x.IsProcessed).ToList());
                        RetireQueueAssets(localMetrics.Where(x => !x.IsProcessed).ToList(), AssetType, Config);
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

                syncRecord.LastSync = (DateTime.UtcNow);
                syncRecord.CurrentSourceId = null;
                syncRecord.State = SyncState.Completed;
                LocalData.AddUpdate(syncRecord, true);
                LocalData.SaveAllBatches();
                await Task.Delay(5000); // make sure send notices the final save
                Logger.LogInformation("[Sync] Processing complete: SourceMetrics (Total: {Count})", totalSourceMetricsCount);
            }
            catch (Exception ex)
            {
                var msg = $"Error processing Sonatype, sync aborting: [{ex.GetType().Name}] {ex.InnerException?.Message ?? ex.Message}";
                Logger.LogError(ex, "[Sync] {Msg}", msg);
                throw new SonatypeException(msg);
            }
            finally
            {
                StillLoading = false;
            }
        }

        private static string MapSourceId(ApplicationDto app, string stage) => $"{app.Id}|{stage}";
        private static DateTime MapScanDate(Report report, bool isNoScan) => isNoScan ? DateTime.UtcNow : report.EvaluationDate.ToUniversalTime();

        private QueueScan MapScan(Report appReport, List<ComponentDto> components, bool noScan = false)
        {
            var now = DateTime.UtcNow;
            var queueScan = new QueueScan
            {
                Loading = true,
                Entity = new()
                {
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueScanInfo
                    {
                        Engagement = null,
                        Scan = new SaltMiner.Core.Entities.QueueScanInfo
                        {
                            AssessmentType = AssessmentType.Open.ToString("g"),
                            Product = "Lifecycle",
                            ReportId = noScan ? GetNoScanReportId(AssessmentType.Open.ToString("g")) : appReport?.ReportId,
                            ScanDate = MapScanDate(appReport, noScan),
                            ProductType = "Open",
                            Vendor = "Sonatype",
                            AssetType = AssetType,
                            IsSaltminerSource = SonatypeConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = noScan ? 1 : GetTotalIssueCount(components),
                            QueueStatus = QueueScanStatus.Loading.ToString("g"),
                        },
                    },
                    Timestamp = now
                },
                Timestamp = now
            };

            if (CustomAssembly != null && !noScan)
            {
                CustomAssembly.CustomizeQueueScan(queueScan, appReport);
                if (CustomAssembly.CancelScan)
                {
                    queueScan.Entity.Saltminer.Internal.QueueStatus = QueueScanStatus.Cancel.ToString();
                    LocalData.DeleteQueueScan(queueScan.Id); 
                    return queueScan;
                }
            }
            return LocalData.AddUpdate(queueScan);
        }

        private QueueAsset MapAsset(ApplicationDto application, OrganizationDto organization, Report appReport, QueueScan queueScan, bool isRetired = false)
        {
            var stage = appReport?.Stage;
            var sourceId = MapSourceId(application, stage);
            var queueAsset = new QueueAsset
            {
                Entity = new()
                {
                    Timestamp = DateTime.UtcNow,
                    Saltminer = new()
                    {
                        Internal = new()
                        {
                            QueueScanId = queueScan.Id
                        },
                        Asset = new()
                        {
                            Description = application.Name,
                            Name = application.Name,
                            Attributes = new Dictionary<string, string>
                            {
                                { "organization", organization.Name },
                                { "stage", stage }
                            },
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = SonatypeConfig.IsSaltminerSource,
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

        private void MapIssues(ApplicationDto application, Report appReport, List<ComponentDto> components, QueueScan queueScan, QueueAsset queueAsset)
        {
            List<QueueIssue> queueIssues = [];
            if (queueScan.Entity.Saltminer.Internal.IssueCount == 0)
            {
                queueIssues.Add(GetZeroQueueIssue(queueScan, queueAsset));
            }
            else
            {
                foreach (var component in components.Where(x => (x.Violations?.Count ?? 0) > 0))
                {
                    foreach (var violation in component.Violations)
                    {
                        // Only import selected types if configured
                        if (Config.VulnerabilityImportTypes.Count > 0 && !Config.VulnerabilityImportTypes.Contains(violation.PolicyThreatCategory))
                            continue;

                        var vulReportLink = $"{Config.AppReportBaseUrl}{application.Name}/{appReport.ReportId}/componentDetails/{component.Hash}/overview";
                        var location = (component.PackageUrl == "" || component.PackageUrl == null) ? "N/A" : component.PackageUrl;
                        queueIssues.Add(new QueueIssue
                        {
                           Entity = new()
                           {
                               Labels = [],
                               Vulnerability = new()
                               {
                                   Audit = new()
                                   {
                                       Audited = true,
                                   },
                                   Category = [ "Application" ],
                                   FoundDate = appReport.EvaluationDate.ToUniversalTime(),
                                   LocationFull = location,
                                   Location = location,
                                   Name = violation.GetViolationName(),
                                   ReportId = appReport.ReportId,
                                   Scanner = new()
                                   {
                                       Id = $"{violation.CompositeId}~{application.Id}~{location}",
                                       AssessmentType = AssessmentType.Open.ToString("g"),
                                       Product = "Lifecycle",
                                       Vendor = "Sonatype",
                                       GuiUrl = vulReportLink
                                   },
                                   Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, violation.PolicyName),
                                   SourceSeverity = violation.PolicyName,
                                   IsSuppressed = violation.Waived || violation.Grandfathered || violation.WaivedWithAutoWaiver
                               },
                               Saltminer = new()
                               {
                                   IssueType = queueScan.Entity.Saltminer.Scan.AssessmentType,
                                   Attributes = new Dictionary<string, string>
                                   {
                                       { "waived", violation.Waived.ToString() },
                                       { "waivedWithAutoWaiver", violation.WaivedWithAutoWaiver.ToString() },
                                       { "grandfathered", violation.Grandfathered.ToString() },
                                       { "policyType", violation.PolicyThreatCategory },
                                       { "policyThreatLevel", violation.PolicyThreatLevel.ToString() },
                                       { "policyThreatCategory", violation.PolicyThreatCategory },
                                       { "policyName", violation.PolicyName }
                                   },
                                   QueueScanId = queueScan.Id,
                                   QueueAssetId = queueAsset.Id,
                                   Source = new SaltMiner.Core.Entities.SourceInfo
                                   {
                                       Analyzer = "Sonatype",
                                   }
                               },
                               Tags = [],
                               Timestamp = DateTime.UtcNow
                           }
                        });
                    }
                }
            }

            foreach (var queueIssue in queueIssues)
            {
                CustomAssembly?.CustomizeQueueIssue(queueIssue, appReport);
                LocalData.AddUpdate(queueIssue); 
            }
        }

        private int GetTotalIssueCount(List<ComponentDto> components)
        {
            int total = 0;
            if (Config.VulnerabilityImportTypes.Count > 0)
            {
                total = components?.SelectMany(c => c?.Violations).Count(v => Config.VulnerabilityImportTypes.Contains(v.PolicyThreatCategory)) ?? 0;
            }
            else
            {
                total = components?.SelectMany(c => c?.Violations).Count() ?? 0;
            }
            return total;
        }

        private static int GetIssueSeverityCount(string severity, List<ComponentDto> components)
        {
            var issueCount = components?
                .SelectMany(c => c?.Violations)
                .Count(v => v.PolicyName.Contains(severity, StringComparison.OrdinalIgnoreCase)) ?? 0;
            return issueCount;
        }
    }
    internal class SonatypeWorkItem(ApplicationDto app, List<Report> newReports, SourceMetric localMetric)
    {
        internal ApplicationDto Application { get; set; } = app;
        internal List<Report> NewReports { get; set; } = newReports;
        internal SourceMetric LocalMetric { get; set; } = localMetric;
    }
}



