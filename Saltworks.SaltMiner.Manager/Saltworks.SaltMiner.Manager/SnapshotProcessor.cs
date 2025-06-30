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
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.Manager
{
    public class SnapshotProcessor
    {
        private readonly ILogger Logger;
        private readonly DataClient.DataClient DataClient;
        private readonly ManagerConfig Config;
        private static DateTime Today = DateTime.UtcNow;
        private static bool IsDaily = Today.Day != 1;
        private SnapshotRuntimeConfig RuntimeConfig;
        private PitPagingInfo BucketPagingInfo;

        public SnapshotProcessor
        (
            ILogger<SnapshotProcessor> logger,
            DataClientFactory<Manager> dataClientFactory,
            ManagerConfig config
        )
        {
            Logger = logger;
            DataClient = dataClientFactory.GetClient();
            Config = config;
        }

        private void CheckCancel()
        {
            if (RuntimeConfig.CancelToken.IsCancellationRequested)
            {
                throw new CancelTokenException();
            }
        }

        private static int GetBucketValue(KeyValuePair<string, Dictionary<string, double?>> bucket, string key)
        {
            if (bucket.Value.ContainsKey(key))
                return (int)(bucket.Value[key] ?? 0);
            else
                return 0;
        }

        private Dictionary<string, Dictionary<string, double?>> GetSnapshotCountsBatch()
        {
            Logger.LogInformation("Gathering snapshot buckets");
            
            var search = new SearchRequest 
            {
                PitPagingInfo = BucketPagingInfo, 
                Filter = new()
                {
                    FilterMatches = new()
                }
            };

            if (!string.IsNullOrEmpty(RuntimeConfig.SourceId))
            {
                search.Filter.FilterMatches.Add("Saltminer.Asset.SourceId", RuntimeConfig.SourceId);
            }
            
            var result = DataClient.SnapshotCounts(search);

            BucketPagingInfo = result.PitPagingInfo;

            return result.Results;
        }

        public void Run(RuntimeConfig config)
        {
            if (config is not SnapshotRuntimeConfig)
            {
                throw new ArgumentException($"Expected type '{typeof(SnapshotRuntimeConfig).Name}', but passed value is '{config.GetType().Name}'", nameof(config));
            }

            RuntimeConfig = config.Validate() as SnapshotRuntimeConfig;
            var complete = false;
            var errorCount = 0;
            
            try // outer
            {

                var bucketKey = "";
                var count = 0;
                var batchCount = 0;
                var retries = 0;
                
                BucketPagingInfo = new PitPagingInfo(Config.SnapshotProcessorApiBatchSize);
                
                var firstBatch = true;
                
                while (!complete)
                {
                    batchCount++;
                    
                    var snapshots = new List<Snapshot>();
                    // Get a batch of snapshot count buckets
                    var data = GetSnapshotCountsBatch();
                    
                    if ((data?.Count ?? 0) == 0)
                    {
                        if (firstBatch)
                        {
                            Logger.LogWarning("Snapshots cannot be created, as no grouped data is being returned. Terminating process");
                            return;
                        }
                        
                        complete = true;
                        continue;
                    }
                    
                    firstBatch = false;

                    // Process buckets - list only
                    if (RuntimeConfig.ListOnly)
                    {
                        foreach (var bucket in data)
                        {
                            Logger.LogInformation("Bucket values '{key}', {value} total issue(s)", bucket.Key, bucket.Value);
                            count++;
                            
                            if (RuntimeConfig.Limit > 0 && count >= RuntimeConfig.Limit)
                            {
                                Logger.LogInformation("Reached limit of {limit}", RuntimeConfig.Limit);
                                complete = true;
                                break;
                            }
                        }
                        
                        CheckCancel();
                        continue;
                    }
                    
                    // Process buckets
                    Logger.LogInformation("Processing aggregate batch {batchCount} for {buckets} buckets", batchCount, data.Count);
                    
                    foreach (var bucket in data)
                    {
                        Logger.LogInformation("Bucket values '{key}', {value} total issue(s)", bucket.Key, bucket.Value);
                        try  // Inner
                        {
                            CheckCancel();

                            bucketKey = bucket.Key;
                            var keySplit = bucketKey.Split("|");
                            var sourceId = keySplit[0].Replace("{P}", "|");
                            var assetType = keySplit[1].Replace("{P}", "|");
                            var sourceType = keySplit[2].Replace("{P}", "|");
                            var instance = keySplit[3].Replace("{P}", "|");
                            //var assessmentType = keySplit[4].Replace("{P}", "|")
                            var vulName = keySplit[5].Replace("{P}", "|");
                            var severity = keySplit[6].Replace("{P}", "|");
                            var today = DateTime.UtcNow;
                            
                            var asset = DataClient.AssetSearch(new SearchRequest
                            {
                                Filter = new Filter
                                {
                                    FilterMatches = new Dictionary<string, string>
                                    {
                                        { "Saltminer.Asset.SourceId", sourceId },
                                        { "Saltminer.Asset.Instance", instance },
                                    }
                                },
                                AssetType = assetType,
                                SourceType = sourceType,
                            }).Data.FirstOrDefault();

                            if(asset == null)
                            {
                                Logger.LogDebug($"Asset not found.");
                                continue;
                            }
                           
                            var vul = DataClient.IssueSearch(new SearchRequest
                            {
                                Filter = new Filter
                                {
                                    AnyMatch = false,
                                    FilterMatches = new Dictionary<string, string>
                                    {
                                        { "Saltminer.Asset.Id", asset.Id },
                                        { "Saltminer.Asset.Instance", instance },
                                        { "Vulnerability.Name", vulName },
                                        { "Vulnerability.Severity", severity }
                                    }
                                },
                                AssetType = assetType,
                                SourceType = sourceType,
                            }).Data.FirstOrDefault();
                           
                            if (vul == null)
                            {
                                Logger.LogDebug($"Vul not found.");
                                continue;
                            }

                            var snapShot = new Snapshot()
                            {
                                Saltminer = new()
                                {
                                    SnapshotDate = IsDaily ? today : new DateTime(today.Year, today.Month - 1, 15, 0, 0, 0, DateTimeKind.Utc).ToUniversalTime(),
                                    Critical = GetBucketValue(bucket, "Saltminer.Critical"),
                                    High = GetBucketValue(bucket, "Saltminer.High"),
                                    Medium = GetBucketValue(bucket, "Saltminer.Medium"),
                                    Low = GetBucketValue(bucket, "Saltminer.Low"),
                                    Info = GetBucketValue(bucket, "Saltminer.Info"),
                                    Asset = new()
                                    {
                                        AssetType = asset.Saltminer.Asset.AssetType,
                                        SourceId = asset.Saltminer.Asset.SourceId,
                                        Attributes = asset.Saltminer.Asset.Attributes,
                                        Instance = asset.Saltminer.Asset.Instance,
                                        Description = asset.Saltminer.Asset.Description,
                                        IsProduction = asset.Saltminer.Asset.IsProduction,
                                        Name = asset.Saltminer.Asset.Name,
                                        Host = asset.Saltminer.Asset.Host,
                                        Ip = asset.Saltminer.Asset.Ip,
                                        IsRetired = asset.Saltminer.Asset.IsRetired,
                                        IsSaltminerSource = asset.Saltminer.Asset.IsSaltminerSource,
                                        LastScanDaysPolicy = asset.Saltminer.Asset.LastScanDaysPolicy,
                                        Port = asset.Saltminer.Asset.Port,
                                        Scheme = asset.Saltminer.Asset.Scheme,
                                        SourceType = asset.Saltminer.Asset.SourceType,
                                        Version = asset.Saltminer.Asset.Version,
                                        VersionId = asset.Saltminer.Asset.VersionId,
                                        Id = asset.Id
                                    },
                                    Scan = new()
                                    {
                                        AssessmentType = vul.Saltminer.Scan.AssessmentType,
                                        Product = vul.Saltminer.Scan.Product,
                                        Vendor = vul.Saltminer.Scan.Vendor,
                                        ProductType = vul.Saltminer.Scan.ProductType
                                    },
                                    Engagement = null,
                                    InventoryAsset = new()
                                    {
                                        Key = asset.Saltminer.InventoryAsset.Key
                                    },
                                    IsHistorical = false,
                                    Source = new()
                                    {
                                        Analyzer = vul.Saltminer.Source.Analyzer,
                                        Confidence = vul.Saltminer.Source.Confidence,
                                        Impact = vul.Saltminer.Source.Impact,
                                        IssueStatus = vul.Saltminer.Source.IssueStatus,
                                        Kingdom = vul.Saltminer.Source.Kingdom,
                                        Likelihood = vul.Saltminer.Source.Likelihood
                                    }
                                },
                                Vulnerability = new()
                                {
                                    Category = vul.Vulnerability.Category,
                                    Classification = vul.Vulnerability.Classification,
                                    Name = vul.Vulnerability.Name,
                                    Scanner = new() {
                                        AssessmentType = vul.Vulnerability.Scanner.AssessmentType,
                                        Product = vul.Vulnerability.Scanner.Product,
                                        Vendor = vul.Vulnerability.Scanner.Vendor
                                    },
                                    Score = new()
                                    {
                                        Base = vul.Vulnerability.Score.Base,
                                        Environmental = vul.Vulnerability.Score.Environmental,
                                        Version = vul.Vulnerability.Score.Version,
                                        Temporal = vul.Vulnerability.Score.Temporal
                                    },
                                    Severity = vul.Vulnerability.Severity,
                                    SourceSeverity = vul.Vulnerability.SourceSeverity
                                },
                                Timestamp = DateTime.UtcNow,
                            };

                            if(asset.Saltminer.Engagement != null)
                            {
                                snapShot.Saltminer.Engagement = new()
                                {
                                    Id = asset.Saltminer.Engagement.Id,
                                    Customer = asset.Saltminer.Engagement.Customer,
                                    Name = asset.Saltminer.Engagement.Name,
                                    PublishDate = asset.Saltminer.Engagement.PublishDate,
                                    Subtype = asset.Saltminer.Engagement.Subtype
                                };
                            }
                            snapshots.Add(snapShot);
                            count++;

                            if (RuntimeConfig.Limit > 0 && count >= RuntimeConfig.Limit)
                            {
                                Logger.LogInformation("Processed requested limit of snapshots ({limit}).", RuntimeConfig.Limit);
                                complete = true;
                                break;
                            }
                        }
                        catch (CancelTokenException) // inner
                        {
                            throw;
                        }
                        catch (Exception ex) // inner
                        {
                            Logger.LogError(ex, "Error processing snapshot for bucket with values '{bucketKey}': {message}", bucketKey, ex.Message);
                            errorCount++;
                            if (errorCount > Config.SnapshotProcessorMaxErrors)
                            {
                                Logger.LogCritical("Exceeded maximum errors ({errors}) while processing snapshots, terminating process", Config.SnapshotProcessorMaxErrors);
                                return;
                            }
                        }
                    }
                    if (!RuntimeConfig.ListOnly)
                    {
                        Logger.LogInformation("Processed {count} snapshot aggregates, sending batch {batchCount} to datastore", count, batchCount);
                        var snapshotAssetGrouping = snapshots.GroupBy(x => x.Saltminer.Asset.AssetType).Select(grp => grp.ToList()).ToList();
                        
                        foreach (var assetSnapshotGroup in snapshotAssetGrouping)
                        {
                            DataClient.SnapshotAddUpdateBatch(assetSnapshotGroup, true);
                            
                            if(DateTime.UtcNow.Day == 2)
                            {
                                var today = DateTime.UtcNow;
                                var lastMonth = new DateTime(today.Year, today.Month, 15, 0, 0, 0, DateTimeKind.Utc);
                                lastMonth = lastMonth.AddMonths(-1);
                                
                                foreach (var snapshot in assetSnapshotGroup)
                                {
                                    snapshot.Saltminer.SnapshotDate = lastMonth;
                                }
                                DataClient.SnapshotAddUpdateBatch(assetSnapshotGroup);
                            }
                        }
                    }
                }

                Logger.LogInformation("Snapshot processing complete");
                Logger.LogDebug("{retries} retries during processing", retries);
            }
            catch (CancelTokenException) // outer
            {
                Logger.LogWarning("Cancellation requested, terminating process");
            }
            catch (Exception ex) // outer
            {
                Logger.LogCritical(ex, "General SnapshotProcessor failure - see inner exception for details");
            }
        }
    }
}
