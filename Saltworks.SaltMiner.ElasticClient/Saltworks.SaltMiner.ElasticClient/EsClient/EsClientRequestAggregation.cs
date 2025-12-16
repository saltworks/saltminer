

using System.Collections.Generic;

namespace Saltworks.SaltMiner.ElasticClient.EsClient
{
    public class EsClientRequestAggregation : IElasticClientRequestAggregation
    {
        public string Name { get; set; }
        public string BucketField { get; set; }

        public IEnumerable<IElasticClientRequestAggregate> Aggregates { get; }
        public EsClientRequestAggregation(string name, string bucketField, IEnumerable<IElasticClientRequestAggregate> aggregates)
        {
            Name = name;
            BucketField = bucketField.ToSnakeCase();
            Aggregates = aggregates;
        }

        public class EsClientRequestAggregate : IElasticClientRequestAggregate
        {
            public string Name { get; set; }
            public string Field { get; set; }
            public ElasticAggregateType AggregateType { get; set; }
            public static IElasticClientRequestAggregate GetMax(string name, string field) => new EsClientRequestAggregate() { AggregateType = ElasticAggregateType.Max, Name = name, Field = field.ToSnakeCase() };
            public static IElasticClientRequestAggregate GetMin(string name, string field) => new EsClientRequestAggregate() { AggregateType = ElasticAggregateType.Min, Name = name, Field = field.ToSnakeCase() };
            public static IElasticClientRequestAggregate GetAverage(string name, string field) => new EsClientRequestAggregate() { AggregateType = ElasticAggregateType.Average, Name = name, Field = field.ToSnakeCase() };
            public static IElasticClientRequestAggregate GetCount(string name, string field) => new EsClientRequestAggregate() { AggregateType = ElasticAggregateType.Count, Name = name, Field = field.ToSnakeCase() };
            public static IElasticClientRequestAggregate GetSum(string name, string field) => new EsClientRequestAggregate() { AggregateType = ElasticAggregateType.Sum, Name = name, Field = field.ToSnakeCase() };
        }
    }
}
