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

﻿using System.Collections.Generic;

namespace Saltworks.SaltMiner.ElasticClient
{
    public class NestClientRequestAggregation : IElasticClientRequestAggregation
    {
        public string Name { get; set; }
        public string BucketField { get; set; }

        public IEnumerable<IElasticClientRequestAggregate> Aggregates { get; }
        public NestClientRequestAggregation(string name, string bucketField, IEnumerable<IElasticClientRequestAggregate> aggregates)
        {
            Name = name;
            BucketField = bucketField.ToSnakeCase();
            Aggregates = aggregates;
        }
    }

    public class NestClientRequestAggregate : IElasticClientRequestAggregate
    {
        public string Name { get; set; }
        public string Field { get; set; }
        public ElasticAggregateType AggregateType { get; set; }
        public static IElasticClientRequestAggregate GetMax(string name, string field) => new NestClientRequestAggregate() { AggregateType = ElasticAggregateType.Max, Name = name, Field = field.ToSnakeCase() };
        public static IElasticClientRequestAggregate GetMin(string name, string field) => new NestClientRequestAggregate() { AggregateType = ElasticAggregateType.Min, Name = name, Field = field.ToSnakeCase() };
        public static IElasticClientRequestAggregate GetAverage(string name, string field) => new NestClientRequestAggregate() { AggregateType = ElasticAggregateType.Average, Name = name, Field = field.ToSnakeCase() };
        public static IElasticClientRequestAggregate GetCount(string name, string field) => new NestClientRequestAggregate() { AggregateType = ElasticAggregateType.Count, Name = name, Field = field.ToSnakeCase() };
        public static IElasticClientRequestAggregate GetSum(string name, string field) => new NestClientRequestAggregate() { AggregateType = ElasticAggregateType.Sum, Name = name, Field = field.ToSnakeCase() };
    }
}
