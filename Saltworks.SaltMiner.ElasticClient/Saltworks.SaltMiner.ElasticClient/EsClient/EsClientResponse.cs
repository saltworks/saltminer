

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Transport.Products.Elasticsearch;
using Saltworks.Common.Data;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.ElasticClient.EsClient
{
    public class EsClientResponse : IElasticClientResponse
    {
        public bool IsSuccessful { get; set; }
        public string Message { get; set; }
        public Dictionary<string, string> BulkErrorMessages { get; set; }
        public long CountAffected { get; set; }
        public int HttpStatus { get; set; }
        public IList<object> AfterKeys { get; set; }

        public static EsClientResponse BuildResponse(bool isSuccess, string message, long affected) => new()
        {
            CountAffected = affected,
            Message = message,
            IsSuccessful = isSuccess
        };
        public static EsClientResponse BuildResponse(bool isSuccess, Dictionary<string, string> bulkErrors, string message, long affected) => new()
        {
            CountAffected = affected,
            Message = message,
            BulkErrorMessages = bulkErrors,
            IsSuccessful = isSuccess
        };
    }

    public class EsClientBucketResponse : EsClientResponse, IElasticClientResponse<ElasticClientCompositeAggregate>
    {
        public PitPagingInfo PitPagingInfo { get; set; } = new PitPagingInfo();
        public IEnumerable<IElasticClientDto<ElasticClientCompositeAggregate>> Results { get; set; }
        public IElasticClientDto<ElasticClientCompositeAggregate> Result { get => throw new NotImplementedException("Use Results instead"); set => throw new NotImplementedException("Use Results instead"); }
        public UIPagingInfo UIPagingInfo { get; set; } = new UIPagingInfo();

        private EsClientBucketResponse() { }

        // // NOT BEING USED ??
        //internal static IElasticClientResponse<ElasticClientCompositeAggregate> BuildBucketResponse(bool isSuccessful, CompositeBucketAggregate agg)
        //{
        //    // At least Es's aggregate result structure is easy to understand, right? RIGHT?!
        //    // The following is based on experimentation because there seems to be no documentation on aggregate results
        //    // Currently we only support one bucket aggregate using this method - more structure would be needed to support multiples
        //    var results = new List<ElasticClientCompositeAggregate>();

        //    if (agg?.Buckets.Any() ?? false)
        //    {
        //        foreach (var b in agg.Buckets)
        //        {
        //            var key = string.Join("|", b.Key.Values.Select(v => v.ToString().Replace("|", "{P}")));
        //            var aggs = new Dictionary<string, double?>();
        //            foreach (var kvp in b)
        //            {
        //                aggs.Add(kvp.Key, b.ValueCount(kvp.Key).Value);
        //            }
        //            var result = new ElasticClientCompositeAggregate { BucketKey = key, DocCount = b.DocCount, Aggregates = aggs };
        //            results.Add(result);
        //        }
        //    }

        //    if (results.Count == 0)
        //    {
        //        return new EsClientBucketResponse { IsSuccessful = true };
        //    }

        //    return new EsClientBucketResponse
        //    {
        //        IsSuccessful = isSuccessful,
        //        Results = results.Select(r => EsClientResult<ElasticClientCompositeAggregate>.From(r)),
        //        PitPagingInfo = new PitPagingInfo { AggregateKeys = agg?.AfterKey.ToDictionary(k => k.Key, v => v.Value) }
        //    };
        //}
    }

    public class EsClientResponse<T> : EsClientResponse, IElasticClientResponse<T> where T : class
    {
        public IEnumerable<IElasticClientDto<T>> Results { get; set; }
        public IElasticClientDto<T> Result { get; set; }
        public PitPagingInfo PitPagingInfo { get; set; }
        public UIPagingInfo UIPagingInfo { get; set; }

        private EsClientResponse()
        {
        }

        //internal static IElasticClientResponse<ElasticClientCompositeAggregate> BuildResponseBucketAgg(bool isSuccessful, AggregateDictionary aggs)
        //{
        //    // At least Es's aggregate result structure is easy to understand, right? RIGHT?!
        //    // The following is based on experimentation because there seems to be no documentation on aggregate results
        //    // Currently we only support one bucket aggregate using this method - more structure would be needed to support multiples
        //    // This is at least partially written to support multiples, but has not yet been tested for that use case
        //    var results = new List<ElasticClientCompositeAggregate>();
        //    foreach (var a in aggs)
        //    {
        //        // Assumes bucket aggregates - this won't go well otherwise
        //        foreach (var i in (a.Value as BucketAggregate)?.Items)s
        //        {
        //            var bagg = i as KeyedBucket<object>;
        //            var result = new ElasticClientCompositeAggregate { BucketKey = bagg?.Key.ToString(), DocCount = bagg?.DocCount ?? 0 };

        //            foreach (var v in bagg)
        //            {
        //                result.Aggregates.Add(v.Key, (v.Value as ValueAggregate)?.Value);
        //            }
        //            results.Add(result);
        //        }
        //    }
        //    return new EsClientResponse<ElasticClientCompositeAggregate>
        //    {
        //        IsSuccessful = isSuccessful,
        //        Results = results.Select(r => EsClientResult<ElasticClientCompositeAggregate>.From(r))
        //    };
        //}

        private static IElasticClientResponse<T> SingleItemResponse(UpdateResponse<T> response, T doc)
        {
            var r = BaseResponse(response);

            if (r.IsSuccessful)
            {
                r.Result = EsClientResult<T>.From(doc, response);
                r.CountAffected = 1;
            }

            return r;
        }

        private static IElasticClientResponse<T> SingleItemResponse(IndexResponse response, T doc)
        {
            var r = BaseResponse(response);

            if (r.IsSuccessful)
            {
                r.Result = EsClientResult<T>.From(doc, response);
                r.CountAffected = 1;
            }

            return r;
        }

        private static IElasticClientResponse<T> SingleItemResponse(DeleteResponse response, T doc)
        {
            var r = BaseResponse(response);

            if (r.IsSuccessful)
            {
                r.Result = EsClientResult<T>.From(doc, response);
                r.CountAffected = 1;
            }

            return r;
        }

        private static IElasticClientResponse<T> SingleItemResponse(GetResponse<T> response)
        {
            var r = BaseResponse(response);

            if (r.IsSuccessful)
            {
                r.Result = EsClientResult<T>.From(response);
                r.CountAffected = 1;
            }

            return r;
        }

        internal static EsClientResponse<T> BaseResponse(ElasticsearchResponse response, bool skipResponseMessage = false)
        {
            var msg = "";
            var success = response.IsValidResponse;

            if (!response.IsValidResponse)
            {
                if (!skipResponseMessage)
                {
                    msg = $"Invalid response ({response.ApiCallDetails.HttpStatusCode})";
                }
                else
                {
                    success = true;
                }
            }

            if (response.ApiCallDetails.HttpStatusCode == 404)
            {
                msg = "Not found (404)";
            }

            if (response.ApiCallDetails.HttpStatusCode == 400)
            {
                msg = "Invalid request (400)";
            }

            return new EsClientResponse<T>
            {
                Message = msg,
                IsSuccessful = success,
                HttpStatus = response.ApiCallDetails.HttpStatusCode ?? 0,
            };
        }

        internal static IElasticClientResponse<T> BuildResponse(T doc, UpdateResponse<T> response) => SingleItemResponse(response, doc);

        internal static IElasticClientResponse<T> BuildResponse(T doc, IndexResponse response) => SingleItemResponse(response, doc);

        internal static IElasticClientResponse<T> BuildResponse(IndexResponse response) => SingleItemResponse(response, null);

        internal static IElasticClientResponse<T> BuildResponse(GetResponse<T> response) => SingleItemResponse(response);

        internal static IElasticClientResponse<T> BuildResponse(SearchResponse<T> response, UIPagingInfo pagingInfo, int? total, bool skipResponseMessage = false)
        {
            var msg = "";
            var success = response.IsValidResponse;

            if (!response.IsValidResponse)
            {
                if (!skipResponseMessage)
                {
                    msg = $"Invalid response ({response.ApiCallDetails.HttpStatusCode})";
                }
                else
                {
                    success = true;
                }
            }

            return new EsClientResponse<T>
            {
                Message = msg,
                IsSuccessful = success,
                CountAffected = success ? response.Hits.Count : 0,
                HttpStatus = response.ApiCallDetails.HttpStatusCode ?? 0,
                Results = response.Hits.Select(h => EsClientResult<T>.From(h)),
                AfterKeys = response.Hits.LastOrDefault()?.Sort?.Cast<object>().ToList(),
                PitPagingInfo = null,
                UIPagingInfo = new UIPagingInfo
                {
                    Page = pagingInfo.Page,
                    Total = total ?? 0,
                    Size = pagingInfo.Size,
                    SortFilters = pagingInfo.SortFilters
                }
            };
        }

        internal static IElasticClientResponse<T> BuildResponse(SearchResponse<T> response, PitPagingInfo pagingInfo, int? total, bool skipResponseMessage = false)
        {
            var msg = "";
            var success = response.IsValidResponse;

            if (!response.IsValidResponse)
            {
                if (!skipResponseMessage)
                {
                    msg = $"Invalid response ({response.ApiCallDetails.HttpStatusCode})";
                }
                else
                {
                    success = true;
                }
            }

            return new EsClientResponse<T>
            {
                Message = msg,
                IsSuccessful = success,
                CountAffected = success ? response.Hits.Count : 0,
                HttpStatus = response.ApiCallDetails.HttpStatusCode ?? 0,
                Results = response.Hits.Select(h => EsClientResult<T>.From(h)),
                AfterKeys = response.Hits.LastOrDefault()?.Sort?.Cast<object>().ToList(),
                PitPagingInfo = new PitPagingInfo
                {
                    Total = total ?? 0,
                    Enabled = pagingInfo.Enabled,
                    PagingToken = pagingInfo.Enabled ? response.PitId : null,
                    Size = pagingInfo.Size,
                    SortFilters = pagingInfo.SortFilters
                },
                UIPagingInfo = null
            };
        }

        internal static IElasticClientResponse<T> BuildResponse(bool isSuccessful, long countAffected)
        {
            return BuildResponse(isSuccessful, (string)null, countAffected);
        }

        internal static IElasticClientResponse<T> BuildResponse(bool isSuccessful, List<string> messages, long countAffected)
        {
            return BuildResponse(isSuccessful, messages, countAffected);
        }

        internal static IElasticClientResponse<T> BuildResponse(bool isSuccessful, string message)
        {
            return BuildResponse(isSuccessful, message, 0);
        }

        internal static new IElasticClientResponse<T> BuildResponse(bool isSuccessful, string message, long countAffected)
        {
            return new EsClientResponse<T>()
            {
                IsSuccessful = isSuccessful,
                CountAffected = countAffected,
                Message = message
            };
        }

        internal static IElasticClientResponse<T2> BuildBucketResponse<T1, T2>(SearchResponse<T1> response, T2 results, IDataRepositoryPitPagingInfo pagingInfo, bool skipResponseMessage = false) where T1 : SaltMinerEntity where T2 : class
        {
            var msg = "";
            var success = response.IsValidResponse;

            if (!response.IsValidResponse)
            {
                if (!skipResponseMessage)
                {
                    msg = $"Invalid response ({response.ApiCallDetails.HttpStatusCode})";
                }
                else
                {
                    success = true;
                }
            }

            return new EsClientResponse<T2>
            {
                Message = msg,
                IsSuccessful = success,
                CountAffected = success ? response.Hits.Count : 0,
                HttpStatus = response.ApiCallDetails.HttpStatusCode ?? 0,
                Results = new List<IElasticClientDto<T2>> { new EsClientResult<T2> { Document = results } },
                AfterKeys = response.Hits.LastOrDefault()?.Sort?.Cast<object>().ToList(),
                PitPagingInfo = new PitPagingInfo
                {
                    Total = (int)response.Total > 0 ? (int)response.Total : 0,
                    Enabled = pagingInfo.Enabled,
                    PagingToken = pagingInfo.Enabled ? response.PitId : null,
                    Size = pagingInfo.Size
                }
            };
        }
    }
}
