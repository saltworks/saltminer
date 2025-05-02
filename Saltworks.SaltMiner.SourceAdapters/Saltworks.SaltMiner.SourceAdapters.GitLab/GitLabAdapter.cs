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
using System.Globalization;
using System.Text.Json;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.SourceAdapters.GitLab
{
    public class GitLabAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private GitLabConfig Config;
        private readonly string AssetType = "app";
        private readonly string Vendor = "GitLab Inc.";
        private readonly string Product = "GitLab";
                
        public GitLabAdapter(IServiceProvider provider, ILogger<GitLabAdapter> logger) : base(provider, logger)
        {
            Logger.LogDebug("GitLabAdapter Initialization complete.");
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            await base.RunAsync(config, token);
            try
            {
                #region Include: Get Config and Validate

                config = config ?? throw new ArgumentNullException(nameof(config));

                if (config is not GitLabConfig)
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(GitLabConfig)}', but got '{config.GetType().Name}'");
                }

                Config = config as GitLabConfig;

                Config.Validate();

                #endregion

                //Include: Check local metrics to make sure there is data, if not run FirstLoad
                FirstLoadSyncUpdate(config);
                
                //Include
                SetApiClientSslVerification(Config.VerifySsl);

                //Write
                var client = new GitLabClient(ApiClient, Config, Logger);

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
                await Task.WhenAll(SyncAsync(client), SendAsync(Config, AssetType));

                //Include: This allows us to track the failure on trying to load any queuescan and reset to load agin until a configureable failure count is hit
                ResetFailures(Config);

                //Inlcude: This deletes any queuescans that hit that configurable failure count
                ResetFailures(Config);

                //Include: This is needed to give the app a moment to finish before finishing
                await Task.Delay(5, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, ex.InnerException?.Message ?? ex.Message);
                throw new GitLabException("Run failure.", ex);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1854:Unused assignments should be removed", Justification = "False positives herein, where variable is assigned based on location in code")]
        private async Task SyncAsync(GitLabClient client)
        {
            CheckCancel();

            if (Config.SourceType != SourceType.GitLab.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{Etype}' but was found to be '{Atype}'", SourceType.GitLab.GetDescription(), Config.SourceType);
                throw new GitLabValidationException("Invalid configuration - source type");
            }

            try
            {
                Logger.LogInformation($"[Sync] Starting GitLab sync...");

                if (Config.TestingAssetLimit > 0)
                {
                    Logger.LogWarning("TestingAssetLimit of {Amt} is in effect.  GitLab loading will stop at this count.", Config.TestingAssetLimit);
                }

                var exceptionCounter = 0;

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

                var totalProjectCount = 0;

                // for testing specific project
                var testProjectFullPath = @"";

                // main group container for all subgroups and projects
                var groupNamespace = Config.GroupNamespace;

                Logger.LogInformation("[Sync] Begin getting all subgroups for {Group}", groupNamespace);
                await foreach (var group in GetGroupsAsync(client, groupNamespace))
                {
                    CheckCancel(true);
                    // testing limit reached or testing 1 specific project - break out of all processing
                    if ((Config.TestingAssetLimit > 0 && totalProjectCount >= Config.TestingAssetLimit))
                    {
                        break;
                    }

                    if (group.ProjectsCount <= 0)
                    {
                        Logger.LogInformation("[Sync] No projects found for the group {Group}. Getting next group.", group.FullPath);
                        continue;
                    }

                    Logger.LogInformation("[Sync] Begin loading projects data for group path {Project}", group.FullPath);
                    await foreach (var asset in GetGroupProjectsAsync(client, group.FullPath))
                    {
                        CheckCancel(true);
                        try
                        {
                            var project = asset;

                            if (testProjectFullPath != "")
                            {
                                var singleProject = await client.GetProjectAssetAsync(testProjectFullPath);
                                project = singleProject.Data.Project;
                                if (project == null)
                                {
                                    break;
                                }
                            }

                            // testing limit reached or testing 1 specific project - break out of all processing
                            if ((Config.TestingAssetLimit > 0 && totalProjectCount >= Config.TestingAssetLimit))
                            {
                                break;
                            }

                            totalProjectCount++;
                            Logger.LogInformation("Total projects processed: {Count}", totalProjectCount);

                            Logger.LogInformation("Getting SourceMetric for Project {Project}", project.FullPath);

                            // get latest scan to put in the source metric
                            GraphQLResponse<ScanDataDto> latestScan = await client.GetLatestProjectScanAsync(project.FullPath);
                            DateTime? latestScanDate = null;
                            if (latestScan?.Data?.Project?.Pipelines?.Nodes?.FirstOrDefault() != null)
                            {
                                latestScanDate = ConvertStringDate(latestScan.Data.Project.Pipelines.Nodes.FirstOrDefault()?.CreatedAt);
                            }

                            SourceMetric metric = client.GetSourceMetric(project, latestScanDate);

                            //Write: Start time for individual sourcemetric
                            // Removing progress helper calls since assessment types are mixed for this source
                            //ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"))

                            //If Recoverymode loop through until you are on that sourcemetric
                            if (RecoveryMode)
                            {
                                if (SyncRecord.CurrentSourceId != metric.SourceId)
                                {
                                    //Write: End Timer for each skipped metric until recovery record hit
                                    Logger.LogDebug("Skipping source ID {SrcId} due to recovery mode", metric.SourceId);
                                    continue;
                                }
                                else
                                {
                                    RecoveryMode = false;
                                }
                            }

                            //Include: Update sync record to reflect the metric currently processed
                            SyncRecord.CurrentSourceId = metric.SourceId;
                            SyncRecord.State = SyncState.InProgress;
                            LocalData.AddUpdate(SyncRecord, true);

                            //Include: Get matching local metric to current metric
                            var localMetric = LocalData.GetSourceMetric(Config.Instance, Config.SourceType, metric.SourceId);
                            if (localMetric != null)
                            {
                                //Include: If found set isProcessed to true for tracking and retiring records
                                localMetric.IsProcessed = true;
                            }

                            //Get all data needed to determine if local metric and source metric match
                            if (NeedsUpdate(metric, localMetric) || RecoveryMode)
                            {
                                Logger.LogInformation("[Sync] Updating Config.Instance '{Instance}', SourceType {SourceType}, SourceId '{SourceId}', AssetType '{AssetType}'", Config.Instance, Config.SourceType, metric.SourceId, AssetType);

                                // If source has a master list of assets, then we can check if there is a new scan for each asset
                                // in this case if there is no scan for this asset, then map a 'noscan' queueScan and queueAsset,
                                // but skip adding a queueIssue. That will be done when the manager processes
                                var noScan = metric.LastScan == null;

                                QueueScan queueScan;
                                QueueAsset queueAsset;
                                var issueReportTypes = new Dictionary<string, Tuple<QueueScan, QueueAsset>>();

                                if (!noScan)
                                {
                                    Logger.LogInformation("[NeedsUpdate] Begin loading project {Asset} issue data", project.FullPath);
                                    var issueFailedAt = "Top of issue loop";

                                    try
                                    {
                                        await foreach (var issue in GetIssueByProjectAsync(client, project.FullPath))
                                        {
                                            CheckCancel(true);

                                            var reportType = issue?.Scanner?.ReportType;
                                            if (!string.IsNullOrEmpty(reportType))
                                            {
                                                Tuple<QueueScan, QueueAsset> queueObjects;

                                                // check to see if issue assessment type is already found,
                                                // if so, use previously created queue objects
                                                issueFailedAt = "issuesReportTypes.ContainsKey if block";
                                                if (issueReportTypes.ContainsKey(reportType))
                                                {
                                                    queueObjects = issueReportTypes[reportType];
                                                }
                                                else
                                                {
                                                    Logger.LogDebug("[Sync] Mapping scan for project {Project}", project.FullPath);
                                                    issueFailedAt = "MapScan";
                                                    queueScan = MapScan(project, reportType, latestScanDate, noScan);
                                                    Logger.LogDebug("[Sync] Mapping asset for project {Project}", project.FullPath);
                                                    issueFailedAt = "MapAsset";
                                                    queueAsset = MapAsset(project, queueScan);
                                                    issueReportTypes[reportType] = Tuple.Create(queueScan, queueAsset);
                                                    queueObjects = issueReportTypes[reportType];
                                                }

                                                Logger.LogInformation("[Sync] Mapping issues for project {Project} with queueScan {Qid} and queueAsset {Aid}", project.FullPath, queueObjects.Item1.Id, queueObjects.Item2.Id);
                                                issueFailedAt = "MapIssue";
                                                MapIssue(project, issue, queueObjects.Item1, queueObjects.Item2);
                                            }
                                            else
                                            {
                                                Logger.LogWarning("[Sync] Issue with ID '{Id}' for project '{Project}' has no report type and will be skipped.", issue.Id, project.FullPath);
                                            }
                                            issueFailedAt = "Top of issue loop";
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        var assetName = asset?.FullPath ?? "?";
                                        var ex1 = new GitLabException($"Exception in issues for project '{assetName}' at {issueFailedAt}: {ex.InnerException?.Message ?? ex.Message}", ex);
                                        DumpException("GitLabSyncIssuesLoop", ex1);
                                        Logger.LogError(ex, "[Sync] Error getting issues for project '{Project}'.", assetName);
                                    }
                                }
                                else // no scan
                                {
                                    // TODO: No assessment type specified, add it (and logic to determine required ones) and then uncomment/adjust
                                    //Logger.LogInformation("[Sync] Mapping 'noScan' scan for project {FullPath}", project.FullPath);
                                    //queueScan = MapScan(project, "noScan", latestScanDate, true);
                                    //MapAsset(project, queueScan);
                                }

                                CheckCancel(false);

                                if (!noScan)
                                {
                                    // iterate the different queue scans/assessments found in issues and close out
                                    #pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions - this is a false positive
                                    foreach (var queue in issueReportTypes.Values)
                                    {
                                        queue.Item1.Loading = false;
                                        queue.Item1.Entity.Saltminer.Internal.IssueCount = LocalData.GetQueueIssuesCountByScanId(queue.Item1.Id);
                                        LocalData.AddUpdate(queue.Item1);
                                    }
                                    #pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions
                                    Logger.LogInformation("Updating queueScan issue counts for project {Project}", project.FullPath);
                                }
                                
                                await LetSendCatchUpAsync(Config);

                                //Update Local metric with source metric, so always current
                                UpdateLocalMetric(metric, localMetric);
                                //Set recoverymode to false, for the next iteration
                                RecoveryMode = false;
                                Logger.LogInformation("Completed processing project {Project}", project.FullPath);
                            }
                        }
                        catch (LocalDataException ex)
                        {
                            var assetName = asset?.FullPath ?? "?";
                            Logger.LogCritical(ex, "{Msg}", ex.InnerException?.Message ?? ex.Message);
                            StillLoading = false;
                            var ex1 = new GitLabException($"Exception while processing asset '{assetName}': {ex.InnerException?.Message ?? ex.Message}", ex);
                            DumpException("GitLabSyncAssetLoop", ex1);
                            throw ex1;
                        }
                        catch (Exception ex)
                        {
                            var assetName = asset?.FullPath ?? "?";
                            var ex1 = new GitLabException($"Exception while processing asset '{assetName}': {ex.InnerException?.Message ?? ex.Message}", ex);
                            DumpException("GitLabSyncAssetLoop", ex1);
                            if (ex.Message == "Not Found")
                            {
                                Logger.LogWarning(ex, "[Sync] Instance '{Instance}', SourceType {SourceType} Sync Processing Error: {Msg}", Config.Instance, Config.SourceType, ex.InnerException?.Message ?? ex.Message);
                            }
                            else
                            {
                                exceptionCounter++;
                                Logger.LogWarning(ex, "[Sync] Instance '{Instance}', SourceType {SourceType} Sync Processing Error {ExCount}: {Msg}", Config.Instance, Config.SourceType, exceptionCounter, ex.InnerException?.Message ?? ex.Message);
                                if (exceptionCounter == Config.SourceAbortErrorCount)
                                {
                                    Logger.LogCritical(ex, "[Sync] Instance '{Instance}', SourceType {SourceType} exceeded {ExCount} errors: {Msg}", Config.Instance, Config.SourceType, Config.SourceAbortErrorCount, ex.InnerException?.Message ?? ex.Message);
                                    StillLoading = false;
                                    break;
                                }
                            }
                        }
                    }

                }

                // todo: RETIRE?
                //if (!Config.DisableRetire)
                //{
                //    try
                //    {
                //        RetireLocalMetrics(localMetrics);
                //        RetireQueueAssets(localMetrics, AssetType, Config);
                //    }
                //    catch (Exception ex)
                //    {
                //        Logger.LogError(ex, "Error occurred when processing retirees, see log for details.");
                //    }
                //}
                //else
                //{
                //    Logger.LogInformation("Asset retirement processing disabled by configuration, skipping.");
                //}

                //Include: indicate your done with this source metric
                SyncRecord.LastSync = (DateTime.UtcNow);
                SyncRecord.CurrentSourceId = null;
                SyncRecord.State = SyncState.Completed;
                LocalData.AddUpdate(SyncRecord, true);
                LocalData.SaveAllBatches();
                Logger.LogInformation("[Sync] Exiting sync loading phase in 5 sec...");
                await Task.Delay(5000);
                //Include: Set this to indiciate your done syncing
                StillLoading = false;
            }
            catch (CancelTokenException ex)
            {
                Logger.LogWarning(ex, "[Sync] Cancellation requested, aborting processing.");
                StillLoading = false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[Sync] Aborting GitLab sync due to exception: [{Type}] {Msg}", ex.GetType().Name, ex.Message);
                DumpException("GitLabSyncOuter", ex);
                StillLoading = false;
            }
        }

        private void DumpException(string traceName, Exception ex)
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            var data = new ExceptionInfo(ex);
            try
            {
                if (Config.DebugEnabled)
                    File.AppendAllText(Path.Combine(path, $"ExceptionTrace-{traceName}.json"), JsonSerializer.Serialize(data) + "\n");
            }
            catch (Exception ex1)
            {
                Logger.LogWarning(ex1, "Failed to dump exception to file for trace name '{Name}': [{ExType}] {Msg}", traceName, ex.GetType().Name, ex.InnerException?.Message ?? ex.Message);
                throw new GitLabException($"Failed to dump exception to file for trace name '{traceName}'.  [{ex1.GetType().Name}] {ex1.InnerException?.Message ?? ex.Message}", ex);
            }
        }

        internal async IAsyncEnumerable<GroupNodeDto> GetGroupsAsync(GitLabClient client, string groupNamespace)
        {
            var complete = false;
            string afterToken = string.Empty;
            PageInfoDto pageInfo;
            while (!complete)
            {
                var groupBatch = (await client.GetNamespaceGroupsAsync(groupNamespace, afterToken, Config.BatchLimit)).Data.Groups;
                pageInfo = groupBatch.PageInfo;
                complete = !(pageInfo?.HasNextPage ?? false);
                afterToken = complete ? string.Empty : pageInfo.EndCursor;
                complete = afterToken == string.Empty;

                foreach (var group in groupBatch.Nodes)
                {
                    if (group != null)
                    {
                        yield return group;
                    }
                }
                if (complete)
                {
                    Logger.LogInformation("[GetGroupsAsync] Complete.");
                }
            }
        }

        internal async IAsyncEnumerable<ProjectNodeDto> GetGroupProjectsAsync(GitLabClient client, string projectGroupFullPath)
        {
            var complete = false;
            string afterToken = string.Empty;
            PageInfoDto pageInfo;
            while (!complete)
            {
                var assetBatch = (await client.GetProjectsByGroupAsync(projectGroupFullPath, afterToken, Config.BatchLimit)).Data.Group.Projects;
                pageInfo = assetBatch.PageInfo;
                complete = !(pageInfo?.HasNextPage ?? false);
                afterToken = complete ? string.Empty : pageInfo.EndCursor;
                complete = afterToken == string.Empty;

                foreach (var asset in assetBatch.Nodes)
                {
                    if (asset != null)
                    {
                        yield return asset;
                    }
                }
                if (complete)
                {
                    Logger.LogInformation("[GetAssetAsync] Complete.");
                }
            }
        }

        internal async IAsyncEnumerable<ScanNodeDto> GetScanByProjectAsync(GitLabClient client, string projectId)
        {
            var complete = false;
            string afterToken = string.Empty;
            PageInfoDto pageInfo;
            while (!complete)
            {
                var scanBatch = (await client.GetProjectScansAsync(projectId, afterToken, Config.BatchLimit)).Data.Project.Pipelines;
                pageInfo = scanBatch.PageInfo;
                complete = !(pageInfo?.HasNextPage ?? false);
                afterToken = complete ? string.Empty : pageInfo.EndCursor;
                complete = afterToken == string.Empty;

                foreach (var scan in scanBatch.Nodes)
                {
                    yield return scan;
                }
                if (complete)
                {
                    Logger.LogInformation("[GetScanByProjectAsync] Complete.");
                }
            }
        }

        internal async IAsyncEnumerable<IssueNodeDto> GetIssueByProjectAsync(GitLabClient client, string projectId)
        {
            var complete = false;
            string afterToken = string.Empty;
            PageInfoDto pageInfo;
            while (!complete)
            {
                var issueBatch = (await client.GetProjectVulnerabilitiesAsync(projectId, afterToken, Config.BatchLimit)).Data.Project.Vulnerabilities;
                pageInfo = issueBatch.PageInfo;
                complete = !(pageInfo?.HasNextPage ?? false);
                afterToken = complete ? string.Empty : pageInfo.EndCursor;
                complete = afterToken == string.Empty;

                foreach (var issue in issueBatch.Nodes)
                {
                    yield return issue;
                }
                if (complete)
                {
                    Logger.LogInformation("[GetIssueByProjectAsync] Complete.");
                }
            }
        }



        private QueueScan MapHistoryScan(ScanNodeDto scan, List<GraphQLResponse<IssueDataDto>> issues)
        {
            var now = DateTime.UtcNow;
            DateTime scanDate = now; // todo
            var queueScan = new QueueScan
            {
                Loading = false,
                Entity = new()
                {
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueScanInfo
                    {
                        Scan = new SaltMiner.Core.Entities.QueueScanInfo
                        {
                            AssessmentType = "",  // todo
                            Product = Product,
                            ReportId = scan.Id,
                            ScanDate = scanDate,
                            ProductType = "Container",
                            Vendor = Vendor,
                            AssetType = AssetType,
                            IsSaltminerSource = GitLabConfig.IsSaltminerSource,
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
            return queueScan;
        }


        private QueueScan MapScan(ProjectNodeDto project, string reportType, DateTime? scanCreateDate, bool noScan = false)
        {
            var sourceId = project.FullPath;
            var now = DateTime.UtcNow;

            // TODO: replace noscan logic, noscan scans require a valid assessment type.  Won't hurt for now as noscans aren't saved.
            var assessmentType = noScan ? reportType : GetAssessmentType(reportType);
            if (string.IsNullOrEmpty(assessmentType))
                throw new GitLabValidationException($"Invalid / unknown report type '{reportType}'.");
            Logger.LogDebug("[MapScan] Assessment type for project {Project} is {Atype}", project.FullPath, assessmentType);

            DateTime scanDate = scanCreateDate ?? now;

            var queueScan = new QueueScan
            {
                Loading = true,
                Entity = new()
                {
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueScanInfo
                    {
                        Scan = new SaltMiner.Core.Entities.QueueScanInfo
                        {
                            AssessmentType = assessmentType,
                            Product = Product,
                            ReportId = noScan ? GetNoScanReportId(assessmentType) : sourceId,
                            ScanDate = noScan ? now : scanDate,
                            ProductType = "Application",
                            Vendor = Vendor,
                            AssetType = AssetType,
                            IsSaltminerSource = GitLabConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
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

        private QueueAsset MapAsset(ProjectNodeDto project, QueueScan queueScan, bool isRetired = false)
        {
            var sourceId = $"{project.FullPath}";
            var queueAsset = new QueueAsset
            {
                Entity = new()
                {
                    Timestamp = DateTime.UtcNow,
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueAssetInfo
                    {
                        Internal = new()
                        {
                            QueueScanId = queueScan.Entity.Id
                        },
                        Asset = new SaltMiner.Core.Entities.AssetInfoPolicy
                        {
                            Description = project.Description,
                            Name = project.Name,
                            Attributes = new Dictionary<string, string> { { "group_name", project.Group.Name }, { "group_fullpath", project.Group.FullPath }, { "project_is_archived", project.Archived.ToString() },
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = GitLabConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = sourceId,
                            Version = project.Repository.RootRef,
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


        private void MapIssue(ProjectNodeDto project, IssueNodeDto issue, QueueScan queueScan, QueueAsset queueAsset, bool zeroRecord = false)
        {
            var sourceId = $"{project.FullPath}";
            List<QueueIssue> queueIssues = new();
            var assessmentType = queueScan.Entity.Saltminer.Scan.AssessmentType;

            if (zeroRecord)
            {
                LocalData.AddUpdate(GetZeroQueueIssue(queueScan, queueAsset));
            }
            else
            {
                Logger.LogInformation("[MapIssue] Adding queue issue {Title} with assessmentType {AssessmentType}", issue.Title, assessmentType);
                queueIssues.Add(new QueueIssue
                {
                    Entity = new()
                    {
                        Labels = new Dictionary<string, string>(),
                        Vulnerability = new SaltMiner.Core.Entities.VulnerabilityInfo
                        {
                            Id = issue.Identifiers.Select(x => CreateIssueId(x.ExternalId, x.Url)).ToArray(), // cve and other identifiers
                            Audit = new SaltMiner.Core.Entities.AuditInfo
                            {
                                Audited = true
                            },
                            Category = new string[1] { "Application" },
                            FoundDate = ConvertStringDate(issue.DetectedAt) ?? DateTime.Now,
                            LocationFull = issue.Location.File ?? issue.Location.Image ?? issue.Location.Path ?? issue.Location.Description ?? "unknown",
                            Location = "N/A", // todo - strip issue.Location path so it is only the file name
                            Name = issue.Title,
                            Description = issue.Description,
                            Recommendation = issue.Solution,
                            ReportId = issue.Id,
                            Scanner = new SaltMiner.Core.Entities.ScannerInfo
                            {
                                Id = issue.Id,
                                AssessmentType = assessmentType,
                                Product = issue.Scanner.Name ?? string.Empty,
                                Vendor = Vendor,
                                ApiUrl = issue.VulnerabilityPath,
                                GuiUrl = issue.WebUrl
                            },
                            Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, issue.Severity),
                            SourceSeverity = issue.Severity,
                            References = string.Join(", ", issue.Links.Select(x => x.Url)),
                            IsSuppressed = issue.State == "DISMISSED",
                            RemovedDate = null
                        },
                        Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                        {
                            Attributes = new Dictionary<string, string>
                            {
                                { "state", issue.State ?? string.Empty },
                                { "scanner_report_type", issue.Scanner.ReportType ?? string.Empty },
                                { "blob_path", issue.Location.BlobPath ?? string.Empty },
                                { "container_repository_url", issue.Location.ContainerRepositoryUrl ?? string.Empty }
                            },
                            QueueScanId = queueScan.Id,
                            QueueAssetId = queueAsset.Id,
                            Source = new SaltMiner.Core.Entities.SourceInfo
                            {
                                Analyzer = Product,
                            }
                        },
                        Tags = Array.Empty<string>(),
                        Timestamp = DateTime.UtcNow
                    }
                });
            }

            foreach (var queueIssue in queueIssues)
            {
                CustomAssembly?.CustomizeQueueIssue(queueIssue, project);
                LocalData.AddUpdate(queueIssue);
            }
        }

        private static string GetAssessmentType(string scannerReportType)
        {
            return scannerReportType.ToUpper() switch
            {
                "CONTAINER_SCANNING" => AssessmentType.Container.ToString("g"),
                "DEPENDENCY_SCANNING" => AssessmentType.Open.ToString("g"),
                "SECRET_DETECTION" => AssessmentType.DAST.ToString("g"),
                "SAST" => AssessmentType.SAST.ToString("g"),
                "SAST_IAC" => AssessmentType.SAST.ToString("g"),
                "DAST" => AssessmentType.DAST.ToString("g"),
                _ => string.Empty
            };
        }

        private static string CreateIssueId(string id, string url)
        {
            var urlValue = "";

            if (!string.IsNullOrEmpty(url))
            {
                urlValue = "|" + url;
            }
            return id + urlValue;
        }

        static DateTime? ConvertStringDate(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
            {
                return null;
            }
            string[] formats = {
                "yyyy-MM-ddTHH:mm:ss'Z'",
                "yyyy-MM-ddTHH:mm:ssZ",
                "yyyy-MM-ddTHH:mmzzz",
                "yyyy-MM-ddTHH:mm:sszzz"
            };

            foreach (var format in formats)
            {
                if (DateTimeOffset.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dateTimeOffset))
                {
                    return dateTimeOffset.UtcDateTime;
                }
            }
            return null;
        }
    }
}



