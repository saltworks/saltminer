/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-10-28
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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.SourceAdapters.CheckmarxSast
{
    // NOTES:
    // We do not track source metrics for this source, as we are given report files directly and there's no API to call.
    // Every report from CxSast is treated as needing an update in SaltMiner.
    public partial class CheckmarxSastAdapter : SourceAdapter
    {
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private CheckmarxSastConfig Config;
        private readonly string AssetType = "app";
        
        public CheckmarxSastAdapter(IServiceProvider provider, ILogger<CheckmarxSastAdapter> logger) : base(provider, logger)
        {
            Logger.LogDebug("Initialization complete.");
        }

        [GeneratedRegex(@"\?scanid=(\d+)&projectid=(\d+)&pathid=(\d+)")]
        private static partial Regex IssueIdRegex();
        [GeneratedRegex(@"\?scanid=(\d+)&projectid=(\d+)")]
        private static partial Regex ScanIdRegex();

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            await base.RunAsync(config, token);
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

                if (Config.HasCustomAssembly)
                {
                    CustomAssembly = AssemblyHelper.LoadClassAssembly<ISourceAdapterCustom>(Config.CustomAssemblyName, Config.CustomAssemblyType);
                    CustomAssembly.PreProcess();
                }

                StillLoading = true;

                await Task.WhenAll(SyncAsync(), SendAsync(Config, AssetType));

                ResetFailures(Config);
                DeleteFailures(Config);

                await Task.Delay(3000, CancellationToken.None);
            }
            catch (Exception ex)
            {
                var msg = $"General failure in source adapter: {ex.InnerException?.Message ?? ex.Message}";
                Logger.LogCritical(ex, "{Msg}", msg);
                throw new CheckmarxSastException(msg, ex);
            }
        }

        private static async IAsyncEnumerable<ReportFileDto> GetAsync(string folderPath)
        {
            var dt = DateTime.UtcNow.Date;
            List<string> dtList = [];
            while (dt > DateTime.UtcNow.Date.AddDays(-7))
            {
                dtList.Add(dt.ToString("yyyyMMdd"));
                dt = dt.AddDays(-1);
            }
            var files = Directory.GetFiles(folderPath)
                .Where(x => x.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && dtList.Contains(Path.GetFileName(x)[..8]));

            foreach (var file in files)
                yield return new()
                {
                    Report = JsonSerializer.Deserialize<ReportDto>(await File.ReadAllTextAsync(file), JsonSerializerOptions.Web),
                    FilePath = file
                };
        }

        private async Task SyncAsync()
        {
            try
            {

                if (Config.SourceType != SourceType.CheckmarxSast.GetDescription())
                {
                    Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{Etype}' but was found to be '{Atype}'", SourceType.CheckmarxSast.GetDescription(), Config.SourceType);
                    throw new CheckmarxSastValidationException("Invalid configuration - source type");
                }

                var exceptionCounter = 0;
                var newLocalIssues = 0;
                var newLocalScans = 0;
                var newLocalAssets = 0;

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

                var counter = 0;
                var foundFiles = false;
                await foreach (var dto in GetAsync(Config.CxFlowFolder))
                {
                    foundFiles = true;
                    var report = dto.Report;
                    if (Config.TestingAssetLimit > 0 && counter >= Config.TestingAssetLimit)
                    {
                        Logger.LogInformation("[Sync] Testing asset limit ({Limit}) reached.  Stopping sync.", Config.TestingAssetLimit);
                        break;
                    }

                    try
                    {
                        if (RecoveryMode)
                        {
                            if (syncRecord.CurrentSourceId != report.SourceId)
                                continue;
                            else
                                RecoveryMode = false;
                        }
                        else
                        {
                            syncRecord = LocalData.GetSyncRecord(Config.Instance, Config.SourceType);
                        }

                        SyncInProgress(syncRecord, report.SourceId);
                        Logger.LogInformation("[Sync] {SourceType} {Instance}, '{SrcId}' - '{App}'", Config.SourceType, Config.Instance, report.SourceId, report.AssetName);

                        QueueScan queueScan = MapScan(report);
                        newLocalScans++;
                        QueueAsset queueAsset = MapAsset(report, queueScan);
                        newLocalAssets++;

                        if (queueScan.Entity.Saltminer.Internal.QueueStatus == QueueScanStatus.Cancel.ToString())
                            continue;

                        queueScan.Entity.Saltminer.Internal.IssueCount = MapIssues(report, queueScan, queueAsset);
                        queueScan.Loading = false;
                        LocalData.AddUpdate(queueScan);
                        SyncComplete(syncRecord);
                        RecoveryMode = false;
                        if (Config.DeleteFileWhenDone)
                            File.Delete(dto.FilePath);
                        newLocalIssues += queueScan.Entity.Saltminer.Internal.IssueCount;
                        CheckCancel(true);
                        counter++;
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
                if (!foundFiles)
                    Logger.LogWarning("[Sync] No report files found to process for the last week. Path searched: '{Path}'", Config.CxFlowFolder);
                LocalData.SaveAllBatches();
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
            var match = ScanIdRegex().Match(appReport.Link);
            string scanId;
            if (match.Success)
                scanId = $"prj{match.Groups[2].Value}-scn{match.Groups[1].Value}";
            else
                throw new CheckmarxSastValidationException($"Invalid report file for project {appReport.ProjectId}-'{appReport.Project}', missing top level link.");

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
                            ReportId = scanId,
                            ScanDate = DateTime.Parse(appReport.AdditionalDetails.ScanStartDate, CultureInfo.InvariantCulture).AddMilliseconds(1).ToUniversalTime(),
                            ProductType = "SAST",
                            ProductVersion = appReport.Version,
                            Vendor = "Checkmarx",
                            AssetType = AssetType,
                            IsSaltminerSource = CheckmarxSastConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance,
                            LinesOfCode = appReport.Loc
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = appReport.UnFilteredIssues?.Count ?? appReport.XIssues.Count,
                            QueueStatus = QueueScanStatus.Loading.ToString("g")
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
                    LocalData.DeleteQueueScan(queueScan.Id); 
                    return queueScan;
                }
            }
            return LocalData.AddUpdate(queueScan);
        }

        private QueueAsset MapAsset(ReportDto appReport, QueueScan queueScan)
        {
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
                            Name = appReport.AssetName,
                            Attributes = appReport.AdditionalDetails.CustomFields,
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = CheckmarxSastConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = appReport.SourceId,
                            Version = appReport.SourceId,
                            VersionId = appReport.SourceId,
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
            queueAsset.Entity.Saltminer.Asset.Attributes.Add("team", appReport.Team);
            return LocalData.AddUpdate(queueAsset);
        }

        private int MapIssues(ReportDto appReport, QueueScan queueScan, QueueAsset queueAsset)
        {
            var issueCounter = 0;
            var issues = appReport.UnFilteredIssues ?? appReport.XIssues;
            if (issues.Count == 0)
            { 
                LocalData.AddUpdate(GetZeroQueueIssue(queueScan, queueAsset));
                return 1;
            }
            foreach (var issue in issues)
            {
                var resultCounter = 0;
                var issId = issue.Link;
                var match = IssueIdRegex().Match(issue.Link);
                if (match.Success)
                    issId = $"scn{match.Groups[1].Value}-prj{match.Groups[2].Value}-pth{match.Groups[3].Value}";
                // Issues contain results, which represent locations in the file where the issue was found
                foreach (var result in issue.AdditionalDetails.Results)
                {
                    if (result.Source == null)
                    {
                        Logger.LogWarning("Result with index {Idx} invalid for issue '{Vuln}' in location '{Loc}'. Skipping.", resultCounter, issue.Vulnerability, issue.Filename);
                        continue;
                    }
                    var isSuppressed = false;
                    var location = $"{issue.Filename}:{result.Source.Line}:{result.Source.Column}";
                    // False positive found in separate detail collection
                    if (issue.Details.TryGetValue(result.Source.Line.ToString(), out var dtl))
                        isSuppressed = dtl.FalsePositive;
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
                                Category = ["Application"],
                                Description = issue.Description, // not available unless following link
                                Classification = issue.Link,
                                FoundDate = DateTime.Parse(appReport.AdditionalDetails.ScanStartDate, CultureInfo.InvariantCulture).AddMilliseconds(1).ToUniversalTime(),
                                Id = [issue.Link],
                                LocationFull = location,
                                Location = location,
                                Name = issue.Vulnerability,
                                Reference = issue.AdditionalDetails.Categories,
                                ReportId = appReport.AdditionalDetails.ScanId,
                                Scanner = new()
                                {
                                    Id = $"{issId}-src{result.Source.Line}-{result.Source.Column}-snk{result.Sink.Line}-{result.Sink.Column}",
                                    AssessmentType = AssessmentType.SAST.ToString("g"),
                                    Product = "Checkmarx SAST",
                                    Vendor = "Checkmarx",
                                    GuiUrl = issue.Link,
                                    ProductType = queueScan.Entity.Saltminer.Scan.ProductType,
                                    ProductVersion = queueScan.Entity.Saltminer.Scan.ProductVersion
                                },
                                Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, issue.Severity),
                                SourceSeverity = issue.Severity,
                                IsSuppressed = isSuppressed,
                                Recommendation = issue.AdditionalDetails.RecommendedFix
                            },
                            Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                            {
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
                    issueCounter++;
                    CheckCancel(false);
                }
            }
            return issueCounter;
        }
    }
}