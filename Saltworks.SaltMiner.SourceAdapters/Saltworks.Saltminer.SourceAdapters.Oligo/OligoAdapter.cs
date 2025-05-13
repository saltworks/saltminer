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
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.SaltMiner.SourceAdapters.Core.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.SourceAdapters.Oligo
{
    public class OligoAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private OligoConfig Config;
        private readonly string AssetType = "app";

        public OligoAdapter(IServiceProvider provider, ILogger<OligoAdapter> logger) : base(provider, logger)
        {
            Logger.LogDebug("OligoAdapter Initialization complete.");
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            await base.RunAsync(config, token);
            try
            {
                #region Include: Get Config and Validate

                config = config ?? throw new ArgumentNullException(nameof(config));

                if (!(config is OligoConfig))
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(OligoConfig)}', but got '{config.GetType().Name}'");
                }

                Config = config as OligoConfig;

                Config.Validate();

                #endregion

                //Include: Check local metrics to make sure there is data, if not run FirstLoad
                //FirstLoadSyncUpdate(config);

                //Include
                SetApiClientSslVerification(Config.VerifySsl);

                //Write
                var client = new OligoClient(ApiClient, Config, Logger);
                //var vulnerabilities = await client.GetVulnerabilitiesAsync();
                CancelToken = token;
                //Write: Possiblilty that custom assembly has custom code
                if (Config.HasCustomAssembly)
                {
                    CustomAssembly = AssemblyHelper.LoadClassAssembly<ISourceAdapterCustom>(Config.CustomAssemblyName, Config.CustomAssemblyType);
                    CustomAssembly.PreProcess();
                }
                //Include: This will allow SendAsync to keep running until your done Syncing records
                StillLoading = true;
                var sync = LocalData.GetSyncRecord(config.Instance, Config.SourceType);
                if (LocalData.GetQueueScans(Config.Instance, Config.SourceType).Any())
                {
                    // This shouldn't actually happen, as resume should fix all fails,
                    // but could be possible if the sync record is set to completed - which might be a desired situation
                    if (sync.State != SyncState.InProgress || string.IsNullOrEmpty(sync.Data))
                    {
                        Logger.LogInformation("Found unsent data from previous sync, will complete sending previous.");
                        ClearQueues();
                        Logger.LogInformation("Previous sync data sent, cancelling process (re-run to restart sync).");
                        throw new CancelTokenException();
                    }
                }

                if (sync.State != SyncState.InProgress)
                    ClearQueues();



                //Include: This will run sync/send synchrononusly until both finish
                await Task.WhenAll(SyncAsync(client, Config), SendAsync(Config, AssetType));

                //Include: This allows us to track the failure on trying to load any queuescan and reset to load agin until a configureable failure count is hit
                ResetFailures(Config);

                //Inlcude: This deletes any queuescans that hit that configurable failure count
                DeleteFailures(Config);

                //Include: This is needed to give the app a moment to finish before finishing
                await Task.Delay(5, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex.InnerException?.Message ?? ex.Message, ex);
                throw;
            }
        }
        internal async IAsyncEnumerable<VulnerabilityDTO> GetAsync(OligoClient client, OligoConfig config)
        {
            int limit = config.ClientBatchSize; 
            int offset = 1;
            int asyncBatchSize = 4;
            int count = 0;

            while (true)
            {
                var tasks = Enumerable.Range(offset, asyncBatchSize)
                    .Select(page => client.GetVulnerabilitiesAsync(limit, page))
                    .ToList();

                var results = await Task.WhenAll(tasks);

                var currentBatch = results.SelectMany(result => result).ToList();

                if (currentBatch.Count == 0)
                    break;

                foreach (var vulnerability in currentBatch)
                {
                    yield return vulnerability;
                }

                count += currentBatch.Count;
                Logger.LogInformation("[GetAsync] Vulnerabilities Added {Count}", count);

                offset += asyncBatchSize;

                CheckCancel();
            }
        }
        //internal async IAsyncEnumerable<VulnerabilityDTO> GetAsync(OligoClient client, OligoConfig config)
        //{
        //    //int limit = config.ClientBatchSize;
        //    int limit = 1500;
        //    int offset = 1;
        //    int count = 0;
        //    List<VulnerabilityDTO> currentBatch = new();
        //    //currentBatch = await client.GetVulnerabilitiesAsync(limit, offset);
        //    //foreach (VulnerabilityDTO vulnerability in currentBatch)
        //    //{
        //    //    yield return vulnerability;
        //    //}
        //    //int iterationCount = 0; // Counter to track iterations
        //    //do
        //    //{
        //    //    currentBatch = await client.GetVulnerabilitiesAsync(limit, offset);
        //    //    foreach (VulnerabilityDTO vulnerability in currentBatch)
        //    //    {
        //    //        yield return vulnerability;
        //    //    }

        //    //    count += limit;
        //    //    limit += 1;
        //    //    Logger.LogInformation("[GetAsync] Vulnerabilities Added {Count}", count);
        //    //    offset++;
        //    //    CheckCancel();

        //    //    iterationCount++; // Increment the iteration counter
        //    //} while (iterationCount < 2); // Exit after two iterations

        //    do
        //    {
        //        currentBatch = await client.GetVulnerabilitiesAsync(limit, offset);
        //        foreach (VulnerabilityDTO vulnerability in currentBatch)
        //        {
        //            yield return vulnerability;
        //        }

        //        count += limit;
        //        Logger.LogInformation("[GetAsync] Vulnerabilities Added {Count}", count);
        //        offset++;
        //        CheckCancel();

        //    } while (currentBatch.Count > 0);


        //}
        internal async Task SyncAsync(OligoClient client, OligoConfig config)
        {
            CheckCancel();
            //List<WorkItemDTO> workList = new();
            Dictionary<string, WorkItemDTO> workList = new();
            Dictionary<string, List<string>> cveIssueMap = new(); 
            List<string> cveList= new(); 
            

            await foreach (VulnerabilityDTO vulnerability in GetAsync(client, Config))
            {
                if(!cveList.Contains(vulnerability.CVE.Code))
                {
                    cveList.Add(vulnerability.CVE.Code);
                    cveIssueMap[vulnerability.CVE.Code] = new List<string>();
                }


                if (vulnerability.Image.Builds == null)
                {
                    throw new OligoException("[Sync] Vulnerability found with no builds");
                }

                DateTime parsedLastScannedDate = DateTime.Parse(vulnerability.Image.LastScannedAt.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind);
                 
                foreach (ImageBuildsDTO build in vulnerability.Image.Builds)
                {
                    if(workList.TryGetValue(build.Id, out WorkItemDTO workItem))
                    {

                          if (workItem.LastScan == DateTime.UtcNow.Date)
                          {
                              continue;
                          }
                        
                        QueueIssue newIssue = MapIssue(workItem, vulnerability);
                        try
                        {
                            if (!cveIssueMap.TryGetValue(vulnerability.CVE.Code, out var issueList))
                            {
                                issueList = new List<string>();
                                cveIssueMap[vulnerability.CVE.Code] = issueList;
                            }

                            issueList.Add(newIssue.Id);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "Error adding data to cveIssueMap for CVE: {CVE}, {msg}", vulnerability.CVE.Code, ex.Message);
                        }

                        workItem.IssueCount++;
                    }
                    else
                    {
                        var sourceMetric = LocalData.GetSourceMetric(Config.Instance, Config.SourceType, build.Id);
                        WorkItemDTO newWorkItemDTO = new();
                        if (sourceMetric != null && sourceMetric.LastScan == DateTime.UtcNow.Date)
                        {
                            newWorkItemDTO = new WorkItemDTO
                            {
                                Id = build.Id,
                                Digest = build.Digest,
                                Tags = build.Tags,
                                LastScan = DateTime.UtcNow.Date

                            };
                            workList[build.Id] = newWorkItemDTO;
                            continue;
                        }
                        QueueScan queueScan = MapScan(build.Id, 0, parsedLastScannedDate.Date);
                        QueueAsset queueAsset = MapAsset(vulnerability.Image.Name, build.Id, queueScan);
                        newWorkItemDTO = new WorkItemDTO
                        {
                            Id = build.Id,
                            Name = vulnerability.Image.Name,
                            QueueScanID = queueScan.Id,
                            QueueScanReportID = queueScan.Entity.Saltminer.Scan.ReportId,
                            QueueAssetID = queueAsset.Id,
                            Digest = build.Digest,
                            Tags = build.Tags,
                            LastScan = null,
                            IssueCount = 0,
                            Link = build.Link ?? " "
                        };
                        workList[build.Id] = newWorkItemDTO;
                        QueueIssue newIssue = MapIssue(newWorkItemDTO, vulnerability);
                        try
                        {
                            if (!cveIssueMap.TryGetValue(vulnerability.CVE.Code, out var issueList))
                            {
                                issueList = new List<string>();
                                cveIssueMap[vulnerability.CVE.Code] = issueList;
                            }

                            issueList.Add(newIssue.Id);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "Error adding data to cveIssueMap for CVE: {CVE}, {msg}", vulnerability.CVE.Code, ex.Message);
                        }



                        workList[build.Id].IssueCount++;
                    }   
                }
            }
            // Used with the merge approach for issues, might not be appropriate now
            

            // Used with the merge approach for issues, might not be appropriate now
            

            Logger.LogInformation("[Sync] Assets present: {Count}", workList.Count);

            //Write: Change Whitesource source type checking to your own.
            if (Config.SourceType != SourceType.Oligo.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{etype}' but was found to be '{atype}'", SourceType.Oligo.GetDescription(), Config.SourceType);
                //Throw an adapter-specific exception here (for example for WS it was WhiteSourceValidationException)
                throw new Exception("Invalid configuration - source type");
            }

            var exceptionCounter = 0;
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
            if (workList.Count == 0)
                Logger.LogWarning("[Sync] No projects in queue in a timely fashion.  Sync will not start.");
            int localIssues = 0;
            int localAssets = 0;
            Dictionary<string, CveDto> cveDict = new();

            int totalCves = cveList.Count; // Total number of CVEs
            int processedCves = 0;         // Counter for processed CVEs

            var progress = new Progress<int>(processedCves =>
            {
                double percentage = (processedCves / (double)totalCves) * 100;
                Logger.LogInformation("Progress: {percentage:F2}%", percentage);
            });

            int batchSize = 2; // Limit to 2 concurrent requests
            var cveQueue = new Queue<string>(cveList); // Queue for CVE processing

            while (cveQueue.Any())
            {
                // Dequeue a batch of CVEs
                var currentBatch = cveQueue.Take(batchSize).ToList();

                var cveTasks = currentBatch.Select(async cve =>
                {
                    var cveData = await client.GetCveAsync(cve);
                    lock (cveDict)
                    {
                        cveDict[cve] = cveData;
                    }

                    // Update progress
                    ((IProgress<int>)progress).Report(Interlocked.Increment(ref processedCves));
                }).ToList();

                // Wait for the batch to complete
                await Task.WhenAll(cveTasks);

                // Remove processed CVEs from the queue
                for (int i = 0; i < currentBatch.Count; i++)
                {
                    cveQueue.Dequeue();
                }
            }



            foreach (KeyValuePair<string, List<string>> kvp in cveIssueMap)
            {
                var cve = kvp.Key;
                List<string> issueIds = kvp.Value;

                foreach (string issueId in issueIds)
                {
                    try
                    {
                        QueueIssue currentIssue = LocalData.Get<QueueIssue>(issueId);

                        if (currentIssue == null)
                        {
                            Logger.LogError("QueueIssue not found for Issue ID: {IssueId}", issueId);
                            continue;
                        }

                        if (currentIssue.Entity == null)
                        {
                            Logger.LogError("Entity is null for Issue ID: {IssueId}", issueId);
                            continue;
                        }

                        if (currentIssue.Entity.Vulnerability == null)
                        {
                            Logger.LogError("Vulnerability is null for Issue ID: {IssueId}", issueId);
                            continue;
                        }

                        if (!cveDict.TryGetValue(cve, out var cveDetails) || cveDetails == null)
                        {
                            Logger.LogError("CVE details not found for CVE: {Cve}", cve);
                            continue;
                        }

                        currentIssue.Entity.Vulnerability.Description = cveDetails.Description;

                        LocalData.AddUpdate(currentIssue);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex,
                            "Error processing Issue ID: {IssueId} with CVE: {Cve}",
                            issueId,
                            cve);
                    }
                }
                Logger.LogInformation("Issues with CVE of {cve} enriched with CVE data", kvp.Key);
            }

            await Task.Delay(10000);
            Logger.LogDebug("Starting to send issues");
            foreach (KeyValuePair<string, WorkItemDTO> entry in workList)
            {
                string buildId = entry.Key;
                WorkItemDTO workItem = entry.Value;
                MergeIssueDates(workItem.Id, workItem.QueueScanID, workItem.QueueAssetID);
                QueueScan currentScan = LocalData.Get<QueueScan>(workItem.QueueScanID);
                currentScan.Entity.Saltminer.Internal.IssueCount = (int)workItem.IssueCount;
                try
                {
                    var metric = client.GetSourceMetric(currentScan);
                    //If Recoverymode loop through until you are on that sourcemetric
                    if (RecoveryMode)
                    {
                        if (SyncRecord.CurrentSourceId != null && SyncRecord.CurrentSourceId != currentScan.Id)
                        {
                            continue;
                        }
                        else
                        {
                            RecoveryMode = false;
                        }
                    }

                    //Include: Update Syncrecord to reflect the metric currently processed
                    SyncRecord.CurrentSourceId = currentScan.Id;
                    SyncRecord.State = SyncState.InProgress;
                    LocalData.AddUpdate(SyncRecord, true); // use true for second paramter to write this update immediately (no queuing)

                    var localMetric = localMetrics.FirstOrDefault(x => x.SourceId == metric.SourceId);
                    if (localMetric != null)
                    {
                        //Include: If found set isProcessed to true for tracking and retiring records
                        localMetric.IsProcessed = true;
                    }
                    currentScan.Loading = false;
                    LocalData.AddUpdate(currentScan);
                    localAssets++;
                    UpdateLocalMetric(metric, localMetric);
                    await LetSendCatchUpAsync(Config);
                    Logger.LogInformation("Scan with report id : {scanId} set to send", currentScan.ReportId);

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
            LocalData.SaveAllBatches();
            CheckCancel();

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
            SyncRecord.LastSync = (DateTime.UtcNow);
            SyncRecord.CurrentSourceId = null;
            SyncRecord.State = SyncState.Completed;
            LocalData.AddUpdate(SyncRecord, true);
            LocalData.SaveAllBatches(); // use this to flush all remaining queued database updates after completing sync processing
        }

        private void MergeIssueDates(string sourceId, string scanId, string assetId)
        {
            var localIssues = LocalData.GetQueueIssues(scanId, assetId).GetEnumerator();
            var serverIssues = GetServerIssues(sourceId).GetEnumerator();

            bool localHasMore = localIssues.MoveNext();
            bool serverHasMore = serverIssues.MoveNext();
            var currentIssue = localIssues.Current;

            while (localHasMore)
            {
                while (localHasMore && serverHasMore && (currentIssue?.Entity.Vulnerability.Scanner.Id.CompareTo(serverIssues?.Current?.Vulnerability.Scanner.Id) ?? 1) < 0)
                {
                    serverHasMore = serverIssues.MoveNext();
                }
                if ((serverIssues?.Current?.Vulnerability.Scanner.Id ?? "nullA") == (currentIssue?.Entity.Vulnerability.Scanner.Id ?? "nullB"))
                {
                    currentIssue.FoundDate = serverIssues.Current.Vulnerability.FoundDate;
                    LocalData.AddUpdate(currentIssue);
                    Logger.LogInformation("Issue added with issue date changed");
                    localHasMore = localIssues.MoveNext();
                    currentIssue = localIssues.Current;
                }
                while (!serverHasMore && localHasMore || localHasMore && (currentIssue?.Entity.Vulnerability.Scanner.Id.CompareTo(serverIssues?.Current?.Vulnerability.Scanner.Id) ?? -1) > 0)
                {
                    LocalData.AddUpdate(currentIssue);
                    localHasMore = localIssues.MoveNext();
                    currentIssue = localIssues.Current;
                }
            }
        }


        private IEnumerable<SaltMiner.Core.Entities.Issue> GetServerIssues(string sourceId)
        {
            var srch = new SearchRequest()
            {
                Filter = new()
                {
                    FilterMatches = new() {
                { "Saltminer.Asset.SourceId", sourceId },
                { "Saltminer.Asset.SourceType", Config.SourceType },
                { "Saltminer.Asset.Instance", Config.Instance }
            }
                },
                UIPagingInfo = new() { Size = 500, SortFilters = new() { { "Vulnerability.Scanner.Id", true } } }
            };

            var srsp = DataClient.IssueSearch(srch);
            var curPage = 1;

            while (srsp.Success && curPage <= srsp.UIPagingInfo.TotalPages)
            {
                foreach (var issue in srsp.Data)
                    yield return issue;

                curPage++;
                srch.UIPagingInfo.Page = curPage;

                if (curPage <= srsp.UIPagingInfo.TotalPages)
                    srsp = DataClient.IssueSearch(srch);
            }
        }

        private QueueScan MapScan(string buildId, int issueCount, DateTime scanDate, bool noScan = false)
        {
            DateTime dateNow = DateTime.UtcNow.Date;
            var reportId = $"{buildId}|{scanDate}";
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
                            Product = "Oligo",
                            ReportId = reportId,
                            ScanDate = noScan ? scanDate : scanDate,
                            ProductType = "Application",
                            Vendor = "Oligo",
                            AssetType = AssetType,
                            IsSaltminerSource = OligoConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = issueCount,
                            QueueStatus = QueueScanStatus.Loading.ToString("g")
                        }
                    },
                    Timestamp = dateNow
                },
                Timestamp = dateNow
            };

            if (CustomAssembly != null)
            {
                CustomAssembly.CustomizeQueueScan(queueScan, buildId);
                if (CustomAssembly.CancelScan)
                {
                    queueScan.Entity.Saltminer.Internal.QueueStatus = QueueScanStatus.Cancel.ToString();
                    LocalData.DeleteQueueScan(queueScan.Id);
                    return queueScan;
                }
            }
            return LocalData.AddUpdate(queueScan);
        }

        private QueueAsset MapAsset(string buildName, string buildId, QueueScan queueScan, bool isRetired = false)
        {
            var sourceId = buildId;
            var queueAsset = new QueueAsset
            {
                Entity = new()
                {
                    Timestamp = DateTime.UtcNow,
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueAssetInfo
                    {
                        Asset = new SaltMiner.Core.Entities.AssetInfoPolicy
                        {
                            Name = buildName,
                            Attributes = new Dictionary<string, string>(),
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = OligoConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = sourceId,
                            Version = sourceId,
                            AssetType = AssetType,
                            LastScanDaysPolicy = Config.LastScanDaysPolicy,
                            IsRetired = isRetired,

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

        private QueueIssue MapIssue(WorkItemDTO workItem, VulnerabilityDTO issue)
        {

            var scannerId = $"{issue.Id}|{workItem.Id}";
            DateTime foundDate = DateTime.UtcNow.Date;
            foreach(ImageBuildsDTO build in issue.Image.Builds)
            {
                if(build.Id == workItem.Id)
                {
                    foundDate = build.FirstScannedAt;
                }
            }

            VulnerabilityCveDTO vulnCveData = issue.CVE;
            QueueIssue queueIssue = new QueueIssue
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
                        // Assuming cveData.Code is a string or can be converted to a string
                        Id = vulnCveData.Cwes.Select(cwe => cwe.ToString()).Append(vulnCveData.Code).ToArray(),
                        Category = new string[1] { AssessmentType.Container.ToString("g") },
                        FoundDate = foundDate,
                        Location = "N/A",
                        LocationFull = "N/A",
                        Name = vulnCveData.Summary ?? vulnCveData.Code ?? "N/A",
                        Description = " ",
                        ReportId = workItem.QueueScanReportID,
                        Scanner = new()
                        {
                            Id = scannerId,
                            AssessmentType = AssessmentType.Container.ToString("g"),
                            Product = "Oligo",
                            Vendor = "Oligo",
                            GuiUrl = workItem.Link ?? " "
                        },
                        Severity = GetSeverity(vulnCveData.Cvss),
                        IsSuppressed = false
                    },
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                    {
                        IssueType = IssueType.Container.ToString("g"),
                        Attributes = new()
                        {
                            { "image_id", issue.Image.Id },
                            { "build_tags", string.Join(", ", workItem.Tags) },
                            { "digest_id", workItem.Digest },
                            { "dependencies_fixes", string.Join(", ", issue.Dependencies
                                .Select(dep => $"{dep.Name} | {dep.FixVersion}")) },
                            { "dependencies_links", string.Join(", ", issue.Dependencies
                                .Select(dep => $"{dep.Name} | {dep.Link}")) },
                            { "dependencies_states", string.Join(", ", issue.Dependencies
                                .Select(dep => $"{dep.Name} | {dep.State}")) },
                            { "vulnerable_functions", issue.VulnerableFunctions != null && issue.VulnerableFunctions.Any()
                                ? string.Join(", ", issue.VulnerableFunctions): "None" },
                            { "affected_versions", string.Join(", ", issue.Dependencies.Select(dep => $"{dep.Name} | {string.Join(", ", dep.AffectedVersions)}")) }

                        },
                        QueueScanId = workItem.QueueScanID,
                        QueueAssetId = workItem.QueueAssetID,
                        Source = new SaltMiner.Core.Entities.SourceInfo
                        {
                            Analyzer = "Oligo",
                        }
                    },
                    Tags = Array.Empty<string>(),
                    Timestamp = DateTime.UtcNow
                }
            };

            if (CustomAssembly != null)
            {
                CustomAssembly.CustomizeQueueIssue(queueIssue, workItem);
            }
            var result = LocalData.AddUpdate(queueIssue);

            return result;
        }


        public static async IAsyncEnumerable<VulnerabilityDTO> GetVulnerabilitiesGeneratorAsync(OligoClient client, OligoConfig config, int offset)
        {
            int limit = config.ClientBatchSize;
            List<VulnerabilityDTO> currentBatch = await client.GetVulnerabilitiesAsync(limit, offset);

            if (currentBatch.Count == 0)
            {
                yield break;
            }

            foreach (VulnerabilityDTO vulnerability in currentBatch)
            {
                yield return vulnerability;
            }
        }


        public static string GetSeverity(double value)
        {
            return value switch
            {
                >= 0.1 and <= 3.9 => "Low",
                >= 4.0 and <= 6.9 => "Medium",
                >= 7.0 and <= 8.9 => "High",
                >= 9.0 and <= 10.0 => "Critical",
                _ => "Info"
            };
        }

        //private string GetImageName(string imageId)
        //{   
        //    foreach(ImageDTO image in ImagesQueue)
        //    {
        //        if(image.Id == imageId)
        //        {
        //            return image.Name;
        //        }
        //    }
        //    return null;
        //}

        // Not all sources allow for history reports
        // NOTE: History field below should not be used unless you have to. It is only for special cases.
        //private QueueScan MapScan(?? appReport, List<??> historyReports, bool noScan = false)
        //{
        //    var sourceId = ?;

        //    ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapScan"));

        //    var queueScan = new QueueScan
        //    {
        //        Loading = true,
        //        History = historyReports.Select(x => MapHistoryScan(x)).ToList(),
        //        Index = QueueScan.GenerateIndex(),
        //        Instance = ?,
        //        IsSaltminerSource = ?,
        //        SourceType = ?,
        //        Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueScanInfo
        //        {
        //            AssessmentType = SaltMiner.Core.Entities.Config.AssessmentType,
        //            Product = ?,
        //            ReportId = noScan ? GetNoScanReportId(AssessmentType.?.ToString("g")) : ?,
        //            ScanDate = noScan ? DateTime.UtcNow : ?,
        //            Type = ?,
        //            Vendor = ?,
        //        },
        //        IssueCount = noScan ? 1 : ?,
        //        Timestamp = DateTime.UtcNow,
        //        QueueStatus = QueueScanStatus.Loading.ToString("g"),
        //        AssetType = ?
        //    };

        //    if (CustomAssembly != null && !noScan)
        //    {
        //        CustomAssembly.CustomizeQueueScan(queueScan, appReport);
        //        if (CustomAssembly.CancelScan)
        //        {
        //            queueScan.Entity.Saltminer.Internal.QueueStatus = QueueScanStatus.Cancel.ToString();
        //            LocalData.DeleteQueueScan(queueScan.LocalId); }); 
        //            ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapScan"), 0, "CustomAssembnly CancelScan Set");
        //            return queueScan;
        //        }
        //    }

        //    ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapScan"), 1);

        //    return LocalData.AddUpdate(queueScan); });
        //}

        //private QueueScan MapHistoryScan(?? appReport)
        //{
        //    //Write Mapper for QueueAsset
        //    var queueScan = new QueueScan();

        //    if (CustomAssembly != null)
        //    {
        //        CustomAssembly.CustomizeQueueScan(queueScan, appReport);
        //        if (CustomAssembly.CancelScan)
        //        {
        //            queueScan.Entity.Saltminer.Internal.QueueStatus = QueueScanStatus.Cancel.ToString();
        //            return queueScan;
        //        }
        //    }

        //    return queueScan;
        //}

        //private QueueAsset MapAsset(?? appReport, QueueScan queueScan, bool isRetired = false)
        //{
        //    var sourceId = ?;

        //    ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapAsset"));

        //    //Write Mapper for QueueAsset
        //    var queueAsset = new QueueAsset();


        //    var result = LocalData.GetAll<QueueAsset>().FirstOrDefault(s => s.Saltminer.Asset.SourceType == Config.SourceType && s.Saltminer.Asset.SourceId == sourceId) ?? LocalData.AddUpdate(queueAsset); });

        //    ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapAsset"), 1);

        //    return result;
        //}

        //private void MapIssues(?? appReport, QueueScan queueScan, QueueAsset queueAsset)
        //{
        //    var sourceId = ??;

        //    ProgressHelper.StartTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"));

        //    List<QueueIssue> queueIssues = new();

        //    var localIssues = LocalData.GetIssues(queueScan.LocalId, queueAsset.LocalId); });
        //    // Use GetZeroQueueIssue() for zero records (queue scan found with no issues)


        //    if (queueScan.Entity.Saltminer.Internal.IssueCount == 0)
        //    {    
        //        //Mapper for empty issue should only need these fields actually filled and the rest defaulted
        //        queueIssues.Add(new QueueIssue
        //        {
        //            Vulnerability = new SaltMiner.Core.Entities.VulnerabilityInfo
        //            {
        //                Audit = new SaltMiner.Core.Entities.AuditInfo
        //                {
        //                    Audited = true,
        //                    Auditor = "",
        //                    LastAudit = DateTime.UtcNow
        //                },
        //                Category = new string[1] { ? },
        //                FoundDate = ?, // check for orginal found or null
        //                ReportId = ?,
        //                Scanner = new SaltMiner.Core.Entities.ScannerInfo
        //                {
        //                    AssessmentType = ?,
        //                    Product = ?,
        //                    Vendor = ?
        //                },
        //                Severity = Severity.Zero.ToString("g")
        //            },
        //            Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
        //            {
        //                QueueScanId = queueScan.Id,
        //                QueueAssetId = queueAsset.Id,
        //                Source = new SaltMiner.Core.Entities.SourceInfo
        //                {
        //                    Analyzer = ?,
        //                }
        //            },
        //            Tags = new string[1] { ? },
        //            Timestamp = DateTime.UtcNow
        //        });
        //    }
        //    else
        //    {
        //        foreach (var issue in ??)
        //        {
        //            //If this property is needed, it will need to come from SecurityIssues in raw data from Report API
        //            //if (localIssues.Any(x => x.Id == violation.Reference))
        //            //{
        //            //    continue;
        //            //}
        //
        //            //Write mapper for queue issue
        //            queueIssues.Add(new QueueIssue());;
        //        }
        //    }


        //    foreach (var queueIssue in queueIssues)
        //    {
        //        if (CustomAssembly != null)
        //        {
        //            CustomAssembly.CustomizeQueueIssue(queueIssue, appReport);
        //        }

        //        LocalData.AddUpdate(queueIssue); }); 
        //    }

        //    ProgressHelper.CompleteTimer(GetProgressKey(Config.Instance, Config.SourceType, AssetType, sourceId, "MapIssues"), queueIssues.Count());
        //}
    }
}




