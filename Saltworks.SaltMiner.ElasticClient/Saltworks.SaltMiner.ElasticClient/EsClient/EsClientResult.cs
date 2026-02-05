using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;

namespace Saltworks.SaltMiner.ElasticClient.EsClient;

public class EsClientResult<T> : IElasticClientDto<T> where T : class
{
    public T Document { get; set; }
    public long? Primary { get; set; }
    public long? Sequence { get; set; }
    public string Index { get; set; }

    internal static IElasticClientDto<T> From(T doc, string index, long? primary = null, long? seq = null)
    {
        return new EsClientResult<T> { Document = doc, Index = index, Primary = primary, Sequence = seq };
    }

    internal static IElasticClientDto<T> From(T doc, long? primary = null, long? seq = null)
    {
        return new EsClientResult<T> { Document = doc, Primary = primary, Sequence = seq };
    }

    internal static IElasticClientDto<T> From(Hit<T> doc)
    {
        return new EsClientResult<T> { Document = doc.Source, Primary = doc.PrimaryTerm, Sequence = doc.SeqNo, Index = doc.Index };
    }

    internal static IElasticClientDto<T> From(T doc, IndexResponse r)
    {
        return new EsClientResult<T> { Document = doc, Primary = r.PrimaryTerm, Sequence = r.SeqNo, Index = r.Index };
    }

    internal static IElasticClientDto<T> From(T doc, UpdateResponse<T> r)
    {
        return new EsClientResult<T> { Document = doc, Primary = r.PrimaryTerm, Sequence = r.SeqNo, Index = r.Index };
    }

    internal static IElasticClientDto<T> From(T doc, DeleteResponse r)
    {
        return new EsClientResult<T> { Document = doc, Primary = r.PrimaryTerm, Sequence = r.SeqNo, Index = r.Index };
    }

    internal static IElasticClientDto<T> From(GetResponse<T> r)
    {
        return new EsClientResult<T> { Document = r.Source, Primary = r.PrimaryTerm, Sequence = r.SeqNo, Index = r.Index };
    }
}
