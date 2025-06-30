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
using System.Globalization;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.SourceAdapters.Polaris
{
    public class PolarisAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private PolarisConfig Config;
        private readonly string AssetType = "app";
        private readonly string Vendor = "Synopsys";
        private readonly string Product = "Polaris";
                
        public PolarisAdapter(IServiceProvider provider, ILogger<PolarisAdapter> logger) : base(provider, logger)
        {
            Logger.LogDebug("SonatypeAdapter Initialization complete.");
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            await base.RunAsync(config, token);
            try
            {
                #region Include: Get Config and Validate

                config = config ?? throw new ArgumentNullException(nameof(config));

                if (!(config is PolarisConfig))
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(PolarisConfig)}', but got '{config.GetType().Name}'");
                }

                Config = config as PolarisConfig;

                Config.Validate();

                #endregion

                //Include: Check local metrics to make sure there is data, if not run FirstLoad
                FirstLoadSyncUpdate(config);
                
                //Include
                SetApiClientSslVerification(Config.VerifySsl);

                //Write
                var client = new PolarisClient(ApiClient, Config, Logger);

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
                Logger.LogCritical(ex.InnerException?.Message ?? ex.Message, ex);
                throw;
            }
        }

        private async Task SyncAsync(PolarisClient client)
        {
            CheckCancel();

            if (Config.SourceType != SourceType.Polaris.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{etype}' but was found to be '{atype}'", SourceType.Polaris.GetDescription(), Config.SourceType);
                throw new PolarisValidationException("Invalid configuration - source type");
            }

            var exceptionCounter = 0;

            Logger.LogInformation($"[Sync] Starting...");
            Logger.LogInformation($"[Sync] Getting Projects...");

            var projectBranches = await client.GetProjectBranchesAsync();
            Logger.LogInformation($"[Sync] Received {projectBranches.Collection.ItemCount} total projects");
            Logger.LogInformation($"[Sync] First batch is {projectBranches.Items.Count()} projects");

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

            var projectCount = 0;
            var totalProjectCount = 0;
            string batchLink = string.Empty;

            while (projectBranches.Items.Any())
            {
                foreach (var projectBranch in projectBranches.Items)
                {
                    try
                    {
                        if (Config.TestingAssetLimit > 0 && totalProjectCount >= Config.TestingAssetLimit)
                        {
                            break;
                        }

                        // get project info for specific branch
                        var project = await client.GetProjectAsync(projectBranch.ProjectId);
                        projectBranch.Project = project;
                        projectBranch.Portfolio = await client.GetPortfolioItemAsync(project.PortfolioItemId);

                        // get scan info for specific branch
                        var scans = await client.GetScansByBranchAsync(projectBranch.Id);
                        while (scans.Items.Any())
                        {
                            projectBranch.Scans.AddRange(scans.Items);

                            // get next batch for branch scans
                            scans.Items = new();
                            batchLink = GetNextBatchLink(scans.Collection.CurrentPage, scans.Collection.PageCount, scans.Links);
                            if (batchLink != string.Empty)
                            {
                                scans = await client.GetScansByBranchAsync(projectBranch.Id, batchLink);
                                if (scans.Items.Any())
                                {
                                    Logger.LogInformation($"[Sync] Received {scans.Items.Count()} more scans.");
                                }
                            }
                        }


                        //Logger.LogInformation($"[Sync] Getting Projects for Organization ID {org.Id}...");
                        //Logger.LogInformation($"[Sync] Received {projects.Data.Count()} projects ");

                        projectCount++;
                        totalProjectCount++;
                        // testing limit reached - break out of all processing
                        if (Config.TestingAssetLimit > 0 && totalProjectCount >= Config.TestingAssetLimit)
                        {
                            break;
                        }
                        Logger.LogInformation($"Getting SourceMetric for Project ID {projectBranch.Id}...");
                        //var issueCount = await client.GetTraceCountsAsync(org.OrganizationUuid, app.AppId);
                        SourceMetric metric = client.GetSourceMetric(projectBranch);

                        //If Recoverymode loop through until you are on that sourcemetric
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

                        //Include: Update sync record to reflect the metric currently processed
                        SyncRecord.CurrentSourceId = metric.SourceId;
                        SyncRecord.State = SyncState.InProgress;
                        LocalData.AddUpdate(SyncRecord, true);

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
                            if (projectCount == projectBranches.Items.Count && (FullSyncProcessing || FullSyncBatchCount == 0))
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

                            var issueCollection = await client.GetIssuesByBranchAsync(projectBranch.ProjectId, projectBranch.Id);
                            var issues = new List<IssueDto>();
                            // batch issues
                            while (issueCollection.Items.Any())
                            {
                                foreach (var issue in issueCollection.Items)
                                {
                                    issues.Add(issue);
                                }

                                // get next batch of issues
                                issueCollection.Items = new();
                                batchLink = GetNextBatchLink(issueCollection.Collection.CurrentPage, issueCollection.Collection.PageCount, issueCollection.Links);
                                if (batchLink != string.Empty)
                                {
                                    issueCollection = await client.GetIssuesByBranchAsync(projectBranch.ProjectId, projectBranch.Id, batchLink);
                                    if (issueCollection.Items.Any())
                                    {
                                        Logger.LogInformation($"[Sync] Received {issueCollection.Items.Count()} more issues.");
                                    }
                                }
                            }

                            QueueScan queueScan = MapScan(projectBranch, issues, noScan);
                            localScans++;

                            QueueAsset queueAsset = MapAsset(projectBranch, queueScan, localMetric == null && localMetrics.Count > 0 && !Config.DisableRetire);
                            localAssets++;

                            if (queueScan.Entity.Saltminer.Internal.QueueStatus == QueueScanStatus.Cancel.ToString())
                            {
                                continue;
                            }

                            if (!noScan)
                            {
                                MapIssues(projectBranch, issues, queueScan, queueAsset);
                            }

                            CheckCancel(false);

                            localIssues = localIssues + queueScan.Entity.Saltminer.Internal.IssueCount;

                            queueScan.Loading = false;
                            LocalData.AddUpdate(queueScan);

                            //Update Local metric with source metric, so always current
                            UpdateLocalMetric(metric, localMetric);
                            //Set recoverymode to false, for the next iteration
                            RecoveryMode = false;
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
                }

                // get next batch for project branches
                projectBranches.Items = new();
                batchLink = GetNextBatchLink(projectBranches.Collection.CurrentPage, projectBranches.Collection.PageCount, projectBranches.Links);
                if (batchLink != string.Empty)
                {
                    projectBranches = await client.GetProjectBranchesAsync(batchLink);
                    if (projectBranches.Items.Any())
                    {
                        Logger.LogInformation($"[Sync] Received {projectBranches.Items.Count()} more projects.");
                    }
                }

                CheckCancel();
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

            //Include: indicate your done with this source metric
            SyncRecord.LastSync = (DateTime.UtcNow);
            SyncRecord.CurrentSourceId = null;
            SyncRecord.State = SyncState.Completed;
            LocalData.AddUpdate(SyncRecord);
            LocalData.SaveAllBatches();
            Logger.LogInformation("[Sync] Exiting sync loading phase in 5 sec...");
            await Task.Delay(5000);
            //Include: Set this to indiciate your done syncing
            StillLoading = false;
        }

        private string GetNextBatchLink(int currentPage, int pageCount, List<LinkDto> links)
        {
            var nextPage = currentPage + 1;
            if (nextPage <= pageCount)
            {
                var nextLink = links.Where(w => w.Rel == "next").Select(x => x.Href).SingleOrDefault();
                if (nextLink != null)
                {
                    return new Uri(nextLink).PathAndQuery.Replace("/api/", "");
                }
            }
            return string.Empty;
        }

        private QueueScan MapHistoryScan(ScanDto scan, List<IssueDto> issues)
        {
            var now = DateTime.UtcNow;
            DateTime scanDate = ConvertStringDate(scan.StartDate) ?? now;
            var queueScan = new QueueScan
            {
                Loading = false,
                Entity = new()
                {
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueScanInfo
                    {
                        Scan = new SaltMiner.Core.Entities.QueueScanInfo
                        {
                            AssessmentType = scan.AssessmentType,
                            Product = Product,
                            ReportId = scan.Id,
                            ScanDate = scanDate,
                            ProductType = "Container",
                            Vendor = Vendor,
                            AssetType = AssetType,
                            IsSaltminerSource = PolarisConfig.IsSaltminerSource,
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


        private QueueScan MapScan(ProjectBranchDto project, List<IssueDto> issues, bool noScan = false)
        {
            var sourceId = project.Id;
            var assessmentType = GetAssessmentType(project.Scans[0].AssessmentType);
            var now = DateTime.UtcNow;
            // scans are in start date (scan date) desc order. So, first scan in the list is most recent.
            var scanDate = ConvertStringDate(project.Scans[0].StartDate) ?? now;
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
                            ProductType = "Container",
                            Vendor = Vendor,
                            AssetType = AssetType,
                            IsSaltminerSource = PolarisConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = noScan ? 1 : issues.Count,
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

        private QueueAsset MapAsset(ProjectBranchDto branch, QueueScan queueScan, bool isRetired = false)
        {
            var sourceId = $"{branch.Id}";
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
                            Description = branch.Project.Description,
                            Name = branch.Project.Name,
                            Attributes = new Dictionary<string, string>(),
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = PolarisConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = sourceId,
                            Version = branch.Name,
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


        private void MapIssues(ProjectBranchDto branch, List<IssueDto> issues, QueueScan queueScan, QueueAsset queueAsset)
        {
            var sourceId = $"{branch.Id}";
            List<QueueIssue> queueIssues = new();
            var nullIssueCounter = 1;
            if (queueScan.Entity.Saltminer.Internal.IssueCount == 0)
            {
                queueIssues.Add(new QueueIssue
                {
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
                            FoundDate = DateTime.UtcNow,
                            Name = "ZeroIssue",
                            Location = "N/A",
                            LocationFull = "N/A",
                            ReportId = queueScan.Entity.Saltminer.Scan.ReportId,
                            Scanner = new SaltMiner.Core.Entities.ScannerInfo
                            {
                                Id = GetZeroScannerId(Config.SourceType, sourceId),
                                AssessmentType = queueScan.Entity.Saltminer.Scan.AssessmentType,
                                Product = Product,
                                Vendor = Vendor
                            },
                            Severity = Severity.Zero.ToString(),
                        },
                        Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                        {
                            IssueType = queueScan.Entity.Saltminer.Scan.AssessmentType,
                            Attributes = new Dictionary<string, string>(),
                            QueueScanId = queueScan.Entity.Id,
                            QueueAssetId = queueAsset.Entity.Id,
                            Source = new SaltMiner.Core.Entities.SourceInfo
                            {
                                Analyzer = Product,
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
                        Entity = new()
                        {
                            Labels = new Dictionary<string, string>(),
                            Vulnerability = new SaltMiner.Core.Entities.VulnerabilityInfo
                            {
                                Audit = new SaltMiner.Core.Entities.AuditInfo
                                {
                                    Audited = true
                                },
                                Category = new string[1] { "Application" },
                                FoundDate = ConvertStringDate(GetValueByKey("published-date", issue.Attributes)) ?? DateTime.UtcNow,
                                LocationFull = GetValueByKey("location", issue.Attributes) ?? "N/A",
                                Location = GetValueByKey("filename", issue.Attributes) ?? GetValueByKey("location", issue.Attributes) ?? "N/A",
                                Name = issue.Type.Localized.Name ?? "N/A",
                                Description = GetValueByKey("description", issue.Type.Localized.OtherDetail) ?? "",
                                ReportId = issue.Id,
                                Scanner = new SaltMiner.Core.Entities.ScannerInfo
                                {
                                    Id = issue.Id,
                                    AssessmentType = queueScan.Entity.Saltminer.Scan.AssessmentType,
                                    Product = Product,
                                    Vendor = Vendor,
                                    GuiUrl = GetGuiUrl(branch, issue),
                                    ApiUrl = issue.Links.Where(w => w.Rel == "self").Select(x => x.Href).SingleOrDefault()
                                },
                                Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, GetValueByKey("severity", issue.Attributes)),
                                SourceSeverity = GetValueByKey("severity", issue.Attributes),
                                IsSuppressed = false, //issue.Attributes.Ignored,
                                RemovedDate = null
                            },
                            Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                            {
                                Attributes = new Dictionary<string, string>(),
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
            }


            foreach (var queueIssue in queueIssues)
            {
                if (CustomAssembly != null)
                    CustomAssembly.CustomizeQueueIssue(queueIssue, branch);
                LocalData.AddUpdate(queueIssue);
            }
        }

        private string GetGuiUrl(ProjectBranchDto branch, IssueDto issue)
        {
            var Url = $"https://polaris.synopsys.com/portfolio/portfolios/{branch.Portfolio.PortfolioId}/portfolio-items/{branch.Project.PortfolioItemId}/projects/{branch.Project.Id}/issues/{issue.FamilyId}?filter=triageProperties%3Astatus%3Dnot-dismissed%2Cto-be-fixed";
            return Url;
        }

        private static string GetValueByKey(string key, List<KeyValueDto> keyValueObjects)
        {
            var result = keyValueObjects?.FirstOrDefault(x => x?.Key == key);
            return result?.Value;
        }

        private DateTime? ConvertStringDate(string date)
        {
            if (string.IsNullOrEmpty(date))
            {
                return null;
            }
            return TryParseDateToUtc(date);
        }

        static DateTime? TryParseDateToUtc(string dateString)
        {
            string[] formats = {
                "yyyy-MM-ddTHH:mm:ss'Z'",
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

        private string GetAssessmentType(string testAssessmentType)
        {
            if (testAssessmentType == "SAST")
            {
                return AssessmentType.SAST.ToString("g");
            }
            if (testAssessmentType == "DAST")
            {
                return AssessmentType.DAST.ToString("g");
            }
            if (testAssessmentType == "SCA")
            {
                return AssessmentType.Open.ToString("g");
            }
            return AssessmentType.SAST.ToString("g");
        }
    }
}



