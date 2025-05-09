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
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.Manager.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.Manager
{
    public class QueueProcessor
    {
        private readonly ILogger Logger;
        private readonly DataClient.DataClient DataClient;
        private readonly ManagerConfig Config;
        private readonly List<Issue> WriteQueue = [];
        private readonly List<Comment> CommentQueue = [];
        private QueueRuntimeConfig RunConfig = null;
        private readonly Queue<string> RecentSourceIds = [];
        private readonly Queue<Asset> RecentAssets = [];
        private readonly List<Comment> engagementComments = [];

        public QueueProcessor
        (
            ILogger<QueueProcessor> logger,
            DataClientFactory<Manager> dataClientFactory,
            ManagerConfig config
        )
        {
            Logger = logger;
            DataClient = dataClientFactory.GetClient();
            Config = config;
        }

        private Asset GetRecentAsset(string sourceType, string sourceId) => RecentAssets.FirstOrDefault(ra => ra.Saltminer.Asset.SourceType == sourceType && ra.Saltminer.Asset.SourceId == sourceId);
        private void SetRecentAsset(Asset asset)
        {
            if (RecentAssets.Count >= Config.QueueProcessorMaxRecentAssetCount)
            {
                RecentAssets.Dequeue();
            }

            RecentAssets.Enqueue(asset);
        }

        private bool IsRecentSourceId(string id) => RecentSourceIds.Contains(id);
        private void SetRecentSourceId(string id)
        {
            if (RecentSourceIds.Count >= Config.QueueProcessorMaxRecentSourceIdCount)
            {
                RecentSourceIds.Dequeue();
            }

            RecentSourceIds.Enqueue(id);
        }
        
        /// <summary>
        /// Runs queue processing for queue updates (scans and issues)
        /// "Main loop", locks status / runs processing / locks status completion for each queue scan
        /// Link 1 in processing chain
        /// </summary>
        public void Run(RuntimeConfig config)
        {
            if (config is not QueueRuntimeConfig)
            {
                throw new ArgumentException($"Expected type '{typeof(QueueRuntimeConfig).Name}', but passed value is '{config.GetType().Name}'", nameof(config));
            }

            RunConfig = config.Validate() as QueueRuntimeConfig;

            // Force single instance for now
            if (Config.QueueProcessorInstances > 1)
            {
                Config.QueueProcessorInstances = 1;
            }

            Logger.LogInformation("Looking for queue scan(s) to process, configured for sourceType '{SourceType}', queue scan ID '{QueueScanId}', limit {Limit}, and listOnly {ListOnly}",
                (string.IsNullOrEmpty(RunConfig.SourceType) ? "[all]" : RunConfig.SourceType), 
                (string.IsNullOrEmpty(RunConfig.QueueScanId) ? "[all]" : RunConfig.QueueScanId), 
                RunConfig.Limit, RunConfig.ListOnly);

            try
            {
                var queueScanSearch = new SearchRequest()
                {
                    Filter = new()
                    {
                        FilterMatches = new Dictionary<string, string>() { { "Saltminer.Internal.QueueStatus", QueueScan.QueueScanStatus.Pending.ToString("g") } }
                    },
                    PitPagingInfo = new PitPagingInfo(Config.QueueProcessorQueueBatchSize)
                    {
                        SortFilters = new Dictionary<string, bool>
                        {
                            { "Timestamp", false }
                        }
                    }
                };

                if (!string.IsNullOrEmpty(RunConfig.SourceType))
                {
                    queueScanSearch.Filter.FilterMatches.Add("Saltminer.Scan.SourceType", RunConfig.SourceType);
                }

                if (!string.IsNullOrEmpty(RunConfig.QueueScanId))
                {
                    queueScanSearch.Filter.FilterMatches.Add("Id", RunConfig.QueueScanId);
                }

                queueScanSearch.Filter.FilterMatches.Add("Saltminer.Internal.CurrentQueueScanId", SaltMiner.DataClient.Helpers.BuildMustNotExistsFilterValue());

                var totalCount = 0;
                var errorCount = 0;
                var queueScans = DataClient.QueueScanSearch(queueScanSearch);
                var indexNames = new List<string>();
                var sourcesRemoved = new List<string>();

                Logger.LogInformation("First search found {Count} queue scan(s) to process.", queueScans.Data.Count());
            
                while (queueScans.Data.Any())
                {
                    var processed = false;
                    var total = queueScans.Data.Count();
                    var count = 1;

                    foreach (var queueScan in queueScans.Data)
                    {
                        if (RunConfig.ListOnly)
                        {
                            Logger.LogInformation("{Count}/{Total}, instance '{Instance}', sourceType '{SourceType}', report ID '{ReportId}', queue scan ID '{QueueScanId}'", count, total, queueScan.Saltminer.Scan.Instance, queueScan.Saltminer.Scan.SourceType, queueScan.Saltminer.Scan.ReportId, queueScan.Id);
                            count++;
                            continue;
                        }

                        if (sourcesRemoved.Contains(queueScan.Saltminer.Scan.SourceType))
                        {
                            continue;
                        }

                        try
                        {
                            CheckCancel();

                            Logger.LogInformation("Begin processing {Count}/{Total}, instance '{Instance}', sourceType '{SourceType}', assessmentType '{AType}', report ID '{ReportId}', queue scan ID '{QueueScanId}'", count, total, queueScan.Saltminer.Scan.Instance, queueScan.Saltminer.Scan.SourceType, queueScan.Saltminer.Scan.AssessmentType, queueScan.Saltminer.Scan.ReportId, queueScan.Id);

                            if (!UpdateStatus(queueScan, QueueScan.QueueScanStatus.Processing, EngagementStatus.Processing, false))
                            {
                                // Skip if status update doesn't work
                                continue;
                            }

                            processed = true;

                            ProcessQueueScan(queueScan);

                            // finish comment queue
                            WriteComment();

                            // issues_active alias maintenance - build a list of issue index names seen during processing
                            if (!indexNames.Contains(Issue.GenerateIndex(queueScan.Saltminer.Scan.AssetType, queueScan.Saltminer.Scan.SourceType, queueScan.Saltminer.Scan.Instance)))
                            {
                                indexNames.Add(Issue.GenerateIndex(queueScan.Saltminer.Scan.AssetType, queueScan.Saltminer.Scan.SourceType, queueScan.Saltminer.Scan.Instance));
                            }

                            UpdateStatus(queueScan, QueueScan.QueueScanStatus.Complete, EngagementStatus.Published);
                            count++;
                            // TODO: write queue log entry here
                        }
                        catch (Exception ex)
                        {
                            // TODO: write queue log entry here
                            UpdateStatus(queueScan, QueueScan.QueueScanStatus.Error, EngagementStatus.Error);

                            if (RunConfig.CancelToken.IsCancellationRequested)
                            {
                                processed = false;
                                break;
                            }

                            errorCount++;
                            
                            Logger.LogError(ex, "Queue scan ID '{QScanId}', instance '{Instance}', sourceType '{SourceType}', source scan ID '{ScanId}' failed to process: [{ExType}] {ExMsg}", queueScan.Id, queueScan.Saltminer.Scan.Instance, queueScan.Saltminer.Scan.SourceType, queueScan.Saltminer.Scan.ReportId, ex.GetType().Name, ex.Message);
                            
                            if (errorCount > Config.QueueProcessorMaxErrors)
                            {
                                var msg = $"Exceeded maximum allowed errors ({Config.QueueProcessorMaxErrors}). Removing source {queueScan.Saltminer.Scan.SourceType}.";
                                
                                Logger.LogWarning("{Msg}", msg);
                               
                                sourcesRemoved.Add(queueScan.Saltminer.Scan.SourceType);
                                errorCount = 0;
                            }
                        }

                        totalCount++;

                        if (RunConfig.Limit > 0 && totalCount >= RunConfig.Limit)
                        {
                            break;
                        }
                    }

                    if (!processed)
                    {
                        break; // break out of while if nothing was processed (in case there's some repeating failure to grab one for processing)
                    }

                    if (RunConfig.Limit > 0 && totalCount >= RunConfig.Limit)
                    {
                        Logger.LogInformation("Limit of {Limit} reached, ending processing.", RunConfig.Limit);
                        break;
                    }

                    // add subfilter to exclude sources from the sources removed list
                    if (sourcesRemoved.Count > 0)
                    {
                        queueScanSearch.Filter.SubFilter = new()
                        {
                            FilterMatches = new Dictionary<string, string>() { { "Saltminer.Scan.SourceType", SaltMiner.DataClient.Helpers.BuildExcludeTermsFilterValue(sourcesRemoved) } }
                        };
                    }

                    queueScans = DataClient.QueueScanSearch(queueScanSearch);
                    Logger.LogInformation("Looking for more - search found {Count} queue scan(s) to process.", queueScans.Data.Count());
                }

                Logger.LogInformation("Queue processing complete, {Total} queue scan(s) processed.", totalCount);

                // issues_active alias maintenance - make sure alias exists for issues indices gathered while processing queues
                UpdateActiveIssueAlias(indexNames);
            }
            catch (CancelTokenException)
            {
                // Already logged, so just do nothing but quit silently
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in queue processor");
                throw new QueueProcessorException($"Error in queue processor: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Processes one QueueScan from validation to updating new scan / issues / assets
        /// Design decision: log validation error details as needed, but throw validation exception to handle marking error status by bubbling up
        /// Link 2 in processing chain
        /// </summary>
        private void ProcessQueueScan(QueueScan queueScan)
        {
            
            // TODO: add queue log entry
            
            if (queueScan == null || queueScan.Saltminer == null) 
                throw new ArgumentNullException(nameof(queueScan), "Queue scan or saltminer section null.");

            Logger.LogInformation("Getting Queue Assets...");

            if (!string.IsNullOrEmpty(queueScan.Saltminer.Engagement?.Id))
            {
                // get engagement comments
                var commentRequest = new SearchRequest
                {
                    Filter = new()
                    {
                        FilterMatches = new Dictionary<string, string>
                        {
                            { "saltminer.engagement.id", queueScan.Saltminer.Engagement.Id }
                        }
                    },
                    UIPagingInfo = new UIPagingInfo(100)
                };

                var commentResponse = DataClient.CommentSearch(commentRequest);
                while (commentResponse.Success && commentResponse.Data != null && commentResponse.Data.Any())
                {
                    engagementComments.AddRange(commentResponse.Data.ToList());

                    commentRequest.UIPagingInfo = commentResponse.UIPagingInfo;
                    commentRequest.UIPagingInfo.Page++;
                    commentRequest.AfterKeys = commentResponse.AfterKeys;

                    Logger.LogDebug("{Count} Comments found in this batch of size {Size} and page {Page}", commentRequest?.Filter?.FilterMatches?.Count ?? 0, commentRequest.UIPagingInfo.Size, commentRequest.UIPagingInfo.Page);

                    commentResponse = DataClient.CommentSearch(commentRequest);
                }
            }
            

            var queueAssets = GetQueueAssets(queueScan.Id);
            
            Logger.LogInformation("{Count} Queue Assets Found", queueAssets.Count);

            // Insertion point for retiree processing?
            if (queueAssets.Any(a => a.Saltminer.Asset.IsRetired))
            {
                // process retirees - (1) get asset & set flag, (2) set flag for all issues
                // ProcessRetirees(queueAssets);

                queueAssets.RemoveAll(a => a.Saltminer.Asset.IsRetired);
                if (!queueAssets.Any())
                {
                    Logger.LogInformation("All assets are retired, skipping further queue scan processing");
                    return; // Don't do scan validation, or regular processing
                }
            }

            var validationErrors = CheckForValidationErrors(queueScan);
            
            if (validationErrors.Any())
            {
                foreach (var validationError in validationErrors)
                {
                    Logger.LogWarning("Validation error for queue scan {Id}: {Err}", queueScan.Id, validationError.ErrorMessage);
                }
            
                throw new ManagerValidationException($"{validationErrors.Count} validation error(s) were thrown when attempting to process queue scan ID {queueScan.Id}");
            }

            Logger.LogInformation("Getting Queue History Scans...");
            
            var queueHistoryScans = GetQueueHistoryScans(queueScan.Id);
            
            Logger.LogInformation("{Count} Queue History Scans Found", queueHistoryScans.Count());

            // Get counts by source type and assessment type for Licensing validation
            var countResult = DataClient.GetValidationCounts(queueScan.Saltminer.Scan.AssetType, queueScan.Saltminer.Scan.SourceType, queueScan.Saltminer.Scan.Instance, queueScan.Saltminer.Scan.AssessmentType);
            var currentSourceTypeCount = countResult.Data[queueScan.Saltminer.Scan.SourceType];
            var currentAssessmentTypeCount = countResult.Data[queueScan.Saltminer.Scan.AssessmentType];

            Tuple<Scan, Asset> result = null;
            List<Comment> comments;

            foreach (var queueAsset in queueAssets) 
            {
                var isNoScan = IsNoScan(queueScan.Saltminer.Scan.ReportId);

                result = ProcessScan(queueScan, queueAsset, isNoScan);
                if (result == null)  // null means we don't process history or issues (retiring asset, etc.)
                {
                    continue;
                }

                // Process scan history
                foreach (var histQueueScan in queueHistoryScans)
                {
                    DataClient.ScanAddUpdate(Translate(result.Item1, result.Item2, histQueueScan));
                }

                UpdateQueueScanHistoryStatus(queueScan.Id, QueueScan.QueueScanStatus.Complete);

                // no scan, just create one placeholder issue with 'NoScan' severity
                if (isNoScan)
                {
                    // remove any previous issue placeholder(s) for 'noscan'
                    var issueSearchRequest = new SearchRequest
                    {
                        Filter = new()
                        {
                            FilterMatches = new()
                            {
                                { "Vulnerability.Severity", Severity.NoScan.ToString("g") },
                                { "Saltminer.Asset.SourceId", result.Item2.Saltminer.Asset.SourceId },
                                { "Saltminer.Scan.AssessmentType", result.Item1.Saltminer.Scan.AssessmentType },
                                { "Saltminer.Asset.SourceType", result.Item2.Saltminer.Asset.SourceType },
                                { "Saltminer.Asset.Instance", result.Item2.Saltminer.Asset.Instance }
                            }
                        },
                        PitPagingInfo = new PitPagingInfo(1)
                    };
                    foreach (var pi in DataClient.IssueSearch(issueSearchRequest).Data)
                    {
                        // This should usually only be 0 or 1, but if dups make it in we will remove them
                        DataClient.IssueDelete(pi.Id, pi.Saltminer.Asset.AssetType, pi.Saltminer.Asset.SourceType, pi.Saltminer.Asset.Instance);
                    }
                    
                    Logger.LogInformation("The Queue Scan is a 'no scan' type. Only one issue with 'no scan' severity will be created.");
                    var queueIssue = new QueueIssue
                    {
                        Labels = new Dictionary<string, string>(),
                        Message = "",
                        Tags = null,
                        Saltminer = new SaltMinerQueueIssueInfo()
                        {   
                            Attributes = new Dictionary<string, string>(),
                            Source = new SourceInfo(),
                        },
                        Vulnerability = new VulnerabilityInfo()
                        {
                            Name = Severity.NoScan.ToString(),
                            Location = "NoScan-Location",
                            LocationFull = "NoScan-LocationFull",
                            ReportId = queueScan.Saltminer.Scan.ReportId,
                            FoundDate = DateTime.Now,
                            RemovedDate = null,
                            Severity = Severity.NoScan.ToString(),
                            Scanner = new ScannerInfo()
                            {
                                Id = queueScan.Saltminer.Scan.ReportId,
                                Vendor = queueScan.Saltminer.Scan.Vendor,
                                Product = queueScan.Saltminer.Scan.Product,
                                AssessmentType = queueScan.Saltminer.Scan.AssessmentType
                            }
                        }
                    };

                    var issue = Translate(result.Item1, queueIssue);
                    WriteQueue.Add(issue);
                    WriteIssue();

                    DataClient.AssetAddUpdate(result.Item2);

                    Logger.LogInformation("Imported 1 issue for scan '{ReportId}'", result.Item1.Saltminer.Scan.ReportId);
                }
                else
                {
                    // Processes issues that can be matched with existing
                    ProcessQueueIssues(result.Item1, queueScan, result.Item2, queueAsset);
                }

                // copy comments for asset level
                comments = engagementComments.Where(x => x.Saltminer?.Asset?.Id == queueAsset.Id && x.Saltminer?.Issue?.Id == null).ToList();
                foreach (var comment in comments)
                {
                    WriteComment(CreateComment(comment, result.Item1.Saltminer.Engagement.Id, result.Item1.Id, result.Item2.Id, null));
                }

                currentSourceTypeCount++;
                currentAssessmentTypeCount++;
            }

            // copy comments scan/egnagement level
            comments = engagementComments.Where(x => x.Saltminer?.Asset?.Id == null && x.Saltminer?.Issue?.Id == null).ToList();
            foreach (var comment in comments)
            {
                WriteComment(CreateComment(comment, queueScan.Saltminer.Engagement.Id, result.Item1.Id, null, null));
            }
        }

        private void UpdateQueueScanHistoryStatus(string id, QueueScan.QueueScanStatus status)
        {
            var request = new UpdateQueryRequest<QueueScan>()
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string>() {
                            { "Saltminer.Internal.QueueStatus", QueueScan.QueueScanStatus.Pending.ToString("g") },
                            { "Saltminer.Internal.CurrentQueueScanId", id }
                        }
                },
                ScriptUpdates = new Dictionary<string, object>()
                {
                    { "Saltminer.Internal.QueueStatus", status.ToString("g") }
                }
            };

            DataClient.QueueScanUpdateByQuery(request);
        }

        /// <summary>
        /// Write data updates for Scan and Issues for the given queue entities
        /// Link 3 in processing chain
        /// </summary>
        private Tuple<Scan, Asset> ProcessScan(QueueScan queueScan, QueueAsset queueAsset, bool isNoScan = false)
        {
            Logger.LogInformation("Processing instance '{Instance}', sourceType '{type}', and queue scan '{Id}'", queueScan.Saltminer.Scan.Instance, queueScan.Saltminer.Scan.SourceType, queueScan.Saltminer.Scan.ReportId);

            if (queueAsset.Saltminer.Asset.IsRetired)
            {
                RetireAsset(queueAsset);
                return null;
            }

            // Process NoScan entries
            Scan scan = null;

            // processing no scan is disabled and a 'no scan' scan is present, just return and don't do anything
            if (!Config.ProcessNoScan && isNoScan)
            {
                return null;
            }

            if (!isNoScan)
            {
                var existingScan = GetScan(queueAsset.Saltminer.Asset.SourceType, queueAsset.Saltminer.Asset.SourceId, queueScan.Saltminer.Scan.ReportId, queueAsset.Saltminer.Asset.AssetType);
                scan = Translate(queueScan, queueAsset, existingScan);
                scan.Saltminer.InventoryAsset.Key = queueAsset.Saltminer.InventoryAsset.Key;
                
                // Write scan first time so asset can access ID
                if (existingScan == null)
                {
                    scan = DataClient.ScanAddUpdate(scan).Data;
                }
            }
            else
            {
                scan = Translate(queueScan, queueAsset);
            }

            var asset = ProcessAsset(queueAsset);

            scan.Saltminer.Asset.Id = asset.Id;
            // TODO: remove this or maybe the one in Translate above that is almost just like it.  Possibly restructure Translate methods, seems like we lost one or more and they are no longer quite right for the use case.
            // TODO: also, seems like "AssetIdInfo" is a bad name at the least - review the partial classes and make sure we are still doing this efficiently after all the changes.
            scan.Saltminer.Asset = new AssetIdInfo
            {
                AssetType = asset.Saltminer.Asset.AssetType,
                Attributes = asset.Saltminer.Asset.Attributes,
                Description = asset.Saltminer.Asset.Description,
                Host = asset.Saltminer.Asset.Host,
                Id = asset.Id,
                Instance = asset.Saltminer.Asset.Instance,
                Ip = asset.Saltminer.Asset.Ip,
                IsProduction = asset.Saltminer.Asset.IsProduction,
                IsRetired = asset.Saltminer.Asset.IsRetired,
                IsSaltminerSource = asset.Saltminer.Asset.IsSaltminerSource,
                Name = asset.Saltminer.Asset.Name,
                Port = asset.Saltminer.Asset.Port,
                ScanCount = asset.Saltminer.Asset.ScanCount,
                Scheme = asset.Saltminer.Asset.Scheme,
                SourceId = asset.Saltminer.Asset.SourceId,
                SourceType = asset.Saltminer.Asset.SourceType,
                Version = asset.Saltminer.Asset.Version,
                VersionId = asset.Saltminer.Asset.VersionId,
            };

            // don't save the scan object, only created to get info for a 'noscan'
            if (!isNoScan)
            {
                scan = DataClient.ScanAddUpdate(scan).Data;
            }

            return new(scan, asset);
        }

        /// <summary>
        /// Write data updates for Asset
        /// Link 3 in processing chain
        /// </summary>
        private Asset ProcessAsset(QueueAsset queueAsset)
        {
            Logger.LogInformation("Processing asset - source type '{SourceType}' and source ID '{SourceId}'", queueAsset.Saltminer.Asset.SourceType, queueAsset.Saltminer.Asset.SourceId);
            var srcId = queueAsset.Saltminer.Asset.SourceType + "|" + queueAsset.Saltminer.Asset.SourceId;
            var asset = GetRecentAsset(queueAsset.Saltminer.Asset.SourceType, queueAsset.Saltminer.Asset.SourceId);
            var skipExisting = queueAsset.Saltminer.Asset.AssetType == AssetType.Pen.ToString("g");

            if (asset == null)
            {
                var assetSearchRequest = new SearchRequest
                {
                    Filter = new()
                    {
                        FilterMatches = new()
                        {
                            { "Saltminer.Asset.SourceId", queueAsset.Saltminer.Asset.SourceId },
                            { "Saltminer.Asset.SourceType", queueAsset.Saltminer.Asset.SourceType }
                        }
                    },
                    PitPagingInfo = new PitPagingInfo(1)
                };

                if (!skipExisting)
                    asset = DataClient.AssetSearch(assetSearchRequest).Data.FirstOrDefault();
                
                if (asset == null && IsRecentSourceId(srcId))
                {
                    Logger.LogInformation("Recent source type/id '{src}' not found in search, waiting a few sec to retry...", srcId);
                    Task.Delay(3000).Wait();
                    if (!skipExisting)
                        asset = DataClient.AssetSearch(assetSearchRequest).Data.FirstOrDefault();
                    
                    if (asset == null)
                    {
                        Logger.LogError("Failed to find recent source type/id '{src}' when searching.  This may result in a duplicate asset.", srcId);
                    }
                }
            }
            
            SetRecentSourceId(srcId);
            var tAsset = Translate(queueAsset, asset);
            SetRecentAsset(tAsset);
            return tAsset;
        }

        private bool IsNoScan(string reportId)
        {
            return reportId.ToLower().StartsWith("noscan");
        }

        private static SaltminerEqualityResponse QueueIssueNeedsUpdate(QueueIssue qIssue, Issue issue, Scan scan, Asset asset)
        {
            var assetInfo = asset.Saltminer.Asset;
            var scanInfo = scan.Saltminer.Scan;
            SaltminerEqualityResponse equalsCheck;
            try
            {
                equalsCheck = qIssue.Equals(issue, assetInfo.Attributes);
                if (!equalsCheck.IsEqual)
                    return equalsCheck;
            }
            catch (ArgumentNullException)
            {
                return new SaltminerEqualityResponse(new() { "Error comparing queue issue to issue, assuming inequal." });
            }

            // TODO: move this into Equals() method on queueIssue.  Do we need scan info compare?
            if (assetInfo.Name != issue.Saltminer.Asset.Name || assetInfo.Version != issue.Saltminer.Asset.Version)
                return new SaltminerEqualityResponse(new() { "Asset info changed." });
            if (scanInfo.ScanDate != issue.Saltminer.Scan.ScanDate)
                return new SaltminerEqualityResponse(new() { "Scan info changed." });

            return new(new());
        }

        private void ProcessQueueIssues(Scan scan, QueueScan queueScan, Asset asset, QueueAsset queueAsset)
        {
            scan = scan ?? throw new ArgumentNullException(nameof(scan));
            queueScan = queueScan ?? throw new ArgumentNullException(nameof(queueScan));
            asset = asset ?? throw new ArgumentNullException(nameof(asset));
            queueAsset = queueAsset ?? throw new ArgumentNullException(nameof(queueAsset));
            var getExistingIssues = true;

            // This was a neat idea to allow us to skip some issue processing, but bad placement because they are always equal, having been set higher up.
            // Let's consider when we can next rework this process.
            //var sameScan = scan.IsSameScanInfo(queueScan)
            //var sameAsset = asset.IsSameAssetInfo(queueAsset)

            var counter = new Counter(); 

            Logger.LogInformation("Getting Queue Issues...");

            // delete existing issues so that all incoming queue issues will be loaded in place of
            if (queueScan.Saltminer.Internal.ReplaceIssues)
            {
                var qAsset = queueAsset.Saltminer.Asset;
                var qScan = queueScan.Saltminer.Scan;
                DataClient.IssuesDeleteBySourceId(qAsset.SourceId, qScan.AssetType, qAsset.SourceType, qAsset.Instance, qScan.AssessmentType);
                getExistingIssues = false;
            }
            if (queueScan.Saltminer.Scan.AssessmentType == AssessmentType.Pen.ToString("g"))
            {
                getExistingIssues = false;
            }

            // Gets a batch of queue issues for the current queue scan ID
            var queueScanIssuesResponse = GetQueueScanIssues(queueScan.Id);
            var queueScanIssues = queueScanIssuesResponse.Data;
            var lastQueueScanIssue = queueScanIssues.LastOrDefault();
            var queueIssueCount = DataClient.QueueIssueCountByScan(queueScan.Id).Affected;

            // "Temp" handling of multiple issues -> zero record use case
            // Regular code could be tested to handle this scenario without this shortcut
            if (queueIssueCount == 1 && queueScanIssues.First().Vulnerability.Severity == Severity.Zero.ToString("g") && getExistingIssues)
            {
                Logger.LogInformation("Zero record found, marking any existing issues as removed.");
                var zeroIssue = queueScanIssues.First();
                var zWritten = false;
                
                // Design decision: assume that getting to zero issues only happens from a relatively small number (get all issues to remove)
                foreach (var issue in GetExistingIssues(queueScan.Saltminer.Scan.AssetType, queueScan.Saltminer.Scan.SourceType, queueAsset.Saltminer.Asset.SourceId, queueScan.Saltminer.Scan.AssessmentType))
                {
                    // Only drop/update issues related to this zero by assessment type unless flag set to consider all issues at once
                    if (issue.Vulnerability.Severity == Severity.Zero.ToString("g"))
                    {
                        UpdateIssue(scan, zeroIssue, issue);
                        zWritten = true;
                    }
                    else
                    {
                        RemoveIssue(issue, scan.Saltminer.Scan.ScanDate);
                    }
                }
                
                if (!zWritten)
                {
                    UpdateIssue(scan, queueScanIssues.First(), null);
                }
                
                WriteIssue();
                
                queueScanIssues = new List<QueueIssue>(); // skip the upcoming while block
            }

            var firstBatch = true;
            var myType = queueScanIssues.FirstOrDefault()?.Vulnerability.Scanner.AssessmentType;
            while (queueScanIssues.Any())
            {
                Logger.LogDebug("{Count} Queue Issues found in this batch", queueScanIssues.Count());

                var validationErrors = CheckForValidationErrors(queueScan, queueIssueCount, queueScanIssues);

                if (validationErrors.Any())
                {
                    foreach (var ve in validationErrors.Take(5))
                    {
                        Logger.LogWarning("Validation error for an issue in queue scan {ScanId}: {Msg}", queueScan.Id, ve.ErrorMessage);
                    }

                    throw new ManagerValidationException($"{validationErrors.Count} validation error(s) were thrown for the issues in queue scan ID '{queueScan.Id}'.  Up to 5 were logged.");
                }

                // TODO: additional business validation, like does queuescan.IssueCount match actual queueissues count (that one is already in place in CheckForValidationErrors, but serves as an example)
                foreach (var queueIssue in queueScanIssues)
                {
                    queueIssue.IsProcessed = false;
                }

                IEnumerable<Issue> existingScanIssues = null;
                // Design decision (hotfix-3.0.0.6394): don't add assessment type to existing issues query (more complex), but check assessment type for update / delete if flag set on each issue (possibly poorer performance)
                // Check to see if it is the first batch of issues. If it is not, then it is at the end of all the batches and you get >= the first scanner id
                if (getExistingIssues)
                {
                    if (queueScanIssues.Count() < Config.QueueProcessorIssueBatchSize && !firstBatch)
                    {
                        existingScanIssues = GetExistingIssues(queueScan.Saltminer.Scan.AssetType, queueScan.Saltminer.Scan.SourceType, queueAsset.Saltminer.Asset.SourceId, queueScan.Saltminer.Scan.AssessmentType, queueScanIssues.First().Vulnerability.Scanner.Id, null);
                    }
                    else
                    {
                        // if it is first batch and issues less than batch size, it is the only batch to be processed
                        // with it being only batch, do not filter when getting existing issues. Get everything (which may include previous zero issues that need to be removed)
                        bool onlyBatch = (queueScanIssues.Count() < Config.QueueProcessorIssueBatchSize && firstBatch);
                        existingScanIssues = GetExistingIssues(queueScan.Saltminer.Scan.AssetType, queueScan.Saltminer.Scan.SourceType, queueAsset.Saltminer.Asset.SourceId, queueScan.Saltminer.Scan.AssessmentType, queueScanIssues.First().Vulnerability.Scanner.Id, lastQueueScanIssue.Vulnerability.Scanner.Id, firstBatch, onlyBatch);
                    }
                }
                
                existingScanIssues ??= new List<Issue>();

                if (existingScanIssues.Any())
                {
                    // Assume that source with multiple assessment types will still have unique scanner IDs
                    var dupGrps = existingScanIssues.GroupBy(i => i.Vulnerability.Scanner.Id).Where(g => g.Count() > 1).Select(g => g.Key);

                    // Delete duplicates if found
                    if (dupGrps.Any())
                    {
                        foreach (var grp in dupGrps)
                        {
                            Logger.LogWarning("Duplicate existing issues found for source ID '{srcId}' and scanner ID '{scannerId}'.  These will be removed and one reloaded.", scan.Saltminer.Asset.SourceId, grp);
                        }

                        foreach (var issue in existingScanIssues.Where(i => dupGrps.Contains(i.Vulnerability.Scanner.Id)))
                        {
                            DeleteIssue(issue);
                        }

                        DataClient.RefreshIndex(Issue.GenerateIndex(queueScan.Saltminer.Scan.AssetType, queueScan.Saltminer.Scan.SourceType, queueScan.Saltminer.Scan.Instance));

                        if (queueScanIssues.Count() < Config.QueueProcessorIssueBatchSize && !firstBatch)
                        {
                            existingScanIssues = GetExistingIssues(queueScan.Saltminer.Scan.AssetType, queueScan.Saltminer.Scan.SourceType, queueAsset.Saltminer.Asset.SourceId, queueScan.Saltminer.Scan.AssessmentType, queueScanIssues.First().Vulnerability.Scanner.Id, null);
                        }
                        else
                        {
                            existingScanIssues = GetExistingIssues(queueScan.Saltminer.Scan.AssetType, queueScan.Saltminer.Scan.SourceType, queueAsset.Saltminer.Asset.SourceId, queueScan.Saltminer.Scan.AssessmentType, queueScanIssues.First().Vulnerability.Scanner.Id, lastQueueScanIssue.Vulnerability.Scanner.Id, firstBatch);
                        }
                    }
                }


                foreach (var issue in existingScanIssues)
                {
                    // Delete zero record if found among other issues
                    if (issue.Vulnerability.Severity == Severity.Zero.ToString("g"))
                    {
                        DeleteIssue(issue);
                        continue;
                    }

                    CheckCancel(false);

                    // Assumption: Vulnerability.Scanner.Id always exists and is unique; if not, then duplication will occur
                    var matchedQueueScanIssues = queueScanIssues.Where(qsi => qsi.Vulnerability.Scanner.Id == issue.Vulnerability.Scanner.Id).ToList();
                    if (matchedQueueScanIssues.Count > 1)
                    {
                        throw new ValidationException($"Duplicate queue issue(s) found for source ID '{scan.Saltminer.Asset.SourceId}' and scanner ID '{issue.Vulnerability.Scanner.Id}'.");
                    }
                                
                    var queueScanIssue = matchedQueueScanIssues.SingleOrDefault();

                    if (queueScanIssue == null)
                    {
                        // Issue was removed (if matches queue assessment type or if flag not set)
                        var isMyType = myType == issue.Vulnerability.Scanner.AssessmentType || !Config.QueueProcessorOneScanOneAssessmentType;
                        if (isMyType)
                        {
                            RemoveIssue(issue, queueScan.Saltminer.Scan.ScanDate);  // marks old issue as removed
                        }

                        continue;
                    }

                    counter.Count(queueScanIssue);

                    var equalsCheck = QueueIssueNeedsUpdate(queueScanIssue, issue, scan, asset);
                    if (equalsCheck.IsEqual)
                    {
                        // Unchanged issue, don't need to process further
                        queueScanIssue.IsProcessed = true;
                    }
                    else
                    {
                        // Needs update
                        Logger.LogDebug(string.Join(",", equalsCheck.Messages.ToArray()));
                        queueScanIssue.Id = issue.Id;
                        UpdateIssue(scan, queueScanIssue, issue);
                        queueScanIssue.IsProcessed = true;
                    }
                }
                
                var newIssues = queueScanIssues.Where(qi => !qi.IsProcessed && qi.Saltminer.QueueAssetId == queueAsset.Id);

                foreach (var qIssue in newIssues)
                {
                    AddIssue(scan, qIssue);
                    counter.Count(qIssue);
                }

                WriteIssue();

                queueScanIssuesResponse = GetQueueScanIssues(queueScan.Id, queueScanIssuesResponse.PitPagingInfo, queueScanIssuesResponse.AfterKeys);
                firstBatch = false;
                queueScanIssues = queueScanIssuesResponse.Data;
                lastQueueScanIssue = queueScanIssues.LastOrDefault();
                queueScanIssues = queueScanIssues.Take(Config.QueueProcessorIssueBatchSize);
            }

            counter.SetCounts(scan.Saltminer);

            DataClient.ScanAddUpdate(scan);
            DataClient.AssetAddUpdate(asset);

            Logger.LogInformation("Imported {Total} issue(s) for scan '{ReportId}'", counter.Total, scan.Saltminer.Scan.ReportId);
        }

        private void RetireAsset(QueueAsset queueAsset)
        {
            Logger.LogInformation("Retiring asset - source type '{SourceType}' and source ID '{SourceId}'", queueAsset.Saltminer.Asset.SourceType, queueAsset.Saltminer.Asset.SourceId);
            
            var asset = DataClient.AssetSearch(new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = new()
                    {
                        { "Saltminer.Asset.SourceType", queueAsset.Saltminer.Asset.SourceType },
                        { "Saltminer.Asset.SourceId", queueAsset.Saltminer.Asset.SourceId },
                        { "Saltminer.Asset.AssetType", queueAsset.Saltminer.Asset.AssetType }
                    }
                },
                AssetType = queueAsset.Saltminer.Asset.AssetType, 
                SourceType = queueAsset.Saltminer.Asset.SourceType,
                PitPagingInfo = new PitPagingInfo(1)
            }).Data.FirstOrDefault();

            if (asset != null)
            {
                asset.Saltminer.Asset.IsRetired = true;

                // TODO: resolve inventory key
                DataClient.AssetAddUpdate(asset);

                // Retire associated asset issues
                RetireIssues(asset, queueAsset);
            }
        }

        private void RetireIssues(Asset asset, QueueAsset queueAsset)
        {
            Logger.LogInformation("Retiring asset issues - source type '{SourceType}' and Asset ID '{AssetId}'", queueAsset.Saltminer.Asset.SourceType, asset.Id);

            var request = new UpdateQueryRequest<Issue>
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "saltminer.asset.id", asset.Id }
                    }
                },
                ScriptUpdates = new Dictionary<string, object>
                {
                    { "Saltminer.Asset.IsRetired", true }
                }
            };

            DataClient.IssueUpdateByQuery(request);
        }

        /// <summary>
        /// Gets a Scan by searching for Source / SourceId / ReportId
        /// </summary>
        private Scan GetScan(string sourceType, string sourceId, string reportId, string assetType)
        {
            var request = new SearchRequest()
            {
                Filter = new() {
                    FilterMatches = new Dictionary<string, string>()
                    {
                        { "Saltminer.Asset.SourceId", sourceId },
                        { "Saltminer.Scan.ReportId", reportId }
                    }
                },
                AssetType = assetType,
                SourceType = sourceType,
                PitPagingInfo = new PitPagingInfo { Size = 2 } // only want 1, but if more than 1 then error later
            };

            var result = DataClient.ScanSearch(request);

            try
            {
                return result.Data.SingleOrDefault();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.StartsWith("Sequence contains more than one element"))
                {
                    Logger.LogCritical("Data problem detected in scans_* (duplication), please review.");
                    throw new ManagerException($"Found more than one scan with source type '{sourceType}', sourceId '{sourceId}', and reportId '{reportId}'.  This indicates a data problem and should be reviewed.");
                }
                throw;
            }
        }

        /// <summary>
        /// Gets all queue issues for a queue scan ID
        /// </summary>
        private DataResponse<QueueIssue> GetQueueScanIssues(string queueScanId, PitPagingInfo paging = null, IList<object> afterKeys = null)
        {
            var queueIssueRequest = new SearchRequest() {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string>() { { "Saltminer.QueueScanId", queueScanId } }
                },
                PitPagingInfo = paging ?? new PitPagingInfo(Config.QueueProcessorIssueBatchSize, true)
                {
                    SortFilters = new Dictionary<string, bool>
                    {
                        { "Vulnerability.Scanner.Id", true },
                    }
                },
                AfterKeys = afterKeys
            };

            return DataClient.QueueIssueSearch(queueIssueRequest);
        }

        /// <summary>
        /// Gets all queue assets for a queue scan ID
        /// </summary>
        private List<QueueAsset> GetQueueAssets(string scanId)
        {
            var queueAssets= new List<QueueAsset>(); 
            var searchRequest = new SearchRequest() { 
                Filter = new() { 
                    FilterMatches = new Dictionary<string, string> { { "Saltminer.Internal.QueueScanId", scanId } }
                },
                PitPagingInfo = new PitPagingInfo(null, true)
            };
            var results = DataClient.QueueAssetSearch(searchRequest);

            while (results.Data.Any())
            {
                CheckCancel(false);
                if (queueAssets.Count > 0)
                {
                    Logger.LogInformation("Retrieved {Count} more Queue Assets in Next Batch", results.Data.Count());
                }
                else
                {
                    Logger.LogInformation("Retrieved {Count} Queue Assets in Initial Batch", results.Data.Count());
                }
                queueAssets.AddRange(results.Data);
                results = DataClient.QueueAssetSearch(searchRequest.NextRequest(results));
            }

            return queueAssets;
        }

        private IEnumerable<QueueScan> GetQueueHistoryScans(string queueScanId)
        {
            var result = new List<QueueScan>();
            var request = new SearchRequest()
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string>() { 
                        { "Saltminer.Internal.QueueStatus", QueueScan.QueueScanStatus.Pending.ToString("g") },
                        { "Saltminer.Internal.CurrentQueueScanId", queueScanId }
                    }
                },
                UIPagingInfo = new UIPagingInfo(200, 1)
            };
            var response = DataClient.QueueScanSearch(request);
            while (response.Success && response.Data != null && response.Data.Any())
            {
                var c = result.Count;
                result.AddRange(response.Data);
                if (result.Count <= c)
                {
                    Logger.LogWarning("Breaking GetQueueHistoryScans loop, no new results by count.");
                    break;
                }
                response = DataClient.QueueScanSearch(request.NextRequest(response));
            }
            return result;
        }

        private IEnumerable<Issue> GetExistingIssues(string assetType, string sourceType, string sourceId, string assessType, string firstScannerId = null, string lastScannerId = null, bool firstBatch = false, bool onlyBatch = false)
        {
            var nulls = new bool[] { string.IsNullOrEmpty(firstScannerId), string.IsNullOrEmpty(lastScannerId) };
            
            if (nulls[0] && !nulls[1])
            {
                throw new ArgumentNullException(nameof(firstScannerId), $"Parameter '{nameof(firstScannerId)}' cannot be null if parameter '{nameof(lastScannerId)}' is not null.");
            }

            var request = new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string> {
                        { "Saltminer.Asset.SourceId", sourceId  },
                        { "Vulnerability.Scanner.AssessmentType", assessType  }
                    }
                },
                AssetType = assetType,
                SourceType = sourceType,
                PitPagingInfo = new PitPagingInfo(Config.IssueProcessingBatchSize + (Config.IssueProcessingBatchSize / 2), false)
                {
                    SortFilters = new Dictionary<string, bool>
                    {
                        { "Vulnerability.Scanner.Id", true }
                    }
                }
            };

            // if both scanner ids are not null, use in a range. batching the issue data for efficiency
            if (!nulls[0] && !nulls[1])
            {
                // this indicates the first pass of the batched data. Need to grab everything before the filtered range as well.
                //if first batch and not only batch filter, otherwise, do not create a scanner Id filter
                if (firstBatch)
                {
                    if (!onlyBatch)
                    {
                        request.Filter.FilterMatches.Add("Vulnerability.Scanner.Id", $"{SaltMiner.DataClient.Helpers.BuildLessThanOrEqualFilterValue(lastScannerId)}");
                    }
                }
                else
                {
                    request.Filter.FilterMatches.Add("Vulnerability.Scanner.Id", $"{SaltMiner.DataClient.Helpers.BuildGreaterThanOrEqualFilterValue(firstScannerId)}{SaltMiner.DataClient.Helpers.BuildLessThanOrEqualFilterValue(lastScannerId)}");
                }
            }

            // first scanner id is not null, but second scanner id IS null, only get >= first scanner id
            // Use: If issue count is less than the issue batching number, get all instead of filtering a range.
            if (!nulls[0] && nulls[1])
            {
                request.Filter.FilterMatches.Add("Vulnerability.Scanner.Id", $"{SaltMiner.DataClient.Helpers.BuildGreaterThanOrEqualFilterValue(firstScannerId)}");
            }

            var response = DataClient.IssueSearch(request);

            // Use case: we intend to return a range of issues based on the first and last scanner id as passed.  Size should be a lot larger than the range passed.  If this
            // exception is thrown, something is wrong with the data and it should be re-loaded, possibly from scratch.
            if (response.PitPagingInfo.Total > response.PitPagingInfo.Size)
            {
                if (Config.QueueProcessorDisableExistingIssuesCountChecking)
                {
                    Logger.LogWarning("Overly large number of existing issues returned that match the comparison set (issue IDs from {firstId} to {lastId}). Expected max returned {size}, Total actually returned {Total}.  Unmatched issues will be removed.", firstScannerId, lastScannerId, response.PitPagingInfo.Size, response.PitPagingInfo.Total);
                }
                else
                {
                    throw new ManagerValidationException($"Overly large number of existing issues returned that match the comparison set (issue IDs from {firstScannerId} to {lastScannerId}). Expected max returned {response.PitPagingInfo.Size}, Total actually returned {response.PitPagingInfo.Total}.  This may indicate the data for the source needs to be reloaded.");
                }
            }

            return response.Data;
        }

        private void UpdateActiveIssueAlias(List<string> issueIndices)
        {
            var curIndexName = "?";
            try
            {
                foreach (var index in issueIndices)
                {
                    curIndexName = index;
                    var indexSplit = index.Split("_");
                    var sourceType = indexSplit[2];
                    var assetType = indexSplit[1];
                    var instance = indexSplit[3];
                    var indexName = Config.IssuesActiveIndexTemplate.Replace("[assetType]", assetType).Replace("[sourceType]", sourceType).Replace("[instance]", instance);
                    DataClient.ActiveIssueAlias(indexName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failure checking alias for index '{idx}'.  No impact to queue processing.", curIndexName);
            }
        }

        private void AddIssue(Scan scan, QueueIssue qi)
        {
            var issue = Translate(scan, qi);
            if (!string.IsNullOrEmpty(issue.Saltminer?.Engagement?.Id))
            {
                issue.Saltminer.SmUrl = BuildSaltMinerIssueUrl(issue.Saltminer.Engagement.Id, issue.Id);
            }
            
            // TODO: add in custom handler if present
            WriteIssue(issue);

            // copy comments for issue level
            var comments = engagementComments.Where(x => x.Saltminer?.Issue?.Id == qi.Id).ToList();
            foreach (var comment in comments)
            {
                WriteComment(CreateComment(comment, issue.Saltminer.Engagement?.Id, issue.Saltminer.Scan.Id, issue.Saltminer.Asset?.Id, issue?.Id));
            }
        }

        private void RemoveIssue(Issue issue, DateTime scanDate)
        {
            issue.Vulnerability.RemovedDate = scanDate;
            
            // TODO: add in custom handler if present
            WriteIssue(issue);
        }

        private void UpdateIssue(Scan scan, QueueIssue queueIssue, Issue existingIssue)
        {
            // Translate does field to field value mapping
            var issue = Translate(scan, queueIssue, existingIssue);
            if (!string.IsNullOrEmpty(queueIssue.Saltminer?.Engagement?.Id))
            {
                if (scan.Saltminer.Scan.AssessmentType == AssessmentType.Pen.ToString("g"))
                {
                    Logger.LogWarning("Unexpected issue update from PenTest queue.");
                }
                
                issue.Saltminer.SmUrl = BuildSaltMinerIssueUrl(queueIssue.Saltminer.Engagement.Id, issue.Id);
            }
                

            // TODO: complete additional update logic if any
            // TODO: add in custom handler if present
            WriteIssue(issue);
        }

        private static Comment CreateComment(Comment comment, string engagementId, string scanId, string assetId = null, string issueId = null)
        {
            return new()
            {
                Saltminer = new()
                {
                    Comment = new()
                    {
                        Message = comment.Saltminer.Comment.Message.ToString(),
                        Type = comment.Saltminer.Comment.Type.ToString(),
                        User = comment.Saltminer.Comment.User.ToString(),
                        UserFullName = comment.Saltminer.Comment.UserFullName.ToString(),
                        Added = comment.Saltminer.Comment.Added
                    },
                    Scan = new()
                    {
                        Id = scanId
                    },
                    Engagement = new()
                    {
                        Id = engagementId
                    },
                    Asset = new()
                    {
                        Id = assetId
                    },
                    Issue = new()
                    {
                        Id = issueId
                    }
                }
            };
        }


        private string BuildSaltMinerIssueUrl(string engagementId, string issueId)
        {
            return $"{Config.WebUiBaseUrl}/engagements/{engagementId}/issues/{issueId}";
        }

        private void WriteIssue(Issue issue = null)
        {
            if (WriteQueue.Count >= Config.QueueProcessorIssueBatchSize || (issue == null && WriteQueue.Count > 0))
            {
                var st = DateTime.Now;
                DataClient.IssuesAddUpdateBulk(WriteQueue);
                Logger.LogInformation("Sent {Count} issue(s) to datastore in {time} ms", WriteQueue.Count, (DateTime.Now - st).TotalMilliseconds);
                WriteQueue.Clear();
            }

            if (issue != null)
            {
                WriteQueue.Add(issue);
            }
        }

        private void WriteComment(Comment comment = null)
        {
            if (CommentQueue.Count >= Config.QueueProcessorIssueBatchSize || (comment == null && CommentQueue.Count > 0))
            {
                var st = DateTime.Now;
                DataClient.CommentAddUpdateBulk(CommentQueue);
                Logger.LogInformation("Sent {Count} comments(s) to datastore in {time} ms", CommentQueue.Count, (DateTime.Now - st).TotalMilliseconds);
                CommentQueue.Clear();
            }

            if (comment != null)
            {
                CommentQueue.Add(comment);
            }
        }

        private void DeleteIssue(Issue issue)
        {
            var r = DataClient.IssueDelete(issue.Id, issue.Saltminer.Asset.AssetType, issue.Saltminer.Asset.SourceType, issue.Saltminer.Asset.Instance);
            if (!r.Success)
            {
                // Error is non-fatal, but log it
                Logger.LogError("Failed to delete issue with id '{Id}' and source type '{type}'", issue.Id, issue.Saltminer.Asset.SourceType);
            }
        }

        private bool UpdateStatus(QueueScan queueScan, QueueScan.QueueScanStatus scanStatus, EngagementStatus engagementStatus, bool logErrOnFail = true)
        {
            try
            {
                DataClient.QueueScanUpdateStatus(queueScan.Id, scanStatus);
                if (queueScan.Saltminer.Engagement?.Id != null)
                {
                    DataClient.EngagementUpdateStatus(queueScan.Saltminer.Engagement.Id, engagementStatus);
                }

                return true;
            }
            catch (DataClientException ex)
            {
                if (logErrOnFail)
                {
                    Logger.LogError(ex, "DataClientException: {Msg}", ex.Message);
                }
                else
                {
                    Logger.LogDebug(ex, "DataClientException: {Msg}", ex.Message);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected failure updating queue scan/engagement status ({message})", ex.Message);
            }

            return false;
        }
        
        private Asset Translate(QueueAsset queueAsset, Asset existingAsset = null)
        {
            var asset = existingAsset ?? new Asset();

            if (existingAsset == null)
            {
                asset.Id = Guid.NewGuid().ToString();
            }

            asset.Timestamp = existingAsset == null ? DateTime.UtcNow : existingAsset.Timestamp;
            asset.Saltminer = new()
            {
                Asset = queueAsset.Saltminer.Asset,
                Engagement = queueAsset.Saltminer.Engagement?.Id == null ? null : new()
                {
                    Id = queueAsset.Saltminer.Engagement.Id,
                    Customer = queueAsset.Saltminer.Engagement.Customer,
                    Name = queueAsset.Saltminer.Engagement.Name,
                    PublishDate = queueAsset.Saltminer.Engagement.PublishDate,
                    Attributes = queueAsset.Saltminer.Engagement.Attributes,
                    Subtype = queueAsset.Saltminer.Engagement.Subtype
                },
                InventoryAsset = new()
                {
                    Key = string.IsNullOrEmpty(queueAsset.Saltminer.InventoryAsset?.Key) ? existingAsset?.Saltminer?.InventoryAsset?.Key : queueAsset.Saltminer.InventoryAsset?.Key
                }
            };

            // If null, make version an empty string so that visualizations for issues by version will display correctly
            asset.Saltminer.Asset.Version = queueAsset.Saltminer.Asset.Version == null ? string.Empty : queueAsset.Saltminer.Asset.Version.ToString();
            asset.Saltminer.Asset.ScanCount =  (int) DataClient.ScansCountByAssetId(asset.Id).Affected;
            return asset;
        }

        private static Scan Translate(QueueScan queueScan, QueueAsset queueAsset, Scan existingScan = null)
        {
            var scan = existingScan ?? new Scan();

            scan.Id = existingScan == null ? Guid.NewGuid().ToString() : scan.Id;
            scan.Saltminer = new SaltMinerScanInfo()
            {
                InventoryAsset = existingScan?.Saltminer?.InventoryAsset ?? new(),
                Engagement = queueScan.Saltminer.Engagement?.Id == null ? null : new EngagementInfo
                {

                    Id = queueScan.Saltminer.Engagement.Id,
                    Customer = queueScan.Saltminer.Engagement.Customer,
                    Name = queueScan.Saltminer.Engagement.Name,
                    PublishDate = queueScan.Saltminer.Engagement.PublishDate,
                    Attributes = queueScan.Saltminer.Engagement.Attributes,
                    Subtype = queueScan.Saltminer.Engagement.Subtype
                },
                Scan = new ScanInfo
                {
                    AssessmentType = queueScan.Saltminer.Scan.AssessmentType,
                    Critical = 0,
                    High = 0,
                    Medium = 0,
                    Low = 0,
                    Info = 0,
                    Product = queueScan.Saltminer.Scan.Product,
                    ReportId = queueScan.Saltminer.Scan.ReportId,
                    ScanDate = queueScan.Saltminer.Scan.ScanDate,
                    ProductType = queueScan.Saltminer.Scan.ProductType,
                    Vendor = queueScan.Saltminer.Scan.Vendor,
                }, 
                Asset = new AssetIdInfo
                {
                    AssetType = queueAsset.Saltminer.Asset.AssetType,
                    Attributes = queueAsset.Saltminer.Asset.Attributes,
                    Description = queueAsset.Saltminer.Asset.Description,
                    Host = queueAsset.Saltminer.Asset.Host,
                    Id = existingScan?.Saltminer.Asset.Id,
                    Instance = queueAsset.Saltminer.Asset.Instance,
                    Ip = queueAsset.Saltminer.Asset.Ip,
                    IsProduction = queueAsset.Saltminer.Asset.IsProduction,
                    IsRetired = queueAsset.Saltminer.Asset.IsRetired,
                    IsSaltminerSource = queueAsset.Saltminer.Asset.IsSaltminerSource,
                    Name = queueAsset.Saltminer.Asset.Name,
                    Port = queueAsset.Saltminer.Asset.Port,
                    ScanCount = queueAsset.Saltminer.Asset.ScanCount,
                    Scheme = queueAsset.Saltminer.Asset.Scheme,
                    SourceId = queueAsset.Saltminer.Asset.SourceId,
                    SourceType = queueAsset.Saltminer.Asset.SourceType,
                    Version = queueAsset.Saltminer.Asset.Version,
                    VersionId = queueAsset.Saltminer.Asset.VersionId
                }
            };
            scan.Timestamp = existingScan == null ? DateTime.UtcNow : existingScan.Timestamp;

            return scan;
        }

        private Scan Translate(Scan newScan, Asset newAsset, QueueScan queueScan)
        {
            var existingScan = GetScan(newAsset.Saltminer.Asset.SourceType, newAsset.Saltminer.Asset.SourceId, queueScan.Saltminer.Scan.ReportId, newAsset.Saltminer.Asset.AssetType);
            var scan = existingScan ?? new Scan();
            
            scan.Id = existingScan == null ? Guid.NewGuid().ToString() : scan.Id;
            scan.Saltminer = new SaltMinerScanInfo()
            {
                Engagement = queueScan.Saltminer.Engagement?.Id == null ? null : new EngagementInfo
                {

                    Id = queueScan.Saltminer.Engagement.Id,
                    Customer = queueScan.Saltminer.Engagement.Customer,
                    Name = queueScan.Saltminer.Engagement.Name,
                    PublishDate = queueScan.Saltminer.Engagement.PublishDate,
                    Attributes = queueScan.Saltminer.Engagement.Attributes,
                    Subtype = queueScan.Saltminer.Engagement.Subtype
                },
                Scan = new ScanInfo
                {
                    AssessmentType = queueScan.Saltminer.Scan.AssessmentType,
                    Critical = 0,
                    High = 0,
                    Medium = 0,
                    Low = 0,
                    Info = 0,
                    Product = queueScan.Saltminer.Scan.Product,
                    ReportId = queueScan.Saltminer.Scan.ReportId,
                    ScanDate = queueScan.Saltminer.Scan.ScanDate,
                    ProductType = queueScan.Saltminer.Scan.ProductType,
                    Vendor = queueScan.Saltminer.Scan.Vendor,
                },
                Asset = newScan.Saltminer.Asset,
                InventoryAsset = newAsset.Saltminer.InventoryAsset
            };
            scan.Timestamp = existingScan == null ? DateTime.UtcNow : existingScan.Timestamp;

            return scan;
        }

        private static Issue Translate(Scan scan, QueueIssue queueIssue, Issue existingIssue = null)
        {
            Validate(scan, queueIssue);

            var issue = existingIssue ?? new Issue();
            
            issue.Id = existingIssue == null ? Guid.NewGuid().ToString() : existingIssue.Id;
            issue.Timestamp = existingIssue == null ? DateTime.UtcNow : existingIssue.Timestamp;
            issue.Labels = queueIssue.Labels;
            issue.Message = queueIssue.Message;
            issue.Tags = queueIssue.Tags;
            issue.Vulnerability = queueIssue.Vulnerability;
            issue.Vulnerability.RemovedDate = queueIssue.Vulnerability.RemovedDate;
            issue.Saltminer = new SaltMinerIssueInfo()
            {
                Attributes = queueIssue.Saltminer.Attributes,
                Scan = new IssueScanInfo
                {
                    Id = scan.Id,
                    ReportId = scan.Saltminer.Scan.ReportId,
                    ScanDate = scan.Saltminer.Scan.ScanDate,
                    AssessmentType = scan.Saltminer.Scan.AssessmentType,
                    Product = scan.Saltminer.Scan.Product,
                    ProductType = scan.Saltminer.Scan.ProductType,
                    Rulepacks = scan.Saltminer.Scan.Rulepacks,
                    Vendor = scan.Saltminer.Scan.Vendor,
                },
                CustomData = null,
                Engagement = queueIssue.Saltminer.Engagement?.Id == null ? null : new EngagementInfo
                {

                    Id = queueIssue.Saltminer.Engagement.Id,
                    Customer = queueIssue.Saltminer.Engagement.Customer,
                    Name = queueIssue.Saltminer.Engagement.Name,
                    PublishDate = queueIssue.Saltminer.Engagement.PublishDate,
                    Attributes = queueIssue.Saltminer.Engagement.Attributes,
                    Subtype = queueIssue.Saltminer.Engagement.Subtype
                },
                Source = queueIssue.Saltminer.Source,
                Asset = scan.Saltminer.Asset,
                InventoryAsset = scan.Saltminer.InventoryAsset
            };

            // if somehow a bad severity value gets through, detect and default it
            if (!Enum.TryParse<Severity>(queueIssue.Vulnerability.Severity, out _))
            {
                queueIssue.Vulnerability.Severity = Severity.Info.ToString("g");
            }

            switch (Enum.Parse<Severity>(queueIssue.Vulnerability.Severity))
            {
                case Severity.Critical:
                    issue.Saltminer.Critical = 1;
                    break;
                case Severity.High:
                    issue.Saltminer.High = 1;
                    break;
                case Severity.Medium:
                    issue.Saltminer.Medium = 1;
                    break;
                case Severity.Low:
                    issue.Saltminer.Low = 1;
                    break;
                case Severity.Zero:
                    issue.Saltminer.Info = 1;
                    break;
                case Severity.NoScan:
                    issue.Saltminer.NoScan = 1;
                    break;
                default:
                    issue.Saltminer.Info = 1;
                    break;
            }

            return issue;
        }

        private static void Validate(Scan scan, QueueIssue queueIssue)
        {
            if (scan.Saltminer.Asset.SourceType == SourceType.Pentest.ToString())
            {
                var status = EnumExtensions.GetValueFromDescription<EngagementIssueStatus>(queueIssue.Vulnerability.TestStatus);
                if (status == 0)
                {
                    var msg = $"Test status for queue issue {queueIssue.Id} is invalid, canceling operation.";
                    throw new OperationCanceledException(msg);
                }
            }
        }

        private void CheckCancel(bool readyToAbort = true)
        {
            if (RunConfig.CancelToken.IsCancellationRequested)
            {
                if (readyToAbort)
                {
                    Logger.LogInformation("Cancellation requested, aborting processing");
                    
                    throw new CancelTokenException();
                }
                else
                {
                    if (!RunConfig.CancelRequestedReported)
                    {
                        Logger.LogInformation("Cancellation requested, finishing current queue scan");
                        
                        RunConfig.CancelRequestedReported = true;
                    }
                }
            }
        }

        #region Validation

        private static List<ValidationResult> CheckForValidationErrors(QueueScan queueScan, long totalQueueIssueFound, IEnumerable<QueueIssue> queueIssues)
        {
            var validationErrors = new List<ValidationResult>();

            if (queueScan.Saltminer.Internal.IssueCount != totalQueueIssueFound && queueScan.Saltminer.Internal.IssueCount > -1)
            {
                validationErrors.Add(new ValidationResult($"Queued Scan {queueScan.Id} had an Issue Count of {queueScan.Saltminer.Internal.IssueCount}. The total count of Queued Issues relating to this scan is {totalQueueIssueFound}"));
            }
            else
            {
                foreach (var queuedIssue in queueIssues)
                {
                    var validationContext = new ValidationContext(queuedIssue);

                    if (Validator.TryValidateObject(queuedIssue, validationContext, validationErrors))
                    {
                        ValidateSaltMinerEntityProperties(queuedIssue, validationErrors);
                    }

                    if (validationErrors.Any())
                    {
                        break; // Design decision: break when validation errors are found on any issue
                    }
                }
            }

            return validationErrors;
        }

        private static List<ValidationResult> CheckForValidationErrors(QueueScan qScan)
        {
            var context = new ValidationContext(qScan);
            List<ValidationResult> validationResults = new();

            if (Validator.TryValidateObject(qScan, context, validationResults))
            {
                ValidateSaltMinerEntityProperties(qScan, validationResults);
            }

            if (qScan.Saltminer.Scan.ScanDate == default)
            {
                validationResults.Add(new ValidationResult("ScanDate is missing"));
            }

            return validationResults;
        }

        private static void ValidateSaltMinerEntityProperties(object objToValidate, List<ValidationResult> validationResults)
        {
            foreach (var property in objToValidate.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                var propertyType = property.PropertyType;

                if (propertyType.IsClass && propertyType.Namespace.StartsWith(typeof(SaltMinerEntity).Namespace.Split('.')[0]))
                {
                    var value = property.GetValue(objToValidate);

                    if (value != null)
                    {
                        var validationContext = new ValidationContext(value);
                        
                        if (Validator.TryValidateObject(value, validationContext, validationResults))
                        {
                            ValidateSaltMinerEntityProperties(value, validationResults);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        #endregion
    }

    [Serializable]
    public class QueueProcessorException : Exception
    {
        public QueueProcessorException() { }
        public QueueProcessorException(string message) : base(message) { }
        public QueueProcessorException(string message, Exception inner) : base(message, inner) { }
        protected QueueProcessorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class QueueProcessorConfigurationException : Exception
    {
        public QueueProcessorConfigurationException() { }
        public QueueProcessorConfigurationException(string message) : base(message) { }
        public QueueProcessorConfigurationException(string message, Exception inner) : base(message, inner) { }
        protected QueueProcessorConfigurationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}