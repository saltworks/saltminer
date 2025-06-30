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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;


namespace Saltworks.SaltMiner.SourceAdapters.CheckmarxOne
{
    public class CheckmarxOneAdapter : SourceAdapter
    {
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private CheckmarxOneConfig Config;
        private readonly string AssetType = "app";
        private readonly ConcurrentQueue<ProjectDto> ProjectsQueue = new();


        public CheckmarxOneAdapter(IServiceProvider provider, ILogger<CheckmarxOneAdapter> logger) : base(provider, logger)
        {
            Logger.LogDebug("CheckmarxOne Adapter Initialization complete.");
        }


        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            await base.RunAsync(config, token);
            try
            {
                #region Include: Get Config and Validate

                config = config ?? throw new ArgumentNullException(nameof(config));

                if (config is not CheckmarxOneConfig)
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(CheckmarxOneConfig)}', but got '{config.GetType().Name}'");
                }

                Config = config as CheckmarxOneConfig;

                Config.Validate();

                #endregion

                //Include: Check local metrics to make sure there is data, if not run FirstLoad
                FirstLoadSyncUpdate(config);

                //Include
                SetApiClientSslVerification(Config.VerifySsl);

                var Client = new CheckmarxOneClient(ApiClient, Config, Logger);

                CancelToken = token;

                //Write: Possiblilty that custom assembly has custom code
                if (Config.HasCustomAssembly)
                {
                    CustomAssembly = AssemblyHelper.LoadClassAssembly<ISourceAdapterCustom>(Config.CustomAssemblyName, Config.CustomAssemblyType);
                    CustomAssembly.PreProcess();
                }

                //Include: This will allow SendAsync to keep running until your done Syncing records
                StillLoading = true;

                //Include: This will run sync/send synchrononusly until both finish
                await Task.WhenAll(GetAsync(Client), SyncAsync(Client), SendAsync(Config, AssetType));

                //Include: This allows us to track the failure on trying to load any queuescan and reset to load agin until a configureable failure count is hit
                ResetFailures(Config);

                //Inlcude: This deletes any queuescans that hit that configurable failure count
                DeleteFailures(Config);

