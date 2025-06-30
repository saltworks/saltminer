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
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

[assembly:InternalsVisibleTo("Saltworks.SaltMiner.SourceAdapters.IntegrationTests")]
namespace Saltworks.SaltMiner.SourceAdapters.MendSca
{
    public class MendScaAdapter : SourceAdapter
    {
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private MendScaConfig Config;
        private readonly string AssetType = "app";
        private readonly ConcurrentQueue<Product> ProductQueue = new();

        public MendScaAdapter(IServiceProvider provider, ILogger<MendScaAdapter> logger) : base(provider, logger)
        {
            Logger.LogDebug("MendScaAdapter Initialization complete.");
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            await base.RunAsync(config, token);
            try
            {
                if (config == null)
                {
                    throw new ArgumentNullException(nameof(config));
                }

                if (config is not MendScaConfig)
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(MendScaConfig)}', but got '{config.GetType().Name}'");
                }

                Config = config as MendScaConfig;
                CancelToken = token;
                Config.Validate();
                
                FirstLoadSyncUpdate(config);
                SetApiClientSslVerification(Config.VerifySsl);

                var client = new MendScaClient(ApiClient, Config, Logger);

                if (Config.HasCustomAssembly)
                {
                    CustomAssembly = AssemblyHelper.LoadClassAssembly<ISourceAdapterCustom>(Config.CustomAssemblyName, Config.CustomAssemblyType);
                    CustomAssembly.PreProcess();
                }

                StillLoading = true;

                await Task.WhenAll(GetAsync(client), SyncAsync(client), SendAsync(Config, AssetType));

