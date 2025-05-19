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
using Saltworks.SaltMiner.SourceAdapters.Core.Helpers;
using Saltworks.SaltMiner.SourceAdapters.Core.Interfaces;
using Saltworks.Utility.ApiHelper;
using System.Runtime.CompilerServices;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

[assembly: InternalsVisibleTo("Saltworks.SaltMiner.SourceAdapters.IntegrationTests")]
namespace Saltworks.SaltMiner.SourceAdapters.Wiz
{
    public class WizAdapter : SourceAdapter
    {
        private ISourceAdapterCustom CustomAssembly = null;
        private WizConfig Config;
        private readonly string MyAssetType = AssetType.Net.ToString("g").ToLower();

        public WizAdapter(IServiceProvider services, ILogger<WizAdapter> logger) : base(services, logger)
        {
            Logger.LogDebug("WizAdapter Initialization complete.");
        }

        public async override Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            await base.RunAsync(config, token);
            try
            {
                ArgumentNullException.ThrowIfNull(config);

                if (config is not WizConfig)
                {
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(WizConfig)}', but got '{config.GetType().Name}'");
                }

                if (config.TestingAssetLimit > 0)
                {
                    Logger.LogWarning("TestingAssetLimit of {Amt} is in effect.  Wiz loading will stop at this count.", config.TestingAssetLimit);
                }

                Config = config as WizConfig;
                CancelToken = token;
                Config.Validate();

                SetApiClientSslVerification(Config.VerifySsl);

                var client = new WizClient(ApiClient, Config, Logger);

                if (Config.HasCustomAssembly)
                {
                    CustomAssembly = AssemblyHelper.LoadClassAssembly<ISourceAdapterCustom>(Config.CustomAssemblyName, Config.CustomAssemblyType);
                    CustomAssembly.PreProcess();
                }

                // If no sync record, this will return in progress as the status
                var sync = LocalData.GetSyncRecord(config.Instance, Config.SourceType);
                if ((LocalData.GetQueueScans(Config.Instance, Config.SourceTypeIssues).Any() || LocalData.GetQueueScans(Config.Instance, Config.SourceTypeVulns).Any()) &&
                    (sync.State != SyncState.InProgress || string.IsNullOrEmpty(sync.Data)))
                {
                    // This shouldn't actually happen, as resume should fix all fails,
                    // but could be possible if the sync record is set to completed - which might be a desired situation
                    Logger.LogInformation("Found unsent data from previous sync, will complete sending previous.");
                    if (Config.OverrideFromDate.HasValue || !string.IsNullOrEmpty(Config.OverrideWizType))
                        Logger.LogWarning("One or more override settings are present, but will be ignored due to resuming previous send.  Use -r (reset db) to avoid this.");
                    await Task.WhenAll(
                        SendAsync(Config, MyAssetType, Config.SourceTypeVulns),
                        SendAsync(Config, MyAssetType, Config.SourceTypeIssues));
                    ClearQueues();
                    Logger.LogInformation("Previous sync data sent, cancelling process (re-run to restart sync).");
                    throw new CancelTokenException();
                }

                if (sync.State != SyncState.InProgress || string.IsNullOrEmpty(sync.Data))
                    ClearQueues();

                StillLoading = true;

                await Task.WhenAll(SyncAsync(client),
                    SendAsync(Config, MyAssetType, Config.SourceTypeIssues),
                    SendAsync(Config, MyAssetType, Config.SourceTypeVulns));

