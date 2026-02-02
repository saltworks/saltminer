using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.Nodes;
using Elastic.Transport.Products.Elasticsearch;
using Saltworks.Common.Data;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Saltworks.SaltMiner.ElasticClient.EsClient;

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

public class EsClientAggregateResponse : EsClientResponse, IElasticClientAggregateResponse
{
    public AggregateDictionary Aggregations { get; set; }
}

public class EsClientBucketResponse : EsClientResponse, IElasticClientResponse<ElasticClientCompositeAggregate>
{
    public IEnumerable<IElasticClientDto<ElasticClientCompositeAggregate>> Results { get; set; }
    public IElasticClientDto<ElasticClientCompositeAggregate> Result { get; set; }
    public PagingInfo PagingInfo { get; set; }

    internal static IElasticClientResponse<ElasticClientCompositeAggregate> BuildBucketResponse(bool isSuccessful, CompositeAggregate agg)
    {
        throw new NotImplementedException("Composite aggregates are not yet implemented.");
        // Composite aggregate result structure processing
        // var results = new List<ElasticClientCompositeAggregate>();

        // if (agg?.Buckets?.Count > 0)
        // {
        //     foreach (var b in agg.Buckets)
        //     {
        //         var key = string.Join("|", b.Key.Select(kvp => kvp.Value?.ToString()?.Replace("|", "{P}") ?? ""));
        //         var aggs = new Dictionary<string, double?>();
                
        //         foreach (var kvp in b.Aggregations ?? new AggregateDictionary())
        //         {
        //             if (kvp.Value is ValueAggregate valueAgg)
        //             {
        //                 aggs.Add(kvp.Key, valueAgg.Value);
        //             }
        //         }
                
        //         var result = new ElasticClientCompositeAggregate 
        //         { 
        //             BucketKey = key, 
        //             DocCount = b.DocCount ?? 0, 
        //             Aggregates = aggs 
        //         };
        //         results.Add(result);
        //     }
        // }

        // if (results.Count == 0)
        // {
        //     return new EsClientBucketResponse { IsSuccessful = true };
        // }

        // return new EsClientBucketResponse
        // {
        //     IsSuccessful = isSuccessful,
        //     Results = results.Select(r => EsClientResult<ElasticClientCompositeAggregate>.From(r)),
        //     PagingInfo = new PagingInfo 
        //     { 
        //         AggregateKeys = agg?.AfterKey?.ToDictionary(k => k.Key, v => v.Value) 
        //     }
        // };
    }

    internal static IElasticClientAggregateResponse BuildResponseBucketAgg(bool isSuccessful, AggregateDictionary aggs)
    {
        // Bucket aggregate result structure processing
        return new EsClientAggregateResponse
        {
            IsSuccessful = isSuccessful,
            Aggregations = aggs
        };
    }
}

public class EsClientResponse<T> : EsClientResponse, IElasticClientResponse<T> where T : class
{
    public IEnumerable<IElasticClientDto<T>> Results { get; set; }
    public IElasticClientDto<T> Result { get; set; }
    public PagingInfo PagingInfo { get; set; }

    private EsClientResponse()
    {
    }

    private static EsClientResponse<T> SingleItemResponse(UpdateResponse<T> response, T doc)
    {
        var r = BaseResponse(response);

        if (r.IsSuccessful)
        {
            r.Result = EsClientResult<T>.From(doc, response);
            r.CountAffected = 1;
        }

        return r;
    }

    private static EsClientResponse<T> SingleItemResponse(IndexResponse response, T doc)
    {
        var r = BaseResponse(response);

        if (r.IsSuccessful)
        {
            r.Result = EsClientResult<T>.From(doc, response);
            r.CountAffected = 1;
        }

        return r;
    }

