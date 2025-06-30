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
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.ElasticClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Saltworks.SaltMiner.DataApi.Data
{
    public class ElasticDataRepo : IDataRepo
    {
        private readonly ILogger Logger;
        private readonly IElasticClient ElasticClient;
        private readonly ApiConfig Config;

        public ElasticDataRepo(ILogger<ElasticDataRepo> logger, IElasticClientFactory factory, ApiConfig config)
        {
            Logger = logger;
            Logger.LogDebug("Initialization complete.");
            ElasticClient = factory.CreateClient();
            Config = config;
        }

        public NoDataResponse GetLicenseType()
        {
            return ElasticClient.GetClusterLicenseLevel().ToNoDataResponse();
        }

        public Tuple<T, ILockingInfo> UpdateWithLocking<T>(T entity, string index, ILockingInfo lockInfo) where T : SaltMinerEntity
        {
            lockInfo = lockInfo ?? throw new ArgumentNullException(nameof(lockInfo));
            if (lockInfo is not ElasticLockingInfo<T>)
            {
                throw new ArgumentException("Incorrect type - expected ElasticLockingInfo<T>", nameof(lockInfo));
            }

            Logger.LogDebug("UpdateWithLocking id {Id} initiated.", entity.Id);
            
            var elasticLockInfo = lockInfo as ElasticLockingInfo<T>;
            var response = ElasticClient.UpdateWithLocking(entity, index, elasticLockInfo.Primary, elasticLockInfo.Sequence);

            Logger.LogDebug("UpdateWithLocking id {Id} complete.", entity.Id);

            if (response.IsSuccessful && response.Result != null)
            {
                return new Tuple<T, ILockingInfo>(response.Result.Document, new ElasticLockingInfo<T>
                {
                    Id = response.Result.Document.Id,
                    Primary = response.Result.Primary,
                    Sequence = response.Result.Sequence
                });
            }
            else
            {
                return null; // probably unreachable, expect exception if response isn't successful
            }
        }

        public Tuple<T, ILockingInfo> GetWithLocking<T>(string id, string indexName) where T : SaltMinerEntity
        {
            var response = ElasticClient.Get<T>(id, indexName);

            if (response.Result == null || !response.IsSuccessful)
            {
                return null;
            }

            return new Tuple<T, ILockingInfo>(response.Result.Document, new ElasticLockingInfo<T> { Primary = response.Result.Primary, Id = id, Sequence = response.Result.Sequence });
        }

        public DataResponse<T> Search<T>(SearchRequest request, string indexName) where T : SaltMinerEntity
        {
            Logger.LogDebug("Search with {request} on index '{index}' initiated.", JsonSerializer.Serialize(request), indexName ?? "(not passed)");

            if (request.UIPagingInfo == null && request.PitPagingInfo == null)
            {
                request.PitPagingInfo = new PitPagingInfo();
            }

            if (request.UIPagingInfo != null)
            {
                request.UIPagingInfo.Size = (request.UIPagingInfo?.Size ?? 0) >= 1 ? request.UIPagingInfo.Size : Config.ElasticDefaultResultSize;
                request.UIPagingInfo.Page = request.UIPagingInfo?.Page ?? 1;
            }
            else
            {
                request.PitPagingInfo.Size = (request.PitPagingInfo?.Size ?? 0) >= 1 ? request.PitPagingInfo.Size : Config.ElasticDefaultResultSize;
            }

            if (!Config.ElasticEnableDiagnosticInfo)
            {
                Logger.LogDebug("Search debug messages may be missing information - set ElasticEnableDiagnosticInfo to populate them.");
            }

            var result = ElasticClient.Search<T>(request, indexName);
            Logger.LogDebug("Search with {filter} on index '{index}' complete.", JsonSerializer.Serialize(request), indexName ?? "(not passed)");

            return result.ToDataResponse();
        }

        public IEnumerable<ElasticAggResponse> SingleGroupAggregation(string groupField, string dataIndex, Dictionary<string, string> fieldAggregates, SearchRequest request = null)
        {
            var alist = new List<IElasticClientRequestAggregate>();

            foreach (var fa in fieldAggregates)
            {
                alist.Add(ElasticClient.BuildRequestAggregate(fa.Key, fa.Key, Enum.Parse<ElasticAggregateType>(fa.Value, true)));
            }

            var ra = ElasticClient.BuildRequestAggregation(groupField, groupField, alist);

            if (string.IsNullOrEmpty(dataIndex))
            {
                throw new ArgumentNullException(nameof(dataIndex));
            }

            Logger.LogDebug("Aggregation query on group field '{groupField}' and index '{dataIndex}' initiated.", groupField, dataIndex);

            var resp = ElasticClient.SearchWithCompositeAgg(ra, request, dataIndex);
            IEnumerable<ElasticAggResponse> results = resp.Results.Select(r => new ElasticAggResponse(r.Document)).ToList();

            Logger.LogDebug("Aggregation query on group field '{bucketField}' and index '{dataIndex}' complete, {count} result(s).", ra.BucketField, dataIndex, results.Count());

            return results;
        }

        public ElasticAggResponse EngagementIssueCountAggregates(string engagementId, PitPagingInfo pitPaging, IEnumerable<string> sourceFields, IEnumerable<IElasticClientRequestAggregate> aggregates, string indexName)
        {
            Logger.LogDebug("SnapshotAggregates with aggFields: {aggFields} initiated.", JsonSerializer.Serialize(sourceFields));
            pitPaging.Size = (pitPaging.Size ?? 0) >= 1 ? pitPaging.Size : Config.ElasticDefaultResultSize;

            if ((pitPaging.AggregateKeys?.Count ?? 0) == 0)
            {
                pitPaging.AggregateKeys = new Dictionary<string, object>();
            }

            foreach (var keyValuePair in pitPaging.AggregateKeys)
            {
                pitPaging.AggregateKeys[keyValuePair.Key] = keyValuePair.Value.ToString(); // Make sure the object is a string inside
            }

            var request = new SearchRequest
            {
                PitPagingInfo = pitPaging,
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "saltminer.engagement.id", engagementId },
                        { "vulnerability.is_active", "true" }
                    }
                }
            };

            var result = ElasticClient.GetCompositeAggregate<Issue>(request, sourceFields, aggregates, indexName);

            Logger.LogDebug("SnapshotAggregates with sourceFields: {sourceFields} and aggregates: {aggs} complete. {count} results.", JsonSerializer.Serialize(sourceFields), aggregates, result?.Results?.Count() ?? 0);

            if ((result?.Results?.Count() ?? 0) == 0)
            {
                return new ElasticAggResponse
                {
                    Results = new(),
                    PitPagingInfo = result?.PitPagingInfo,
                    AfterKeys = result?.AfterKeys
                };
            }
            return new ElasticAggResponse()
            {
                Results = result.Results.Select(agg => new ElasticAggResult(agg.Document)).ToList(),
                PitPagingInfo = result.PitPagingInfo,
                AfterKeys = result.AfterKeys
            };
        }

        public ElasticAggResponse SnapshotAggregates(PitPagingInfo pitPaging, IEnumerable<string> sourceFields, IEnumerable<IElasticClientRequestAggregate> aggregates, string assetType)
        {
            Logger.LogDebug("SnapshotAggregates with aggFields: {aggFields} initiated.", JsonSerializer.Serialize(sourceFields));
            pitPaging.Size = (pitPaging.Size ?? 0) >= 1 ? pitPaging.Size : Config.ElasticDefaultResultSize;

            if ((pitPaging.AggregateKeys?.Count ?? 0) == 0)
            {
                pitPaging.AggregateKeys = new Dictionary<string, object>();
            }

            foreach (var keyValuePair in pitPaging.AggregateKeys)
            {
                pitPaging.AggregateKeys[keyValuePair.Key] = keyValuePair.Value.ToString(); // Make sure the object is a string inside
            }

            var request = new SearchRequest
            {
                PitPagingInfo = pitPaging,
                Filter = new Filter
                {
                    AnyMatch = false,
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.IsHistorical", "false" },
                        { "Vulnerability.IsActive", "true" }
                    }
                }
            };

            var result = ElasticClient.GetCompositeAggregate<Issue>(request, sourceFields, aggregates, "issues_active");

            Logger.LogDebug("SnapshotAggregates with sourceFields: {sourceFields} and aggregates: {aggs} complete. {count} results.", JsonSerializer.Serialize(sourceFields), aggregates, result?.Results?.Count() ?? 0);

            if ((result?.Results?.Count() ?? 0) == 0)
            {
                return new ElasticAggResponse
                {
                    Results = new(),
                    PitPagingInfo = result?.PitPagingInfo,
                    AfterKeys = result?.AfterKeys
                };
            }
            return new ElasticAggResponse()
            {
                Results = result.Results.Select(agg => new ElasticAggResult(agg.Document)).ToList(),
                PitPagingInfo = result.PitPagingInfo,
                AfterKeys = result.AfterKeys
            };
        }

        public List<SaltMinerIndexData> GetMetadata(List<string> templateNames)
        {
            var checkedIndices = ElasticClient.GetAllTemplates().Where(x => templateNames.Contains(x)).ToList();

            var result = new List<SaltMinerIndexData>();

            foreach(var name in checkedIndices)
            {
                var metaData = ElasticClient.Search<IndexMeta>(new SearchRequest
                {
                    Filter = new Filter
                    {
                        FilterMatches = new Dictionary<string, string>
                        {
                            { "template_name", name }
                        },
                    },
                }, IndexMeta.GenerateIndex());
                
                var first = metaData.ToDataResponse()?.Data?.FirstOrDefault();

                if (first != null)
                {
                    result.Add(new(first.Index, first.Version, first.TemplateName));
                }
                else
                {
                    result.Add(new(null, null, name));
                }
            }

            return result;
        }

        public BulkResponse AddUpdateBulk<T>(IEnumerable<T> Documents, string indexName) where T : SaltMinerEntity
        {
            return ElasticClient.AddUpdateBulk(Documents, indexName).ToBulkResponse();
        }

        public NoDataResponse ActiveIssueAlias(string indexName, string alias)
        {
            return ElasticClient.AddActiveIssueAlias(indexName, alias).ToNoDataResponse();
        }

        public string GetIndexMapping(string index)
        {
            return ElasticClient.GetIndexMapping(index);
        }

        public string GetIndexTemplate(string index)
        {
            return ElasticClient.GetIndexTemplate(index);
        }

        public string SearchForJson(SearchRequest request, string index)
        {
            return ElasticClient.SearchForJson(request, index);
        }

        public IElasticClientResponse UpdateIndexTemplate(string templateName, string newTemplate)
        {
            return ElasticClient.AddUpdateIndexTemplate(templateName, newTemplate);
        }

        public IElasticClientResponse ReIndex(string indexName, string newIndexName)
        {
            return ElasticClient.ReIndex(indexName, newIndexName);
        }

        public IElasticClientResponse DeleteIndex(string indexName)
        {
            return ElasticClient.DeleteIndex(indexName);
        }
    }

    public class ElasticLockingInfo<T> : ILockingInfo where T : SaltMinerEntity
    {
        internal ElasticLockingInfo() { }
        public string Id { get; init; }
        internal long? Primary { get; init; }
        internal long? Sequence { get; init; }
    }

    public class ElasticDataFilter : SearchRequest
    {

        public ElasticDataFilter() { }
        public ElasticDataFilter(string key, string value)
        {
            Filter = new()
            {
                FilterMatches = new Dictionary<string, string> { { key, value } }
            };
        }
        public ElasticDataFilter(string key, string value, PitPagingInfo paging, IList<object> afterKeys = null)
        {
            Filter = new()
            {
                FilterMatches = new Dictionary<string, string> { { key, value } }
            };
            PitPagingInfo = paging;
            AfterKeys = afterKeys;
        }
        public ElasticDataFilter(string key, string value, UIPagingInfo paging, IList<object> afterKeys = null)
        {
            Filter = new()
            {
                FilterMatches = new Dictionary<string, string> { { key, value } }
            };
            UIPagingInfo = paging;
            AfterKeys = afterKeys;
        }
        public ElasticDataFilter(Dictionary<string, string> filterMatches)
        {
            Filter = new()
            {
                FilterMatches = filterMatches
            };
        }
        public ElasticDataFilter(Dictionary<string, string> filterMatches, PitPagingInfo paging, IList<object> afterKeys = null)
        {
            PitPagingInfo = paging;
            Filter = new()
            {
                FilterMatches = filterMatches
            };
            AfterKeys = afterKeys;
        }
        public ElasticDataFilter(Dictionary<string, string> filterMatches, UIPagingInfo paging, IList<object> afterKeys = null)
        {
            UIPagingInfo = paging;
            Filter = new()
            {
                FilterMatches = filterMatches
            };
            AfterKeys = afterKeys;
        }
    }

    public class ElasticAggResponse
    {
        public ElasticAggResponse() { }
        public ElasticAggResponse(ElasticClientCompositeAggregate agg)
        {
            Result = new ElasticAggResult(agg);
        }

        public ElasticAggResult Result { get; set; }
        public List<ElasticAggResult> Results { get; set; } = new();
        public UIPagingInfo UIPagingInfo { get; set; }
        public PitPagingInfo PitPagingInfo { get; set; }
        public IList<object> AfterKeys { get; set; }
    }

    public class ElasticAggResult
    {
        public ElasticAggResult() { }
        public ElasticAggResult(ElasticClientCompositeAggregate agg)
        {
            Key = agg.BucketKey;
            DocCount = agg.DocCount;
            foreach (var a in agg.Aggregates)
            {
                Aggs.Add(a.Key, a.Value);
            }
        }

        public string Key { get; set; }
        public long? DocCount { get; set; }
        public Dictionary<string, double?> Aggs { get; set; } = new();
    }
}
