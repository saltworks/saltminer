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
using System.Diagnostics;
using System.Xml;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;
using static System.Net.Mime.MediaTypeNames;

namespace Saltworks.SaltMiner.SourceAdapters.Snyk
{
    public class SnykAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private SnykConfig Config;
        private readonly string AssetType = "app";
        private readonly ProgressHelper ProgressHelper;
                
        public SnykAdapter(DataClientFactory<DataClient.DataClient> dataFactory, ApiClientFactory<SourceAdapter> clientFactory, IServiceProvider provider, ILogger<SnykAdapter> logger) : base(dataFactory, clientFactory, provider, logger)
        {
            Logger.LogDebug("SonatypeAdapter Initialization complete.");
            ProgressHelper = new ProgressHelper(Logger);
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {

            try
            {
                #region Include: Get Config and Validate

                config = config ?? throw new ArgumentNullException(nameof(config));

                if (!(config is SnykConfig))
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(SnykConfig)}', but got '{config.GetType().Name}'");
                }

                Config = config as SnykConfig;

                Config.Validate();

                #endregion

                //Write
                ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, null, "RunAsync"));

                //Include: Check local metrics to make sure there is data, if not run FirstLoad
                FirstLoadSyncUpdate(config);
                
                //Include
                SetApiClientSslVerification(Config.VerifySsl);

                //Write
                var client = new SnykClient(ApiClient, Config, Logger);

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
                await Task.WhenAll(SyncAsync(client), SendAsync(ProgressHelper, Config, AssetType));

                //Include: This allows us to track the failure on trying to load any queuescan and reset to load agin until a configureable failure count is hit
                ResetFailures(Config);

                //Inlcude: This deletes any queuescans that hit that configurable failure count
                ResetFailures(Config);

                //Include: This is needed to give the app a moment to finish before finishing
                await Task.Delay(5, CancellationToken.None);

                //Write: End Timer for overall Run
                ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, null, "RunAsync"), 1);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex.InnerException?.Message ?? ex.Message, ex);
                throw;
            }
        }

        private async Task SyncAsync(SnykClient client)
        {
            CheckCancel();

            if (Config.SourceType != SourceType.Snyk.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{etype}' but was found to be '{atype}'", SourceType.Snyk.GetDescription(), Config.SourceType);
                throw new SnykValidationException("Invalid configuration - source type");
            }

            //Write: Start Timer for Sync
            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, "", "SyncAsync"));

            var exceptionCounter = 0;

            Logger.LogInformation($"[Sync] Starting...");
            Logger.LogInformation($"[Sync] Getting Organizations...");

            var orgs = await client.GetAllOrganizationsAsync();
            Logger.LogInformation($"[Sync] Received {orgs.Data.Count()} organizations");

            Logger.LogInformation($"Getting LocalMetrics...");
            var localMetrics = LocalData.GetSourceMetrics(Config.Instance, Config.SourceType).ToList();

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

            var localScans = 0;
            var localAssets = 0;
            var localIssues = 0;

            var totalProjectCount = 0;

            foreach (var org in orgs.Data)
            {
                if (Config.TestingAssetLimit > 0 && totalProjectCount >= Config.TestingAssetLimit)
                {
                    break;
                }

                Logger.LogInformation($"[Sync] Getting Projects for Organization ID {org.Id}...");
                var projects = (await client.GetAllProjectsAsync(org.Id));
                Logger.LogInformation($"[Sync] Received {projects.Data.Count()} projects ");

                try
                {
                    while (projects.Data.Any())
                    {
                        var projectCount = 0;
                        foreach (var project in projects.Data)
                        {
                            projectCount++;
                            totalProjectCount++;
                            // testing limit reached - break out of all processing
                            if (Config.TestingAssetLimit > 0 && totalProjectCount >= Config.TestingAssetLimit)
                            {
                                break;
                            }
                            Logger.LogInformation($"Getting SourceMetric for Project ID {project.Id}...");
                            //var issueCount = await client.GetTraceCountsAsync(org.OrganizationUuid, app.AppId);
                            SourceMetric metric = client.GetSourceMetric(project);

                            //Write: Start time for individual sourcemetric
                            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"));

                            //If Recoverymode loop through until you are on that sourcemetric
                            if (RecoveryMode)
                            {
                                if (SyncRecord.CurrentSourceId != metric.SourceId)
                                {
                                    //Write: End Timer for each skipped metric until recovery record hit
                                    ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"), 0, "Skipped due to recovery");
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
                            LocalData.AddUpdate(SyncRecord);

                            //Include: Get matching local metric to current metric
                            var localMetric = localMetrics.FirstOrDefault(x => x.SourceId == metric.SourceId);
                            if (localMetric != null)
                            {
                                //Include: If found set isProcessed to true for tracking and retiring records
                                localMetric.IsProcessed = true;
                            }

                            if (Config.FullSyncMaintEnabled)
                            {
                                FullSyncBatchProcess(SyncRecord, Config.FullSyncBatchSize);
                                // End of metrics and full sync processing is still true,
                                // means last batch did not meet batch size... reset source ID to cycle through again
                                // End of metrics and batch count is zero,
                                // means nothing was found to process (ie. maybe a source metric doesn't exist anymore)...reset source ID
                                if (projectCount == projects.Data.Count && (FullSyncProcessing || FullSyncBatchCount == 0))
                                {
                                    SyncRecord.FullSyncSourceId = "";
                                }
                            }

                            //Get all data needed to determine if local metric and source metric match
                            if (NeedsUpdate(metric, localMetric, Config.LogNeedsUpdate) || RecoveryMode)
                            {
                                Logger.LogInformation($"[Sync] Updating Config.Instance '{Config.Instance}', SourceType {Config.SourceType}, SourceId '{metric.SourceId}', AssetType '{AssetType}'");

                                // If source has a master list of assets, then we can check if there is a new scan for each asset
                                // in this case if there is no scan for this asset, then map a 'noscan' queueScan and queueAsset,
                                // but skip adding a queueIssue. That will be done when the manager processes
                                var noScan = metric.LastScan == null;

                                var issueCollection = await client.GetIssuesByOrgAsync(org.Id, project.Id, project.Type);
                                var issues = new List<IssueDto>();
                                // batch issues
                                while (issueCollection.Data.Any())
                                {
                                    foreach (var issue in issueCollection.Data)
                                    {
                                        issues.Add(issue);
                                    }

                                    issueCollection = (await client.GetIssuesByOrgAsync(org.Id, project.Id, project.Type, issueCollection.Links.Next));
                                    if (issueCollection.Data.Any())
                                    {
                                        Logger.LogInformation($"[Sync] Received {issueCollection.Data.Count()} more issues ");
                                    }
                                }

                                QueueScan queueScan = MapScan(project, issues, noScan);
                                localScans++;

                                QueueAsset queueAsset = MapAsset(project, queueScan, localMetric == null && localMetrics.Count > 0 && !Config.DisableRetire);
                                localAssets++;

                                if (queueScan.Entity.Saltminer.Internal.QueueStatus == QueueScanStatus.Cancel.ToString())
                                {
                                    continue;
                                }

                                if (!noScan)
                                {
                                    MapIssues(project, issues, queueScan, queueAsset);
                                }

                                CheckCancel(false);

                                localIssues = localIssues + queueScan.Entity.Saltminer.Internal.IssueCount;

                                queueScan.Loading = false;
                                LocalData.AddUpdate(queueScan);

                                ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"), queueScan.Entity.Saltminer.Internal.IssueCount, null);
                               
                                //Update Local metric with source metric, so always current
                                UpdateLocalMetric(metric, localMetric);
                                //Set recoverymode to false, for the next iteration
                                RecoveryMode = false;
                            }
                        }


                        // Batching projects
                        if (projects.Links.Next == null)
                        {
                            break;
                        }

                        projects = (await client.GetAllProjectsAsync(org.Id, projects.Links.Next));
                        if (projects.Data.Any())
                        {
                            Logger.LogInformation($"[Sync] Received {projects.Data.Count()} more projects ");
                        }
                        CheckCancel();
                    }
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

                // Batching Orgs
                if (orgs.Links.Next == null)
                {
                    break;
                }
                orgs = await client.GetAllOrganizationsAsync(orgs.Links.Next);
                if (orgs.Data.Any())
                {
                    Logger.LogInformation($"[Sync] Received {orgs.Data.Count()} more orgs ");
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

            //Include: indicate your done with this source metric
            SyncRecord.LastSync = (DateTime.UtcNow);
            SyncRecord.CurrentSourceId = null;
            SyncRecord.State = SyncState.Completed;
            LocalData.AddUpdate(SyncRecord);

            //Write: End Timer for entrie sync process
            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, "", "SyncAsync"));
        }


        private QueueScan MapScan(ProjectDto project, List<IssueDto> issues, bool noScan = false)
        {
            var sourceId = project.Id;

            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapScan"));
            var now = DateTime.UtcNow;
            var scanDate = project.Meta.LatestIssueCounts.UpdatedAt?.ToUniversalTime() ?? DateTime.UtcNow;
            var queueScan = new QueueScan
            {
                Loading = true,
                Entity = new()
                {
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueScanInfo
                    {
                        Scan = new SaltMiner.Core.Entities.QueueScanInfo
                        {
                            AssessmentType = AssessmentType.Container.ToString("g"),
                            Product = "Snyk",
                            ReportId = noScan ? GetNoScanReportId(AssessmentType.Container.ToString("g")) : sourceId,
                            ScanDate = noScan ? DateTime.UtcNow : scanDate,
                            ProductType = "Container",
                            Vendor = "Snyk",
                            AssetType = AssetType,
                            IsSaltminerSource = SnykConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = issues.Count,
                            QueueStatus = QueueScanStatus.Loading.ToString("g")
                        },
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

                    LocalData.DeleteQueueScan(queueScan.LocalId);

                    ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapScan"), 0, "CustomAssembnly CancelScan Set");

                    return queueScan;
                }
            }

            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapScan"), 1);

            return LocalData.AddUpdate(queueScan);
        }

        private QueueAsset MapAsset(ProjectDto project, QueueScan queueScan, bool isRetired = false)
        {
            var sourceId = $"{project.Id}";
            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapAsset"));

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
                            Description = project.Attributes.Name,
                            Name = project.Attributes.Name.Split(":")[0], // Name is ApplicationName:TargetFile. Split and get just ApplicationName
                            Attributes = new Dictionary<string, string>(),
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = SnykConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = sourceId,
                            Version = project.Attributes.TargetFile,
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


        private void MapIssues(ProjectDto project, List<IssueDto> issues, QueueScan queueScan, QueueAsset queueAsset)
        {
            var sourceId = $"{project.Id}";
            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"));

            List<QueueIssue> queueIssues = new();
            var nullIssueCounter = 1;
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
                            Category = new string[1] { "Container" },
                            FoundDate = DateTime.UtcNow,
                            Name = "ZeroIssue",
                            Location = "N/A",
                            LocationFull = "N/A",
                            ReportId = queueScan.Entity.Saltminer.Scan.ReportId,
                            Scanner = new SaltMiner.Core.Entities.ScannerInfo
                            {
                                Id = GetZeroScannerId(Config.SourceType, sourceId),
                                AssessmentType = AssessmentType.Container.ToString("g"),
                                Product = "Snyk",
                                Vendor = "Snyk"
                            },
                            Severity = Severity.Zero.ToString(),
                        },
                        Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                        {
                            Attributes = new Dictionary<string, string>(),
                            QueueScanId = queueScan.Id,
                            QueueAssetId = queueAsset.Id,
                            Source = new SaltMiner.Core.Entities.SourceInfo
                            {
                                Analyzer = "Snyk",
                            }
                        },
                        Tags = Array.Empty<string>(),
                        Timestamp = DateTime.UtcNow
                    }
                });
                nullIssueCounter++;
            }
            else
            {
                foreach (var issue in issues)
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
                                    Audited = true
                                },
                                Category = new string[1] { "Container" },
                                FoundDate = GetDate(issue.Attributes.Problems?.FirstOrDefault().DiscoveredAt),
                                LocationFull = issue.Attributes.Problems.FirstOrDefault().Uri ?? "N/A",
                                Location = issue.Attributes.Problems.FirstOrDefault().Uri ?? "N/A",
                                Name = issue.Attributes.Title ?? "N/A",
                                Description = issue.Attributes.Description ?? "",
                                ReportId = issue.Id,
                                Scanner = new SaltMiner.Core.Entities.ScannerInfo
                                {
                                    Id = issue.Id,
                                    AssessmentType = AssessmentType.Container.ToString("g"),
                                    Product = "Snyk",
                                    Vendor = "Snyk",
                                    GuiUrl = "" // issue.Paths - API schema doesn't have one?
                                },
                                Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, issue.Attributes.EffectiveSeverityLevel),
                                SourceSeverity = issue.Attributes.EffectiveSeverityLevel,
                                IsSuppressed = issue.Attributes.Ignored,
                                RemovedDate = GetRemovedDate(issue)
                            },
                            Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                            {
                                Attributes = new Dictionary<string, string> { 
                                    { "disclosed_at", GetDate(issue.Attributes.Problems?.FirstOrDefault().DisclosedAt).ToString() },
                                    { "status", issue.Attributes.Status }
                                },
                                QueueScanId = queueScan.Id,
                                QueueAssetId = queueAsset.Id,
                                Source = new SaltMiner.Core.Entities.SourceInfo
                                {
                                    Analyzer = "Snyk",
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
                    CustomAssembly.CustomizeQueueIssue(queueIssue, project);
                LocalData.AddUpdate(queueIssue);
            }
            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"), queueIssues.Count);
        }

        private DateTime? GetDate(DateTime? sourceDate)
        {
            if (sourceDate?.Year.ToString() == "1")
            {
                return DateTime.UtcNow;
            }

            return sourceDate;
        }

        private DateTime? GetRemovedDate(IssueDto issue)
        {
            if (issue.Attributes.Status.ToUpper() == "RESOLVED")
            {
                return issue.Attributes.Resolution?.ResolvedAt ?? DateTime.UtcNow;
            }

            return null;
        }
    }
}