    private static EsClientResponse<T> SingleItemResponse(GetResponse<T> response)
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
                msg = $"Invalid response ({response.ApiCallDetails.HttpStatusCode})";
            else
                success = true;
        }

        if (response.ApiCallDetails.HttpStatusCode == 404)
            msg = "Not found (404)";

        if (response.ApiCallDetails.HttpStatusCode == 400)
            msg = "Invalid request (400)";

        return new EsClientResponse<T>
        {
            Message = msg,
            IsSuccessful = success,
            HttpStatus = response.ApiCallDetails.HttpStatusCode ?? 0,
        };
    }

    private static IElasticClientDto<JsonObject> ToJsonObjectDto<TEntity>(IElasticClientDto<TEntity> dto) where TEntity : SaltMinerEntity
    {
        if (dto == null) return null;
        var jsonDoc = JsonSerializer.SerializeToNode(dto.Document, JsonSerializerOptions.Web).AsObject();
        return EsClientResult<JsonObject>.From(jsonDoc, dto.Index, dto.Primary, dto.Sequence);
    }
    
    internal static IElasticClientResponse<JsonObject> BuildResponse(IElasticClientResponse<SaltMinerEntity> response)
    {
        return new EsClientResponse<JsonObject> {
            Results = response.Results?.Select(d => ToJsonObjectDto<SaltMinerEntity>(d)).ToList() ?? new List<IElasticClientDto<JsonObject>>(),
            Result = response.Result != null ? ToJsonObjectDto<SaltMinerEntity>(response.Result) : null,
            IsSuccessful = response.IsSuccessful,
            Message = response.Message,
            CountAffected = response.CountAffected,
            PagingInfo = response.PagingInfo
        };
    }

    internal static IElasticClientResponse<JsonObject> BuildResponse<TEntity>(IElasticClientResponse<TEntity> response) where TEntity : SaltMinerEntity
    {
        return new EsClientResponse<JsonObject> {
            Results = response.Results?.Select(dto => ToJsonObjectDto<TEntity>(dto)).ToList() ?? new List<IElasticClientDto<JsonObject>>(),
            Result = response.Result != null ? ToJsonObjectDto<TEntity>(response.Result) : null,
            IsSuccessful = response.IsSuccessful,
            Message = response.Message,
            CountAffected = response.CountAffected,
            PagingInfo = response.PagingInfo
        };
    }
    internal static IElasticClientResponse<T> BuildResponse(T doc, UpdateResponse<T> response) => SingleItemResponse(response, doc);

    internal static IElasticClientResponse<T> BuildResponse(T doc, IndexResponse response) => SingleItemResponse(response, doc);

    internal static IElasticClientResponse<T> BuildResponse(IndexResponse response) => SingleItemResponse(response, null);

    internal static IElasticClientResponse<T> BuildResponse(GetResponse<T> response) => SingleItemResponse(response);

    internal static IElasticClientResponse<T> BuildResponse(SearchResponse<T> response, PagingInfo pagingInfo, bool skipResponseMessage = false)
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

        var rsp = new EsClientResponse<T>
        {
            Message = msg,
            IsSuccessful = success,
            CountAffected = success ? response.Hits.Count : 0,
            HttpStatus = response.ApiCallDetails.HttpStatusCode ?? 0,
            Results = response.Hits.Select(h => EsClientResult<T>.From(h)),
            AfterKeys = response.Hits.LastOrDefault()?.Sort?.Cast<object>().ToList(),
            PagingInfo = pagingInfo
        };
        rsp.PagingInfo.CurrentAfterKeys = pagingInfo.NextAfterKeys;
        // Only set NextAfterKeys if we got a full page of results (more results may exist)
        // If we got fewer results than requested, we're at the end, so set to null
        var hasMoreResults = response.Hits.Count >= pagingInfo.Size;
        rsp.PagingInfo.NextAfterKeys = hasMoreResults ? response.Hits.LastOrDefault()?.Sort?.Cast<object>().ToList() : null;
        rsp.PagingInfo.PitPagingToken = response.PitId;
        if (string.IsNullOrEmpty(rsp.PagingInfo.PitPagingToken) && pagingInfo.EnablePit)
            rsp.PagingInfo.EnablePit = false;
        return rsp;
    }

    internal static IElasticClientResponse<T> BuildResponse(bool isSuccessful, long countAffected)
    {
        return BuildResponse(isSuccessful, (string)null, countAffected);
    }

    internal static IElasticClientResponse<T> BuildResponse(bool isSuccessful, List<string> messages, long countAffected)
    {
        return BuildResponse(isSuccessful, string.Join("; ", messages), countAffected);
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
                msg = $"Invalid response ({response.ApiCallDetails.HttpStatusCode})";
            else
                success = true;
        }
        return new EsClientResponse<T2>
        {
            Message = msg,
            IsSuccessful = success,
            CountAffected = success ? response.Hits.Count : 0,
            HttpStatus = response.ApiCallDetails.HttpStatusCode ?? 0,
            Results = [new EsClientResult<T2> { Document = results }],
            AfterKeys = response.Hits.LastOrDefault()?.Sort?.Cast<object>().ToList(),
            PagingInfo = new()
            {
                TotalHits = pagingInfo.Total,
                PitPagingToken = pagingInfo.PagingToken,
                CurrentAfterKeys = pagingInfo.AfterKeys.ToList(),
                AggregateKeys = pagingInfo.AggregateKeys,
                EnablePit = true,
                Size = pagingInfo.Size
            }
        };
    }
}
