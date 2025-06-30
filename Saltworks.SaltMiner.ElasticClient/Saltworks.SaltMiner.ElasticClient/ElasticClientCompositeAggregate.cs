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

﻿using System.Collections.Generic;

namespace Saltworks.SaltMiner.ElasticClient
{
    // Only one type of aggregate currently supported
    public class ElasticClientCompositeAggregate
    {
        public string BucketKey { get; set; }
        public long? DocCount { get; set; }
        public Dictionary<string, double?> Aggregates { get; set; } = new();
    }
}
