/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
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
            return ElasticClient.ClusterLicenseLevel().ToNoDataResponse();
        }

        public Tuple<T, ILockingInfo> UpdateWithLocking<T>(T entity, string index, ILockingInfo lockInfo) where T : SaltMinerEntity
        {
            lockInfo = lockInfo ?? throw new ArgumentNullException(nameof(lockInfo));
            if (lockInfo is not ElasticLockingInfo)
            {
                throw new ArgumentException("Incorrect type - expected ElasticLockingInfo", nameof(lockInfo));
            }

            Logger.LogDebug("UpdateWithLocking id {Id} initiated.", entity.Id);
            
            var elasticLockInfo = lockInfo as ElasticLockingInfo;
            var response = ElasticClient.UpdateWithLocking(entity, index, elasticLockInfo.Primary, elasticLockInfo.Sequence);

            Logger.LogDebug("UpdateWithLocking id {Id} complete.", entity.Id);

            if (response.IsSuccessful && response.Result != null)
            {
                return new Tuple<T, ILockingInfo>(response.Result.Document, new ElasticLockingInfo
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

            return new Tuple<T, ILockingInfo>(response.Result.Document, new ElasticLockingInfo { Primary = response.Result.Primary, Id = id, Sequence = response.Result.Sequence });
        }

        public DataResponse<T> Search<T>(string index, SearchRequest request) where T : SaltMinerEntity
        {
            Logger.LogDebug("Search with {Request} on index '{Index}' initiated.", JsonSerializer.Serialize(request), index ?? "(not passed)");
            request.PagingInfo ??= new();
            
            if (request.PagingInfo.Size < 1)
                request.PagingInfo.Size = Config.ElasticDefaultResultSize;
            
            if (!Config.ElasticEnableDiagnosticInfo)
                Logger.LogDebug("Search debug messages may be missing information - set ElasticEnableDiagnosticInfo to populate them.");

            var result = ElasticClient.Search<T>(index, request);
            Logger.LogDebug("Search with {Filter} on index '{Index}' complete.", JsonSerializer.Serialize(request.Filter), index ?? "(not passed)");

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

            Logger.LogDebug("Aggregation query on group field '{GroupField}' and index '{DataIndex}' initiated.", groupField, dataIndex);

            var result = ElasticClient.GetCompositeAggregate<Issue>(request, new[] { groupField }, alist, dataIndex);
            IEnumerable<ElasticAggResponse> results = result.Results.Select(r => new ElasticAggResponse(r.Document)).ToList();

            Logger.LogDebug("Aggregation query on group field '{BucketField}' and index '{DataIndex}' complete, {Count} result(s).", ra.BucketField, dataIndex, results.Count());

            return results;
        }

        public ElasticAggResponse EngagementIssueCountAggregates(string engagementId, PagingInfo pager, IEnumerable<string> sourceFields, IEnumerable<IElasticClientRequestAggregate> aggList, string assetType)
        {
            Logger.LogDebug("EngagementIssueCountAggregates with aggFields: {AggFields} initiated.", JsonSerializer.Serialize(sourceFields));
            pager.Size = (pager.Size ?? 0) >= 1 ? pager.Size : Config.ElasticDefaultResultSize;

            if ((pager.AggregateKeys?.Count ?? 0) == 0)
            {
                pager.AggregateKeys = [];
            }

            foreach (var keyValuePair in pager.AggregateKeys)
            {
                pager.AggregateKeys[keyValuePair.Key] = keyValuePair.Value.ToString(); // Make sure the object is a string inside
            }

            var request = new SearchRequest
            {
                PagingInfo = pager,
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "saltminer.engagement.id", engagementId },
                        { "vulnerability.is_active", "true" }
                    }
                }
            };

            var result = ElasticClient.GetCompositeAggregate<Issue>(request, sourceFields, aggList, assetType);

            Logger.LogDebug("EngagementIssueCountAggregates with sourceFields: {SourceFields} and aggregates: {Aggs} complete. {Count} results.", JsonSerializer.Serialize(sourceFields), aggList, result?.Results?.Count() ?? 0);

            if ((result?.Results?.Count() ?? 0) == 0)
            {
                return new ElasticAggResponse
                {
                    Results = [],
                    PagingInfo = null
                };
            }
            return new ElasticAggResponse()
            {
                Results = result.Results.Select(agg => new ElasticAggResult(agg.Document)).ToList(),
                    PagingInfo = null
            };
        }

        public ElasticAggResponse SnapshotAggregates(PagingInfo pager, IEnumerable<string> sourceFields, IEnumerable<IElasticClientRequestAggregate> aggList, string assetType)
        {
            Logger.LogDebug("SnapshotAggregates with aggFields: {AggFields} initiated.", JsonSerializer.Serialize(sourceFields));
            pager.Size = (pager.Size ?? 0) >= 1 ? pager.Size : Config.ElasticDefaultResultSize;

            if ((pager.AggregateKeys?.Count ?? 0) == 0)
            {
                pager.AggregateKeys = new Dictionary<string, object>();
            }

            foreach (var keyValuePair in pager.AggregateKeys)
            {
                pager.AggregateKeys[keyValuePair.Key] = keyValuePair.Value.ToString(); // Make sure the object is a string inside
            }

            var request = new SearchRequest
            {
                PagingInfo = pager,
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

            var result = ElasticClient.GetCompositeAggregate<Issue>(request, sourceFields, aggList, "issues_active");

            Logger.LogDebug("SnapshotAggregates with sourceFields: {SourceFields} and aggregates: {Aggs} complete. {Count} results.", JsonSerializer.Serialize(sourceFields), aggList, result?.Results?.Count() ?? 0);

            if ((result?.Results?.Count() ?? 0) == 0)
            {
                return new ElasticAggResponse
                {
                    Results = new(),
                        PagingInfo = null
                };
            }
            return new ElasticAggResponse()
            {
                Results = result.Results.Select(agg => new ElasticAggResult(agg.Document)).ToList(),
                    PagingInfo = null
            };
        }

        public List<SaltMinerIndexData> GetMetadata(List<string> templateNames)
        {
            var checkedIndices = ElasticClient.IndexTemplateGetList().Where(x => templateNames.Contains(x)).ToList();

            var result = new List<SaltMinerIndexData>();

            foreach(var name in checkedIndices)
            {
                var metaData = ElasticClient.Search<IndexMeta>(IndexMeta.GenerateIndex(), 
                    new SearchRequest
                    {
                        Filter = new Filter
                        {
                            FilterMatches = new Dictionary<string, string>
                            {
                                { "template_name", name }
                            },
                        },
                    });
                
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

        public BulkResponse AddUpdateBulk<T>(IEnumerable<T> docs, string indexName) where T : SaltMinerEntity
        {
            return ElasticClient.BulkAddUpdate(docs, indexName).ToBulkResponse();
        }

        public NoDataResponse ActiveIssueAlias(string indexName, string alias)
        {
            return ElasticClient.AddActiveIssueAlias(indexName, alias).ToNoDataResponse();
        }

        public string GetIndexMapping(string index)
        {
            return ElasticClient.IndexMappingGet(index);
        }

        public string GetIndexTemplate(string template)
        {
            var result = ElasticClient.IndexTemplateGet(template);
            return result?.Result?.Document;
        }

        public string SearchForJson(SearchRequest request, string indexName)
        {
            return ElasticClient.SearchForJson(request, indexName);
        }

        public IElasticClientResponse UpdateIndexTemplate(string templateName, string newTemplate)
        {
            return ElasticClient.IndexTemplateAddUpdate(templateName, newTemplate);
        }

        public IElasticClientResponse ReIndex(string indexName, string newIndexName)
        {
            return ElasticClient.IndexReindex(indexName, newIndexName);
        }

        public IElasticClientResponse DeleteIndex(string indexName)
        {
            return ElasticClient.IndexDelete(indexName);
        }
    }

    public class ElasticLockingInfo : ILockingInfo
    {
        internal ElasticLockingInfo() { }
        public string Id { get; init; }
        internal long? Primary { get; init; }
        internal long? Sequence { get; init; }
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
        public PagingInfo PagingInfo { get; set; }
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
