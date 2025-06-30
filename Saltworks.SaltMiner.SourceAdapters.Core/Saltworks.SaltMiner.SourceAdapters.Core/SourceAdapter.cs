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

﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.Utility.ApiHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.SourceAdapters.Core
{
    public abstract class SourceAdapter
    {
        protected readonly DataClient.DataClient DataClient;
        protected readonly ApiClient ApiClient;
        // Using two local repos, so can get different scopes for each
        // If we need two or more scopes in source adapters then we'll need to adjust this approach again
        // https://docs.microsoft.com/en-us/ef/core/dbcontext-configuration/#avoiding-dbcontext-threading-issues
        protected readonly ILocalDataRepository LocalData;
        private readonly ILocalDataRepository MyLocalData;
        protected readonly ILogger Logger;
        protected CancellationToken CancelToken = CancellationToken.None;
        protected bool CancellationRequested = false;
        protected bool StillLoading = false;
        // Some adapters produce more than one source type, so we need dictionaries of lists here
        protected Dictionary<string, List<SaltMinerEntity>> ApiSendBatches = [];
        private readonly Dictionary<string, List<string>> QueueScanFinishIds = [];
        protected int FullSyncBatchCount = 0;
        protected bool FullSyncProcessing = false;
        private SourceAdapterConfig Config = null;
        private readonly static SemaphoreSlim SemaFinishScanSend = new(1, 1);

        public bool ForceUpdate { get; set; } = false;
        public bool ResetLocal { get; set; } = false;
        
        protected SourceAdapter(IServiceProvider provider, ILogger logger)
        {
            DataClient = provider.GetRequiredService<DataClientFactory<DataClient.DataClient>>().GetClient();
            ApiClient = provider.GetRequiredService<ApiClientFactory<SourceAdapter>>().CreateApiClient();
            LocalData = provider.CreateScope().ServiceProvider.GetRequiredService<ILocalDataRepository>();
            MyLocalData = provider.CreateScope().ServiceProvider.GetRequiredService<ILocalDataRepository>();
            Logger = logger;
        }

        /// <summary>
        /// For unit tests only, doesn't initialize API client or Data client or local data repo
        /// </summary>
        /// <param name="logger">Needs logger</param>
        protected SourceAdapter(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Starts the source adapter processing.  Make sure to call base.RunAsync() from source adapter first.
        /// </summary>
        /// <param name="config">Source adapter configuration</param>
        /// <param name="token">Cancellation token</param>
        /// <remarks>Typically GetAsync(), SyncAsync(), and SendAsync() are called from RunAsync().</remarks>
        public virtual async Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            var dbName = $"{config.SourceType.ToLower().Replace("saltworks.", "sw-")}-{config.Instance.ToLower()}.db";
            if (ResetLocal)
            {
                Logger.LogInformation("Reset local flag (-r) present, clearing local data for this source adapter.");
                try
                {
                    string[] files = [dbName, $"{dbName}-shm", $"{dbName}-wal"];
                    foreach (var f in files.Where(x => File.Exists(x)))
                        File.Delete(f);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to reset db files due to error: [{Type}] {Msg}", ex.GetType().Name, ex.Message);
                    // ignore other than logging
                }
            }

            Config = config;
            LocalData.SetDbConnection($"Data Source={dbName}");
            MyLocalData.SetDbConnection($"Data Source={dbName}");
            await Task.Delay(1, token); // keep it async so children can have it that way
        }

        protected virtual string GetZeroScannerId(string source, string sourceId) => $"{source}|{sourceId}|zero";
        protected virtual string GetNoScanReportId(string assessmentType) => $"noscan|{assessmentType}";

        protected virtual Data.QueueScan GetNoScanScan(string assessmentType, string assetType, bool isSaltMinerSource=true)
        {
            var now = DateTime.UtcNow;
            return new Data.QueueScan
            {
                Loading = true,
                Entity = new()
                {
                    Saltminer = new()
                    {
                        Engagement = null,
                        Scan = new()
                        {
                            AssessmentType = assessmentType,
                            Product = "None",
                            ReportId = GetNoScanReportId(assessmentType),
                            ScanDate = now,
                            ProductType = "Open",
                            Vendor = "None",
                            AssetType = assetType,
                            IsSaltminerSource = isSaltMinerSource,
                            SourceType = Config.SourceType,
                            Instance = Config.Instance
                        },
                        Internal = new()
                        {
                            IssueCount = 1,
                            QueueStatus = SaltMiner.Core.Entities.QueueScan.QueueScanStatus.Loading.ToString("g"),
                        },
                    },
                    Timestamp = now
                },
                Timestamp = now
            };
        }

        protected virtual Data.QueueIssue GetZeroQueueIssue(Data.QueueScan qScan, Data.QueueAsset qAsset)
        {
            return new()
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
                        Category = ["Application"],
                        Description = "",
                        Name = "ZeroIssue",
                        Location = "N/A",
                        LocationFull = "N/A",
                        Scanner = new()
                        {
                            AssessmentType = qScan.Entity.Saltminer.Scan.AssessmentType,
                            Product = qAsset.SourceType,
                            Vendor = qAsset.SourceType,
                            Id = GetZeroScannerId(qAsset.SourceType, qAsset.SourceId)
                        },
                        Severity = Severity.Zero.ToString("g"),
                        FoundDate = DateTime.UtcNow,
                        ReportId = qScan.Entity.Saltminer.Scan.ReportId
                    },
                    Saltminer = new()
                    {
                        Attributes = [],
                        QueueScanId = qScan.Id,
                        QueueAssetId = qAsset.Id,
                        Source = new()
                        {
                            Analyzer = "Zero"
                        }
                    },
                    Tags = [],
                    Timestamp = DateTime.UtcNow
                }
            };
        }

        protected virtual async Task LetSendCatchUpAsync(SourceAdapterConfig config, string instance="", string sourceType="")
        {
            if (string.IsNullOrEmpty(instance)) instance = config.Instance;
            if (string.IsNullOrEmpty(sourceType)) sourceType = config.SourceType;
            if (LocalData.CountQueueScans(instance, sourceType) >= config.SyncHoldForSendThreshold)
            {
                do
                {
                    CheckCancel(true);
                    Logger.LogInformation("[Sync] Waiting for Send to catch up (120 sec)...");
                    await Task.Delay(TimeSpan.FromMinutes(2), CancelToken);
                } while (LocalData.CountQueueScans(config.Instance, config.SourceType) > config.SyncResumeWhenSendThreshold);
            }
        }

        protected virtual void SetCancelToken()
        {
            CancellationRequested = true;
        }

        protected virtual void CheckCancel(bool readyToAbort = true)
        {
            if (CancelToken == CancellationToken.None)
            {
                return;
            }

            if (CancelToken.IsCancellationRequested || CancellationRequested)
            {
                if (readyToAbort)
                {
                    Logger.LogInformation("Cancellation requested, aborting processing");
                    throw new CancelTokenException();
                }
                else
                {
                    if (!CancellationRequested)
                    {
                        Logger.LogInformation("Cancellation request acknowledged");
                        CancellationRequested = true;
                    }
                }
            }
        }

        protected virtual void SetApiClientSslVerification(bool verifySsl)
        {
            if (!verifySsl)
            {
                Logger.LogWarning("It is not recommended to bypass SSL verification in production");
            }

            ApiClient.Options.VerifySsl = verifySsl;
        }

        protected virtual async Task SendAsync(SourceAdapterConfig config, string assetType) =>
            await SendAsync(config, assetType, null);
        protected virtual async Task SendAsync(SourceAdapterConfig config, string assetType, string sourceType)
        {
            var totalCounter = 0;
            var exceptionCounter = 0;
            var batchTotal = 0L;

            if (string.IsNullOrEmpty(sourceType))
                sourceType = config.SourceType;

            try
            {
                // Enable if desired for debugging
                // Grab any local queue scans that are ready for processing
                Data.QueueScan localQueueScanDto = null;
                Logger.LogDebug("[Send] Begin GetQueueScans");
                batchTotal = MyLocalData.CountQueueScans(config.Instance, sourceType);
                Logger.LogDebug("[Send] GetQueueScans for source type '{SourceType}' - batch total {Total}", sourceType, batchTotal);
                if (batchTotal > 0)
                    localQueueScanDto = MyLocalData.GetNextQueueScan(config.Instance, sourceType, 0);

                Logger.LogDebug("[Send] First pass, pending local queue scan {Not}found", localQueueScanDto?.Entity != null ? "" : "not ");
                var scanCounter = 0;
                var qScanIds = new List<string>();

                while (localQueueScanDto != null || StillLoading)
                {
                    // Setting a var to error handling messages in advance is only unused when no exceptions occur
                    #pragma warning disable S1854 // Unused assignments should be removed
                    if (localQueueScanDto != null)
                    {
                        if (localQueueScanDto.Entity.Saltminer.Internal.QueueStatus != SaltMiner.Core.Entities.QueueScan.QueueScanStatus.Loading.ToString("g"))
                        {
                            throw new SourceException($"[Send] Queue status for source type '{sourceType}' & queue scan with ID {localQueueScanDto.Entity.Id} is {localQueueScanDto.Entity.Saltminer.Internal.QueueStatus}, when it should be Loading.");
                        }
                        var errDuring = "?";
                        try
                        {
                            Logger.LogInformation("[Send] ({BatchTotal} remain) for {SourceType}.  Queue scan: local ID '{Id}', instance {Instance}, report ID '{ReportTotal}'", batchTotal, sourceType, localQueueScanDto.Id, config.Instance, localQueueScanDto.Entity.Saltminer.Scan.ReportId);

                            var scanIssueCounter = 0;
                            var assetCounter = 0;
                            var issueCounter = 0;
                            var localQueueAssets = MyLocalData.GetQueueAssets(localQueueScanDto.Id);

                            Logger.LogDebug("[Send] {Count} asset(s) to process for queue scan with report ID '{Id}'", localQueueAssets.Count(), localQueueScanDto.Entity.Saltminer.Scan.ReportId);

                            foreach (var localQueueAsset in localQueueAssets)
                            {
                                Logger.LogDebug("[Send] Beginning asset loop, queue asset local id {Id}", localQueueAsset.Id);
                                CheckCancel(false);
                                var curSourceId = localQueueAsset.SourceId;
                                if (string.IsNullOrEmpty(curSourceId))
                                    throw new SourceValidationException("Queue Asset Source ID required but found to be missing/empty.");
                                if (string.IsNullOrEmpty(localQueueAsset.Entity.Saltminer.Asset.Name))
                                    throw new SourceValidationException("Queue Asset Name required but found to be missing/empty.");

                                // STEP 1: Create queue scan from localQueueScan, then create queue asset using the new ID
                                // NOTE: "duplicate" queue scans are expected, 1 for each queue asset when more than 1 asset
                                errDuring = $"creating queue scan for source type {sourceType} and source ID {curSourceId}";
                                var queueScan = localQueueScanDto.Clone(); 
                                queueScan.Entity.Id = Guid.NewGuid().ToString(); // create ID locally so can batch scans

                                // STEP 2: Create additional queue scan docs to send from queue scan history
                                var histCounter = 0;
                                errDuring = $"creating and sending queue scan history for source type {sourceType}, source ID {curSourceId}, and queue scan id {queueScan.Entity.Id}";
                                foreach (var newRecord in localQueueScanDto.History ?? [])
                                {
                                    newRecord.Entity.Saltminer.Internal.CurrentQueueScanId = queueScan.Entity.Id;
                                    newRecord.Entity.Id = Guid.NewGuid().ToString();
                                    await SendApiBatchAsync(newRecord, sourceType);  // Queue to send to API
                                    histCounter++;
                                }
                                Logger.LogDebug("[Send] {Count} history queue scan(s) for queue scan with source type '{SourceType}', report ID '{ReportId}' & queue asset '{SourceId}' ", histCounter, sourceType, localQueueScanDto.Entity.Saltminer.Scan.ReportId, localQueueAsset.Entity.Saltminer.Asset.SourceId);

                                // STEP 3: Create queue asset from localQueueAsset and the newly created queueScan
                                errDuring = $"creating queue asset with source type {sourceType}, source ID {curSourceId}, queue scan id {queueScan.Entity.Id}, and id {localQueueAsset.Entity.Id}";
                                localQueueAsset.Entity.Saltminer.Internal.QueueScanId = queueScan.Entity.Id;
                                localQueueAsset.Entity.Id = Guid.NewGuid().ToString(); // create ID locally so can batch assets

                                if (!string.IsNullOrEmpty(config.InventoryAssetKeyAttribute))
                                {
                                    var assetInventoryKeyAttribute = config.InventoryAssetKeyAttribute;
                                    var queueAssetAttributes = localQueueAsset.Entity.Saltminer.Asset.Attributes;

                                    if (queueAssetAttributes != null && queueAssetAttributes.TryGetValue(assetInventoryKeyAttribute, out string value) && !string.IsNullOrEmpty(value))
                                    {
                                        var assetInventoryKeyBase = new InventoryAssetKeyInfo()
                                        {
                                            Key = value
                                        };

                                        localQueueAsset.Entity.Saltminer.InventoryAsset = assetInventoryKeyBase;
                                    }
                                    else
                                    {
                                        Logger.LogWarning("[Send] Asset inventory key attribute {Key} could not be found for source type '{SourceType}' and queue asset id {Id}", assetInventoryKeyAttribute, sourceType, localQueueAsset.Id);
                                    }
                                }

                                errDuring = $"sending a queue asset with source type {sourceType}, source ID {curSourceId}, queue scan id {queueScan.Entity.Id}, and id {localQueueAsset.Entity.Id}";
                                await SendApiBatchAsync(localQueueAsset, sourceType);  // Queue to send to API

                                assetCounter++;

                                Logger.LogDebug("[Send] Created instance {Instance} for {SourceType} queue asset '{SourceId}' with id '{Id}'", config.Instance, sourceType, localQueueAsset.Entity.Saltminer.Asset.SourceId, localQueueAsset.Entity.Id);

                                // STEP 4: Process queue issues using IDs from the previous two entities
                                errDuring = $"retrieving queue issues for source type {sourceType}, source ID {curSourceId}, queue scan with id {queueScan.Entity.Id}";
                                var issues = MyLocalData.GetQueueIssues(localQueueScanDto.Id, localQueueAsset.Id);

                                // group issues by scanner.id to ensure unique values
                                var issuesById = issues.GroupBy(x => new { x.Entity.Vulnerability.Scanner.Id });

                                foreach (var issueGroup in issuesById)
                                {
                                    var needNumberSequence = false;
                                    var numberSequence = 2;
                                    var firstIssue = true;

                                    // this shows duplicate vul.scanner.id
                                    if (issueGroup.Count() > 1)
                                    {
                                        needNumberSequence = true;
                                    }

                                    foreach (var issue in issueGroup)
                                    {
                                        Logger.LogDebug("[Send] Beginning issue loop, queue issue id {Id}", issue.Id);
                                        if (issue == null)
                                        {
                                            continue;
                                        }

                                        CheckCancel(false);

                                        // need seq number added to scanner id, but not on the first issue
                                        if (needNumberSequence && !firstIssue && config.EnableScannerIdNumberSequence)
                                        {
                                            issue.Entity.Vulnerability.Scanner.Id = $"{issue.Entity.Vulnerability.Scanner.Id}#{numberSequence}";
                                            numberSequence++;
                                            Logger.LogWarning("[Send] Instance {Instance} duplicate issue {IssueId} imported for source type '{SourceType}', queue scan report Id {ReportId} and Id {Id}", config.Instance, issue.Entity.Vulnerability.Scanner.Id, sourceType, localQueueScanDto.Entity.Saltminer.Scan.ReportId, localQueueScanDto.Id);
                                        }

                                        totalCounter++;
                                        scanIssueCounter++;
                                        issue.Entity.Saltminer.QueueScanId = queueScan.Entity.Id;
                                        issue.Entity.Saltminer.QueueAssetId = localQueueAsset.Entity.Id;
                                        errDuring = $"sending a queue issue with source type {sourceType}, source ID {curSourceId}, queue scan id {queueScan.Entity.Id}, and id {issue.Entity.Id}";
                                        await SendApiBatchAsync(issue, sourceType);  // Queue to send to API

                                        firstIssue = false;
                                        issueCounter++;
                                    }
                                }

                                if (!issues.Any())
                                {
                                    Logger.LogDebug("[Send] 0 QueueIssues sent.");
                                }

                                // STEP 5: Update issue counts, then send queue scan
                                queueScan.Entity.Saltminer.Internal.IssueCount = issueCounter;
                                errDuring = $"sending queue scan with source type {sourceType}, source ID {curSourceId}, and id {queueScan.Entity.Id}";

                                await SendApiBatchAsync(queueScan, sourceType);
                                qScanIds.Add(queueScan.Entity.Id);

                                Logger.LogDebug("[Send] Created queue scan: {Count} issue(s), instance {Instance} for {SourceType}, '{IssueId}' id, report ID '{ReportTotal}', local ID '{Id}'", issueCounter, config.Instance, sourceType, queueScan.Entity.Id, queueScan.Entity.Saltminer.Scan.ReportId, localQueueScanDto.Id);
                                issueCounter = 0;
                            }

                            if (config.LoadingDelay > 0)
                            {
                                Logger.LogDebug("[Send] Waiting {Delay} ms as configured in LoadingDelay", config.LoadingDelay);
                                await Task.Delay(config.LoadingDelay, CancelToken);
                            }

                            // Update API queue scan status
                            foreach (var id in qScanIds)
                                await FinishScanAsync(id, sourceType);
                            qScanIds.Clear();

                            // Delete local queue entities
                            errDuring = $"cleaning up after status updates for local queue scan id {localQueueScanDto.Id} and report id {localQueueScanDto.ReportId}";
                            MyLocalData.DeleteQueueScan(localQueueScanDto.Id, true);

                            Logger.LogDebug("[Send] Deleted local queue scan '{Id}', including related queue assets and queue issues", localQueueScanDto.Id);
                            Logger.LogInformation("[Send] {SourceType} queue scan with local ID '{Id}':  {AssetCounter} queue asset(s), {ScanIssueCounter} queue issues sent.",  sourceType, localQueueScanDto.Id, assetCounter, scanIssueCounter);
                        }
                        catch (Exception ex)
                        {
                            // Exception counter check
                            exceptionCounter++;

                            Logger.LogError(ex, "[Send] General failure {ErrDuring}, queue scan with ID '{Id}' and report ID '{RId}' will be aborted: [{ExType}] {ExMsg}", errDuring, localQueueScanDto.Id, localQueueScanDto.ReportId, ex.GetType().Name, ex.Message);

                            localQueueScanDto.Entity.Saltminer.Internal.QueueStatus = SaltMiner.Core.Entities.QueueScan.QueueScanStatus.Error.ToString("g");
                            localQueueScanDto.FailureCount += 1;
                            MyLocalData.AddUpdate(localQueueScanDto, true);
                            try
                            {
                                await DataClient.QueueScanUpdateStatusAsync(localQueueScanDto.Id, SaltMiner.Core.Entities.QueueScan.QueueScanStatus.Error);
                            }
                            catch (Exception ex1)
                            {
                                Logger.LogDebug(ex1, "[Send] Failed to set error status for source type '{SourceType}' on failed queue scan with ID '{Id}'", sourceType, localQueueScanDto.Id);
                            }

                            if (exceptionCounter >= config.SourceAbortErrorCount)
                            {
                                SetCancelToken();
                                Logger.LogInformation("[Send] Reached max errors allowed for this source ({Count}), cancelling.", config.SourceAbortErrorCount);
                            }
                            else
                            {
                                Logger.LogInformation("[Send] Current error count {Counter} ({Config} to cancel).", exceptionCounter, config.SourceAbortErrorCount);
                            }
                        }
                        scanCounter++;
                    }
                    else
                    {
                        // Add configured delay if scan is null and we're still loading
                        if (StillLoading)
                        {
                            Logger.LogDebug("[Send] Still loading for source type '{SourceType}'...", sourceType);
                            await Task.Delay(config.StillLoadingDelay * 1000, CancelToken);
                        }
                    }
                    #pragma warning restore S1854 // Unused assignments should be removed
                    CheckCancel(true);

                    // Check for next local queue scan ready to process
                    Logger.LogDebug("[Send] Next GetQueueScans");
                    batchTotal = MyLocalData.CountQueueScans(config.Instance, sourceType);
                    Logger.LogDebug("[Send] Next GetQueueScans for source type '{SourceType}' - batch total {Total}", sourceType, batchTotal);
                    if (batchTotal > 0)
                        localQueueScanDto = MyLocalData.GetNextQueueScan(config.Instance, sourceType, 0);
                    else
                        localQueueScanDto = null;
                }
                // Send any remaining batch items
                Logger.LogDebug("[Send] Finishing sending any remaining batch items");
                // Finish any remaining queue scans
                foreach (var id in qScanIds)
                    await FinishScanAsync(id, sourceType);
                await FinishScanAsync(null, sourceType);
                await SendApiBatchAsync<Data.QueueScan>(null, sourceType);  // Process any remaining in API queue

                Logger.LogInformation("[Send] Processing complete for source type '{SourceType}', {ScanCounter} queue scan(s) sent, {TotalCounter} queue issues sent.", sourceType, scanCounter, totalCounter);

                ClearQueues(sourceType);
                Logger.LogDebug("[Send] Removed local queue scan(s) for source type '{SourceType}' after processing.", sourceType);
            }
            catch (CancelTokenException ex)
            {
                Logger.LogInformation(ex, "[Send] Cancelling source processing for source type '{SourceType}'.", sourceType);
                throw new CancelTokenException($"[Send] Cancelling source processing for source type '{sourceType}'.");
            }
            catch (Exception ex)
            {
                SetCancelToken();
                Logger.LogCritical(ex, "[Send] Error for source type '{SourceType}': {Msg}", sourceType, ex.InnerException?.Message ?? ex.Message);
                throw new SourceException($"Error for source type '{sourceType}': {ex.InnerException?.Message ?? ex.Message}", ex);
            }
        }

        protected virtual async Task FinishScanAsync(string scanId, string sourceType)
        {
            var debugHoldOneSendAsync = false; // Set this when debugging Wiz, but turn off after!

            // Some sources have multiple source types
            if (!QueueScanFinishIds.ContainsKey(sourceType))
                QueueScanFinishIds.Add(sourceType, []);
            var idList = QueueScanFinishIds[sourceType];

            // null/empty scanId is signal to send the rest
            if (!string.IsNullOrEmpty(scanId))
            {
                if (idList.Contains(scanId))
                    Logger.LogError("Duplicate scan ID '{Id}' found when finishing queue scan (updating status to Pending).", scanId);
                else
                    idList.Add(scanId);
            }
            if (idList.Count < Config.QueueSendScanUpdateBatchSize && !string.IsNullOrEmpty(scanId))
                return;

            // Complete running batch to make sure all docs are sent
            Logger.LogInformation("[Send] Refreshing queue indices");
            RefreshElasticQueues();
            await SendApiBatchAsync<Data.QueueAsset>(null, sourceType);

            if (debugHoldOneSendAsync)
                await SemaFinishScanSend.WaitAsync();

            Logger.LogInformation("[Send] Finishing {Count} queue scans after a 5 sec pause...", idList.Count);
            await Task.Delay(TimeSpan.FromSeconds(5));
            foreach (var id in idList)
            {
                // We might get ahead of Elasticsearch, so we will retry up to 5 times (configurable) with a 5 sec (configurable) delay between retries.
                var i = 0;
                while (true)
                {
                    try
                    {
                        var rsp = await DataClient.QueueScanUpdateStatusAsync(id, SaltMiner.Core.Entities.QueueScan.QueueScanStatus.Pending);
                        if (!rsp.Success)
                            Logger.LogWarning("[Send] Failed to finalize queue scan '{Id}' for source type '{SrcType}'.  API response: {Response}", id, sourceType, rsp.Message);
                        break;
                    }
                    catch (DataClientValidationException ex)
                    {

                        if (!ex.Message.StartsWith("Counted"))
                        {
                            throw;
                        }
                        i++;
                        if (i >= Config.SendCountValidationErrorRetries)
                        {
                            Logger.LogWarning(ex, "[Send] Count validation failure after max retries, consider increasing SendCountValidationErrorRetryDelaySec and SendCountValidationErrorRetries to give Elasticsearch more time to catch up.");
                            throw;
                        }
                        Logger.LogWarning("[Send] Validation count error finalizing queue scan '{Id}' for source type '{SourceType}', attempt {Count}/{Total}, retry after a {Delay} sec delay...", id, sourceType, i, Config.SendCountValidationErrorRetries, Config.SendCountValidationErrorRetryDelaySec * i);
                        await Task.Delay(Config.SendCountValidationErrorRetryDelaySec * i * 1000);
                    }
                }
                Logger.LogDebug("[Send] Updated QueueScanStatus for queueScan ID '{Id}' to Pending", id);
            }
            idList.Clear();
            if (debugHoldOneSendAsync)
                SemaFinishScanSend.Release();
        }

        protected void RefreshElasticQueues()
        {
            DataClient.RefreshIndex(SaltMiner.Core.Entities.QueueIssue.GenerateIndex());
            DataClient.RefreshIndex(SaltMiner.Core.Entities.QueueScan.GenerateIndex());
            DataClient.RefreshIndex(SaltMiner.Core.Entities.QueueAsset.GenerateIndex());
        }

        protected void RefreshElasticObjects(string assetType = null, string sourceType = null, string instance = null)
        {
            DataClient.RefreshIndex(Issue.GenerateIndex(assetType, sourceType, instance));
            DataClient.RefreshIndex(Scan.GenerateIndex(assetType, sourceType, instance));
            DataClient.RefreshIndex(Asset.GenerateIndex(assetType, sourceType, instance));
        }

        protected virtual void ClearQueues(string sourceType="")
        {
            MyLocalData.DeleteAllQueues(sourceType);
        }

        protected virtual void ResetFailures(SourceAdapterConfig config) => ResetFailures(config, null);
        protected virtual void ResetFailures(SourceAdapterConfig config, string sourceType)
        {
            if (string.IsNullOrEmpty(sourceType))
                sourceType = config.SourceType;

            foreach (var qScan in LocalData.GetQueueScans(config.Instance, sourceType, config.SendFailureCount))
            {
                qScan.FailureCount++;
                qScan.Entity.Saltminer.Internal.QueueStatus = SaltMiner.Core.Entities.QueueScan.QueueScanStatus.Loading.ToString("g");
                LocalData.AddUpdate(qScan);
            }
        }

        protected virtual void DeleteFailures(SourceAdapterConfig config) => DeleteFailures(config, null);
        protected virtual void DeleteFailures(SourceAdapterConfig config, string sourceType)
        {
            if (string.IsNullOrEmpty(sourceType))
                sourceType = config.SourceType;

            var idList = LocalData.GetQueueScans(config.Instance, sourceType, config.SendFailureCount, DateTime.UtcNow.AddDays(-config.SendFailureDeleteDays), false)
                .Select(x => x.Id);
            foreach (var id in idList)
            {
                LocalData.DeleteQueueScan(id);
            }
        }

        protected virtual void FullSyncBatchProcess(SyncRecord syncRecord, int fullSyncBatchSize)
        {
            syncRecord = syncRecord ?? throw new ArgumentNullException(nameof(syncRecord));

            if (string.IsNullOrEmpty(syncRecord.FullSyncSourceId))
            {
                Logger.LogInformation("[ProcessFullSync] Source Id {CurId} to begin batch", syncRecord.CurrentSourceId);
                syncRecord.FullSyncSourceId = syncRecord.CurrentSourceId;
            }

            //found a match with source to sync rec, begin full sync process
            if (syncRecord.CurrentSourceId == syncRecord.FullSyncSourceId)
            {
                Logger.LogInformation("[ProcessFullSync] Starting full sync batch with Source Id {CurId}", syncRecord.CurrentSourceId);
                FullSyncProcessing = true;
                ForceUpdate = true;
                FullSyncBatchCount++;
            }
            else
            {
                if (FullSyncProcessing)
                {
                    if (FullSyncBatchCount < fullSyncBatchSize)
                    {
                        //less than batch size, increment batch count and keep processing
                        FullSyncBatchCount++;
                    }
                    else
                    {
                        //reached the max batch size, completed full sync batch
                        Logger.LogInformation("[ProcessFullSync] Completed full sync batch. Next Source Id {CurId}", syncRecord.CurrentSourceId);
                        FullSyncProcessing = false;
                        ForceUpdate = false;
                        syncRecord.FullSyncSourceId = syncRecord.CurrentSourceId;
                    }
                }
            }
        }

        [Obsolete("The addToLogger parameter will be removed in a future version - use the other overload of this method instead.")]
        protected virtual bool NeedsUpdate(SourceMetric sourceMetric, SourceMetric localMetric, bool addToLogger, List<NeedsUpdateEnum> skipChecks = null) =>
            NeedsUpdate(sourceMetric, localMetric, skipChecks);

        /// <summary>
        /// Compares two sets of metrics, returning true if they do NOT match
        /// </summary>
        protected virtual bool NeedsUpdate(SourceMetric sourceMetric, SourceMetric localMetric, List<NeedsUpdateEnum> skipChecks = null)
        {
            string message = null;

            Logger.LogDebug("[NeedsUpdate] Checking NeedsUpdate on SourceId: {MetricSourceId} and SourceType: {MetricSourceType}", sourceMetric.SourceId, sourceMetric.SourceType);
            
            if (ForceUpdate)
            {
                message = "ForceUpdate Set, processing metric";
            }
            else if ((skipChecks?.Count ?? 0) == Enum.GetValues<NeedsUpdateEnum>().Length)
            {
                Logger.LogWarning("[NeedsUpdate] ALL checks are being skipped - this may have unintended consequences.");
            }
            else if (localMetric == null)
            {
                message = "Local Metric Missing, processing metric";
            }
            else if ((skipChecks == null || !skipChecks.Contains(NeedsUpdateEnum.LastScan)) && sourceMetric.LastScan != localMetric.LastScan)
            {
                message = $"LastScan different L: {localMetric.LastScan} S: {sourceMetric.LastScan}, processing metric";
            }
            else if ((skipChecks == null || !skipChecks.Contains(NeedsUpdateEnum.IssueCount)) && sourceMetric.IssueCount != localMetric.IssueCount)
            {
                message = $"IssueCount different L: {localMetric.IssueCount} S: {sourceMetric.IssueCount}, processing metric";
            }
            else if ((skipChecks == null || !skipChecks.Contains(NeedsUpdateEnum.IssueCountSev1)) && sourceMetric.IssueCountSev1 != localMetric.IssueCountSev1)
            {
                message = $"IssueCountSev1 different L: {localMetric.IssueCountSev1} S: {sourceMetric.IssueCountSev1}, processing metric";
            }
            else if ((skipChecks == null || !skipChecks.Contains(NeedsUpdateEnum.IssueCountSev2)) && sourceMetric.IssueCountSev2 != localMetric.IssueCountSev2)
            {
                message = $"IssueCountSev2 different L: {localMetric.IssueCountSev2} S: {sourceMetric.IssueCountSev2}, processing metric";
            }
            else if ((skipChecks == null || !skipChecks.Contains(NeedsUpdateEnum.IssueCountSev3)) && sourceMetric.IssueCountSev3 != localMetric.IssueCountSev3)
            {
                message = $"IssueCountSev3 different L: {localMetric.IssueCountSev3} S: {sourceMetric.IssueCountSev3}, processing metric";
            }
            else if ((skipChecks == null || !skipChecks.Contains(NeedsUpdateEnum.Instance)) && sourceMetric.Instance != localMetric.Instance)
            {
                message = $"Instance different L: {localMetric.Instance} S: {sourceMetric.Instance}, processing metric";
            }
            else if ((skipChecks == null || !skipChecks.Contains(NeedsUpdateEnum.SourceId)) && sourceMetric.SourceId != localMetric.SourceId)
            {
                message = $"SourceId different L: {localMetric.SourceId} S: {sourceMetric.SourceId}, processing metric";
            }
            else if ((skipChecks == null || !skipChecks.Contains(NeedsUpdateEnum.SourceType)) && sourceMetric.SourceType != localMetric.SourceType)
            {
                message = $"SourceType different L: {localMetric.SourceType} S: {sourceMetric.SourceType}, processing metric";
            }
            else if ((skipChecks == null || !skipChecks.Contains(NeedsUpdateEnum.IsNotScanned)) && sourceMetric.IsNotScanned != localMetric.IsNotScanned)
            {
                message = $"IsNotScanned different L: {localMetric.IsNotScanned} S: {sourceMetric.IsNotScanned}, processing metric";
            }
            else if ((skipChecks == null || !skipChecks.Contains(NeedsUpdateEnum.Attributes)) && sourceMetric.Attributes != null && localMetric.Attributes == null)
            {
                message = "Attributes were not empty and now are, processing metric";
            }
            else if ((skipChecks == null || !skipChecks.Contains(NeedsUpdateEnum.Attributes)) && sourceMetric.Attributes == null && localMetric.Attributes != null)
            {
                message = "Attributes were empty and now are not, processing metric";
            }
            else if ((skipChecks == null || !skipChecks.Contains(NeedsUpdateEnum.Attributes)) && sourceMetric.Attributes != null)
            {
                if (sourceMetric.Attributes.Count != localMetric.Attributes.Count)
                {
                    message = $"Different number of Attributes L: {localMetric.Attributes.Count} S: {sourceMetric.Attributes.Count}, processing metric";
                }
                else
                {
                    foreach (var attribute in sourceMetric.Attributes)
                    {

                        if (!localMetric.Attributes.ContainsKey(attribute.Key))
                        {
                            message = $"{attribute.Key} Attribute Missing, processing metric";
                        }

                        if (attribute.Value != localMetric.Attributes[attribute.Key])
                        {
                            message = $"{attribute.Key} Attribute Value L: {localMetric.Attributes[attribute.Key]} S: {attribute.Value} changed, processing metric";
                        }
                    }
                }
            }

            // message is set to first update need found above, so it will be empty if no update is needed.
            var update = !string.IsNullOrEmpty(message);
            if (!update) 
                message = "no update needed";

            if (Config.LogNeedsUpdate)
                Logger.LogInformation("[NeedsUpdate] Source ID '{MetricSourceId}', source type '{MetricSourceType}': {Msg}.", sourceMetric.SourceId, sourceMetric.SourceType, message);
            else
                Logger.LogDebug("[NeedsUpdate] Source ID '{MetricSourceId}', source type '{MetricSourceType}': {Msg}.", sourceMetric.SourceId, sourceMetric.SourceType, message);

            return update;
        }

        /// <summary>
        /// Retires assets, based on IsProcessed flag set not set to true in local metrics during sync.
        /// </summary>
        /// <param name="localMetrics">Local metrics to process</param>
        protected virtual void RetireLocalMetrics(List<SourceMetric> localMetrics)
        {
            Logger.LogWarning("Retirement currently disabled, cannot retire local metrics");
            if ((localMetrics?.Count ?? 0) > 0)
                return;
            
            // Do nothing if empty metrics
            if ((localMetrics?.Count ?? 0) == 0)
                return;

            try
            {
                foreach (var local in localMetrics.Where(x => !x.IsProcessed))
                {
                    if (!local.IsRetired)
                    {
                        local.IsRetired = true;
                        LocalData.AddUpdate(local);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error retiring local metrics");
                // Swallow the error, don't let retiring error stop processing
            }
        }

        protected virtual void RetireQueueAssets(List<SourceMetric> localMetrics, string assetType, SourceAdapterConfig config)
        {
            Logger.LogWarning("Retirement currently disabled, cannot retire queue assets");
            if ((localMetrics?.Count ?? 0) > 0)
                return;

            try
            {
                // return if nothing to do
                if (localMetrics.Count == 0)
                    return;

                foreach (var metric in localMetrics.Where(x => !x.IsProcessed))
                {
                    Logger.LogInformation("[Retire Queue Assets] Config '{Config}', Source ID '{Name}' did not appear in source data, retiring related asset", metric.Instance, metric.SourceId);
                    var queueAsset = new SaltMiner.Core.Entities.QueueAsset
                    {
                        Saltminer = new SaltMinerQueueAssetInfo
                        {
                            Internal = new()
                            {
                                QueueScanId = "0"
                            },
                            Asset = new()
                            {
                                SourceId = metric.SourceId,
                                SourceType = metric.SourceType,
                                Instance = metric.Instance,
                                AssetType = assetType,
                                IsRetired = true,
                                Name = $"{metric.SourceId}|{assetType}",
                                Attributes = metric.Attributes,
                                Description = "",
                                Host = "",
                                Ip = "0.0.0.0",
                                Port = 0,
                                Scheme = "",
                                IsProduction = true,
                                IsSaltminerSource = config.IsSaltminerSource,
                                LastScanDaysPolicy = config.LastScanDaysPolicy,
                                Version = "",
                                VersionId = ""
                            }
                        }
                    };
                    DataClient.QueueAssetAddUpdate(queueAsset);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error retiring queue assets");
                // Swallow the error, don't let retiring error stop processing
            }
        }

        protected virtual void UpdateLocalMetric(SourceMetric sourceMetric, SourceMetric localMetric)
        {
            if (localMetric == null)
            {
                localMetric = sourceMetric;
            }
            else
            {
                localMetric.IssueCount = sourceMetric.IssueCount;
                localMetric.IssueCountSev1 = sourceMetric.IssueCountSev1;
                localMetric.IssueCountSev2 = sourceMetric.IssueCountSev2;
                localMetric.IssueCountSev3 = sourceMetric.IssueCountSev3;
                localMetric.LastScan = sourceMetric.LastScan;
                localMetric.SourceId = sourceMetric.SourceId;
                localMetric.Attributes = sourceMetric.Attributes;
                localMetric.IsNotScanned = sourceMetric.IsNotScanned;
                localMetric.IsRetired = sourceMetric.IsRetired;
            }

            LocalData.AddUpdate(localMetric);
        }

        public virtual void FirstLoadSyncUpdate(SourceAdapterConfig config, Action<SourceMetric, Asset> metricCustomization = null) =>
            FirstLoadSyncUpdate(config, null, metricCustomization);
        public virtual void FirstLoadSyncUpdate(SourceAdapterConfig config, string sourceType, Action<SourceMetric, Asset> metricCustomization = null)
        {
            if (string.IsNullOrEmpty(sourceType))
                sourceType = config.SourceType;

            if (config.DisableFirstLoad)
            {
                Logger.LogInformation("[First Load] Disabled in configuration, exiting first load");
                return;
            }

            if (LocalData.GetSourceMetrics(config.Instance, sourceType).Any())
            {
                Logger.LogInformation("[First Load] Local Metrics found, no action needed");
                return;
            }

            Logger.LogInformation("[First Load] Local Metrics Missing, Starting First Load");

            // Asset Search Request Set Up
            var assetsRequest = new SearchRequest()
            {
                PitPagingInfo = new PitPagingInfo(config.FirstLoadBatchSize, true),
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string>()
                    {
                        { "Saltminer.Asset.SourceType", sourceType }
                    },
                    AnyMatch = true
                },
                SourceType = sourceType,
                Instance = config.Instance
            };

            // Priming read
            var assetsResults = DataClient.AssetSearch(assetsRequest);
            var totalAssets = assetsResults.PitPagingInfo.Total;
            Logger.LogInformation("[First Load] {Total} Assets Found", totalAssets);

            if (!assetsResults.Data.Any())
            {
                Logger.LogInformation("[First Load] Exiting First Load");
                return;
            }

            var metricCount = 0;
            var sourceMetricList = new List<SourceMetric>();

            // Iterate over assets to create source metric and save to local data.
            do
            {
                foreach (var asset in assetsResults.Data)
                {
                    var sourceMetric = new SourceMetric()
                    {
                        Attributes = asset.Saltminer.Asset.Attributes,
                        IsProcessed = true,
                        IsRetired = asset.Saltminer.Asset.IsRetired,
                        Instance = asset.Saltminer.Asset.Instance,
                        IsSaltminerSource = asset.Saltminer.Asset.IsSaltminerSource,
                        SourceId = asset.Saltminer.Asset.SourceId,
                        SourceType = asset.Saltminer.Asset.SourceType,
                        VersionId = asset.Saltminer.Asset.VersionId
                    };

                    metricCustomization?.Invoke(sourceMetric, asset);
                    LocalData.AddUpdate(sourceMetric);
                    sourceMetricList.Add(sourceMetric);
                    sourceMetric.Attributes ??= [];
                    metricCount++;
                }

                Logger.LogInformation("[First Load] Adding {Count} metrics of {Total} ", sourceMetricList.Count, totalAssets);
                sourceMetricList = [];

                assetsRequest.PitPagingInfo = assetsResults.PitPagingInfo;
                assetsRequest.AfterKeys = assetsResults.AfterKeys;
                assetsResults = DataClient.AssetSearch(assetsRequest);
            } while (assetsResults.Data.Any());

            Logger.LogInformation("[First Load] {Count} Local Metrics Saved", metricCount);
            Logger.LogInformation("[First Load] First Load Finished");
        }

        private async Task SendApiBatchAsync<T>(T item, string sourceType) where T: class, ILocalDataEntity
        {
            if (!ApiSendBatches.ContainsKey(sourceType))
                ApiSendBatches.Add(sourceType, []);
            var sendBatch = ApiSendBatches[sourceType];
            string[] types = ["QueueIssue", "QueueAsset", "QueueScan"];
            if (!types.Contains(typeof(T).Name))
                throw new ArgumentException($"Invalid/unsupported type {typeof(T).Name}.", nameof(item));
            if (item != null)
            {
                SaltMinerEntity e = null;
                if (item is Data.QueueAsset qa)
                    e = qa.Entity;
                if (item is Data.QueueIssue qi)
                    e = qi.Entity;
                if (item is Data.QueueScan qs)
                    e = qs.Entity;
                sendBatch.Add(e);
            }
            if (item == null || sendBatch.Count >= Config.QueueSendBatchSize)
            {
                if (sendBatch.Count == 0)
                {
                    Logger.LogInformation("[Send] Bulk send for {SrcType}: empty queue, sending nothing.", sourceType);
                    return;
                }
                var timer = Stopwatch.StartNew();
                await DataClient.QueueAddUpdateBulkAsync(sendBatch);
                timer.Stop();
                Logger.LogInformation("[Send] Bulk send {Count} {SrcType} docs to API in {Sec} secs.", sendBatch.Count, sourceType, timer.Elapsed.TotalSeconds);
                sendBatch.Clear();
            }
        }

        public virtual bool IsSourceAdapterCompatible(SourceAdapterConfig config)
        {
            var apiVersion = DataClient.GetApiVersion().Message;
            if (apiVersion.Contains('-'))
                apiVersion = apiVersion[..apiVersion.IndexOf("-")];

            if (IsApiVersionLessThanSourceMinimum(config.MinimumCompatibleApiVersion, apiVersion))
            {
                Logger.LogError("Source type {SourceType} will not process. Its minimum compatible version is {MinVer} and the current Data API version is {ApiVer}.", config.SourceType, config.MinimumCompatibleApiVersion, apiVersion);
                return false;
            }

            if (IsApiVersionGreaterThanSourceCurrent(config.CurrentCompatibleApiVersion, apiVersion))
            {
                Logger.LogWarning("Source type {SourceType} has a current compatible version of {CurVer} and is less than the current Data API version of {ApiVer}.", config.SourceType, config.CurrentCompatibleApiVersion, apiVersion);
            }

            return true;
        }

        private static bool IsApiVersionLessThanSourceMinimum(string sourceVersion, string apiVersion)
        {
            var srcTmp = sourceVersion.Split('.');
            if (srcTmp.Length > 3) 
                sourceVersion = sourceVersion.Replace($".{srcTmp[3]}", "");
            var apiTmp = apiVersion.Split(".");
            if (srcTmp.Length > 3) 
                apiVersion = apiVersion.Replace($".{apiTmp[3]}", "");
            Version source = Version.Parse(sourceVersion);
            Version api = Version.Parse(apiVersion);

            return api < source;
        }

        private static bool IsApiVersionGreaterThanSourceCurrent(string sourceVersion, string apiVersion)
        {
            var srcTmp = sourceVersion.Split('.');
            if (srcTmp.Length > 3)
                sourceVersion = sourceVersion.Replace($".{srcTmp[3]}", "");
            var apiTmp = apiVersion.Split(".");
            if (apiTmp.Length > 3)
                apiVersion = apiVersion.Replace($".{apiTmp[3]}", "");
            Version source = Version.Parse(sourceVersion);
            Version api = Version.Parse(apiVersion);

            return api > source;
        }
    }
}