                ResetFailures(Config);
            }
            catch (CancelTokenException ex)
            {
                Logger.LogInformation(ex, "Wiz adapter cancelling processing.");
                StillLoading = false;  // probably unnecessary
            }
            catch (SourceException ex)
            {
                if ((ex.InnerException?.GetType().Name ?? "") != typeof(TaskCanceledException).Name)
                {
                    Logger.LogCritical(ex, "Unexpected failure in Wiz source adapter: [{Type}] {Err}", ex.GetType().Name, ex.InnerException?.Message ?? ex.Message);
                    throw new WizException("Unexpected failure in Wiz source adapter.", ex);
                }
                // if we're just canceling, in the immortal words of Princess Elsa, "let it go"
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Unexpected failure in Wiz source adapter: [{Type}] {Err}", ex.GetType().Name, ex.InnerException?.Message ?? ex.Message);
                throw new WizException("Unexpected failure in Wiz source adapter.", ex);
            }
        }

        internal async Task SyncAsync(WizClient client)
        {
            // StillLoading = true, set outside this method
            CheckCancel();
            Logger.LogInformation("Current sync hold threshold: {Hold} send items remaining, resume: {Resume} items remaining.", Config.SyncHoldForSendThreshold, Config.SyncResumeWhenSendThreshold);

            try
            {
                var stype = SourceType.Wiz.GetDescription();
                if (Config.SourceType != stype)
                {
                    Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{EType}' but was found to be '{AType}'", stype, Config.SourceType);
                    throw new WizValidationException("Invalid configuration - source type");
                }

                var assetExceptionCounter = 0;

                Logger.LogInformation($"[Sync] Starting...");
                var newLocalIssues = 0;
                var newLocalScans = 0;
                var newLocalAssets = 0;
                var isDiffUpdate = true;
                // culture info doesn't matter, as long as it is consistent - this date is internal only
                var nextRunFromDate = DateTime.UtcNow.Date;

                // For this source we are processing either full reports or daily diffs.
                // We do not need to track the progress of either of these.
                // Instead we will track the next starting date for DIFF data pulls.

                // Setup sync record for current run - previous completed run placed our "updated after" date into the Data property
                Logger.LogDebug("[Sync] Pulling sync record to determine run type");
                var syncRecord = LocalData.GetSyncRecord(Config.Instance, Config.SourceType);
                var currRunFromDate = LastCreatedDate(); // look for previously synced data in SM data
                Logger.LogDebug("[Sync] Sync record run from date is {Date}", currRunFromDate);

                if (syncRecord.State != SyncState.InProgress || string.IsNullOrEmpty(syncRecord.Data))
                {
                    if (Config.OverrideFromDate.HasValue)
                    {
                        Logger.LogInformation("Using configured override from date '{Dt:o}'.", Config.OverrideFromDate);
                        currRunFromDate = Config.OverrideFromDate.Value;
                    }
                    if (currRunFromDate.HasValue && DateTime.UtcNow.Subtract(currRunFromDate.Value).Days <= Config.MaxApiDaysToPull)
                        isDiffUpdate = true; // since we have previously recorded data this should be a diff update
                    if (!currRunFromDate.HasValue)
                    {
                        isDiffUpdate = false;
                        currRunFromDate = new(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        ClearQueues(Config.SourceTypeIssues);
                        ClearQueues(Config.SourceTypeVulns);
                    }
                    var wizType = "";
                    if (!string.IsNullOrEmpty(Config.OverrideWizType))
                    {
                        Logger.LogInformation("Using configured override for wiz type: {Type}", Config.OverrideWizType == "i" ? "issues" : "vulns (this is default anyway)");
                        wizType = Config.OverrideWizType;
                    }

                    // Save new sync record
                    syncRecord.SetData(currRunFromDate.Value, vOri:wizType);  // take defaults on id
                    syncRecord.State = SyncState.InProgress;
                    LocalData.AddUpdate(syncRecord, true);
                }
                else
                {
                    if (Config.OverrideFromDate.HasValue || !string.IsNullOrEmpty(Config.OverrideWizType))
                        Logger.LogWarning("One or more override settings are present, but will be ignored due to resuming sync in progress.  Use -r (reset db) to avoid this.");
                    var prms = syncRecord.GetData();
                    var cdt = prms.Item1;
                    if (cdt != null)
                    {
                        currRunFromDate = cdt;
                        isDiffUpdate = true; // this may need to change if report support added
                        Logger.LogInformation("[Sync] Resuming previous run using from date '{Dt}' and id '{Id}', starting with wiz {VorI}.", prms.Item1, prms.Item2, prms.Item3 == "i" ? "issues" : "vulns");
                    }
                    else
                    {
                        throw new WizException("[Sync] Resume data found but failed to understand it.  Correct or remove the sync record.");
                    }
                }

                // Use case 1: initial load - load all assets from API.  REPORT API IS AVAILABLE BUT DEEMED TOO DIFFICULT TO USE
                // Use case 2: diff update - find updated assets in changed issues from API, then pull all issues for those assets and create queues
                // Using metrics to indicate the last scan date (only) by asset id.

                // 1. For each asset found in source, pull and process issues & vulns
                // DESIGN DECISION: empirical data shows that there are some issues that have no findings/vulns associated.  This means we must spin through all
                // vulns AND issues to ID updated assets.  These assets are then updated ONCE (both vulns and issues).
                Logger.LogInformation("[Sync] Begin loading data.");
                await foreach(var asset in GetAsync(client, syncRecord))
                {
                    Logger.LogDebug("[Sync] Begin asset foreach loop");

                    // break if configured to stop
                    if (Config.TestingAssetLimit > 0 && newLocalAssets >= Config.TestingAssetLimit)
                    {
                        Logger.LogInformation("[Sync] Stopping issue loading at test limit of {Count}.", newLocalAssets);
                        break;
                    }

                    // Had to do custom "wait for sync to catch up" functionality for Wiz because of dual source types
                    if (newLocalAssets % 50 == 0 && LocalData.CountQueueScans(Config.Instance, Config.SourceTypeIssues) + LocalData.CountQueueScans(Config.Instance, Config.SourceTypeVulns) >= Config.SyncHoldForSendThreshold)
                    {
                        do
                        {
                            CheckCancel(true);
                            Logger.LogInformation("[Sync] Waiting for Send to catch up...");
                            await Task.Delay(TimeSpan.FromMinutes(3), CancelToken);
                        } while (LocalData.CountQueueScans(Config.Instance, Config.SourceTypeIssues) + LocalData.CountQueueScans(Config.Instance, Config.SourceTypeVulns) > Config.SyncResumeWhenSendThreshold);
                    }

                    Logger.LogInformation("[Sync] Processing ({Count} so far) asset ID: {Id}, name: {Name}.", newLocalAssets, asset.Id, asset.Name);

                    var issueExceptionCounter = 0;
                    var currentThing = "[unknown]";
                    var metric = LocalData.GetSourceMetric(Config.Instance, Config.SourceType, asset.Id); // source ID value (asset.Id) also set in Vulnerabilities region
                    metric ??= new()
                    {
                        Instance = Config.Instance,
                        SourceType = Config.SourceType,
                        IsSaltminerSource = true,
                        SourceId = asset.Id,
                        LastScan = DateTime.MinValue
                    };
                    if (metric.LastScan >= currRunFromDate)
                    {
                        Logger.LogInformation("[Sync] Asset with ID {Id} doesn't need an update.", asset.Id);
                        continue;
                    }
                    try
                    {
                        Logger.LogDebug("[Sync] Begin issues import for asset");

                        #region Issues

                        var count = 0;
                        var firstIssue = true;
                        string sourceId = asset.Id;
                        QueueScan qScan = null;
                        QueueAsset qAsset = null;

                        // 1a. Issues - for current asset, pull and process issues
                        await foreach (var issue in GetListFromApiAsync<Issue>(client, asset.Id))
                        {
                            CheckCancel(true);
                            currentThing = "[unknown]";
                            try
                            {
                                // Dates in the API and report seem to be UTC, so not converting.
                                if (!isDiffUpdate)
                                    issue.ResolveReportFields(Config.WizUiIssueUriLeft, Config.WizUiIssueUriRight);
                                if (issue.Control == null || string.IsNullOrEmpty(issue.Control.Id) || string.IsNullOrEmpty(issue.Control.Name))
                                {
                                    Logger.LogWarning("[Sync] Invalid Wiz issue, missing required control information (ID and/or name).  Skipping.");
                                    continue;
                                }
                                else
                                {
                                    currentThing = $"ID: {issue.Control.Id}, Issue: {issue.Control.Name}, Asset: {issue.EntitySnapshot.Name}";
                                }
                                if (firstIssue)
                                {
                                    sourceId = string.IsNullOrEmpty(issue.EntitySnapshot?.Id) ? sourceId : issue.EntitySnapshot.Id;
                                    if (string.IsNullOrEmpty(sourceId))
                                        throw new WizValidationException("Invalid asset, couldn't determine source ID");
                                    qScan = MapScan<Issue>(sourceId);
                                    qAsset = MapAsset(qScan, issue, sourceId);
                                    newLocalScans++;
                                    newLocalAssets++;
                                }
                                MapIssue(issue, qScan, qAsset);
                                newLocalIssues++;
                                firstIssue = false;
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "[Sync] Failure loading issue '{Issue}': [{Type}] {Msg}", currentThing, ex.GetType().Name, ex.Message);
                                issueExceptionCounter++;
                                if (issueExceptionCounter > Config.MaxIssueOrVulnExceptions)
                                    throw new WizIssueFailedException($"[Sync] Max error count of {issueExceptionCounter} reached, aborting processing asset.");
                            }
                            count++;
                            if (count % 1000 == 0)
                            {
                                Logger.LogInformation("[Sync] Loading issues for asset ID {Id}, {Count} processed so far.", asset.Id, count);
                                Logger.LogDebug("Current issue: '{Issue}'", currentThing);
                            }
                        }
                        if (qScan == null)
                        {
                            Logger.LogDebug("[Sync] No issues found for asset with ID '{Id}' and name '{Name}'.", asset.Id, asset.Name);
                        }
                        else
                        {
                            qScan = MapScanLastScan(qScan, LocalData.GetLatestFoundDate(qScan.Id), sourceId);
                            qScan.Loading = false;
                            LocalData.AddUpdate(qScan);
                        }

                        #endregion

                        Logger.LogDebug("[Sync] End issues import, begin vulns import for asset");

                        #region Vulnerabilities

                        count = 0;
                        firstIssue = true;
                        sourceId = asset.Id;  // source ID value (asset.Id) also set in GetSourceMetric call above
                        qScan = null;
                        qAsset = null;
                        // 1b. Vulns - for current asset, pull and process vulnerabilities
                        await foreach (var vuln in GetListFromApiAsync<Vulnerability>(client, asset.Id))
                        {
                            CheckCancel(true);
                            currentThing = "[unknown]";
                            try
                            {
                                // Dates in the API and report seem to be UTC, so not converting.
                                if (!isDiffUpdate)
                                    vuln.ResolveReportFields(Config.WizUiVulnUriLeft, Config.WizUiVulnUriRight);
                                currentThing = $"ID: {vuln.Id}, Vuln: {vuln.Name}, Asset: {vuln.VulnerableAsset.Name}";
                                if (firstIssue)
                                {
                                    sourceId = vuln.VulnerableAsset.Id;
                                    if (string.IsNullOrEmpty(sourceId))
                                        throw new WizValidationException("Invalid asset, couldn't determine source ID");
                                    qScan = MapScan<Vulnerability>(sourceId);
                                    metric.LastScan = qScan.Entity.Saltminer.Scan.ScanDate;
                                    qAsset = MapAsset(qScan, vuln, sourceId);
                                    newLocalScans++;
                                    newLocalAssets++;
                                }
                                MapIssue(vuln, qScan, qAsset);
                                newLocalIssues++;
                                firstIssue = false;
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "[Sync] Failure loading vulnerability '{Vuln}': [{Type}] {Msg}", currentThing, ex.GetType().Name, ex.Message);
                                issueExceptionCounter++;
                                if (issueExceptionCounter > Config.MaxIssueOrVulnExceptions)
                                    throw new WizIssueFailedException($"[Sync] Max error count of {issueExceptionCounter} reached, aborting processing issues.");
                            }
                            count++;
                            if (count % 5000 == 0)
                            {
                                Logger.LogInformation("[Sync] Loading vulnerabilities for asset ID {Id}, {Count} processed so far.", asset.Id, count);
                                Logger.LogDebug("Current vulnerability: '{Vuln}'", currentThing);
                            }
                        }
                        if (qScan == null)
                        {
                            Logger.LogDebug("[Sync] No vulns found for asset with ID '{Id}' and name '{Name}'.", asset.Id, asset.Name);
                        }
                        else
                        {
                            qScan = MapScanLastScan(qScan, LocalData.GetLatestFoundDate(qScan.Id), sourceId);
                            qScan.Loading = false;
                            LocalData.AddUpdate(qScan);
                        }

                        #endregion

                        Logger.LogDebug("[Sync] End vulns import for asset");
                    }
                    catch (WizIssueFailedException ex)
                    {
                        Logger.LogError(ex, "[Sync] Failure loading asset with ID '{Id}': [{Type}] {Msg}", currentThing, ex.GetType().Name, ex.Message);
                        assetExceptionCounter++;
                        if (assetExceptionCounter > Config.MaxAssetExceptions)
                            throw new WizException($"[Sync] Max asset error count of {assetExceptionCounter} reached, aborting processing.");
                    }
                }
                Logger.LogInformation("[Sync] Loading complete, totals: {Scans} qScans, {Assets} qAssets, {Issues} qIssues", newLocalScans, newLocalAssets, newLocalIssues);

                // 2. Record run completion information in sync record
                Logger.LogInformation("[Sync] Updating sync record with run completion information");
                syncRecord.LastSync = DateTime.UtcNow;
                syncRecord.State = SyncState.Completed;
                syncRecord.SetData(nextRunFromDate); // take defaults on id and vOri
                LocalData.AddUpdate(syncRecord, true);
                LocalData.SaveAllBatches(); // send remaining queued entities to db
                Logger.LogInformation("[Sync] Exiting sync loading phase in 5 sec...");
                await Task.Delay(5000); // make sure on short runs to avoid race condition of finishing in the gap...
                StillLoading = false;
                Logger.LogInformation("[Sync] Sync complete.");


                // TODO: design and implement basic orphan handling &&/|| retirement
                //if (!Config.DisableRetire)
                //{
                //    try
                //    {
                //        RetireLocalMetrics(localMetrics);
                //        RetireQueueAssets(localMetrics, MyAssetType, Config);
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

                if (newLocalScans == 0)
                    Logger.LogInformation("No new Wiz data found to import since {Dt:o}", currRunFromDate);
            }
            catch (CancelTokenException ex)
            {
                Logger.LogWarning(ex, "[Sync] Cancellation requested, aborting processing.");
                StillLoading = false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[Sync] Aborting Wiz sync due to exception: [{Type}] {Msg}", ex.GetType().Name, ex.Message);
                StillLoading = false;
            }
            Logger.LogDebug("[Sync] End asset import foreach loop");
        }

        private DateTime? LastCreatedDate()
        {
            var srch = new SearchRequest()
            {
                Filter = new()
                { FilterMatches = new() {
                    { "Saltminer.Asset.SourceType", Config.SourceTypeVulns },
                    { "Saltminer.Asset.Instance", Config.Instance }
                } },
                UIPagingInfo = new() { Size = 1, SortFilters = new() { { "Timestamp", false } } }
            };
            var srsp = DataClient.IssueSearch(srch);
            if (srsp?.Data?.Any() ?? false)
                return srsp.Data.First().Timestamp.Date;
            else
                return null;
        }

        /// <summary>
        /// Returns asset information from vulns, then issues, for use as a generator to process all assets having updated date after date in syncRecord.
        /// </summary>
        internal async IAsyncEnumerable<AssetInfo> GetAsync(WizClient client, SyncRecord syncRecord)
        {
            var assetIds = new List<string>();  // track seen IDs so can skip duplicates
            var mySync = syncRecord.GetData();
            if (mySync.Item3 != "i")
            {
                await foreach (var a in GetAsync<Vulnerability>(client, syncRecord, assetIds))
                    yield return a;
                syncRecord.SetData(mySync.Item1 ?? default, mySync.Item2, "i");
                LocalData.AddUpdate(syncRecord, true);
            }
            await foreach (var a in GetAsync<Issue>(client, syncRecord, assetIds))
                yield return a;
        }

        /// <summary>
        /// Returns asset information (issue or vuln), for use as a generator to process all assets having updated data since date fromDate
        /// </summary>
        internal async IAsyncEnumerable<AssetInfo> GetAsync<T>(WizClient client, SyncRecord syncRecord, List<string> skipIds) where T : class
        {
            var prms = syncRecord.GetData();
            Logger.LogDebug("[Get] Sync record data: {Data}", syncRecord.Data);
            var fromDate = prms.Item1 ?? throw new ArgumentNullException(nameof(syncRecord), "[Get] Invalid/missing data in sync record.");
            Logger.LogInformation("[Get] Start date for data retrieval: {Dt:o}", fromDate);
            var resumeIdFromSync = prms.Item2;
            if (!string.IsNullOrEmpty(resumeIdFromSync))
                Logger.LogInformation("[Get] Resume data found, skipping to/past ID {Id} for type {Type}.", resumeIdFromSync, typeof(T).Name);
            var isIssue = typeof(T) == typeof(Issue);
            if (!isIssue && typeof(T) != typeof(Vulnerability))
                throw new WizException($"Invalid type '{typeof(T).Name}'.");

            var complete = false;
            string afterToken = null;
            PageInfo pageInfo = null;
            const int COUNT_BY = 200;
            IEnumerable<AssetInfo> assets = [];
            var label = typeof(T) == typeof(Vulnerability) ? "vulns" : "issues";
            var issueCount = 0;
            var assetCount = 0;
            var skipCount = 0;
            var resumeIssueId = "";

            while (!complete)
            {
                if (isIssue)
                {
                    var batchDto = (await client.IssuesGetUpdatedAssetsAsync(fromDate, afterToken, null)).Content;
                    if (batchDto.Data.IssuesV2 != null && batchDto.Data.IssuesV2.Nodes != null)
                    {
                        if (!string.IsNullOrEmpty(resumeIdFromSync))
                        {
                            var removeIds = batchDto.Data.IssuesV2.Nodes
                                .Where(n => !skipIds.Contains(n.EntitySnapshot.Id) && n.Id.CompareTo(resumeIdFromSync) < 0)
                                .Select(n => n.Id)
                                .ToList();
                            batchDto.Data.IssuesV2.Nodes.RemoveAll(n => removeIds.Contains(n.Id));
                            skipCount += removeIds.Count;
                            Logger.LogInformation("[Get] Skipping {Count} issues to resume processing where last left off.", skipCount);
                        }

                        assets = batchDto.Data.IssuesV2.Nodes.Where(n => !skipIds.Contains(n.EntitySnapshot.Id)).Select(n => n.EntitySnapshot);
                        pageInfo = batchDto.Data.IssuesV2.PageInfo;
                        issueCount += batchDto.Data.IssuesV2.Nodes.Count;
                        resumeIssueId = batchDto.Data.IssuesV2.Nodes[0].Id;
                    }
                }
                else
                {
                    var batchDto = (await client.VulnsGetUpdatedAssetsAsync(fromDate, afterToken, null)).Content;
                    if (batchDto.Data.VulnerabilityFindings != null && batchDto.Data.VulnerabilityFindings.Nodes != null)
                    {
                        if (!string.IsNullOrEmpty(resumeIdFromSync))
                        {
                            var removeIds = batchDto.Data.VulnerabilityFindings.Nodes
                                .Where(n => !skipIds.Contains(n.VulnerableAsset.Id) && n.Id.CompareTo(resumeIdFromSync) < 0)
                                .Select(n => n.Id)
                                .ToList();
                            batchDto.Data.VulnerabilityFindings.Nodes.RemoveAll(n => removeIds.Contains(n.Id));
                            skipCount += removeIds.Count;
                            Logger.LogInformation("[Get] Skipping {Count} vulns to resume processing where last left off.", skipCount);
                        }

                        assets = batchDto.Data.VulnerabilityFindings.Nodes.Where(n => !skipIds.Contains(n.VulnerableAsset.Id)).Select(n => n.VulnerableAsset);
                        pageInfo = batchDto.Data.VulnerabilityFindings.PageInfo;
                        issueCount += batchDto.Data.VulnerabilityFindings.Nodes.Count;
                        resumeIssueId = batchDto.Data.VulnerabilityFindings.Nodes[0].Id;
                    }
                }
                complete = !(pageInfo?.HasNextPage ?? false);
                afterToken = complete ? null : pageInfo.EndCursor;

                foreach (var asset in assets)
                {
                    // Catch duplicates in results
                    if (!skipIds.Contains(asset.Id))
                    {
                        skipIds.Add(asset.Id);
                        assetCount++;  // assetIds.Count property found to sometimes be off by one in testing, probably a race condition.
                    }
                    else
                    {
                        continue;
                    }
                    if (assetCount >= COUNT_BY && assetCount % COUNT_BY == 0)
                        Logger.LogInformation("[Get] Unique assets of type wiz {Label1}: {Count} in {Issues} {Label2} so far.", label, assetCount, issueCount, label);
                    if (assetCount > 0 && (assetCount % 50) == 0)
                    {
                        // If interrupted, resume from first issue in the current batch
                        syncRecord.SetData(fromDate, resumeIssueId, prms.Item3);
                        LocalData.AddUpdate(syncRecord, true);
                    }
                    yield return asset;
                }
                if (complete)
                {
                    Logger.LogInformation("[Get] Got all assets from wiz {Label1}. {Count} total unique assets found in {Issues} {Label2}.", label, assetCount, issueCount, label);
                }
            }
        }

        private async IAsyncEnumerable<T> GetListFromApiAsync<T>(WizClient client, string assetId) where T : class
        {
            var isIssue = typeof(T) == typeof(Issue);
            if (!isIssue && typeof(T) != typeof(Vulnerability))
                throw new WizException($"Invalid type '{typeof(T).Name}'.");

            Logger.LogDebug("[Client] Getting batch of issues or vulns from API");
            string afterToken = null;
            var complete = false;
            IEnumerable<T> returnList;
            PageInfo pageInfo;
            while (!complete)
            {
                if (isIssue)
                {
                    try
                    {
                        var batchDto = (await client.IssuesGetAsync(assetId, afterToken, null)).Content;
                        if (batchDto.Data?.IssuesV2?.Nodes == null)
                            break;
                        returnList = batchDto.Data.IssuesV2?.Nodes.Select(n => n as T);
                        pageInfo = batchDto.Data.IssuesV2?.PageInfo;
                    }
                    catch (ApiClientSerializationException ex)
                    {
                        Logger.LogError(ex, "[Client] Deserialization failure loading data from API.  Check previous log entries for API error response.");
                        throw new WizClientException("Deserialization failure loading data from API.", ex);
                    }
                }
                else
                {
                    try
                    {
                        var batchDto = (await client.VulnsGetAsync(assetId, afterToken, null)).Content;
                        if (batchDto.Data?.VulnerabilityFindings?.Nodes == null)
                            break;
                        returnList = batchDto.Data.VulnerabilityFindings.Nodes.Select(n => n as T);
                        pageInfo = batchDto.Data.VulnerabilityFindings.PageInfo;
                    }
                    catch (ApiClientSerializationException ex)
                    {
                        Logger.LogError(ex, "[Client] Deserialization failure loading data from API.  Check previous log entries for API error response.");
                        throw new WizClientException("Deserialization failure loading data from API.", ex);
                    }
                }
                complete = !(pageInfo?.HasNextPage ?? false);
                afterToken = pageInfo?.EndCursor;
                foreach (var node in returnList ?? [])
                {
                    yield return node;
                }
                Logger.LogDebug("[Client] API batch complete - {Count} items, after token: {Token}", returnList.Count(), afterToken);
            }
        }

        private static QueueScan MapScanLastScan(QueueScan qScan, DateTime? scanDate, string sourceId)
        {
            // This should always be true if issues exist at all (and they always should...), but if not it shouldn't break...
            if (scanDate.HasValue)
                qScan.Entity.Saltminer.Scan.ScanDate = scanDate.Value.ToUniversalTime();
            var reportId = $"{qScan.Entity.Saltminer.Scan.ScanDate:o}|{sourceId}";
            qScan.Entity.Saltminer.Scan.ReportId = reportId;
            return qScan;
        }
        
        private QueueScan MapScan<T>(string sourceId) where T: class
        {
            Logger.LogDebug("[Map] Setting reportId for source ID {SrcId}", sourceId);
            var isVuln = typeof(T) == typeof(Vulnerability);
            if (!isVuln && typeof(T) != typeof(Issue))
                throw new ArgumentException($"Type parameter must be of type {nameof(Vulnerability)} or {nameof(Issue)}.");
            var reportId = $"{DateTime.UtcNow:o}|{sourceId}";
            return MapScan(reportId, sourceId, isVuln ? Config.SourceTypeVulns : Config.SourceTypeIssues);
        }

        private QueueScan MapScan(string reportId, string sourceId, string sourceType)
        {
            Logger.LogDebug("[Map] Mapping scan with source ID {SrcId} and report ID {RptId}", sourceId, reportId);
            var now = DateTime.UtcNow;
            var queueScan = new QueueScan
            {
                Loading = true,
                Entity = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueScanInfo
                    {
                        Scan = new SaltMiner.Core.Entities.QueueScanInfo
                        {
                            AssessmentType = AssessmentType.Net.ToString("g"),
                            Product = "Wiz",
                            ReportId = reportId,
                            ScanDate = now, // scan date and report ID are updated after issues are loaded
                            ProductType = MyAssetType,
                            Vendor = "Wiz",
                            AssetType = MyAssetType,
                            IsSaltminerSource = WizConfig.IsSaltminerSource,
                            SourceType = sourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = -1,
                            QueueStatus = QueueScanStatus.Loading.ToString("g"),
                        },
                    },
                    Timestamp = now
                },
                Timestamp = now
            };

            // We may be unable to customize scans, or at least we won't have any data other than report ID to pass...
            if (CustomAssembly != null)
            {
                CustomAssembly.CustomizeQueueScan(queueScan, reportId);
                if (CustomAssembly.CancelScan)
                {
                    queueScan.Entity.Saltminer.Internal.QueueStatus = QueueScanStatus.Cancel.ToString("g");
                    LocalData.DeleteQueueScan(queueScan.Id);
                    return queueScan;
                }
            }
            var qs = LocalData.AddUpdate(queueScan);
            return qs;
        }

        private QueueAsset MapAsset(QueueScan qScan, Issue issue, string sourceId, bool isRetired = false)
        {
            if (string.IsNullOrEmpty(issue.EntitySnapshot.Name))
                throw new WizValidationException("Invalid asset (WizIssue), couldn't determine name");
            Logger.LogDebug("[Map - WizIssue] Asset source ID {Id}, name {Name}, setting attributes", sourceId, issue.EntitySnapshot.Name);
            var attributes = new Dictionary<string, string>
            {
                { "ResourceRegion", issue.EntitySnapshot.Region },
                { "ResourceStatus", issue.EntitySnapshot.Status },
                { "ResourcePlatform", issue.EntitySnapshot.CloudPlatform },
                { "SubscriptionName", issue.EntitySnapshot.SubscriptionName },
                { "SubscriptionExternalId", issue.EntitySnapshot.SubscriptionExternalId },
                { "ExternalId", issue.EntitySnapshot.ExternalId }
            };
            return MapAsset(qScan, sourceId, issue.EntitySnapshot.Name, issue.EntitySnapshot.NativeType, attributes, isRetired);
        }

        private QueueAsset MapAsset(QueueScan qScan, Vulnerability vuln, string sourceId, bool isRetired = false)
        {
            var appname = GetAppName(vuln);
            if (string.IsNullOrEmpty(appname))
                throw new WizValidationException("Invalid asset (WizVuln), couldn't determine name");
            Logger.LogDebug("[Map - WizVuln] Asset source ID {Id}, name {Name}, setting attributes", sourceId, vuln.VulnerableAsset.Name);
            var attributes = new Dictionary<string, string>
            {
                { "ProviderUniqueId", vuln.VulnerableAsset.ProviderUniqueId },
                { "CloudProviderURL", vuln.VulnerableAsset.CloudProviderUrl },
                { "CloudPlatform", vuln.VulnerableAsset.CloudPlatform },
                { "ResourceOs", vuln.ResourceOs },
                { "Status", vuln.VulnerableAsset.Status },
                { "WizAssetType", vuln.VulnerableAsset.Type },
                { "SubscriptionId", vuln.VulnerableAsset.SubscriptionId },
                { "SubscriptionName", vuln.VulnerableAsset.SubscriptionName },
                { "SubscriptionExternalId", vuln.VulnerableAsset.SubscriptionExternalId }
            };
            if (vuln.VulnerableAsset.Tags != null)
            {
                foreach (var kv in vuln.VulnerableAsset.Tags)
                {
                    if (!attributes.ContainsKey(kv.Key))
                        attributes.Add(kv.Key, kv.Value);
                }
            }
            var appdesc = GetAppDescription(vuln);
            return MapAsset(qScan, sourceId, appname, appdesc, attributes, isRetired);
        }

        private QueueAsset MapAsset(QueueScan qScan, string sourceId, string name, string description, Dictionary<string, string> attributes = null, bool isRetired = false)
        {
            Logger.LogDebug("[Map] Asset source ID {Id}, name {Name}, mapping...", sourceId, name);
            var queueAsset = new QueueAsset
            {
                Entity = new()
                {
                    Timestamp = DateTime.UtcNow,
                    Id = Guid.NewGuid().ToString(),
                    Saltminer = new()
                    {
                        Internal = new()
                        {
                            QueueScanId = qScan.Id
                        },
                        Asset = new()
                        {
                            Description = description,
                            Name = name,
                            VersionId = sourceId,
                            Version = "",
                            Attributes = attributes,
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = WizConfig.IsSaltminerSource,
                            SourceType = qScan.SourceType,
                            SourceId = sourceId,
                            AssetType = MyAssetType,
                            LastScanDaysPolicy = Config.LastScanDaysPolicy,
                            IsRetired = isRetired
                        }
                    }
                }
            };
            var result = LocalData.AddUpdate(queueAsset);
            return result;
        }

        private static string GetAppDescription(Vulnerability vuln)
        {
            string[] lst = [ "SERVERLESS", "VIRTUAL_MACHINE" ];
            if (lst.Contains(vuln.VulnerableAsset.Type))
                return vuln.VulnerableAsset.CloudProviderUrl;
            return "SubscriptionExternalId: " + vuln.VulnerableAsset.SubscriptionExternalId;
        }

        private static string GetAppName(Vulnerability vuln)
        {
            var appName = vuln.VulnerableAsset.SubscriptionExternalId;
            // ## in subscription external ID
            if (appName != null && appName.Contains("##") && !appName.EndsWith("##"))
                appName = appName[(appName.LastIndexOf("##") + 2)..];
            else
                appName = null; // Try something else
            // Container images / VMs
            if (vuln.VulnerableAsset.Name != null)
                appName = vuln.VulnerableAsset.Name;
            // Fallback
            appName ??= vuln.VulnerableAsset.Id;
            return appName;
        }

        private QueueIssue MapZeroIssue(QueueScan qScan, QueueAsset qAsset)
        {
            Logger.LogDebug("[Map] Mapping zero issue for asset source ID {SrcId} and name {Name}", qAsset.SourceId, qAsset.Entity.Saltminer.Asset.Name);
            return new QueueIssue
            {
                Entity = new()
                {
                    Labels = [],
                    Vulnerability = new SaltMiner.Core.Entities.VulnerabilityInfo
                    {
                        Audit = new SaltMiner.Core.Entities.AuditInfo
                        {
                            Audited = true,
                        },
                        Category = [ "Application" ],
                        Description = "keyword",
                        Name = "ZeroIssue",
                        Location = "N/A",
                        LocationFull = "N/A",
                        Scanner = new SaltMiner.Core.Entities.ScannerInfo
                        {
                            AssessmentType = AssessmentType.Open.ToString("g"),
                            Product = "Wiz",
                            Vendor = "Wiz",
                            Id = GetZeroScannerId(Config.SourceType, qAsset.SourceId)
                        },
                        Severity = Severity.Zero.ToString("g"),
                        FoundDate = DateTime.UtcNow,
                        ReportId = qScan.ReportId
                    },
                    Saltminer = new SaltMiner.Core.Entities.SaltMinerQueueIssueInfo
                    {
                        Attributes = [],
                        QueueScanId = qScan.Id,
                        QueueAssetId = qAsset.Id,
                        Source = new SaltMiner.Core.Entities.SourceInfo
                        {
                            Analyzer = "Wiz"
                        }
                    },
                    Tags = [],
                    Timestamp = DateTime.UtcNow
                }
            };
        }

        private void MapIssue(Issue issue, QueueScan qScan, QueueAsset qAsset)
        {
            QueueIssue qIssue;
            // Map zero record for empty issue
            if (issue == null)
            {
                qIssue = MapZeroIssue(qScan, qAsset);
            }
            else
            {
                if (issue.Control?.Name == null && issue.Control?.Id == null)
                {
                    Logger.LogWarning("[Sync][Map Issue-Issue] Wiz issue with id {Id} has no data and will not be imported.", issue.Id);
                    qIssue = null;
                }
                else
                {
                    Logger.LogDebug("[Map - WizIssue] Setting attributes for issue ID {IssId}, asset source ID {SrcId}, and name {Name}", issue.Id, qAsset.SourceId, qAsset.Entity.Saltminer.Asset.Name);
                    var name = issue.Control?.Name;
                    if (string.IsNullOrEmpty(name))
                        name = issue.Control.Description[..100];
                    var attributes = new Dictionary<string, string>
                    {
                        { "Status", issue.Status },
                        { "DueAt", issue.DueAt?.ToString("o") },
                        { "IssueType", "issue" },
                        { "ControlId", issue.Control?.Id },
                        { "ServiceTickets", string.Join(", ", (issue.ServiceTickets?.Select(serv => serv.Name)) ?? []) }
                    };
                    qIssue = MapIssue(qScan, qAsset, name, issue.Control.Description, issue.CreatedAt, issue.ResolvedAt, issue.Id, Coalesce("N/A", issue.EntitySnapshot.CloudProviderUrl), issue.Severity, issue.WizUrl, attributes);
                }
            }
            if (qIssue != null)
            {
                CustomAssembly?.CustomizeQueueIssue(qIssue, issue);
                Logger.LogDebug("[Sync][Map Issue-Issue] Issue document added with Control Id of {Id}", issue.ControlId); 
                LocalData.AddUpdate(qIssue);
            }
        }

        private void MapIssue(Vulnerability vuln, QueueScan qScan, QueueAsset qAsset)
        {
            QueueIssue qIssue;
            // Map zero record for empty issue
            if (vuln == null)
            {
                qIssue = MapZeroIssue(qScan, qAsset);
            }
            else
            {
                Logger.LogDebug("[Map - WizVuln] Setting attributes for vuln ID {IssId}, asset source ID {SrcId}, and name {Name}", vuln.Id, qAsset.SourceId, qAsset.Entity.Saltminer.Asset.Name);
                var severity = Coalesce("INFORMATIONAL", vuln.CvssSeverity, vuln.VendorSeverity);
                var attributes = new Dictionary<string, string>
                {
                    { "Status", vuln.Status },
                    { "HasExploit", vuln.HasExploit.ToString() },
                    { "Version", vuln.Version },
                    { "FixedVersion", vuln.FixedVersion },
                    { "Link", vuln.Link },
                    { "ImageId", vuln.VulnerableAsset.ImageId },
                    { "IssueType", "vulnerability" }
                };
                var location = Coalesce("N/A", vuln.LocationPath, vuln.VulnerableAsset.SubscriptionExternalId);
                qIssue = MapIssue(qScan, qAsset, vuln.Name, vuln.Description, vuln.FirstDetectedAt, vuln.ResolvedAt, vuln.Id, location, severity, vuln.WizUrl, attributes);
                qIssue.Entity.Vulnerability.Location = Coalesce("N/A", vuln.DetailedName);
            }
            CustomAssembly?.CustomizeQueueIssue(qIssue, vuln);
            Logger.LogDebug("[Sync][Map Issue-Vulnerability] Vulnerability {VulnName} mapped for asset {Asset}", vuln.DetailedName, qAsset.Entity.Saltminer.Asset.Name);
            LocalData.AddUpdate(qIssue);
        }

        private QueueIssue MapIssue(QueueScan qScan, QueueAsset qAsset, string name, string description, DateTime foundDate, DateTime? removedDate, string id, string locationFull, string severity, string guiUrl, Dictionary<string, string> attributes)
        {
            Logger.LogDebug("[Map] Mapping issue ID {IssId}, asset source ID {SrcId}, and name {Name}", id, qAsset.SourceId, qAsset.Entity.Saltminer.Asset.Name);
            if (string.IsNullOrEmpty(name)) 
                name = "[not available]";
            return new QueueIssue
            {
                Entity = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Labels = [],
                    Vulnerability = new()
                    {
                        Audit = new()
                        {
                            Audited = true,
                        },
                        Category = [ "Application" ],
                        Description = description,
                        FoundDate = foundDate,
                        RemovedDate = removedDate,
                        Id = [ id ],
                        LocationFull = Coalesce("N/A", locationFull),
                        Location = "N/A",
                        Name = name,
                        ReportId = qScan.ReportId,
                        Scanner = new()
                        {
                            Id = id,
                            AssessmentType = AssessmentType.Net.ToString("g"),
                            Product = "Wiz",
                            Vendor = "Wiz",
                            GuiUrl = guiUrl
                        },
                        Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, severity),
                        SourceSeverity = severity
                    },
                    Saltminer = new()
                    {
                        IssueType = qScan.Entity.Saltminer.Scan.AssessmentType,
                        Attributes = attributes,
                        QueueScanId = qScan.Id,
                        QueueAssetId = qAsset.Id,
                        Source = new()
                        {
                            Analyzer = "Wiz"
                        }
                    },
                    Tags = [],
                    Timestamp = DateTime.UtcNow
                }
            };
        }

        private static string Coalesce(string defaultValue, params string[] strings )
        {
            var ans = Array.Find(strings, s => !string.IsNullOrEmpty(s));
            return string.IsNullOrEmpty(ans) ? defaultValue : ans;
        }
    }
}
