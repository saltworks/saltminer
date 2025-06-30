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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.SourceAdapters.WhiteSource
{
    public class WhiteSourceAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private WhiteSourceConfig Config;
        private readonly string AssetType = "app";
        private readonly ProgressHelper ProgressHelper;

        public WhiteSourceAdapter(DataClientFactory<DataClient.DataClient> dataFactory, ApiClientFactory<SourceAdapter> clientFactory, IServiceProvider provider, ILogger<WhiteSourceAdapter> logger) : base(dataFactory, clientFactory, provider, logger)
        {
            Logger.LogDebug("WhiteSourceAdapter Initialization complete.");
            ProgressHelper = new ProgressHelper(Logger);
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            try
            {
                if (config == null)
                {
                    throw new ArgumentNullException(nameof(config));
                }
                if (config is not WhiteSourceConfig)
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(WhiteSourceConfig)}', but got '{config.GetType().Name}'");
                }
                Config = config as WhiteSourceConfig;
                CancelToken = token;
                Config.Validate();
                
                ProgressHelper.StartTimer(GetProgressKey(Config.Instance, config.SourceType, AssetType, null, "RunAsync"));

                FirstLoadSyncUpdate(config);
                SetApiClientSslVerification(Config.VerifySsl);

                var client = new WhiteSourceClient(ApiClient, Config, Logger);

                if (Config.HasCustomAssembly)
                {
                    CustomAssembly = AssemblyHelper.LoadClassAssembly<ISourceAdapterCustom>(Config.CustomAssemblyName, Config.CustomAssemblyType);
                    CustomAssembly.PreProcess();
                }

                StillLoading = true;
                await Task.WhenAll(SyncAsync(client), SendAsync(ProgressHelper, Config, AssetType));
                ResetFailures(Config);
                DeleteFailures(Config);
                //await Task.Delay(5, CancellationToken.None);

                ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, null, "RunAsync"), 1);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Unexpected failure in WhiteSource source adapter: [{type}] {err}", ex.GetType().Name, ex.InnerException?.Message ?? ex.Message);
                throw;
            }
        }

        public void SyncTest(WhiteSourceClient client, WhiteSourceConfig config)
        {
            Config = config;
            if (Config.TestingAssetLimit > 100 || Config.TestingAssetLimit < 1)
                Config.TestingAssetLimit = 100;
            SyncAsync(client).Wait();
        }

        public void SendTest()
        {
            SendAsync(ProgressHelper, Config, AssetType).Wait();
        }

        protected async Task SyncAsync(WhiteSourceClient client)
        {
            CheckCancel();

            if (Config.SourceType != SourceType.WhiteSource.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{etype}' but was found to be '{atype}'", SourceType.WhiteSource.GetDescription(), Config.SourceType);
                throw new WhiteSourceValidationException("Invalid configuration - source type");
            }

            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, "", "SyncAsync"));

            var exceptionCounter = 0;

            Logger.LogInformation($"Getting Products...");
            var products = (await client.GetHydratedProductsAsync()).ToList();
            var projectCount = 0;
            foreach (var product in products)
            {
                projectCount += product.Projects.Count;
            }
            Logger.LogInformation("[Sync] Received {productTotal} total products and {projectTotal} total projects", products.Count, projectCount);
            Logger.LogInformation("[Sync] Getting SourceMetrics...");
            var sourceMetrics = client.SourceMetrics(products).Results.ToList();
            Logger.LogInformation("[Sync] Received {total} source metrics. Beginning processing", sourceMetrics.Count);

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
            var count = 0;
            foreach (var metric in sourceMetrics)
            {
                count++;
                if (count % 100 == 0)
                {
                    Logger.LogInformation("Processed {count} / {total} source metrics for source '{source}'", count, sourceMetrics.Count, Config.Instance);
                }
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

                    SyncRecord.CurrentSourceId = metric.SourceId;
                    SyncRecord.State = SyncState.InProgress;
                    LocalData.AddUpdate(SyncRecord);

                    var localMetric = localMetrics.FirstOrDefault(x => x.SourceId == metric.SourceId);
                    if (localMetric != null)
                    {
                        localMetric.IsProcessed = true;
                    }

                    var splitIndex = metric.SourceId.IndexOf("|");
                    var productToken = metric.SourceId[..splitIndex];
                    var projectToken = metric.SourceId[(splitIndex + 1)..];
                    var product = products.First(x => x.ProductToken == productToken);

                    if (string.IsNullOrEmpty(projectToken))
                    {
                        Logger.LogInformation("[Sync] Product (Organization: '{orgName}') '{productName}' does not have any projects. Will not process.", product.OrganizationDetails.OrgName, product.ProductName);
                        continue;
                    }

                    var project = product.Projects.First(x => x.ProjectToken == projectToken);
                    var skipUpdateChecks = new List<NeedsUpdateEnum> { NeedsUpdateEnum.IssueCount };
                    var alertsCount = -1;
                    if (Config.IncludeCountsInMetrics) 
                    { 
                        skipUpdateChecks = null;
                        project.Alerts = await client.GetProjectAlertsAsync(projectToken);
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
                        if (count == sourceMetrics.Count && (FullSyncProcessing || FullSyncBatchCount == 0))
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
                        Logger.LogInformation("[Sync] Updating {orgName} Product/Project: '{productName}'/'{projectName}' - {count} of {total}", product.OrganizationDetails.OrgName, product.ProductName, project.ProjectName, count, sourceMetrics.Count);

                        if (alertsCount == -1)
                        {
                            project.Alerts = await client.GetProjectAlertsAsync(projectToken);
                            alertsCount = project.Alerts.Count;
                        }

                        Logger.LogInformation("[Sync] Project '{projName}' issue count {alertsCount}", project.ProjectName, alertsCount);

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
                            MapAsset(product, project, queueScan); // this includes saving the new asset
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

                        LocalData.AddUpdate(queueScan);
                        UpdateLocalMetric(metric, localMetric);

                        RecoveryMode = false;

                        ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"), queueScan.Entity.Saltminer.Internal.IssueCount, null);
                        newLocalIssues += queueScan.Entity.Saltminer.Internal.IssueCount;
                    }
                    else
                    {
                        Logger.LogDebug("[Sync] {orgName} Product/Project '{productName}'/'{projectName}' does not need an update.", product.OrganizationDetails.OrgName, product.ProductName, project.ProjectName);
                        ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, metric.SourceId, "SyncAsync"), 0, "Skipped due to not needing update");
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
            // end foreach

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

        private void LoadIssueSeverityCounts(SourceMetric metric, List<ProjectAlertDTO> alerts)
        {
            foreach (var alert in alerts)
            {
                if (alert.Vulnerability == null || !Config.VulnerabilityImportTypes.Contains(alert.Type.ToUpper()))
                {
                    // Skip if vulnerability type is not configured for import
                    continue;
                }

                metric.IssueCountSev1 += GetIssueSeverityCount("high", alert?.Vulnerability?.CVSS3Severity ?? alert?.Vulnerability?.Severity ?? "");
                metric.IssueCountSev2 += GetIssueSeverityCount("medium", alert?.Vulnerability?.CVSS3Severity ?? alert?.Vulnerability?.Severity ?? "");
                metric.IssueCountSev3 += GetIssueSeverityCount("low", alert?.Vulnerability?.CVSS3Severity ?? alert?.Vulnerability?.Severity ?? "");
            }
        }

        private long GetIssueSeverityCount(string severity, string vulSeverity)
        {
            if (vulSeverity.ToLower().Contains(severity))
            {
                return 1;
            }

            return 0;
        }

        private QueueScan MapScan(HydratedProduct product, HydratedProject project)
        {
            var sourceId = $"{product.ProductToken}|{project.ProjectToken}";

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
                            AssessmentType = AssessmentType.Open.ToString("g"),
                            Product = "WhiteSource",
                            ReportId = CreateUniqueReportId(project),
                            ScanDate = project.Vitals[0].LastUpdatedDate.ToUniversalTime(),
                            ProductType = "Open",
                            Vendor = "WhiteSource",
                            AssetType = AssetType,
                            IsSaltminerSource = WhiteSourceConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
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
                    LocalData.DeleteQueueScan(queueScan.LocalId); 
                    ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapScan"), 0, "CustomAssembnly CancelScan Set");
                    return queueScan;
                }
            }

            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapScan"), 1);

            return LocalData.AddUpdate(queueScan);
        }

        private QueueAsset MapAsset(HydratedProduct product, HydratedProject project, QueueScan queueScan, bool isRetired = false)
        {
            var sourceId = $"{product.ProductToken}|{project.ProjectToken}";

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
                            Description = $"{product.ProductName}",
                            Name = $"{product.ProductName}",
                            VersionId = project.ProjectToken,
                            Version = project.ProjectName,
                            Attributes = project.Tags.ToDictionary(),
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = WhiteSourceConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = sourceId,
                            AssetType = AssetType,
                            LastScanDaysPolicy = Config.LastScanDaysPolicy,
                            IsRetired = isRetired
                        }
                    }
                }
            };
            queueAsset.Entity.Saltminer.Asset.Attributes.Add("WSOrganization", product?.OrganizationDetails?.OrgName ?? "[Unknown]");

            var result = LocalData.AddUpdate(queueAsset);

            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapAsset"), 1);

            return result;
        }

        private void MapIssues(HydratedProduct product, HydratedProject project, QueueScan queueScan, QueueAsset queueAsset)
        {
            var sourceId = $"{product.ProductToken}|{project.ProjectToken}";

            ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"));

            List<QueueIssue> queueIssues = new();

            var localIssues = LocalData.GetQueueIssues(queueScan.LocalId, queueAsset.LocalId); 

            // Design decision: if zero issues, send in a zero record, even if one may already exist.  Trust the Manager to prevent duplicates.
            // Use project alerts to determine if issues exist, not queueScan
            if (!project.Alerts.Any(a => Config.VulnerabilityImportTypes.Contains(a.Type.ToUpper())))
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
                            Description = "keyword",
                            Name = "ZeroIssue",
                            Location = "N/A",
                            LocationFull = "N/A",
                            Scanner = new SaltMiner.Core.Entities.ScannerInfo
                            {
                                AssessmentType = AssessmentType.Open.ToString("g"),
                                Product = SourceType.WhiteSource.ToString("g"),
                                Vendor = "WhiteSource",
                                Id = GetZeroScannerId(Config.SourceType, sourceId)
                            },
                            Severity = Severity.Zero.ToString("g"),
                            FoundDate = DateTime.UtcNow,
                            ReportId = queueScan.Entity.Saltminer.Scan.ReportId
                        },
                        Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                        {
                            Attributes = new Dictionary<string, string>(),
                            QueueScanId = queueScan.Id,
                            QueueAssetId = queueAsset.Id,
                            Source = new SaltMiner.Core.Entities.SourceInfo
                            {
                                Analyzer = "WhiteSource"
                            }
                        },
                        Tags = Array.Empty<string>(),
                        Timestamp = DateTime.UtcNow
                    }
                });
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
                                Description = "keyword",
                                FoundDate = alert.CreationDate.ToUniversalTime(),
                                Id = CreateUniqueIssueId(alert),
                                LocationFull = alert.Library.Filename,
                                Location = alert.Library.Filename,
                                Name = alert?.Vulnerability?.Name ?? alert.Type,
                                ReportId = CreateUniqueReportId(project),
                                Scanner = new SaltMiner.Core.Entities.ScannerInfo
                                {
                                    Id = CreateUniqueIssueId(alert),
                                    AssessmentType = AssessmentType.Open.ToString("g"),
                                    Product = "WhiteSource",
                                    Vendor = "WhiteSource"
                                },
                                Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, alert?.Vulnerability?.CVSS3Severity ?? alert?.Vulnerability?.Severity ?? ""),
                                SourceSeverity = alert?.Vulnerability?.CVSS3Severity ?? alert?.Vulnerability?.Severity ?? ""
                            },
                            Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                            {
                                Attributes = new Dictionary<string, string>(),
                                QueueScanId = queueScan.Id,
                                QueueAssetId = queueAsset.Id,
                                Source = new SaltMiner.Core.Entities.SourceInfo
                                {
                                    Analyzer = "WhiteSource"
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
                    CustomAssembly.CustomizeQueueIssue(queueIssue, product);
                LocalData.AddUpdate(queueIssue); 
            }
            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"), queueIssues.Count);
        }

        private string CreateUniqueIssueId(ProjectAlertDTO alert)
        {
            return $"{alert.AlertUuid}-{alert?.Vulnerability?.Name ?? alert.Type}";
        }

        private string CreateUniqueReportId(HydratedProject project)
        {
            return $"{project.ProjectToken}-{project.Vitals[0].LastUpdatedDate.ToUniversalTime():yyyy-MM-dd-hh-mm-ss}";
        }
    }
}



