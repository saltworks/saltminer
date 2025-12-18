using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Transport.Products.Elasticsearch;
using Saltworks.Common.Data;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.ElasticClient.EsClient;

public class EsOldClientResponse : IElasticClientResponse
{
    public bool IsSuccessful { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string> BulkErrorMessages { get; set; }
    public long CountAffected { get; set; }
    public int HttpStatus { get; set; }
    public IList<object> AfterKeys { get; set; }

    public static EsOldClientResponse BuildResponse(bool isSuccess, string message, long affected) => new()
    {
        CountAffected = affected,
        Message = message,
        IsSuccessful = isSuccess
    };
    public static EsOldClientResponse BuildResponse(bool isSuccess, Dictionary<string, string> bulkErrors, string message, long affected) => new()
    {
        CountAffected = affected,
        Message = message,
        BulkErrorMessages = bulkErrors,
        IsSuccessful = isSuccess
    };
}

public class EsOldClientBucketResponse : EsOldClientResponse, IElasticClientResponse<ElasticClientCompositeAggregate>
{
    [Obsolete("Use PagingInfo instead.")]
    public PitPagingInfo PitPagingInfo { get; set; }
    public IEnumerable<IElasticClientDto<ElasticClientCompositeAggregate>> Results { get; set; }
    public IElasticClientDto<ElasticClientCompositeAggregate> Result { get => throw new NotImplementedException("Use Results instead"); set => throw new NotImplementedException("Use Results instead"); }
    public PagingInfo PagingInfo { get; set; }

    public EsOldClientBucketResponse() { }
}

public class EsOldClientResponse<T> : EsOldClientResponse, IElasticClientResponse<T> where T : class
{
    public IEnumerable<IElasticClientDto<T>> Results { get; set; }
    public IElasticClientDto<T> Result { get; set; }
    [Obsolete("Use PagingInfo instead.")]
    public PitPagingInfo PitPagingInfo { get; set; }
    public PagingInfo PagingInfo { get; set; }

    public EsOldClientResponse()
    {
    }
}

