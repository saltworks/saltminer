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
    public interface IElasticClientRequestAggregation
    {
        string Name { get; set; }
        string BucketField { get; set; }
        IEnumerable<IElasticClientRequestAggregate> Aggregates { get; }
    }

    public interface IElasticClientRequestAggregate
    {
        string Name { get; set; }
        string Field { get; set; }
        ElasticAggregateType AggregateType { get; set; }
    }

    public enum ElasticAggregateType
    {
        Average,
        Max,
        Min,
        Count,
        Sum
    }
}
