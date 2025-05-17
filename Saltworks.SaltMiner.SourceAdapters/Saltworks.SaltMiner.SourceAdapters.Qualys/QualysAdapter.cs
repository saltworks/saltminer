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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.SourceAdapters.Qualys
{
    // Architecture
    // Qualys is a bit different than other sources.Instead of comparing metrics from the source to local,
    // we can simply pull the latest scan(s) and update all affected assets.
    // Except, scans only include a subset of assets, so we actually have to also pull all assets not available
    // in scans and make up scans for them.However, Qualys has a pretty aggressive rate limiting mechanism on its API,
    // and so we have to keep that in mind while calling the API.The responses are all XML, which is...suboptimal.
    // But we press on. Here's the approach as an outline:

    // 1.  (GetAsync) Pull a list of scans newer than where we left off after the last session (or from 1/1/2001 if first run).
    // 2a. (SyncScansAsync) Each scan includes a list of target IPs(and ranges) that were scanned.Pull issues(detections) for these,
    //     using batching to reduce the API calls.For each asset(host) returned(API returns hosts with issues inside them), build out scan/asset,
    //     and then build issues("first pass") and add them to a queue.We need two passes on issues because they are not complete enough
    //     from the detections API to complete a full issue (the deal-killer is the missing vuln name). Also add scan to a "finish" queue
    //     to close out once the issues are all completely processed.
    // 2b. (SyncHostsAsync) Pull all hosts, then for each host that is NOT of tracking_method=IP, pull detections for these in
    //     batches as with scans.Create queue scan & asset, add queue scan to "finish" queue, create issues for each detection and
    //     add them to the issues queue, and continue till all hosts are processed.
    // 3.  (SyncIssuesAsync) Pull issues from the queue until we have a batch, then bounce these against a local dictionary store
    //     to complete their details. Any not completed are then queried from the "KB" API to get the missing data. The API call results
    //     are added to the local dictionary store, so over time the KB API is phased out. Complete the issues, saving them to local storage.
    // 4.  (SyncFinishAsync) Watch a queuescan queue, when issues match the count in the qScan, complete the qScan for Send.
    public class QualysAdapter : SourceAdapter
    {
        private SyncRecord SyncRecord;
        private ISourceAdapterCustom CustomAssembly;
        private bool RecoveryMode = false;
        private bool StillSyncingScans = true;
        private bool StillSyncingHosts = true;
        private bool StillSyncing => StillSyncingHosts || StillSyncingScans;
        private bool StillSyncingIssues = true;
        private const string AssetType = "net";
        private const string QidDataType = "qid";
        private const string QIssueSyncInProgress = "SyncInProgress";
        private readonly ConcurrentQueue<QueueIssue> QueueIssues = new();
        private readonly ConcurrentQueue<QueueScan> QueueScans = new();
        internal QualysConfig Config { get; set; }
        internal QualysClient Client { get; set; }
        public QualysAdapter(IServiceProvider provider, ILogger<QualysAdapter> logger) : base(provider, logger)
        {
            Logger.LogDebug("QualysAdapter Initialization complete.");
        }
        public override async Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            await base.RunAsync(config, token);
            try
            {
                // Config setup
                ArgumentNullException.ThrowIfNull(config, nameof(config));
                if (config is not QualysConfig)
                    throw new SourceConfigurationException($"Config type incorrect; expected '{nameof(QualysConfig)}', but got '{config.GetType().Name}'");
                Config = config as QualysConfig;

                // Setup, load custom source adapter if configured, run custom pre-preprocessing
                CancelToken = token;
                Config.Validate();
                SetApiClientSslVerification(Config.VerifySsl);
                Client = new QualysClient(ApiClient, Config, Logger);

                // We record the scan date of the scan report ID being worked
                // If failure to complete before next report then repeat from current scan date (still have to record and observe error state)
                SyncRecord = LocalData.GetSyncRecord(Config.Instance, Config.SourceType);
                RecoveryMode = SyncRecord.State == SyncState.InProgress;
                var startDate = StringToDate(SyncRecord.Data ?? "2001-01-01 00:00");
                if (RecoveryMode) startDate = startDate.Value.AddDays(-1);  // make sure we catch the last report launch date since it failed to complete
                if (Config.OverrideStartDate.HasValue)
                {
                    startDate = Config.OverrideStartDate.Value;
                    Logger.LogInformation("Using override start date from config: {Dt:yyyy-MM-dd}", startDate.Value);
                }

                if (Config.HasCustomAssembly)
                {
                    CustomAssembly = AssemblyHelper.LoadClassAssembly<ISourceAdapterCustom>(Config.CustomAssemblyName, Config.CustomAssemblyType);
                    CustomAssembly.PreProcess();
                }
                CheckCancel();
                StillLoading = true;
                Client.CheckConnection();

                // This source adds a thread to "finish" issues before finalizing them to send.  See SyncIssuesAsync for more info.
                await Task.WhenAll(SyncAsync(startDate), SyncIssuesAsync(), SyncFinishAsync(), SendAsync(Config, AssetType));
                ResetFailures(Config);
                DeleteFailures(Config);
            }
            catch (TaskCanceledException)
            {
                // Already logged, silently exit
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "{Msg}", ex.InnerException?.Message ?? ex.Message);
                StillLoading = false;
                throw new SourceException("Source adapter failed.", ex);
            }
        }

        // Sync Step 0a? Generator for Sync Step 1a (SyncScansAsync)
        internal async IAsyncEnumerable<ScanListItem> GetAsync(DateTime? lastScanDate)
        {
            // Get list of scans from API
            var scanStartDate = lastScanDate ?? DateTime.MinValue;
            if (lastScanDate != null && RecoveryMode)
                scanStartDate = scanStartDate.AddMinutes(-1);

            var allScans = await Client.ScanListAsync(scanStartDate);
            Logger.LogInformation("[Get] Found {Count} scans to process.", allScans.Count());

            // For each scan, pull hosts using their IPs, then return scan and host as a generator
            foreach (var scan in allScans)
            {
                yield return scan;
            }
        }

        // Sync Step 1
        internal async Task SyncAsync(DateTime? startDate)
        {
            if (Config.SourceType != SourceType.Qualys.GetDescription())
            {
                Logger.LogCritical("[Sync] Invalid configuration - SourceType expected to be 'Saltworks.{EType}' but was found to be '{AType}'", SourceType.Qualys.GetDescription(), Config.SourceType);
                throw new QualysValidationException("Invalid configuration - source type");
            }

            SyncRecord.State = SyncState.InProgress;
            SyncRecord.Data = DateToString(DateTime.UtcNow);
            LocalData.AddUpdate(SyncRecord, true);
            try
            {
                await Task.WhenAll(SyncHostsAsync(startDate), SyncScansAsync(startDate));
                SyncRecord.State = SyncState.Completed;
                LocalData.AddUpdate(SyncRecord);
                Logger.LogInformation("[Sync] Sync complete.");
            }
            catch (TaskCanceledException)
            {
                // exit gracefully
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[Sync] General failure: {Msg}", ex.Message);
            }
        }

        // Sync Step 1a
        internal async Task SyncScansAsync(DateTime? startDate)
        {
            List<string> targetIps = [];
            List<string> hostIds = [];
            var scanCounter = 0;
            var assetCounter = 0;
            var issueCounter = 0;

            if (!Config.EnableScanSync)
            {
                Logger.LogInformation("[SyncScans] Scan sync disabled via configuration, will not sync scans...");
                StillSyncingScans = false;
                return;
            }

            try
            {
                // Get calls the Qualys API to return scans we haven't seen yet (based on startdate), which have target IPs, which can be a single IP or a range
                await foreach (var scan in GetAsync(startDate))
                {
                    Logger.LogInformation("[SyncScans] Processing scan '{ScanId}', {Count} target IPs / ranges.", scan.ScanId, scan.Targets.Count);
                    scanCounter++;

                    // Split into batches, skipping any Target IPs we've already seen this session
                    foreach (var batch in GetStringBatch(scan.Targets, targetIps, Config.HostDetectionHostBatchSize))
                    {
                        await LetSendCatchUpAsync(Config);
                        var hostCount = 0;
                        var skippedHostCount = 0;
                        if (batch.Count == 0)
                            throw new QualysException("[SyncScans] Empty batch not allowed.");
                        CheckCancel(true);
                        // Get detections (vulns) for the current batch of Target IPs (we do batches to save on API calls to avoid rate limits)
                        await foreach (var host in Client.HostDetectionsAsync(batch, null))
                        {
                            // Skip this returned host if we've already seen the host ID in this session
                            if (hostIds.Contains(host.Id))
                            {
                                skippedHostCount++;
                                continue;
                            }
                            if (host.TrackingMethod != "IP")
                            {
                                Logger.LogWarning("[SyncScans] Unexpected tracking method {Method} encountered.  Skipping.  IP: {Ip}, HostID: {HostId}", host.TrackingMethod, host.Ip, host.Id);
                                skippedHostCount++;
                                continue;
                            }
                            hostIds.Add(host.Id);
                            hostCount++;

                            var qScan = MapScan(scan, host.Detections.Count);
                            QueueScans.Enqueue(qScan);
                            var qAsset = MapAsset(host, qScan);
                            Logger.LogInformation("[SyncScans] Updating host '{Host}', IP '{Ip}', {Count} issue(s)", qAsset.Entity.Saltminer.Asset.Name, host.Ip, host.Detections.Count);
                            assetCounter++;
                            foreach (var detection in host.Detections)
                            {
                                // Drop new issues into queue so that separate process can add in KB vuln information
                                QueueIssues.Enqueue(MapIssue(detection, qScan, qAsset));
                                issueCounter++;
                                CheckCancel(false);
                            }
                        }
                        Logger.LogInformation("[SyncScans] {Count} hosts updated, {Skipped} skipped, {Total} IP address / ranges in this batch.", hostCount, skippedHostCount, batch.Count);
                        targetIps.AddRange(batch);
                    }
                    Logger.LogInformation("[SyncScans] Processed {Scans} scan(s), {Assets} asset(s), {Issues} issue(s) overall.", scanCounter, assetCounter, issueCounter);
                }
                Logger.LogInformation("[SyncScans] Sync complete.");
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogInformation(ex, "[SyncScans] Cancellation requested, cancelling processing.");
                // exit gracefully
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[SyncScans] General failure: {Msg}", ex.Message);
            }
            finally
            {
                StillSyncingScans = false;
            }
        }

        // Sync Step 1b
        internal async Task SyncHostsAsync(DateTime? startDate)
        {
            Logger.LogDebug("[SyncHosts] Starting");
            var count = 0;
            List<HostDto> hosts = [];
            try
            {
                await foreach (var host in Client.HostListAsync([], startDate))
                {
                    Logger.LogDebug("[Sync] [Hosts] {Host}", host.Id);
                    count++;
                    if (count % 100 == 0)
                        Logger.LogDebug("[SyncHosts] {Count} Hosts loaded", count);
                    if (host.TrackingMethod == "IP")
                        continue;  // do not want IP, they are handled via SyncScansAsync
                    hosts.Add(host);
                    if (hosts.Count >= Config.HostDetectionHostBatchSize)
                    {
                        await SyncHostsBatchAsync(hosts);
                        hosts.Clear();
                    }
                    await LetSendCatchUpAsync(Config);
                }
                if (hosts.Count > 0)
                {
                    await SyncHostsBatchAsync(hosts);
                }
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogInformation(ex, "[SyncHosts] Cancellation requested, cancelling processing.");
                // gracefully exit
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[SyncHosts] General failure: {Msg}", ex.Message);
            }
            finally
            {
                StillSyncingHosts = false;
            }
        }

        // Sync Step 1b subsection romeo-delta-potpourri-niner
        private async Task SyncHostsBatchAsync(List<HostDto> hosts)
        {
            var pcount = 0;
            await foreach (var hostDetect in Client.HostDetectionsAsync(null, hosts.Select(h => h.Id).Distinct()))
            {
                CheckCancel(true);
                var qScan = MapScan(hostDetect);
                qScan.Entity.Saltminer.Internal.IssueCount = hostDetect.Detections.Count;
                var qAsset = MapAsset(hostDetect, qScan);
                QueueScans.Enqueue(qScan);
                Logger.LogInformation("[SyncHosts] Updating host '{Host}', ID '{Id}', {Count} issue(s)", qAsset.Entity.Saltminer.Asset.Name, hostDetect.Id, hostDetect.Detections.Count);
                foreach (var detect in hostDetect.Detections)
                {
                    QueueIssues.Enqueue(MapIssue(detect, qScan, qAsset));
                    CheckCancel(false);
                }
                pcount++;
            }
            Logger.LogInformation("[SyncHosts] {PCount} hosts updated, {Skipped} skipped, {Total} hosts in this batch.", pcount, pcount - hosts.Count, hosts.Count);
        }

        // Sync Step 2
        internal async Task SyncIssuesAsync()
        {
            try
            {
                // Collect a batch of vuln IDs (QIDs) before querying to complete the issues.  We query local data first, then call API for these.
                // Rate limit avoidance and better performance to "cache" the QIDs we've seen before.
                List<QueueIssue> qissues = [];
                List<string> qids = [];
                var pcount = 0;
                var kbcount = 0;
                var logblock = 1000;
                var pnextlog = logblock;
                while (StillSyncing || !QueueIssues.IsEmpty || qissues.Exists(i => i.Entity.Saltminer.Attributes.ContainsKey(QIssueSyncInProgress)))
                {
                    CheckCancel(true);
                    if (QueueIssues.TryDequeue(out var qIssue))
                    {
                        qissues.Add(qIssue);
                        if (!qids.Contains(qIssue.Entity.Saltminer.Attributes["qid"]))
                            qids.Add(qIssue.Entity.Saltminer.Attributes["qid"]);
                    }
                    if (qissues.Count >= Config.KbIssueBatchSize || qids.Count >= Config.KbBatchSize || (QueueIssues.IsEmpty && !StillSyncing))
                    {
                        Logger.LogInformation("[SyncIssues] Processing {Count} issue(s)", qissues.Count);

                        // Retrieve kb "mini" DTOs from local data dictionary matching cached QIDs
                        foreach (var dict in LocalData.GetDataDictionary(Config.Instance, Config.SourceType, QidDataType, qids))
                        {
                            var kb = JsonSerializer.Deserialize<KnowledgeBaseMiniDto>(dict.Value);
                            // Get matching issues for current QID and complete the mapping, saving the queue issue and then
                            // marking it complete by removing the marker attribute.
                            var found = qissues.Where(i => i.Entity.Saltminer.Attributes.ContainsKey(QIssueSyncInProgress) && i.Entity.Saltminer.Attributes["qid"] == kb.Qid.ToString());
                            foreach (var f in found)
                            {
                                MapIssue(f, kb);
                                pcount++;
                                f.Entity.Saltminer.Attributes.Remove(QIssueSyncInProgress);
                            }
                            qids.Remove(dict.Key);
                        }
                        // Any unprocessed issues (still having the marker attribute) need to be looked up via API
                        foreach (var bigKb in await Client.KnowledgeBaseAsync(qissues
                            .Where(i => i.Entity.Saltminer.Attributes.ContainsKey(QIssueSyncInProgress))
                            .Select(i => i.Entity.Saltminer.Attributes["qid"])
                            .Distinct()))
                        {
                            var kb = bigKb.ToMiniDto();
                            // Get matching issues for current QID and complete the mapping, saving the queue issue and then
                            // marking it complete by removing the marker attribute.
                            var found = qissues.Where(i => i.Entity.Saltminer.Attributes.ContainsKey(QIssueSyncInProgress) && i.Entity.Saltminer.Attributes["qid"] == kb.Qid.ToString());
                            foreach (var f in found)
                            {
                                // Save the retrieved KB locally
                                LocalData.AddUpdate(new DataDict()
                                {
                                    Instance = Config.Instance,
                                    SourceType = Config.SourceType,
                                    DataType = QidDataType,
                                    Key = kb.Qid.ToString(),
                                    Value = JsonSerializer.Serialize(kb)
                                });
                                kbcount++;
                                MapIssue(f, kb); // removes QIssueSyncInProgress attribute
                                pcount++;
                            }
                        }
                        if (pcount >= pnextlog)
                        {
                            Logger.LogInformation("[SyncIssues] Processed {Issues} issues and added {Kb} KB entries so far...", pcount, kbcount);
                            while (pnextlog <= pcount) pnextlog += logblock;
                        }
                        qissues.Clear();
                        qids.Clear();
                    }
                    // Wait a bit to try again if queue empty
                    if (QueueIssues.IsEmpty && StillSyncing)
                    {
                        Logger.LogDebug("[SyncIssues] Empty queue, waiting a bit...");
                        await Task.Delay(10000);
                    }
                    if (QueueIssues.IsEmpty && !StillSyncing && !qissues.Exists(i => i.Entity.Saltminer.Attributes.ContainsKey(QIssueSyncInProgress)))
                    {
                        Logger.LogInformation("[SyncIssues] Processed a total of {Issues} issues and added {Kb} KB entries.  SyncIssues complete.", pcount, kbcount);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // exit gracefully
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[SyncIssues] Failure looking up vulnerability (QID) information.");
                throw new QualysException("Failure looking up vulnerability (QID) information.", ex);
            }
            finally
            {
                StillSyncingIssues = false;
            }
        }

        // Sync Step 3
        internal async Task SyncFinishAsync()
        {
            int waitsec = 10;
            int warnreps = 10;
            int skipreps = 30;
            bool finalSaveAll = false;
            QueueScan qScan = null;
            try
            {
                while (StillSyncingIssues || !QueueScans.IsEmpty)
                {
                    if (!StillSyncingIssues && !finalSaveAll)
                    {
                        LocalData.SaveAllBatches();
                        finalSaveAll = true;
                    }
                    CheckCancel(true);
                    if (QueueScans.TryDequeue(out qScan))
                    {
                        Logger.LogInformation("[SyncFinish] Watching queue scan with report ID '{ScanId}' and ID '{Id}'.", qScan.ReportId, qScan.Id);
                    }
                    while (qScan != null)
                    {
                        var lookcount = 0;
                        var isErr = false;
                        if (qScan.Entity.Saltminer.Internal.IssueCount == 0)
                        {
                            Logger.LogError("[SyncFinish] No issue count set in qscan with report ID '{ScanId}' and ID '{Id}'.  This scan will not be sent.", qScan.ReportId, qScan.Id);
                            isErr = true;
                        }
                        var count = LocalData.GetQueueIssuesCountByScanId(qScan.Id);
                        lookcount++;
                        if (!isErr && count == qScan.Entity.Saltminer.Internal.IssueCount)
                        {
                            Logger.LogInformation("[SyncFinish] Queue scan with report ID '{ScanId}' and ID '{Id}' ready to send, {Count} issue(s).", qScan.ReportId, qScan.Id, qScan.Entity.Saltminer.Internal.IssueCount);
                            qScan.Loading = false;
                            LocalData.AddUpdate(qScan);
                            break; // Valid issue count; break while
                        }
                        if (!isErr && !StillSyncingIssues && qScan.Entity.Saltminer.Internal.IssueCount != count)
                        {
                            Logger.LogError("[SyncFinish] Sync is complete. Found {Found} of {Expected} issue(s) for qscan with report ID '{ScanId}' and ID '{Id}'.  This scan will not be sent.", count, qScan.ReportId, qScan.Entity.Saltminer.Internal.IssueCount, qScan.Id);
                            isErr = true;
                        }
                        if (!isErr && qScan.Entity.Saltminer.Internal.IssueCount < count)
                        {
                            Logger.LogError("[SyncFinish] Found {Found} of expected {Expected} issue(s) for qscan with report ID '{ScanId}' and ID '{Id}'.  This scan will not be sent.", count, qScan.Entity.Saltminer.Internal.IssueCount, qScan.ReportId, qScan.Id);
                            isErr = true;
                        }
                        if (!isErr && lookcount >= skipreps)
                        {
                            Logger.LogError("[SyncFinish] Queue scan with report ID '{ScanId}' and ID '{Id}' has {Count}/{Expected} issue(s) after {Total} sec. It will not be sent.", qScan.ReportId, qScan.Id, count, qScan.Entity.Saltminer.Internal.IssueCount, lookcount * waitsec);
                            isErr = true;
                        }
                        if (!isErr && lookcount > warnreps)
                        {
                            Logger.LogWarning("[SyncFinish] Queue scan with report ID '{ScanId}' and ID '{Id}' has {Count}/{Expected} issue(s) after {Total} sec.", qScan.ReportId, qScan.Id, count, qScan.Entity.Saltminer.Internal.IssueCount, lookcount * waitsec);
                        }
                        if (isErr)
                        {
                            qScan.Loading = false;
                            qScan.Entity.Saltminer.Internal.QueueStatus = QueueScanStatus.Error.ToString("g");
                            LocalData.AddUpdate(qScan);
                            break; // Saved error status; break while
                        }
                        // Wait for a bit, then check again
                        await Task.Delay(waitsec * 1000);
                    }
                    // Wait a bit to try again if queue empty
                    if (QueueScans.IsEmpty && StillSyncingIssues)
                    {
                        Logger.LogDebug("[SyncFinish] Empty queue, waiting a bit...");
                        await Task.Delay(waitsec * 1000);
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogInformation(ex, "[SyncFinish] Cancellation requested, cancelling processing.");
                // exit gracefully
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[SyncFinish] Failure completing queue scan(s).  Last one was Local ID {Id}.", (qScan?.Id ?? "??"));
                throw new QualysException("Failure looking up vulnerability (QID) information.", ex);
            }
            finally
            {
                LocalData.SaveAllBatches();
                Logger.LogInformation("[SyncFinish] Final local data buffer saved, delaying a bit to give send a chance to notice.");
                await Task.Delay(5000); // give Send a chance to notice the previous operation if it's not busy
                StillLoading = false;
            }
        }

        private string DateToString(DateTime date) => date.ToUniversalTime().ToString(Config.SyncRecordDateFormat);
        private DateTime? StringToDate(string date)
        {
            try
            {
                return DateTime.ParseExact(date, Config.SyncRecordDateFormat, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Invalid date string {Dt}, returning null)", date);
                return null;
            }
        }

        private static IEnumerable<List<string>> GetStringBatch(IEnumerable<string> list, IEnumerable<string> skipThese, int batchSize)
        {
            List<string> result = [];
            foreach (var value in list.Where(v => !skipThese.Contains(v)))
            {
                result.Add(value);
                if (result.Count >= batchSize)
                {
                    yield return result;
                    result.Clear();
                }
            }
            if (result.Count > 0)
                yield return result; // return stragglers
        }

        private QueueScan MapScan(string scanId, DateTime scanDate, int issueCount, ScanListItem scan, HostDetectDto hostDetect)
        {
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
                            AssessmentType = AssessmentType.Net.ToString("g"),
                            Product = "Qualys",
                            ReportId = scanId,
                            ScanDate = scanDate.ToUniversalTime(),
                            ProductType = "Net",
                            Vendor = "Qualys",
                            AssetType = AssetType,
                            IsSaltminerSource = QualysConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new SaltMiner.Core.Entities.QueueScanInternal
                        {
                            IssueCount = issueCount,
                            QueueStatus = QueueScanStatus.Loading.ToString("g"),
                            ReplaceIssues = true
                        },
                    },
                    Timestamp = now
                },
                Timestamp = now
            };

            if (CustomAssembly != null)
            {
                if (scan == null)
                {
                    CustomAssembly.CustomizeQueueScan(queueScan, hostDetect);
                }
                else
                { 
                    CustomAssembly.CustomizeQueueScan(queueScan, scan);
                }
                if (CustomAssembly.CancelScan)
                {
                    queueScan.Entity.Saltminer.Internal.QueueStatus = QueueScanStatus.Cancel.ToString();
                    LocalData.DeleteQueueScan(queueScan.Id);
                    return queueScan;
                }
            }
            return LocalData.AddUpdate(queueScan);
        }

        private QueueScan MapScan(HostDetectDto hostDetectDto)
        {
            if (hostDetectDto.LastScan == null)
                throw new ArgumentException("LastScan cannot be null", nameof(hostDetectDto));
            var scanId = $"{hostDetectDto.Ip}|{hostDetectDto.Id}|{hostDetectDto.LastScan.Value:yyyy-MM-dd}";
            Logger.LogDebug("Mapping scan '{ScanId}'", scanId);
            return MapScan(scanId, hostDetectDto.LastScan.Value, hostDetectDto.Detections.Count, null, hostDetectDto);
        }
        
        private QueueScan MapScan(ScanListItem scan, int issueCount)
        {
            Logger.LogDebug("Mapping scan '{ScanId}'", scan.ScanId);
            return MapScan(scan.ScanId, scan.ScanDate, issueCount, scan, null);
        }

        private static string GetSourceId(HostDetectDto host) => $"{host.Id}:{host.TrackingMethod ?? "UNK"}";

        private QueueAsset MapAsset(HostDetectDto host, QueueScan queueScan)
        {
            var sourceId = GetSourceId(host);
            Logger.LogDebug("Mapping asset '{SrcId}'", sourceId);

            var queueAsset = new QueueAsset
            {
                Entity = new()
                {
                    Timestamp = DateTime.UtcNow,
                    Saltminer = new()
                    {
                        Asset = new()
                        {
                            Description = host.Dns,
                            Name = string.IsNullOrEmpty(host.Dns) ? host.Ip : host.Dns,
                            Host = host.DnsData?.Hostname ?? host.Dns,
                            Ip = host.Ip,
                            Attributes = new() {
                                { "host_id", host.Id },
                                { "asset_id", host.AssetId },
                                { "os", host.Os },
                                { "tracking_method", host.TrackingMethod }
                            },
                            IsProduction = true,
                            Instance = Config.Instance,
                            IsSaltminerSource = QualysConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = sourceId,
                            AssetType = AssetType,
                            LastScanDaysPolicy = Config.LastScanDaysPolicy,
                        },
                        Internal = new()
                        {
                            QueueScanId = queueScan.Id
                        }
                    }
                }
            };

            foreach (var attr in host.Metadata?.AzureAttributes?.Where(a => !a.Name.Contains('/')) ?? [])
                queueAsset.Entity.Saltminer.Asset.Attributes.Add("azure_" + attr.Name, attr.Value);
            foreach (var attr in host.Metadata?.GoogleAttributes?.Where(a => !a.Name.Contains('/')) ?? [])
                queueAsset.Entity.Saltminer.Asset.Attributes.Add("google_" + attr.Name, attr.Value);
            foreach (var attr in host.Metadata?.Ec2Attributes?.Where(a => !a.Name.Contains('/')) ?? [])
                queueAsset.Entity.Saltminer.Asset.Attributes.Add("ec2_" + attr.Name, attr.Value);

            var result = LocalData.AddUpdate(queueAsset);
            Logger.LogDebug("[Sync] [MapAsset] Host ID {Host}", host.Id);
            return result;
        }

        private void MapIssue(QueueIssue qIssue, KnowledgeBaseMiniDto kb)
        {
            ArgumentNullException.ThrowIfNull(qIssue, nameof(qIssue));
            ArgumentNullException.ThrowIfNull(kb, nameof(kb));
            qIssue.Entity.Vulnerability.Name = kb.Title;
            qIssue.Entity.Vulnerability.Id = kb.CveIdList?.ToArray() ?? [];
            qIssue.Entity.Vulnerability.Details = kb.Diagnosis;
            qIssue.Entity.Vulnerability.Recommendation = kb.Solution;
            if ((qIssue.Entity.Vulnerability.Id?.Length ?? 0) > 0)
            {
                qIssue.Entity.Vulnerability.Reference = "https://www.cve.org/CVERecord?id=" + qIssue.Entity.Vulnerability.Id;
                qIssue.Entity.Vulnerability.Enumeration = "CVE";
            }
            qIssue.Entity.Saltminer.Attributes["pci_flag"] = kb.PciFlag.ToString();
            qIssue.Entity.Saltminer.Attributes.Remove(QIssueSyncInProgress);
            LocalData.AddUpdate(qIssue); // saves issue after second update pass to add KB info
        }

        private QueueIssue MapIssue(DetectionDto issue, QueueScan queueScan, QueueAsset queueAsset)
        {
            ArgumentNullException.ThrowIfNull(issue, nameof(issue));
            ArgumentNullException.ThrowIfNull(queueScan, nameof(queueScan));
            ArgumentNullException.ThrowIfNull(queueAsset, nameof(queueAsset));

            var queueIssue = new QueueIssue
            {
                Entity = new()
                {
                    Labels = [],
                    Vulnerability = new()
                    {
                        Audit = new(),
                        Category = ["Application"],
                        FoundDate = issue.FirstFound?.ToUniversalTime(),
                        ReportId = queueScan.Entity.Saltminer.Scan.ReportId,
                        Location = "N/A",
                        LocationFull = "N/A",
                        Scanner = new()
                        {
                            Id = $"{issue.Qid}:{queueAsset.Entity.Saltminer.Asset.SourceId}",
                            AssessmentType = AssessmentType.Net.ToString("g"),
                            Product = "Qualys",
                            Vendor = "Qualys", 
                            GuiUrl = Config.GuiUrlTemplate.Replace("{id}", issue.Id)
                        },
                        Severity = SeverityHelper.ValidSeverity(Config.IssueSeverityMap, issue.Severity.ToString() ?? "0"),
                        SourceSeverity = issue.Severity.ToString() ?? "0",
                        IsSuppressed = issue.Ignored == 1 || issue.Disabled == 1,
                        RemovedDate = issue.LastFixed?.ToUniversalTime()
                    },
                    Saltminer = new()
                    {
                        IssueType = queueScan.Entity.Saltminer.Scan.AssessmentType,
                        Attributes = new() { 
                            { "type", issue.Type },
                            { "status", issue.Status },
                            { "qid", issue.Qid },
                            { "results", issue.Results },
                            { QIssueSyncInProgress, QIssueSyncInProgress },
                            { "times_found", issue.TimesFound.ToString() },
                            { "last_found", issue.LastFound?.ToUniversalTime().ToString("O") }
                        },
                        QueueScanId = queueScan.Id,
                        QueueAssetId = queueAsset.Id,
                        Source = new()
                        {
                            Analyzer = "Qualys",
                        }
                    },
                    Tags = [],
                    Timestamp = DateTime.UtcNow
                }
            };
            CustomAssembly?.CustomizeQueueIssue(queueIssue, issue);
            // Just return the queue issue, this source adapter is a bit different.
            // We need to update the queue issue to add a few more fields before saving.
            // See the other MapIssue or SyncAsync for more info.
            //LocalData.AddUpdate(queueIssue)
            return queueIssue;
        }
    }
}
