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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.SourceAdapters.Burp
{
    public class BurpAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private BurpConfig Config;
        private readonly string AssetType = "app";
        private readonly ProgressHelper ProgressHelper;

        public BurpAdapter(DataClientFactory<DataClient.DataClient> dataFactory, ApiClientFactory<SourceAdapter> clientFactory, IServiceProvider provider, ILogger<BurpAdapter> logger) : base(dataFactory, clientFactory, provider, logger)
        {
            Logger.LogDebug("BurpAdapter Initialization complete.");
            ProgressHelper = new ProgressHelper(Logger);
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {

            try
            {
                config = config ?? throw new ArgumentNullException(nameof(config));

                if (!(config is BurpConfig))
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(BurpConfig)}', but got '{config.GetType().Name}'");
                }

                Config = config as BurpConfig;
                CancelToken = token;
                Config.Validate();

                ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, null, "RunAsync"));

                FirstLoadSyncUpdate(config);

                SetApiClientSslVerification(Config.VerifySsl);

                if (Config.HasCustomAssembly)
                {
                    CustomAssembly = AssemblyHelper.LoadClassAssembly<ISourceAdapterCustom>(Config.CustomAssemblyName, Config.CustomAssemblyType);
                    CustomAssembly.PreProcess();
                }

                var client = new BurpClient(ApiClient, Config, Logger);

                StillLoading = true;

                var reports = ParseFiles(client);

                if (Config.TestingAssetLimit > 0)
                {
                    reports = reports.Take(Config.TestingAssetLimit).ToList();
                }

                await Task.WhenAll(SyncAsync(reports), SendAsync(ProgressHelper, Config, AssetType));

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

        private async Task SyncAsync(List<Report> reports)
        {
            CheckCancel();

            if (Config.SourceType != SourceType.Burp.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{etype}' but was found to be '{atype}'", SourceType.Burp.GetDescription(), Config.SourceType);
                throw new BurpValidationException("Invalid configuration - source type");
            }

            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, "", "SyncAsync"));

            var exceptionCounter = 0;

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

            for (var i = 0; i < reports.Count; i++)
            {
                var report = reports[i];

                try
                {
                    ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, report.SourceId, "SyncAsync"));

                    if (RecoveryMode)
                    {
                        if (SyncRecord.CurrentSourceId != report.SourceId)
                        {
                            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, report.SourceId, "SyncAsync"), 0, "Skipped due to recovery");
                            continue;
                        }
                        else
                        {
                            RecoveryMode = false;
                        }
                    }

                    SyncRecord.CurrentSourceId = report.SourceId;
                    SyncRecord.State = SyncState.InProgress;
                    LocalData.AddUpdate(SyncRecord);

                    Logger.LogInformation($"[Sync] Updating Config.Instance '{Config.Instance}', SourceType {Config.SourceType}, SourceId '{report.SourceId}', AssetType '{AssetType}'");

                    if (report.LastScan != null)
                    {
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

                        newLocalIssues = newLocalIssues + queueScan.Entity.Saltminer.Internal.IssueCount;
                        ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, report.SourceId, "SyncAsync"), queueScan.Entity.Saltminer.Internal.IssueCount, null);
                    }
                    else
                    {
                        QueueAsset queueAsset = MapAsset(report, null);
                        ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, report.SourceId, "SyncAsync"), 0, null);
                    }

                    RecoveryMode = false;
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

                CheckCancel();
            }

            StillLoading = false;

            SyncRecord.LastSync = (DateTime.UtcNow);
            SyncRecord.CurrentSourceId = null;
            SyncRecord.State = SyncState.Completed;
            LocalData.AddUpdate(SyncRecord);

            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, "", "SyncAsync"), 0, $"Local QueueScans: {newLocalScans}; QueueAssets: {newLocalAssets}; QueueIssues: {newLocalIssues}");
        }

        private QueueScan MapScan(Report report)
        {
            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, report.SourceId, "MapScan"));
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
                            Product = "Burp Suite",
                            ReportId = report.Host,
                            ScanDate = report.LastScan.Value,
                            ProductType = "DAST",
                            Vendor = "PortSwigger",
                            AssetType = AssetType,
                            IsSaltminerSource = BurpConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = report.Issues.Count,
                            QueueStatus = QueueScanStatus.Loading.ToString("g"),
                        },
                    },
                    Timestamp = now
                },
                Timestamp = now
            };

            if (CustomAssembly != null)
            {
                CustomAssembly.CustomizeQueueScan(queueScan, report);
                if (CustomAssembly.CancelScan)
                {
                    queueScan.Entity.Saltminer.Internal.QueueStatus = QueueScanStatus.Cancel.ToString();

                    LocalData.DeleteQueueScan(queueScan.LocalId); 

                    ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, report.SourceId, "MapScan"), 0, "CustomAssembnly CancelScan Set");

                    return queueScan;
                }
            }

            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, report.SourceId, "MapScan"), 1);

            return LocalData.AddUpdate(queueScan);
        }

        private QueueAsset MapAsset(Report report, QueueScan queueScan)
        {
            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, report.SourceId, "MapAsset"));

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
                            Description = report.Host,
                            Name = report.Host,
                            Attributes = new Dictionary<string, string>(),
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = BurpConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = report.SourceId,
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

            var result = LocalData.GetQueueAsset(Config.SourceType, report.SourceId) ?? LocalData.AddUpdate(queueAsset);
            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, report.SourceId, "MapAsset"), 1);
            return result;
        }

        private void MapIssues(Report report, QueueScan queueScan, QueueAsset queueAsset)
        {
            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, report.SourceId, "MapIssues"));
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
                            FoundDate = report.LastScan,
                            Name = "ZeroIssue",
                            ReportId = report.Host,
                            Location = "N/A",
                            LocationFull = "N/A",
                            Scanner = new SaltMiner.Core.Entities.ScannerInfo
                            {
                                AssessmentType = AssessmentType.DAST.ToString("g"),
                                Product = "Burp Suite",
                                Vendor = "PortSwigger"
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
                                Analyzer = "Burp Suite",
                            }
                        },
                        Timestamp = DateTime.UtcNow
                    }
                });
            }
            else
            {
                foreach (var issue in report.Issues)
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
                                FoundDate = report.LastScan,
                                LocationFull = issue.Location,
                                Location = issue.Location,
                                Name = issue.Name,
                                ReportId = report.Host,
                                Scanner = new SaltMiner.Core.Entities.ScannerInfo
                                {
                                    Id = issue.SerialNumber,
                                    AssessmentType = AssessmentType.DAST.ToString("g"),
                                    Product = "Burp Suite",
                                    Vendor = "PortSwigger"
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
                                    Analyzer = "Burp Suite",
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
                    CustomAssembly.CustomizeQueueIssue(queueIssue, report);
                LocalData.AddUpdate(queueIssue); 
            }
            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, report.SourceId, "MapIssues"), queueIssues.Count);
        }

        private List<Report> ParseFiles(BurpClient client)
        {
            var files = GetFiles(Config.FileFolder);
            var results = new List<Report>();
            foreach (var file in files)
            {
                try
                {
                    var report = new Report(Config);
                    report.Issues.AddRange(client.GetIssues(file));
                    results.Add(report);

                    if (Config.DeleteFileWhenDone)
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error in Parsing files.");
                    throw;
                }
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
            return Directory.GetFiles(folderPath).ToList();
        }
    }
}



