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
using System;
using System.Collections.Generic;
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
        private readonly ProgressHelper ProgressHelper;

        public CheckmarxSastAdapter(DataClientFactory<DataClient.DataClient> dataFactory, ApiClientFactory<SourceAdapter> clientFactory, IServiceProvider provider, ILogger<CheckmarxSastAdapter> logger) : base(dataFactory, clientFactory, provider, logger)
        {
            Logger.LogDebug("Initialization complete.");
            ProgressHelper = new ProgressHelper(Logger);
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

                ProgressHelper.StartTimer(GetProgressKey(Config.Instance, config.SourceType, AssetType, null, "RunAsync"));

                FirstLoadSyncUpdate(config);

                if (!GetFiles(Config.CxFlowFolder).Any())
                {
                    Logger.LogWarning($"No JSON CxFlow files dated for today {DateTime.UtcNow.ToString("yyyyMMdd")} in {Config.CxFlowFolder}");
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
                {
                    reports = reports.Take(Config.TestingAssetLimit).ToList();
                }

                await Task.WhenAll(SyncAsync(reports), SendAsync(ProgressHelper, Config, AssetType));

                ResetFailures(Config);
                DeleteFailures(Config);

                if (Config.DeleteFileWhenDone)
                {
                    DeleteFiles(Config.CxFlowFolder);
                }

                await Task.Delay(5, CancellationToken.None);
                ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, null, "RunAsync"), 1);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex.InnerException?.Message ?? ex.Message, ex);
                throw;
            }
        }

        // TODO: if possible, add async file reading and await that.  Otherwise, add an await Task.Delay(1)
        private async Task SyncAsync(List<ReportDTO> reports)
        {
            CheckCancel();

            if (Config.SourceType != SourceType.CheckmarxSast.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{etype}' but was found to be '{atype}'", SourceType.CheckmarxSast.GetDescription(), Config.SourceType);
                throw new CheckmarxSastValidationException("Invalid configuration - source type");
            }

            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, "", "SyncAsync"));

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
                        }
                    }
                    else
                    {
                        SyncRecord = LocalData.GetSyncRecord(Config.Instance, Config.SourceType);
                    }

                    SyncRecord.CurrentSourceId = metric.SourceId;
                    SyncRecord.State = SyncState.InProgress;
                    LocalData.AddUpdate(SyncRecord);

                    Logger.LogInformation($"[Sync] Updating Config.Instance '{Config.Instance}', Config.SourceType '{Config.SourceType}', SourceId '{metric.SourceId}', AssetType '{AssetType}'");

                    var report = reports.First(x => x.AdditionalDetails.ScanId.ToString() == metric.SourceId);
                    QueueScan queueScan = MapScan(report);
                    newLocalScans++;

                    QueueAsset queueAsset = MapAsset(report, queueScan);
                    newLocalAssets++;

                    if (queueScan.Entity.Saltminer.Internal.QueueStatus == QueueScanStatus.Cancel.ToString())
                    {
                        continue;
                    }

                    MapIssues(report, queueScan, queueAsset);

                    CheckCancel(false);

                    queueScan.Loading = false;
                    LocalData.AddUpdate(queueScan);

                    RecoveryMode = false;

                    newLocalIssues = newLocalIssues + queueScan.Entity.Saltminer.Internal.IssueCount;
                    ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"), queueScan.Entity.Saltminer.Internal.IssueCount, null);
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

            StillLoading = false;

            SyncRecord.LastSync = DateTime.UtcNow;
            SyncRecord.CurrentSourceId = null;
            SyncRecord.State = SyncState.Completed;
            LocalData.AddUpdate(SyncRecord);

            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, "", "SyncAsync"), 0, $"Local QueueScans: {newLocalScans}; QueueAssets: {newLocalAssets}; QueueIssues: {newLocalIssues}");
        }

        private QueueScan MapScan(ReportDTO appReport)
        {
            var sourceId = appReport.AdditionalDetails.ScanId.ToString();

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
                            AssessmentType = AssessmentType.SAST.ToString("g"),
                            Product = "Checkmarx SAST",
                            ReportId = appReport.ProjectId,
                            ScanDate = DateTime.Parse(appReport.AdditionalDetails.ScanStartDate).AddMilliseconds(1).ToUniversalTime(),
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
                    LocalData.DeleteQueueScan(queueScan.LocalId); 
                    ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapScan"), 0, "CustomAssembly requested cancellation for scan");
                    return queueScan;
                }
            }

            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapScan"), 1);
            return LocalData.AddUpdate(queueScan);
        }

        private QueueAsset MapAsset(ReportDTO appReport, QueueScan queueScan)
        {
            var sourceId = appReport.AdditionalDetails.ScanId.ToString();

            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapAsset"));

            var queueAsset = new QueueAsset
            {
                LocalScanId = queueScan.LocalId,
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

            var result = LocalData.GetQueueAsset(Config.SourceType, sourceId) ?? LocalData.AddUpdate(queueAsset);
            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapAsset"), 1);
            return result;
        }

        private void MapIssues(ReportDTO appReport, QueueScan queueScan, QueueAsset queueAsset)
        {
            var sourceId = appReport.AdditionalDetails.ScanId.ToString();
            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"));
            List<QueueIssue> queueIssues = new();
            var localIssues = LocalData.GetQueueIssues(queueScan.LocalId, queueAsset.LocalId).ToList();
            if (queueScan.Entity.Saltminer.Internal.IssueCount == localIssues.Count)
            {
                ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"), 0, "No Issues to Map");
                return;
            }

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
                            FoundDate = DateTime.Parse(appReport.AdditionalDetails.ScanStartDate).AddMilliseconds(1).ToUniversalTime(),
                            Name = "ZeroIssue",
                            ReportId = sourceId,
                            Location = "N/A",
                            LocationFull = "N/A",
                            Scanner = new SaltMiner.Core.Entities.ScannerInfo
                            {
                                AssessmentType = AssessmentType.SAST.ToString("g"),
                                Product = "Checkmarx SAST",
                                Vendor = "Checkmarx"
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
                                Analyzer = "Checkmarx SAST",
                            }
                        },
                        Tags = Array.Empty<string>(),
                        Timestamp = DateTime.UtcNow
                    }
                });
            }
            else
            {
                foreach (var issue in appReport.XIssues)
                {
                    if (localIssues.Any(x => x.Entity.Vulnerability.Id == issue.Link))
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
                                Audit = new SaltMiner.Core.Entities.AuditInfo
                                {
                                    Audited = true,
                                },
                                Category = new string[1] { "Application" },
                                Description = issue.Description, //not avaiable unless following link
                                Classification = issue.Link,
                                FoundDate = DateTime.Parse(appReport.AdditionalDetails.ScanStartDate).AddMilliseconds(1).ToUniversalTime(),
                                Id = issue.Link,
                                LocationFull = (issue.Filename == "" || issue.Filename == null) ? "N/A" : issue.Filename,
                                Location = (issue.Filename == "" || issue.Filename == null) ? "N/A" : issue.Filename,
                                Name = issue.Vulnerability,
                                Reference = issue.Link,
                                ReportId = sourceId,
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
                                Attributes = new Dictionary<string, string>(),
                                QueueScanId = queueScan.Id,
                                QueueAssetId = queueAsset.Id,
                                Source = new SaltMiner.Core.Entities.SourceInfo
                                {
                                    Analyzer = "Checkmarx SAST",
                                }
                            },
                            Tags = Array.Empty<string>(),
                            Timestamp = DateTime.UtcNow
                        }
                    });
                }
            }

            localIssues.AddRange(queueIssues);

            foreach (var queueIssue in localIssues)
            {
                if (CustomAssembly != null)
                    CustomAssembly.CustomizeQueueIssue(queueIssue, appReport);
                LocalData.AddUpdate(queueIssue); 
            }

            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"), queueIssues.Count);
        }

        private List<ReportDTO> ParseFiles(string folderPath)
        {
            var files = GetFiles(folderPath);
            var results = new List<ReportDTO>();
            foreach (var file in files)
            {
                results.Add(JsonSerializer.Deserialize<ReportDTO>(File.ReadAllText(file), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }));
            }
            return results;
        }

        private void DeleteFiles(string folderPath)
        {
            var files = GetFiles(folderPath);
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        private List<string> GetFiles(string folderPath)
        {
            return Directory.GetFiles(folderPath).Where(x => x.Contains(DateTime.UtcNow.ToString("yyyyMMdd"))).ToList();
        }
    }
}



