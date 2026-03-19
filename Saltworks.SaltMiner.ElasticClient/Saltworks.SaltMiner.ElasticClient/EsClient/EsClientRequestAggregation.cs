/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
*
* ----
*/

﻿using System.Collections.Generic;

namespace Saltworks.SaltMiner.ElasticClient.EsClient;

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
