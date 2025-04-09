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
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.SourceAdapters.WebInspect
{
    public class WebInspectAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private WebInspectConfig Config;
        private readonly string AssetType = "app";
        private readonly ProgressHelper ProgressHelper;

        public WebInspectAdapter(DataClientFactory<DataClient.DataClient> dataFactory, ApiClientFactory<SourceAdapter> clientFactory, IServiceProvider provider, ILogger<WebInspectAdapter> logger) : base(dataFactory, clientFactory, provider, logger)
        {
            Logger.LogDebug("WebInspectAdapter Initialization complete.");
            ProgressHelper = new ProgressHelper(Logger);
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            try
            {
                config = config ?? throw new ArgumentNullException(nameof(config));

                if (config is not WebInspectConfig)
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(WebInspectConfig)}', but got '{config.GetType().Name}'");
                }

                Config = config as WebInspectConfig;
                CancelToken = token;
                Config.Validate();

                ProgressHelper.StartTimer(GetProgressKey(Config.Instance, config.SourceType, AssetType, null, "RunAsync"));

                FirstLoadSyncUpdate(config);

                if (!Directory.Exists(Config.ZipFolder))
                {
                    Logger.LogError("No directory called {zipFolder} that is defined in the source config could be found. Create a directory with that name and put the Web Inspect FPR files there to process.", Config.ZipFolder);
                    Thread.Sleep(5000);
                    return;
                }

                if (!GetFiles(Config.ZipFolder).Any())
                {
                    Logger.LogWarning("No WebInspect files in {zipfolder}", Config.ZipFolder);
                    if (SyncRecord != null)
                    {
                        LocalData.Delete<SyncRecord>(SyncRecord.LocalId);
                    }
                    Thread.Sleep(5000);
                    return;
                }

                if (Config.HasCustomAssembly)
                {
                    CustomAssembly = AssemblyHelper.LoadClassAssembly<ISourceAdapterCustom>(Config.CustomAssemblyName, Config.CustomAssemblyType);
                    CustomAssembly.PreProcess();
                }

                var client = new WebInspectClient(ApiClient, Config, Logger);

                StillLoading = true;

                await Task.WhenAll(SyncAsync(client), SendAsync(ProgressHelper, Config, AssetType));

                ResetFailures(Config);
                DeleteFailures(Config);

                if (Config.DeleteFileWhenDone)
                {
                    DeleteFiles(Config.ZipFolder);
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
        private async Task SyncAsync(WebInspectClient client)
        {
            CheckCancel();

            if (Config.SourceType != SourceType.WebInspect.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{etype}' but was found to be '{atype}'", SourceType.WebInspect.GetDescription(), Config.SourceType);
                throw new WebInspectValidationException("Invalid configuration - source type");
            }

            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, "", "SyncAsync"));

            var exceptionCounter = 0;

            var zips = GetFiles(Config.ZipFolder);
            var fprZips = zips.Where(x => x.ToLower().Contains(".fpr"));

            if (Config.TestingAssetLimit > 0)
            {
                fprZips = fprZips.Take(Config.TestingAssetLimit).ToList();
            }

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

            foreach (var zip in fprZips)
            {
                var i = 0;
                var report = ParseFile(zip, client);

                var totalIssueCount = GetIssueCount(report.Sessions);
                report.SourceMetric.IssueCount = totalIssueCount;

                Logger.LogInformation("Getting SourceMetrics for {zip}", zip);
                Logger.LogInformation("Received {issueCount} issues", totalIssueCount);

                var metric = report.SourceMetric;

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

                    var localMetric = localMetrics.FirstOrDefault(x => x.SourceId == metric.SourceId);
                    if (localMetric != null)
                    {
                        localMetric.IsProcessed = true;
                    }

                    if (Config.FullSyncMaintEnabled)
                    {
                        FullSyncBatchProcess(SyncRecord, Config.FullSyncBatchSize);
                        // End of metrics and full sync processing is still true,
                        // means last batch did not meet batch size... reset source ID to cycle through again
                        // End of metrics and batch count is zero,
                        // means nothing was found to process (ie. maybe a source metric doesn't exist anymore)...reset source ID
                        if (i + 1 == fprZips.Count() && (FullSyncProcessing || FullSyncBatchCount == 0))
                        {
                            SyncRecord.FullSyncSourceId = "";
                        }
                    }

                    if (NeedsUpdate(metric, localMetric, Config.LogNeedsUpdate) || RecoveryMode)
                    {
                        Logger.LogInformation($"[Sync] Updating Config.Instance '{Config.Instance}', Config.SourceType '{Config.SourceType}', SourceId '{metric.SourceId}', AssetType '{AssetType}'");

                        var emptySession = report.Sessions.First(x => string.IsNullOrEmpty(x.Url));
                        
                        QueueScan queueScan = MapScan(metric.SourceId, emptySession, report.Sessions);
                        newLocalScans++;

                        QueueAsset queueAsset = MapAsset(metric.SourceId, report.Sessions[1], queueScan, localMetric == null && localMetrics.Count > 0 && !Config.DisableRetire);
                        newLocalAssets++;

                        if (queueScan.Entity.Saltminer.Internal.QueueStatus == QueueScanStatus.Cancel.ToString())
                        {
                            continue;
                        }

                        MapIssues(metric.SourceId, emptySession, report.Sessions, queueScan, queueAsset);

                        CheckCancel(false);

                        queueScan.Loading = false;
                        LocalData.AddUpdate(queueScan);
                        UpdateLocalMetric(metric, localMetric);

                        RecoveryMode = false;

                        newLocalIssues = newLocalIssues + queueScan.Entity.Saltminer.Internal.IssueCount;
                        ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"), queueScan.Entity.Saltminer.Internal.IssueCount, null);
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

                    SetCancelToken();

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

                        SetCancelToken();

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

        private QueueScan MapScan(string sourceId, SessionDTO emptySession, List<SessionDTO> sessions)
        {
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
                            Product = "WebInspect",
                            ReportId = emptySession.RequestId,
                            ScanDate = DateTime.Parse(emptySession.Response.Headers.First().Value).ToUniversalTime(),
                            ProductType = "DAST",
                            Vendor = "Fortify",
                            AssetType = AssetType,
                            IsSaltminerSource = WebInspectConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = GetIssueCount(sessions),
                            QueueStatus = QueueScanStatus.Loading.ToString("g"),
                        },
                    },
                    Timestamp = now
                },
                Timestamp = now
            };

            if (CustomAssembly != null)
            {
                CustomAssembly.CustomizeQueueScan(queueScan, emptySession);
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
        private QueueAsset MapAsset(string sourceId, SessionDTO session, QueueScan queueScan, bool isRetired = false)
        {
            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapAsset"));

            var queueAsset = new QueueAsset
            {
                LocalScanId = queueScan?.LocalId,
                Entity =new()
                {
                    Timestamp = DateTime.UtcNow,
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueAssetInfo
                    {
                        Asset = new SaltMiner.Core.Entities.AssetInfoPolicy
                        {
                            Description = session.Host,
                            Name = session.Host,
                            Attributes = new Dictionary<string, string>(),
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = WebInspectConfig.IsSaltminerSource,
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

        private void MapIssues(string SourceId, SessionDTO emptySession, List<SessionDTO> sessions, QueueScan queueScan, QueueAsset queueAsset)
        {
            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, queueAsset.Entity.Saltminer.Asset.SourceId, "MapIssues"));
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
                            FoundDate = queueScan.Entity.Saltminer.Scan.ScanDate,
                            Name = "ZeroIssue",
                            ReportId = queueScan.Entity.Saltminer.Scan.ReportId,
                            Location = "N/A",
                            LocationFull = "N/A",
                            Scanner = new SaltMiner.Core.Entities.ScannerInfo
                            {
                                Id = GetZeroScannerId(Config.SourceType, SourceId),
                                AssessmentType = AssessmentType.DAST.ToString("g"),
                                Product = "WebInspect",
                                Vendor = "Fortify"
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
                                Analyzer = "WebInspect",
                            }
                        },
                        Tags = Array.Empty<string>(),
                        Timestamp = DateTime.UtcNow
                    }
                });
            }
            else
            {
                foreach (var issue in sessions.SelectMany(x => x.Issues))
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
                                Classification = issue.Classifications.Count > 0 ? issue.Classifications.First() : string.Empty,
                                FoundDate = queueScan.Entity.Saltminer.Scan.ScanDate,
                                LocationFull = "N/A",
                                Location = "N/A",
                                Name = issue.Name,
                                ReportId = queueScan.Entity.Saltminer.Scan.ReportId,
                                Scanner = new SaltMiner.Core.Entities.ScannerInfo
                                {
                                    Id = issue.VulnerabilityID,
                                    AssessmentType = AssessmentType.DAST.ToString("g"),
                                    Product = "WebInspect",
                                    Vendor = "Fortify"
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
                                    Analyzer = "WebInspect",
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
                    CustomAssembly.CustomizeQueueIssue(queueIssue, emptySession);
                LocalData.AddUpdate(queueIssue); 
            }
            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, queueAsset.Entity.Saltminer.Asset.SourceId, "MapIssues"), queueIssues.Count());
        }

        private int GetIssueCount(List<SessionDTO> sessions)
        {
            return sessions?.SelectMany(c => c?.Issues).Count() ?? 0;
        }

        private Report ParseFile(string zip, WebInspectClient client)
        {
            var report = new Report();

            if (Directory.Exists(Config.ExtractFolder))
            {
                Directory.Delete(Config.ExtractFolder, true);
            }

            Directory.CreateDirectory(Config.ExtractFolder);

            try
            {
                ZipFile.ExtractToDirectory(zip, Config.ExtractFolder);

                var xmlFilePath = (from x in Directory.GetFiles(Config.ExtractFolder)
                                    where x.Contains(Path.GetExtension(x).TrimStart('.').ToLowerInvariant())
                                    select x).First(x => x.Contains("webinspect"));

                var xmlFileName = xmlFilePath.Substring(Config.ExtractFolder.Length + 1);

                var text = File.ReadAllText(xmlFilePath);

                File.WriteAllText(xmlFilePath, text);

                report.Sessions.AddRange(client.GetSessions(xmlFilePath));
                report.SourceMetric = client.GetSourceMetric(report.Sessions[1], Config);

                var processedPath = Path.Combine(Config.ZipFolder, Config.ProcessedFolder);

                var newFileName = Path.GetFileName(zip);

                Directory.CreateDirectory(processedPath);

                var path = Path.Combine(processedPath, newFileName.Substring(0, newFileName.IndexOf(".fpr")));

                File.WriteAllText(path + ".xml", text);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in Parsing files.");
                throw;
            }

            Directory.Delete(Config.ExtractFolder, true);
            return report;
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

        //private void ParseFiles(WebInspectClient client, List<Scan> scans, List<Session> sessions)
        //{
        //    var zips = GetFiles(Config.ZipFolder);

        //    var scanZips = zips.Where(x => x.ToLower().Contains(".scan"));
        //    var fprZips = zips.Where(x => x.ToLower().Contains(".fpr"));

        //    foreach(var scan in scanZips)
        //    {
        //        var fileNameIndex = scan.IndexOf(".scan");
        //        var fileName = scan.Substring(0, fileNameIndex);
        //        var session = fileName + ".fpr";
        //        if (!File.Exists(session))
        //        {
        //            Logger.LogError($"FPR file is missing for scan {scan}.");
        //            throw new Exception($"FPR file is missing for scan {scan}.");
        //        }

        //        scans.AddRange(ParseScans(client, scan));
        //        sessions.AddRange(ParseSessions(client, session));
        //    }
        //}

        //private List<Scan> ParseScans(WebInspectClient client, string zip)
        //{
        //    var results = new List<Scan>();

        //    Directory.Delete(Config.ExtractFolder, true);
        //    Directory.CreateDirectory(Config.ExtractFolder);

        //    try
        //    {
        //        ZipFile.ExtractToDirectory(zip, Config.ExtractFolder);

        //        var xmlFilePath = (from x in Directory.GetFiles(Config.ExtractFolder)
        //                        where x.Contains(Path.GetExtension(x).TrimStart('.').ToLowerInvariant())
        //                        select x).First();

        //        var xmlFileName = xmlFilePath.Substring(Config.ExtractFolder.Length + 1);

        //        var text = File.ReadAllText(xmlFilePath);
        //        text = text.Replace("&lt;", "<");
        //        text = text.Replace("&gt;", ">");
        //        text = text.Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", "");
        //        text = text.Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "");
        //        text = text.Replace("<br>", "");
        //        text = text.Replace("<br/>", "");

        //        File.WriteAllText(xmlFilePath, text);

        //        results.AddRange(client.GetScan(xmlFilePath).ToList());

        //        var processedPath = Path.Combine(Config.ZipFolder, Config.ProcessedFolder);

        //        Directory.CreateDirectory(processedPath);

        //        var path = Path.Combine(processedPath, xmlFileName);

        //        File.WriteAllText(path, text);
        //    }
        //     catch (Exception ex)
        //    {
        //        Logger.LogError("Error in Parsing files.");
        //        throw;
        //    }

        //    return results;
        //}
    }
}