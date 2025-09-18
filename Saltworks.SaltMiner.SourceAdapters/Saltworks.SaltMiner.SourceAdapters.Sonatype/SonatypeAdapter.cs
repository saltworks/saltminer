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
            var apps = (await client.GetAllApplicationsAsync());

            if (sourceFilters.Length > 0)
            {
                var filters = new HashSet<string>(sourceFilters);
                apps = apps.Where(x => filters.Contains(x.Id) || filters.Contains(x.PublicId) || filters.Contains(x.Name)).ToList();
                Logger.LogWarning("[Get] Filter file will limit apps processed to a max of {Count}", filters.Count);
            }
            if (Config.TestingAssetLimit > 0)
            {
                apps = apps.Take(Config.TestingAssetLimit).ToList();
            }

            var appTotal = apps.Count();
            Logger.LogInformation("[Get] {AppTotal} applications", appTotal);
            var counter = 0;

            // We define asset as being application + stage.  Stage must be obtained from a report, so it's possible
            // reports returned for a Sonatype application will end up being multiple apps.  Group by stage and process accordingly.
            foreach (var asset in apps)
            {
                var reportGroups = (await client.GetReportsAsync(asset))
                    .OrderByDescending(x => x.EvaluationDate)
                    .GroupBy(x => x.Stage);

                if (reportGroups == null || !reportGroups.Any())
                    Logger.LogInformation("No reports found for application name '{Name}'.", asset.Name);

                foreach (var grp in reportGroups ?? [])
                {
                    var sourceId = Application.GetSourceId(asset, grp.Key);
                    var localMetric = localMetrics.FirstOrDefault(x => x.SourceId == sourceId);
                    var reports = grp.Where(x => MapScanDate(x, false) > (localMetric?.LastScan ?? DateTime.MinValue)).ToList();
                    if (reports.Count == 0)
                        reports.Add(grp.First()); // must include latest report even if no reports are new
                    localMetric ??= reports[0].ToSourceMetric(asset, Config);
                    yield return new SonatypeWorkItem(asset, reports, localMetric);
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

                        // Set sync record to show this source ID in progress
                        SyncInProgress(syncRecord, sourceMetric.SourceId);

                        var appReports = wrk.NewReports;
                        var historyReports = appReports.Where(x => x != latestReport).ToList();
                        var application = wrk.Application;
                        List<Component> components = null;
                        components = (await client.GetAppReportComponentsAsync(application.PublicId, latestReport.ReportId)).ToList();

                        sourceMetric.IssueCount = GetTotalIssueCount(components);
                        sourceMetric.IssueCountSev1 = GetIssueSeverityCount("high", components);
                        sourceMetric.IssueCountSev2 = GetIssueSeverityCount("medium", components);
                        sourceMetric.IssueCountSev3 = GetIssueSeverityCount("low", components);

                        // Removed full sync maintenance for now, may revisit later.  Currently no benefit due to diff sync taking as long as full sync

                        // Bail out on this one if doesn't need update (and not force flag set)
                        if (!NeedsUpdate(sourceMetric, localMetric) && !ForceUpdate)
                        {
                            Logger.LogInformation("[Sync] App {App}, stage {Stage} - no update needed, {Count} application/stages processed so far.", application.Name, latestReport.Stage, totalSourceMetricsCount);
                            continue;
                        }
                        Logger.LogInformation("[Sync] Processing app {App}, stage {Stage}, {RptCount} new report(s), {Count} application/stages processed so far.", application.Name, latestReport.Stage, historyReports.Count + 1, totalSourceMetricsCount);

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

                // This allows us to track the failure on trying to load any queuescan and reset to load agin until a configureable failure count is hit
                ResetFailures(Config);
                // This deletes any queuescans that hit that configurable failure count
                DeleteFailures(Config);
                SyncComplete(syncRecord);
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

        private DateTime? MapScanDate(Report report, bool isNoScan) => isNoScan ? DateTime.UtcNow : FixTimezone(report.EvaluationDate);

        private QueueScan MapScan(Report appReport, List<Component> components, bool noScan = false)
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
                            ScanDate = MapScanDate(appReport, noScan).Value,
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

        private QueueAsset MapAsset(Application application, Organization organization, Report appReport, QueueScan queueScan, bool isRetired = false)
        {
            var stage = appReport?.Stage;
            var sourceId = Application.GetSourceId(application, stage);
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
                            IsProduction = stage == Stages.Release,
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

        private void MapIssues(Application application, Report appReport, List<Component> components, QueueScan queueScan, QueueAsset queueAsset)
        {
            CheckCancel();
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
                                   FoundDate = FixTimezone(appReport.EvaluationDate).Value,
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

        private int GetTotalIssueCount(List<Component> components)
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

        private static int GetIssueSeverityCount(string severity, List<Component> components)
        {
            var issueCount = components?
                .SelectMany(c => c?.Violations)
                .Count(v => v.PolicyName.Contains(severity, StringComparison.OrdinalIgnoreCase)) ?? 0;
            return issueCount;
        }
    }
    internal class SonatypeWorkItem(Application app, List<Report> newReports, SourceMetric localMetric)
    {
        internal Application Application { get; set; } = app;
        internal List<Report> NewReports { get; set; } = newReports;
        internal SourceMetric LocalMetric { get; set; } = localMetric;
    }
}