                //Include: This is needed to give the app a moment to finish before finishing
                await Task.Delay(5, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "{Msg}", ex.InnerException?.Message ?? ex.Message);
                throw new CheckmarxOneException("Run error", ex);
            }
        }


        private async Task GetAsync(CheckmarxOneClient client)
        {
            Logger.LogInformation($"[Get] Starting, getting projects...");
            int qLowerLimit = 10;
            int qUpperLimit = 100;
            int batchSize = 200;
            int projectOffset = 0;
            int totalProjects;

            do
            {
                var projects = await client.GetProjectsAsync(batchSize, projectOffset);

                foreach (var project in projects.Projects)
                {
                    ProjectsQueue.Enqueue(project);
                }

                projectOffset += batchSize;
                totalProjects = projects.TotalCount;

                if (ProjectsQueue.Count >= qUpperLimit)
                {
                    Logger.LogDebug("[Get] Queue reached limit of {Limit}, waiting for queue to be processed by sync.", ProjectsQueue.Count);
                    while (ProjectsQueue.Count > qLowerLimit && StillLoading)
                    {
                        await Task.Delay(5000);
                        CheckCancel(true);
                    }
                }

            } while (totalProjects > projectOffset);

        }

        private async Task SyncAsync(CheckmarxOneClient client)
        {
            CheckCancel();

            if (Config.SourceType != SourceType.CheckmarxOne.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{Etype}' but was found to be '{Atype}'", SourceType.CheckmarxOne.GetDescription(), Config.SourceType);
               
                throw new CheckmarxOneException("Invalid configuration - source type");
            }
            if (Config.TestingAssetLimit > 0)
                Logger.LogInformation("[Sync] TestingAssetLimit set to {Limit} - sync will be stopped once processed asset count reaches this limit.", Config.TestingAssetLimit);


            var exceptionCounter = 0;

            // Write: Gather SourceMetrics from client
            //Use Config.TestingAssetLimit to limit the assets if > 0(set)
            //Design decision: expectations to handle 10k assets

            // Write: Gather SourceMetrics from LocalData
            Logger.LogInformation($"Getting local source metrics...");
            var localMetrics = LocalData.GetSourceMetrics(Config.Instance, Config.SourceType).ToList();

            //Include: Check for existing Run that did not finish
            var syncRecord = LocalData.CheckSyncRecordSourceForFailure(Config.Instance, Config.SourceType);

            //Include: Set recoverymode based on whether not there is a prior syncrecod
            if (syncRecord != null)
            {
                RecoveryMode = true;
            }
            else
            {
                //Include: If not create a new record for this run
                RecoveryMode = false;
                syncRecord = LocalData.GetSyncRecord(Config.Instance, Config.SourceType);

                //Include: Clear any leftover queue data from previous run
                ClearQueues();
            }

            var localScans = 0;
            var localAssets = 0;
            var localIssues = 0;

            var totalProjectCount = 0;
            int counter = 0;

            // Wait for GetAsync to get started
            Logger.LogWarning("[Sync] Short delay while [Get] starts...");
            await Task.Delay(5000);
            if (ProjectsQueue.IsEmpty)
                Logger.LogWarning("[Sync] No projects in queue in a timely fashion.  Sync will not start.");

            while (ProjectsQueue.TryDequeue(out var queueProject))
            {
                try
                {
                    if (Config.TestingAssetLimit > 0 && totalProjectCount >= Config.TestingAssetLimit)
                    {
                        Logger.LogInformation("[Sync] Configured asset limit of {Limit} reached, stopping sync.", Config.TestingAssetLimit);
                        break;
                    }
                    var metric = await client.GetSourceMetric(queueProject);

                    //If Recoverymode loop through until you are on that sourcemetric
                    if (RecoveryMode)
                    {
                        if (syncRecord.CurrentSourceId != metric.SourceId)
                        {
                            continue;
                        }
                        else
                        {
                            RecoveryMode = false;
                        }
                    }

                    //Include: Update Syncrecord to reflect the metric currently processed
                    syncRecord.CurrentSourceId = metric.SourceId;
                    syncRecord.State = SyncState.InProgress;
                    LocalData.AddUpdate(syncRecord, true);

                    //Include: Get matching local metric to current metric
                    var localMetric = localMetrics.FirstOrDefault(x => x.SourceId == metric.SourceId);
                    if (localMetric != null)
                    {
                        //Include: If found set isProcessed to true for tracking and retiring records
                        localMetric.IsProcessed = true;
                    }

                    if (Config.FullSyncMaintEnabled)
                    {
                        FullSyncBatchProcess(syncRecord, Config.FullSyncBatchSize);
                        // End of metrics and full sync processing is still true,
                        // means last batch did not meet batch size... reset source ID to cycle through again
                        // End of metrics and batch count is zero,
                        // means nothing was found to process (ie. maybe a source metric doesn't exist anymore)...reset source ID
                        if (counter + 1 == ProjectsQueue.Count && (FullSyncProcessing || FullSyncBatchCount == 0))
                        {
                            syncRecord.FullSyncSourceId = "";
                        }
                    }

                    //Include:  Get all data needed to determine if local metric and source metric match
                    if (NeedsUpdate(metric, localMetric) || RecoveryMode)
                    {
                        Logger.LogInformation("[Sync] Updating {stype} project ID '{srcId}', asset type '{atype}'", Config.SourceType, metric.SourceId, AssetType);

                        //Write: Map scan and add to localData, including if there are reports that are from the last scan. Pass noScan flag if necessary
                        //Example: var queueScan = MapScan(appReport, historyReports, noScan);
                        //Write: Map asset and add to localData, including data from the scan.
                        //Write: Map issues and add to localData, including data from the scan and asset. Skip if 'noscan'. Manager will create a 'noscan' issue.
                        //if (!noScan)
                        //Example: var queueIssue = MapIssue(?, ?, ?);
                        var scans = await client.GetScansAsync(queueProject.ID);
                        var lastScan = scans.Scans[0];
                        var lastScanSummary = await client.GetScanSummaryAsync(lastScan.ID);

                        string assessmentType = "None";
                        bool scanAssetMapped = false;
                        QueueScan queueScan = new();
                        QueueAsset queueAsset = new();
                        List<QueueIssue> queueIssues = new();

                        await foreach (var issue in GetResultsAsync(lastScan.ID, client))
                        {

                            var currentIssue = issue;
                           
                            if (FindAssessmentType(assessmentType) != FindAssessmentType(currentIssue.Type))
                            {

                                if (scanAssetMapped)
                                {
                                    queueScan.Loading = false;
                                    LocalData.AddUpdate(queueScan);
                                    totalProjectCount++;
                                    
                                }
                                queueScan = MapScan(queueProject, lastScan, lastScanSummary.ScansSummaries[0], currentIssue.Type);
                                localScans++;

                                queueAsset = MapAsset(queueProject, queueScan);
                                localAssets++;
                                assessmentType = currentIssue.Type;
                                scanAssetMapped = true;
                            }
                            var mappedIssue = MapIssue(queueProject, currentIssue, queueScan, queueAsset, queueIssues, lastScan.ID, false);
                            localIssues++;

                        }

                        //Include: Update Scan with loading = false in localData
                        //Write: End time for individual sourcemetric
                        //write a issue counter and pass it in.
                        //****************************

                        queueScan.Loading = false;
                        LocalData.AddUpdate(queueScan);
                        totalProjectCount++;
                        await LetSendCatchUpAsync(Config);

                        CheckCancel(false);
                        //Include: Update Local metric with source metric, so always current
                        UpdateLocalMetric(metric, localMetric);
                        //Include: Set recoverymode to false, for the next iteration
                        RecoveryMode = false;

                    }
                }
                catch (LocalDataException ex)
                {
                    Logger.LogCritical(ex, "{Msg}", ex.InnerException?.Message ?? ex.Message);
                    StillLoading = false;
                    throw new CheckmarxOneException("Local data error", ex);
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
            LocalData.SaveAllBatches();

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
            syncRecord.LastSync = (DateTime.UtcNow);
            syncRecord.CurrentSourceId = null;
            syncRecord.State = SyncState.Completed;
            LocalData.AddUpdate(syncRecord);
        }

        // Not all sources allow for history reports
        // NOTE: History field below should not be used unless you have to. It is only for special cases.

        private QueueScan MapScan(ProjectDto project, ScanDTO scan, ScanSummaryDTO scanSummary, string assessmentType, bool noScan = false)
        {
            var reportId = $"{scan.ID}|{FindAssessmentType(assessmentType)}";
            var now = DateTime.UtcNow;
            //scan Data here is in scan details and they are separated by most recent scan and then the data types scanned. 
            var scanDate = scan.UpdatedAt;
            var queueScan = new QueueScan
            {            
                Loading = true,
                Entity = new()
                {
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueScanInfo
                    {
                        Scan = new SaltMiner.Core.Entities.QueueScanInfo
                        {
                            AssessmentType = FindAssessmentType(assessmentType),
                            Product = "CheckmarxOne",
                            ReportId = noScan ? $"{GetNoScanReportId(FindAssessmentType(assessmentType))}|{assessmentType}" : reportId,
                            ScanDate = noScan ? now : scanDate,
                            ProductType = "Application",
                            Vendor = "Checkmarx",
                            AssetType = AssetType,
                            IsSaltminerSource = CheckmarxOneConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = noScan ? 1 : FindAssessementCounter(assessmentType, scanSummary),  
                            QueueStatus = QueueScanStatus.Loading.ToString("g")
                        }
                    },
                    Timestamp = now
                },
                Timestamp = now
            };

            if (CustomAssembly != null)
            {
                CustomAssembly.CustomizeQueueScan(queueScan, project);
                if (CustomAssembly.CancelScan)
                {
                    queueScan.Entity.Saltminer.Internal.QueueStatus = QueueScanStatus.Cancel.ToString();
                    LocalData.DeleteQueueScan(queueScan.Id);
                    return queueScan;
                }
            }
            return LocalData.AddUpdate(queueScan);
        }


        private QueueAsset MapAsset(ProjectDto project, QueueScan queueScan, bool isRetired = false)
        {
            var sourceId = $"{project.ID}";
            var queueAsset = new QueueAsset
            {
                Entity = new()
                {
                    Timestamp = DateTime.UtcNow,
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueAssetInfo
                    {
                        Asset = new SaltMiner.Core.Entities.AssetInfoPolicy
                        {
                            Name = project.Name,
                            Attributes = [],
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = CheckmarxOneConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = sourceId,
                            Version = project.Name,
                            AssetType = AssetType,
                            LastScanDaysPolicy = Config.LastScanDaysPolicy,
                            IsRetired = isRetired
                        },
                        Internal = new()
                        {
                            QueueScanId = queueScan.Id
                        }
                    }
                }
            };

            
            var result = LocalData.AddUpdate(queueAsset);
            return result;
        }

        private QueueIssue MapIssue(ProjectDto project, ScanResultsResultDTO issue, QueueScan queueScan, QueueAsset queueAsset, List<QueueIssue> queueIssues,string scanId, bool zeroRecord = false)
        {
            var sourceId = queueAsset.SourceId;

            if (zeroRecord)
            {

                var result = LocalData.AddUpdate(GetZeroQueueIssue(queueScan, queueAsset));
                return result;
            }
            else
            {

                var issueScannerId = issue.Id;
                string package = null;
                string locationFull = BuildLocationFullByType(issue);
                List<string> splitLocationFull= locationFull.Split(',').ToList();
                string singleLocation = splitLocationFull[0];

                if (issue.Type.Contains("sca"))
                {
                    package = issue.Data.PackageIdentifier;
                }

                if (issue.Type.Contains("kics") || issue.Type.Contains("sca"))
                {
                    var resultData = issue.Data;
                    issueScannerId = $"{issue.Type}|{issue.Id}|{resultData.FileName}|{resultData.PackageIdentifier}|{issue.FoundAt}|{issue.FirstScanId}|{resultData.RecommendedVersion}|{queueScan.Entity.Id}";
                }
                QueueIssue queueIssue = new()
                {
                    Entity = new()
                    {
                        Labels = [],
                        Vulnerability = new()
                        {
                            Audit = new()
                            {
                                Audited = true
                            },
                            Category = [issue.Type ?? "Unknown" ],
                            FoundDate = issue.FirstFoundAt,
                            LocationFull = locationFull,
                            Location = singleLocation,
                            Name = issue.Data.QueryName ?? "N/A",
                            Description = issue.Description ?? "",
                            ReportId = queueScan.ReportId,
                            Scanner = new()
                            {
                                Id = issueScannerId,
                                AssessmentType = FindAssessmentType(issue.Type),
                                Product = "Checkmarx",
                                Vendor = "CheckmarxOne",
                                GuiUrl = BuildGuiUrl(Config, issue.Type, queueAsset.SourceId, scanId, issue.Id, package) 
                            },
                            Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, issue.Severity),
                            IsSuppressed = false
                        },
                        Saltminer = new()
                        {
                            IssueType = FindIssueType(issue.Type),
                            Attributes = new()
                            {
                                {"org_assessment_type", issue.Type},
                                {"cmx1_issue_status", issue.Status?? " " },
                                {"cmx1_issue_state", issue.State?? " " }
                            },
                            QueueScanId = queueScan.Entity.Id,
                            QueueAssetId = queueAsset.Entity.Id,
                            Source = new()
                            {
                                Analyzer = "Checkmarx",
                            }
                        },
                        Tags = [],
                        Timestamp = DateTime.UtcNow
                    }
                };

                CustomAssembly?.CustomizeQueueIssue(queueIssue, project);
                var result = LocalData.AddUpdate(queueIssue);
                return result;
            }
        }

        private static int FindAssessementCounter(string assessmentType, ScanSummaryDTO scanSummary)
        {
            return assessmentType switch
            {
                "sast" => scanSummary.SastCounters.TotalCounter,
                "sca" => scanSummary.ScaCounters.TotalCounter,
                "kics" => scanSummary.KicsCounters.TotalCounter,
                "apisec" => scanSummary.KicsCounters.TotalCounter,
                _ => 0,
            };
        }

        private static string BuildGuiUrl(CheckmarxOneConfig Config, string assessmentType, string projectId, string scanId, string resultId, string package= null)
        {
            return assessmentType switch
            {
                "sast" => Config.GuiAddress + "sast-results/" + projectId + "/" + scanId + "?resultId=" + resultId,
                "sca" => Config.GuiAddress + "results/" + projectId + "/" + scanId + "sca?internalPath=%2Fvulnerabilities%2F" + resultId + "%253A" + package + "%2FvulnerabilityDetailsGql",
                "kics" => Config.GuiAddress + "results/" + projectId + "/" + scanId + "kics?result-id=" + resultId,
                "iac" => Config.GuiAddress + "results/" + projectId + "/" + scanId + "kics?result-id=" + resultId,
                _ => "N/A"
            };
        }

        private static string FindAssessmentType(string assessmentType)
            
        {
            if (assessmentType.Contains("sca"))
            {
                assessmentType = "sca";
            }
            return assessmentType switch
            {
                "sast" => AssessmentType.SAST.ToString("g"),
                "sca" => AssessmentType.SAST.ToString("g"),
                "kics" => AssessmentType.SAST.ToString("g"),
                "apisec" => AssessmentType.DAST.ToString("g"),
                "containers" => AssessmentType.Container.ToString("g"),
                _ => AssessmentType.Open.ToString("g"),
            };
        }

        private static string FindIssueType(string issueType)

        {
            if (issueType.Contains("sca"))
            {
                issueType = "sca";
            }
            return issueType switch
            {
                "sast" => IssueType.SAST.ToString("g"),
                "sca" => IssueType.SAST.ToString("g"),
                "kics" => IssueType.KICS.ToString("g"),
                "apisec" => IssueType.DAST.ToString("g"),
                "containers" => IssueType.Container.ToString("g"),
                _ => IssueType.Open.ToString("g"),
            };
        }

        public static string BuildLocationFullByType(ScanResultsResultDTO issue)
        {
            string assessmentType = issue.Type;
            if (assessmentType.Contains("sca"))
            {
                assessmentType = "sca";
            }
            return assessmentType switch
            {
                "sca" => issue.Data.PackageIdentifier,
                "sast" => BuildSCALocation(issue),
                "kics" => issue.Data.FileName + ":" + "line: " + issue.Data.Line,
                _ => "N/A"
            };
        }

        public static string BuildSCALocation(ScanResultsResultDTO issue)
        {
            var nodeData = issue.Data.Nodes?.Select(n => n.FileName + ":line: "+ n.Line + " :Column: " + n.Column);
            return nodeData.Any() ? string.Join(", ", nodeData) : "N/A";
        }

        public static string GetIdObjectAsString(object idObject)
        {
            if (idObject is null)
            {
                return null;
            }

            if (idObject is string str)
            {
                return str;
            }

            if (idObject is int intValue)
            {
                return intValue.ToString();
            }

            if (idObject is JsonElement jsonElement)
            {
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.Number:
                        if (jsonElement.TryGetInt32(out int jsonIntValue))
                        {
                            return jsonIntValue.ToString();
                        }
                        if (jsonElement.TryGetInt64(out long jsonLongValue))
                        {
                            return jsonLongValue.ToString();
                        }
                        if (jsonElement.TryGetDouble(out double jsonDoubleValue))
                        {
                            return jsonDoubleValue.ToString();
                        }
                        throw new InvalidOperationException($"Unsupported number type for JsonElement: {jsonElement}");

                    case JsonValueKind.String:
                        return jsonElement.GetString();

                    default:
                        throw new InvalidOperationException($"Unsupported JsonElement type for QueryId: {jsonElement.ValueKind}");
                }
            }
            throw new InvalidOperationException($"Unsupported type for QueryId {idObject.GetType()}");
        }

        private static async IAsyncEnumerable<ScanResultsResultDTO> GetResultsAsync(string scanId, CheckmarxOneClient client)
        {
            int issueLimit = 100;
            int issueCounter = 0;
            int issueOffset = 0;
            int totalIssues;

            do
            {
                var issues = await client.GetScanResultsAsync(scanId, issueLimit, issueOffset);

                foreach (var issue in issues.Results)
                {

                    yield return issue;
                }

                issueOffset++;
                issueCounter += issueLimit;
                totalIssues = issues.TotalCount;

            } while (totalIssues >= issueCounter);

            
        }



    }
    

}