                ResetFailures(Config);
                DeleteFailures(Config);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Unexpected failure in MendSca source adapter: [{type}] {err}", ex.GetType().Name, ex.InnerException?.Message ?? ex.Message);
                throw;
            }
        }

        internal void GetTest(MendScaClient client)
        {
            GetAsync(client).Wait();
        }

        internal void SyncTest(MendScaClient client, MendScaConfig config)
        {
            Config = config;

            if (Config.TestingAssetLimit > 100 || Config.TestingAssetLimit < 1)
            {
                Config.TestingAssetLimit = 100;
            }

            SyncAsync(client).Wait();
        }

        internal void SendTest()
        {
            SendAsync(Config, AssetType).Wait();
        }

        protected async Task GetAsync(MendScaClient client)
        {
            Logger.LogInformation("[Get] Loading products...");
            await client.LoadProductsAsync(ProductQueue, CancelToken);
        }

        protected async Task SyncAsync(MendScaClient client)
        {
            CheckCancel();

            if (Config.SourceType != SourceType.MendSca.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{etype}' but was found to be '{atype}'", SourceType.MendSca.GetDescription(), Config.SourceType);
                throw new MendScaValidationException("Invalid configuration - source type");
            }

            var exceptionCounter = 0;

            Logger.LogInformation($"[Sync] Starting...");
            while (ProductQueue.IsEmpty && client.StillLoading)
            {
                await Task.Delay(5000);
                Logger.LogInformation("[Sync] Waiting for [Get] to get started...");
            }
            var projectCount = 0;
            var localMetrics = LocalData.GetSourceMetrics(Config.Instance, Config.SourceType).ToList();
            var newLocalIssues = 0;
            var newLocalScans = 0;
            var newLocalAssets = 0;

            var SyncRecord = LocalData.CheckSyncRecordSourceForFailure(Config.Instance, Config.SourceType);
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

            var count = 0;
            while (!ProductQueue.IsEmpty && ProductQueue.TryDequeue(out Product product))
            {
                projectCount += product.Projects.Count;
                count++;
                
                Logger.LogInformation("[Sync] Processing org '{oName}', product '{pName}', {count} / {total} products for source '{source}'", product.OrganizationDetails.OrgName, product.ProductName, count, client.TotalProductCount, Config.Instance);
                if ((product.Projects?.Count ?? 0) < 1)
                {
                    Logger.LogInformation("[Sync] Org '{orgName}', product '{productName}' does not have any projects. Will not process.", product.OrganizationDetails.OrgName, product.ProductName);
                    continue;
                }
                foreach (var project in product.Projects)
                {
                    CheckCancel();
                    var metric = project.ToSourceMetric(product.ProductToken, Config.Instance, Config.SourceType, MendScaConfig.IsSaltminerSource);
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

                        var localMetric = localMetrics.Find(x => x.SourceId == metric.SourceId);
                        if (localMetric != null)
                        {
                            localMetric.IsProcessed = true;
                        }

                        var splitIndex = metric.SourceId.IndexOf("|");
                        var projectToken = metric.SourceId[(splitIndex + 1)..];
                        var skipUpdateChecks = new List<NeedsUpdateEnum> { NeedsUpdateEnum.IssueCount };
                        var alertsCount = -1;
                       
                        if (Config.IncludeCountsInMetrics)
                        {
                            skipUpdateChecks = null;
                            project.Alerts = await client.GetProjectAlertsAsync(projectToken);  // Pulls in alerts early, but we account for this later
                            CheckCancel(false);
                            alertsCount = project.Alerts.Count;
                            metric.IssueCount = alertsCount;
                        }

                        LoadIssueSeverityCounts(metric, project.Alerts);

                        if (Config.FullSyncMaintEnabled)
                        {
                            FullSyncBatchProcess(SyncRecord, Config.FullSyncBatchSize);
                            // End of metrics and full sync processing is still true,
                            // means last batch did not meet batch size... reset source ID to cycle through again
                            // End of metrics and batch count is zero,
                            // means nothing was found to process (ie. maybe a source metric doesn't exist anymore)...reset source ID
                            if (ProductQueue.IsEmpty && count == projectCount && (FullSyncProcessing || FullSyncBatchCount == 0))
                            {
                                SyncRecord.FullSyncSourceId = "";
                            }
                        }

                        if (NeedsUpdate(metric, localMetric, Config.LogNeedsUpdate, skipUpdateChecks) || RecoveryMode)
                        {
                            if (localMetric != null && Config.IncludeCountsInMetrics && metric.IssueCount != localMetric.IssueCount && metric.LastScan == localMetric.LastScan)
                            {
                                Logger.LogInformation("[Sync] WS-BUG? Issue counts differ but last scan date does not for this project.");
                            }
                            Logger.LogDebug("[Sync] Updating config '{config}', source type '{sourceType}', id '{sourceId}', asset type '{asset_type}'", Config.Instance, Config.SourceType, metric.SourceId, AssetType);

                            if (alertsCount == -1)
                            {
                                project.Alerts = await client.GetProjectAlertsAsync(projectToken);
                                alertsCount = project.Alerts.Count;
                            }
                            Logger.LogInformation("[Sync] Updating {orgName} Product/Project: '{productName}'/'{projectName}', {count} issue(s)", product.OrganizationDetails.OrgName, product.ProductName, project.ProjectName, alertsCount);

                            QueueScan queueScan = MapScan(product, project);
                            newLocalScans++;

                            // IsNotScanned is set when hydrating projects, so if somehow we have alerts and IsNotScanned that's an error.
                            if (metric.IsNotScanned && alertsCount > 0)
                            {
                                Logger.LogWarning("[Sync] {orgName} Product/Project: '{productName}'/'{projectName}' - last scan == created, but alerts were found.", product.OrganizationDetails.OrgName, product.ProductName, project.ProjectName);
                                metric.IsNotScanned = false; // fall back to this if alerts present
                            }
                            // If project never scanned send asset and scan only.
                            // Don't expect localMetric to be non-null if this is the case, but will work if it is
                            if (metric.IsNotScanned && !(localMetric?.IsNotScanned ?? false))
                            {
                                Logger.LogInformation("[Sync] Project '{projName}' has never been scanned. Adding asset.", project.ProjectName);
                                MapAsset(product, project, queueScan, false, true); // this includes saving the new asset
                                
                                queueScan.Loading = false;
                               
                                UpdateLocalMetric(metric, localMetric);
                                
                                LocalData.AddUpdate(queueScan); // update because changed Loading
                               
                                newLocalAssets++;
                                continue;
                            }

                            QueueAsset queueAsset = MapAsset(product, project, queueScan);
                            newLocalAssets++;

                            if (queueScan.Entity.Saltminer.Internal.QueueStatus == QueueScanStatus.Cancel.ToString("g"))
                            {
                                Logger.LogInformation("[Sync] Cancelling update for Project '{projectName}'. Usually caused by custom assembly.", project.ProjectName);
                                continue;
                            }

                            MapIssues(product, project, queueScan, queueAsset);

                            CheckCancel(false);

                            queueScan.Loading = false;

                            UpdateLocalMetric(metric, localMetric);
                            LocalData.AddUpdate(queueScan);

                            RecoveryMode = false;

                            newLocalIssues += queueScan.Entity.Saltminer.Internal.IssueCount;

                            await LetSendCatchUpAsync(Config);
                        }
                        else
                        {
                            Logger.LogDebug("[Sync] {orgName} Product/Project '{productName}'/'{projectName}' does not need an update.", product.OrganizationDetails.OrgName, product.ProductName, project.ProjectName);
                        }
                    }
                    catch (LocalDataException ex)
                    {
                        Logger.LogCritical(ex, "Local data exception during sync: {ex}", ex.InnerException?.Message ?? ex.Message);
                        StillLoading = false;
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exceptionCounter++;
                        Logger.LogWarning(ex, "{Instance} for {SourceType} Sync Processing Error {exceptionCounter}: {errMsg}", Config.Instance, Config.SourceType, exceptionCounter, ex.InnerException?.Message ?? ex.Message);

                        if (exceptionCounter == Config.SourceAbortErrorCount)
                        {
                            Logger.LogCritical(ex, "{Instance} for {SourceType} Exceeded {SourceAbortErrorCount} Sync Processing Errors: {errMsg}", Config.Instance, Config.SourceType, Config.SourceAbortErrorCount, ex.InnerException?.Message ?? ex.Message);
                            StillLoading = false;
                            break;
                        }
                    }

                    CheckCancel();
                }

                LocalData.SaveAllBatches(); // send any remaining pending docs to db

                while (ProductQueue.IsEmpty && client.StillLoading)
                {
                    Logger.LogInformation("[Sync] Waiting for [Get]...");
                    await Task.Delay(5000);
                }
            }
            // end foreach

            // Correct possible error condition
            if (client.StillLoading)
            {
                Logger.LogCritical("[Sync] Client still has products to load, but sync processing loop exited.");
                client.StillLoading = false;
            }

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
                Logger.LogDebug("Asset retirement processing disabled by configuration, skipping.");
            }

            SyncRecord.LastSync = DateTime.UtcNow;
            SyncRecord.CurrentSourceId = null;
            SyncRecord.State = SyncState.Completed;
            
            LocalData.AddUpdate(SyncRecord, true);
        }

        private void LoadIssueSeverityCounts(SourceMetric metric, List<ProjectAlertDto> alerts)
        {
            foreach (var alert in alerts)
            {
                if (alert.Vulnerability == null || !Config.VulnerabilityImportTypes.Contains(alert.Type.ToUpper()))
                {
                    // Skip if vulnerability type is not configured for import
                    continue;
                }
               
                var sev = alert.Vulnerability?.CVSS3Severity ?? alert.Vulnerability?.Severity ?? "";
                metric.IssueCountSev1 += sev.ToLower().Contains("critical") ? 1 : 0;
                metric.IssueCountSev2 += sev.ToLower().Contains("high") ? 1 : 0;
                metric.IssueCountSev3 += sev.ToLower().Contains("medium") ? 1 : 0;
                metric.IssueCountSev4 += sev.ToLower().Contains("low") ? 1 : 0;
            }
        }

        private QueueScan MapScan(Product product, Project project)
        {
            var sourceId = $"{product.ProductToken}|{project.ProjectToken}";
            var now = DateTime.UtcNow;
            var queueScan = new QueueScan
            {
                Loading = true,
                Entity = new()
                {
                    Saltminer = new()
                    {
                        Scan = new()
                        {
                            AssessmentType = AssessmentType.Open.ToString("g"),
                            Product = "MendSca",
                            ReportId = CreateUniqueReportId(project),
                            ScanDate = project.Vitals[0].LastUpdatedDate.ToUniversalTime(),
                            ProductType = "Open",
                            Vendor = "MendSca",
                            AssetType = AssetType,
                            IsSaltminerSource = MendScaConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new()
                        {
                            IssueCount = project.Alerts.Count,
                            QueueStatus = QueueScanStatus.Loading.ToString("g"),
                        },
                    },
                    Timestamp = now
                },
                Timestamp = now
            };
            
            if (CustomAssembly != null)
            {
                CustomAssembly.CustomizeQueueScan(queueScan, product);
                if (CustomAssembly.CancelScan)
                {
                    queueScan.Entity.Saltminer.Internal.QueueStatus = QueueScanStatus.Cancel.ToString("g");
                    LocalData.DeleteQueueScan(queueScan.Id); 
                    return queueScan;
                }
            }

            return LocalData.AddUpdate(queueScan);
        }

        private QueueAsset MapAsset(Product product, Project project, QueueScan queueScan, bool isRetired = false, bool isNoscan = false)
        {
            var sourceId = $"{product.ProductToken}|{project.ProjectToken}";
            var queueAsset = new QueueAsset
            {
                Entity = new()
                {
                    Timestamp = DateTime.UtcNow,
                    Saltminer = new()
                    {
                        Asset = new()
                        {
                            Description = $"{product.ProductName}",
                            Name = $"{product.ProductName}",
                            VersionId = project.ProjectToken,
                            Version = project.ProjectName,
                            Attributes = project.Tags.ToDictionary(),
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = MendScaConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = sourceId,
                            AssetType = AssetType,
                            LastScanDaysPolicy = Config.LastScanDaysPolicy,
                            IsRetired = isRetired
                        },
                        Internal = new()
                        {
                            QueueScanId = queueScan.Id,
                            NeverScanned = isNoscan
                        }
                    }
                }
            };
            queueAsset.Entity.Saltminer.Asset.Attributes.Add("WSOrganization", product.OrganizationDetails?.OrgName ?? "[Unknown]");

            var result = LocalData.AddUpdate(queueAsset);
            return result;
        }

        private void MapIssues(Product product, Project project, QueueScan queueScan, QueueAsset queueAsset)
        {
            var sourceId = $"{product.ProductToken}|{project.ProjectToken}";

            List<QueueIssue> queueIssues = new();

            var localIssues = LocalData.GetQueueIssues(queueScan.Id, queueAsset.Id); 

            // Design decision: if zero issues, send in a zero record, even if one may already exist.  Trust the Manager to prevent duplicates.
            // Use project alerts to determine if issues exist, not queueScan
            if (!project.Alerts.Exists(a => Config.VulnerabilityImportTypes.Contains(a.Type.ToUpper())))
            {
                queueIssues.Add(GetZeroQueueIssue(queueScan, queueAsset));
            }
            else
            {

                foreach (var alert in project.Alerts)
                {
                    if (localIssues.Any(x => x.Id == alert.AlertUuid))
                    {
                        // Skip if already exists
                        continue;
                    }
                    if (alert.Vulnerability == null || !Config.VulnerabilityImportTypes.Contains(alert.Type.ToUpper()))
                    {
                        // Skip if vulnerability type is not configured for import
                        continue;
                    }
                    // Dates aren't reliable in Mend - attempt these in order: CreationDate, Date, Time (millis convert), ModifiedDate, current date/time
                    var found = GetFoundDate(alert);

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
                                Category = [ "Application" ],
                                Description = "keyword",
                                FoundDate = found,
                                Id = [ CreateUniqueIssueId(alert) ],
                                LocationFull = alert.Library.Filename,
                                Location = alert.Library.Filename,
                                Name = alert?.Vulnerability?.Name ?? alert.Type,
                                ReportId = CreateUniqueReportId(project),
                                Scanner = new()
                                {
                                    Id = CreateUniqueIssueId(alert),
                                    AssessmentType = AssessmentType.Open.ToString("g"),
                                    Product = "MendSca",
                                    Vendor = "MendSca"
                                },
                                Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, alert?.Vulnerability?.CVSS3Severity ?? alert?.Vulnerability?.Severity ?? ""),
                                SourceSeverity = alert?.Vulnerability?.CVSS3Severity ?? alert?.Vulnerability?.Severity ?? ""
                            },
                            Saltminer = new()
                            {
                                IssueType = queueScan.Entity.Saltminer.Scan.AssessmentType,
                                Attributes = [],
                                QueueScanId = queueScan.Id,
                                QueueAssetId = queueAsset.Id,
                                Source = new()
                                {
                                    Analyzer = "MendSca"
                                }
                            },
                            Tags = [],
                            Timestamp = DateTime.UtcNow
                        }
                    });
                }
            }

            foreach (var queueIssue in queueIssues)
            {
                CustomAssembly?.CustomizeQueueIssue(queueIssue, product);
                LocalData.AddUpdate(queueIssue); 
            }
        }

        private string CreateUniqueIssueId(ProjectAlertDto alert)
        {
            return $"{alert.AlertUuid}-{alert.Vulnerability?.Name ?? alert.Type}";
        }

        private string CreateUniqueReportId(Project project)
        {
            return $"{project.ProjectToken}-{project.Vitals[0].LastUpdatedDate.ToUniversalTime():yyyy-MM-dd-hh-mm-ss}";
        }

        private DateTime GetFoundDate(ProjectAlertDto alert)
        {
            if (alert.CreationDate.Year > 1970)
            {
                return alert.CreationDate.ToUniversalTime();
            }
            else if (alert.Date.Year > 1970)
            {
                return alert.Date.ToUniversalTime();
            }
            else if (alert.Time > 0)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(alert.Time).UtcDateTime;
            }
            else if (alert.ModifiedDate.Year > 1970)
            {
                return alert.ModifiedDate.ToUniversalTime();
            }
            else return DateTime.UtcNow;
        }
    }
}



