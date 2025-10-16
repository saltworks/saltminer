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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.SourceAdapters.CheckmarxSast
{
    public class CheckmarxSastAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private CheckmarxSastConfig Config;
        private readonly string AssetType = "app";

        public CheckmarxSastAdapter(IServiceProvider provider, ILogger<CheckmarxSastAdapter> logger) : base(provider, logger)
        {
            Logger.LogDebug("Initialization complete.");
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            try
            {
                config = config ?? throw new ArgumentNullException(nameof(config));

                if (config is not CheckmarxSastConfig)
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(CheckmarxSastConfig)}', but got '{config.GetType().Name}'");
                }

                Config = config as CheckmarxSastConfig;
                CancelToken = token;
                Config.Validate();

                FirstLoadSyncUpdate(config);

                if (GetFiles(Config.CxFlowFolder).Count == 0)
                {
                    Logger.LogWarning("No JSON CxFlow files dated for today {Now:yyyyMMdd} in {Folder}", DateTime.UtcNow, Config.CxFlowFolder);
                    return;
                }

                if (Config.HasCustomAssembly)
                {
                    CustomAssembly = AssemblyHelper.LoadClassAssembly<ISourceAdapterCustom>(Config.CustomAssemblyName, Config.CustomAssemblyType);
                    CustomAssembly.PreProcess();
                }

                StillLoading = true;

                var reports = ParseFiles(Config.CxFlowFolder);

                if (Config.TestingAssetLimit > 0)
                    reports = reports.Take(Config.TestingAssetLimit).ToList();

                await Task.WhenAll(SyncAsync(reports), SendAsync(Config, AssetType));

                ResetFailures(Config);
                DeleteFailures(Config);

                if (Config.DeleteFileWhenDone)
                    DeleteFiles(Config.CxFlowFolder);

                await Task.Delay(5, CancellationToken.None);
            }
            catch (Exception ex)
            {
                var msg = $"General failure in source adapter: {ex.InnerException?.Message ?? ex.Message}";
                Logger.LogCritical(ex, "{Msg}", msg);
                throw new CheckmarxSastException(msg, ex);
            }
        }

        private async Task SyncAsync(List<ReportDto> reports, CancellationToken cancel = default)
        {
            try
            {
                await Task.Delay(1, cancel);

                if (Config.SourceType != SourceType.CheckmarxSast.GetDescription())
                {
                    Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{Etype}' but was found to be '{Atype}'", SourceType.CheckmarxSast.GetDescription(), Config.SourceType);
                    throw new CheckmarxSastValidationException("Invalid configuration - source type");
                }

                var exceptionCounter = 0;
                var sourceMetrics = reports.Select(x => x.GetSourceMetric(Config)).ToList();
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

                for (var i = 0; i < sourceMetrics.Count; i++)
                {
                    var metric = sourceMetrics[i];

                    try
                    {
                        if (RecoveryMode)
                        {
                            if (SyncRecord.CurrentSourceId != metric.SourceId)
                                continue;
                            else
                                RecoveryMode = false;
                        }
                        else
                        {
                            SyncRecord = LocalData.GetSyncRecord(Config.Instance, Config.SourceType);
                        }

                        SyncInProgress(SyncRecord, metric.SourceId);
                        Logger.LogInformation("[Sync] {SourceType} {Instance}, Src ID '{SrcId}'", Config.SourceType, Config.Instance, metric.SourceId);

                        var report = reports.First(x => x.AdditionalDetails.ScanId.ToString() == metric.SourceId);
                        QueueScan queueScan = MapScan(report);
                        newLocalScans++;
                        QueueAsset queueAsset = MapAsset(report, queueScan);
                        newLocalAssets++;

                        if (queueScan.Entity.Saltminer.Internal.QueueStatus == QueueScanStatus.Cancel.ToString())
                            continue;

                        MapIssues(report, queueScan, queueAsset);
                        CheckCancel(true);
                        queueScan.Loading = false;
                        LocalData.AddUpdate(queueScan);
                        RecoveryMode = false;
                        newLocalIssues += queueScan.Entity.Saltminer.Internal.IssueCount;
                    }
                    catch (LocalDataException ex)
                    {
                        var msg = $"Local data exception: {ex.InnerException?.Message ?? ex.Message}";
                        Logger.LogCritical(ex, "{Msg}", msg);
                        throw new CheckmarxSastException(msg, ex);
                    }
                    catch (Exception ex)
                    {
                        exceptionCounter++;
                        var msg = $"{Config.SourceType}_{Config.Instance} Sync Processing Error {exceptionCounter}: {ex.InnerException?.Message ?? ex.Message}";
                        Logger.LogWarning(ex, "[Sync] {Msg}", msg);

                        if (exceptionCounter == Config.SourceAbortErrorCount)
                        {
                            msg = $"{Config.SourceType}_{Config.Instance} Exceeded {Config.SourceAbortErrorCount} Sync Processing Errors: {exceptionCounter}: {ex.InnerException?.Message ?? ex.Message}";
                            Logger.LogCritical(ex, "[Sync] {Msg}", msg);
                            break;
                        }
                    }
                    CheckCancel(true);
                }
                SyncComplete(SyncRecord);
            }
            catch (Exception ex)
            {
                var msg = $"{Config.SourceType}_{Config.Instance} general sync error: {ex.InnerException?.Message ?? ex.Message}";
                Logger.LogCritical(ex, "[Sync] {Msg}", msg);
            }
            finally
            {
                StillLoading = false;
            }
        }

        private QueueScan MapScan(ReportDto appReport)
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
                            Product = "Checkmarx SAST",
                            ReportId = appReport.ProjectId,
                            ScanDate = DateTime.Parse(appReport.AdditionalDetails.ScanStartDate, new CultureInfo("en-US")).AddMilliseconds(1).ToUniversalTime(),
                            ProductType = "SAST",
                            Vendor = "Checkmarx",
                            AssetType = AssetType,
                            IsSaltminerSource = CheckmarxSastConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = appReport.XIssues.Count,
                            QueueStatus = QueueScanStatus.Loading.ToString("g"),
                        },
                    },
                    Timestamp = now
                }, Timestamp = now
            };

            if (CustomAssembly != null)
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

        private QueueAsset MapAsset(ReportDto appReport, QueueScan queueScan)
        {
            var sourceId = appReport.ProjectId;
            var queueAsset = new QueueAsset
            {
                Entity = new()
                {
                    Timestamp = DateTime.UtcNow,
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueAssetInfo
                    {
                        Asset = new SaltMiner.Core.Entities.AssetInfoPolicy
                        {
                            Description = appReport.Project,
                            Name = appReport.Project,
                            Attributes = appReport.AdditionalDetails.CustomFields,
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = CheckmarxSastConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = sourceId,
                            AssetType = AssetType,
                            LastScanDaysPolicy = Config.LastScanDaysPolicy
                        },
                        Internal = new SaltMiner.Core.Entities.QueueAssetInternal
                        {
                            QueueScanId = queueScan?.Id
                        }
                    }
                }
            };
            return LocalData.AddUpdate(queueAsset);
        }

        private void MapIssues(ReportDto appReport, QueueScan queueScan, QueueAsset queueAsset)
        {
            if (appReport.XIssues.Count == 0)
            { 
                LocalData.AddUpdate(GetZeroQueueIssue(queueScan, queueAsset));
                return;
            }
            else
            {
                foreach (var issue in appReport.XIssues)
                {
                    var qIssue = new QueueIssue
                    {
                        QueueScanId = queueScan.Id,
                        QueueAssetId = queueAsset.Id,
                        Entity = new()
                        {
                            Labels = [],
                            Vulnerability = new SaltMiner.Core.Entities.VulnerabilityInfo
                            {
                                Audit = new SaltMiner.Core.Entities.AuditInfo
                                {
                                    Audited = true,
                                },
                                Category =[ "Application" ],
                                Description = issue.Description, //not avaiable unless following link
                                Classification = issue.Link,
                                FoundDate = DateTime.Parse(appReport.AdditionalDetails.ScanStartDate, new CultureInfo("en-us")).AddMilliseconds(1).ToUniversalTime(),
                                Id = [ issue.Link ],
                                LocationFull = (issue.Filename == "" || issue.Filename == null) ? "N/A" : issue.Filename,
                                Location = (issue.Filename == "" || issue.Filename == null) ? "N/A" : issue.Filename,
                                Name = issue.Vulnerability,
                                Reference = issue.Link,
                                ReportId = appReport.AdditionalDetails.ScanId,
                                Scanner = new SaltMiner.Core.Entities.ScannerInfo
                                {
                                    ApiUrl = issue.Link,
                                    Id = $"{issue?.SimilarityId ?? ""}|{issue?.Vulnerability ?? ""}|{issue?.VulnerabilityStatus ?? ""}",
                                    AssessmentType = AssessmentType.SAST.ToString("g"),
                                    Product = "Checkmarx SAST",
                                    Vendor = "Checkmarx"
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
                                    Analyzer = "Checkmarx SAST",
                                }
                            },
                            Tags = [],
                            Timestamp = DateTime.UtcNow
                        }
                    };
                    CustomAssembly?.CustomizeQueueIssue(qIssue, appReport);
                    LocalData.AddUpdate(qIssue);
                }
            }
        }

        private static List<ReportDto> ParseFiles(string folderPath)
        {
            var files = GetFiles(folderPath);
            var results = new List<ReportDto>();
            foreach (var file in files)
            {
                results.Add(JsonSerializer.Deserialize<ReportDto>(File.ReadAllText(file), JsonSerializerOptions.Web));
            }
            return results;
        }

        private static void DeleteFiles(string folderPath)
        {
            var files = GetFiles(folderPath);
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        private static List<string> GetFiles(string folderPath)
        {
            return Directory.GetFiles(folderPath).Where(x => x.Contains(DateTime.UtcNow.ToString("yyyyMMdd"))).ToList();
        }
    }
}



