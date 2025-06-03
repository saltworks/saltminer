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
using Saltworks.Utility.ApiHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.SourceAdapters.Sonatype
{
    public class SonatypeAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
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
                Logger.LogCritical(ex, "Error in RunAsync: {Error}", ex.InnerException?.Message ?? ex.Message);
                throw;
            }
        }

        private async Task SyncAsync(SonatypeClient client)
        {
            try
            {
                CheckCancel();

                if (Config.SourceType != SourceType.Sonatype.GetDescription())
                {
                    Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{Etype}' but was found to be '{Atype}'", SourceType.Sonatype.GetDescription(), Config.SourceType);
                    throw new SonatypeValidationException("Invalid configuration - source type");
                }

                var exceptionCounter = 0;
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

                OrganizationDto Organization = new();

                string[] sourceFilters = [];
                string fileName = "debugSourceFilters.txt";
                if (File.Exists(fileName))
                {
                    Logger.LogWarning("Using {FileName} to process specific source applications only", fileName);
                    sourceFilters = await File.ReadAllLinesAsync(fileName);
                }

                //Design decision: expectations to handle 10k applications
                Logger.LogInformation($"[Sync] Getting Applications...");
                var assets = (await client.GetAppsAsync());

                if (Config.TestingAssetLimit > 0)
                {
                    assets.Applications = assets.Applications.Take(Config.TestingAssetLimit).ToList();
                }
                else if (sourceFilters.Length > 0)
                {
                    var filters = new HashSet<string>(sourceFilters);
                    assets.Applications = assets.Applications.Where(x => filters.Contains(x.Id)).ToList();
                    Logger.LogWarning("A filter file will limit the processing to only {Count} apps", assets.Applications.Count);
                }

                var appTotal = assets.Applications.Count;

                Logger.LogInformation("[Sync] Received {AppTotal} applications, starting SourceMetrics", appTotal);

                var localMetrics = LocalData.GetSourceMetrics(Config.Instance, Config.SourceType).ToList();
                var sourceMetrics = new List<SourceMetric>();
                var totalSourceMetricsCount = 0;
                var appCount = 1;
                var newLocalIssues = 0;
                var newLocalScans = 0;
                var newLocalAssets = 0;

                foreach (var app in GetAssetApplications(assets.Applications))
                {
                    try
                    {
                        sourceMetrics = new List<SourceMetric>();

                        var appMetrics = await client.SourceMetricsAsync(app, Config);
                        var metricList = appMetrics.Results.ToList();
                        if (string.IsNullOrEmpty(metricList?.FirstOrDefault()?.VersionId))
                        {
                            Logger.LogInformation("[Sync] No Source Metric(s) for application {AppId}, creating NULL record. {AppCount} of {AppTotal}.", app.Id, appCount, appTotal);
                        }
                        else
                        {
                            Logger.LogInformation("[Sync] Found {MetricListCount} Source Metric(s) for application {AppId}. {AppCount} of {AppTotal}", metricList.Count, app.Id, appCount, appTotal);
                        }
                        sourceMetrics.AddRange(metricList);
                        Organization = await client.GetOrganizationByOrgIdAsync(app.OrganizationId);
                        appCount++;
                        CheckCancel();
                    }
                    catch (ApiClientException apiEx)
                    {
                        exceptionCounter++;

                        Logger.LogError(apiEx, "[Sync] Api Error for {Uri}. Message: {ErrorMessage}. Response: {ResponseContent}", apiEx.RequestUri, apiEx.InnerException?.Message ?? apiEx.Message, apiEx.ResponseContent);

                        if (exceptionCounter == Config.SourceAbortErrorCount)
                        {
                            Logger.LogCritical(apiEx, "[Sync] {Instance} for {SourceType} Exceeded {AbortErrorCount} Sync Processing Errors: {Message}", Config.Instance, Config.SourceType, Config.SourceAbortErrorCount, apiEx.InnerException?.Message ?? apiEx.Message);
                            StillLoading = false;
                            SetCancelToken();
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptionCounter++;

                        Logger.LogError(ex, "[Sync] {Instance} for {SourceType} Sync Processing Error {Counter}: {Message}", Config.Instance, Config.SourceType, exceptionCounter, ex.InnerException?.Message ?? ex.Message);

                        if (exceptionCounter == Config.SourceAbortErrorCount)
                        {
                            Logger.LogCritical(ex, "[Sync] {Instance} for {SourceType} Exceeded {AbortErrorCount} Sync Processing Errors: {Message}", Config.Instance, Config.SourceType, Config.SourceAbortErrorCount, ex.InnerException?.Message ?? ex.Message);
                            StillLoading = false;
                            SetCancelToken();
                            break;
                        }
                    }

                    var sourceCount = 1;

                    // each asset application metric can have 1 or more versions
                    // so we itereate thru those here to process using a source Id of app id and version name
                    for (var i = 0; i < sourceMetrics.Count; i++)
                    {
                        CheckCancel();
                        totalSourceMetricsCount++;

                        var metric = sourceMetrics[i];
                        Logger.LogInformation("[Sync] Processing Source Metric(s) for Source ID {SourceId}. {SourceCount} of {SourceMetricsCount}", metric.SourceId, sourceCount, sourceMetrics.Count);

                        try
                        {
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
                            LocalData.AddUpdate(SyncRecord, true);

                            var localMetric = localMetrics.FirstOrDefault(x => x.SourceId == metric.SourceId);
                            if (localMetric != null)
                            {
                                localMetric.IsProcessed = true;
                            }

                            var indexSplit = metric.SourceId.IndexOf("|");
                            var appId = metric.SourceId.Substring(0, indexSplit);
                            var stage = metric.SourceId.Substring(indexSplit + 1);

                            var appReports = (await client.GetAppReportsAsync(appId, stage)).ToList();
                            var historyReports = appReports.Where(x => (SyncRecord.LastSync == null || x.EvaluationDate.ToUniversalTime() > SyncRecord.LastSync) && x.EvaluationDate.ToUniversalTime() < appReports.FirstOrDefault().EvaluationDate.ToUniversalTime()).ToList();

                            Logger.LogInformation("[Sync] Received {AppReportsCount} Reports and {HistoryReportsCount} History Reports", appReports.Count, historyReports.Count);

                            var appResult = await ApiClient.GetAsync<ApplicationDto>($"applications/{appId}");
                            var application = appResult.Content;
                            var report = appReports.FirstOrDefault();
                            List<ComponentDto> components = null;

                            if (stage != string.Empty)
                            {
                                components = (await client.GetAppReportComponentsAsync(application.PublicId, report.ReportId)).ToList();

                                metric.IssueCount = GetTotalIssueCount(components);
                                metric.IssueCountSev1 = GetIssueSeverityCount("high", components);
                                metric.IssueCountSev2 = GetIssueSeverityCount("medium", components);
                                metric.IssueCountSev3 = GetIssueSeverityCount("low", components);
                            }

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

                            if (NeedsUpdate(metric, localMetric) || RecoveryMode)
                            {
                                Logger.LogInformation("[Sync] Updating Config.Instance '{Instance}', SourceType {SourceType}, SourceId '{MetricSourceId}', AssetType '{AssetType}'", Config.Instance, Config.SourceType, metric.SourceId, AssetType);

                                var noScan = metric.LastScan == null;
                                QueueScan queueScan = MapScan(application, report, components, historyReports, noScan);
                                newLocalScans++;

                                QueueAsset queueAsset = MapAsset(application, Organization, report, queueScan, localMetric == null && localMetrics.Count > 0 && !Config.DisableRetire);
                                newLocalAssets++;

                                if (queueScan.Entity.Saltminer.Internal.QueueStatus == QueueScanStatus.Cancel.ToString())
                                {
                                    continue;
                                }

                                if (!noScan)
                                {
                                    MapIssues(application, report, components, queueScan, queueAsset);
                                }

                                newLocalIssues = newLocalIssues + queueScan.Entity.Saltminer.Internal.IssueCount;
                                UpdateLocalMetric(metric, localMetric);
                                queueScan.Loading = false;
                                LocalData.AddUpdate(queueScan);
                                CheckCancel(true);
                                await LetSendCatchUpAsync(Config);
                                RecoveryMode = false;
                            }
                        }
                        catch (LocalDataException ex)
                        {
                            Logger.LogCritical(ex, "{Msg}", ex.InnerException?.Message ?? ex.Message);
                            StillLoading = false;
                            SetCancelToken();
                            throw;
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
                                    StillLoading = false;
                                    SetCancelToken();
                                    break;
                                }
                            }
                        }

                        CheckCancel();
                        sourceCount++;
                        await LetSendCatchUpAsync(Config);
                    }
                }

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
                LocalData.AddUpdate(SyncRecord, true);
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

        static IEnumerable<ApplicationDto> GetAssetApplications(List<ApplicationDto> applications)
        {
            foreach (var app in applications)
            {
                if (app != null)
                    yield return app;
            }
        }

        private QueueScan MapScan(ApplicationDto application, Report appReport, List<ComponentDto> components, List<Report> historyReports, bool noScan = false)
        {
            var stage = appReport?.Stage;
            var sourceId = $"{application.Id}|{stage}";
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
                            ScanDate = noScan ? now : appReport.EvaluationDate.ToUniversalTime(),
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
            var sourceId = $"{application.Id}|{stage}";
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
                        if (Config.VulnerabilityImportTypes.Count > 0 && !Config.VulnerabilityImportTypes.Contains(violation.PolicyThreatCategory))
                        {
                            continue;
                        }

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
                                   Category = new string[1] { "Application" },
                                   FoundDate = appReport.EvaluationDate.ToUniversalTime(),
                                   LocationFull = location,
                                   Location = location,
                                   Name = violation.Constraints[0]?.Conditions[0]?.ConditionReason ?? "N/A",
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
                                   IsSuppressed = IsSuppressed(violation)
                               },
                               Saltminer = new()
                               {
                                   IssueType = queueScan.Entity.Saltminer.Scan.AssessmentType,
                                   Attributes = new Dictionary<string, string>
                                   {
                                       {"waived", violation.Waived.ToString() },
                                       {"grandfathered", violation.Grandfathered.ToString() },
                                       {"policyType", violation.PolicyThreatCategory.ToString() }

                                   },
                                   QueueScanId = queueScan.Id,
                                   QueueAssetId = queueAsset.Id,
                                   Source = new SaltMiner.Core.Entities.SourceInfo
                                   {
                                       Analyzer = "Sonatype",
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
                CustomAssembly?.CustomizeQueueIssue(queueIssue, appReport);
                LocalData.AddUpdate(queueIssue); 
            }
        }

        private int GetTotalIssueCount(List<ComponentDto> components)
        {
            int total = 0;
            if (Config.VulnerabilityImportTypes.Count > 0)
            {
                total = components?.SelectMany(c => c?.Violations).Count(v => Config.VulnerabilityImportTypes.Contains(v.PolicyThreatCategory)) ?? 0; ;
            }
            else
            {
                total = components?.SelectMany(c => c?.Violations).Count() ?? 0;
            }
            return total;
        }

        private static int GetIssueSeverityCount(string severity, List<ComponentDto> components)
        {
            var issueCount = components?.SelectMany(c => c?.Violations).Count(v => v.PolicyName.ToLower().Contains(severity)) ?? 0;
            return issueCount;
        }

        private static bool IsSuppressed(ViolationDto violation)
        {
            if (violation.Waived || violation.Grandfathered) return true;
            return false;
        }
    }
}



