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

namespace Saltworks.Common.Data
{
    /// <summary>
    /// Represents the result of an aggregation query
    /// </summary>
    public interface IDataRepositoryAggregateResult
    {
        /// <summary>
        /// The identifier for the aggregation result
        /// </summary>
        string Key { get; set; }
        /// <summary>
        /// Total count of documents retreived for this aggregation result
        /// </summary>
        long? DocCount { get; set; }
        /// <summary>
        /// Dictionary of keys and aggregation result values for this aggregation result
        /// </summary>
        Dictionary<string, double?> Results { get; set; }
        /// <summary>
        /// Paging information to support batching for this aggregation query (if implemented)
        /// </summary>
        IDataRepositoryPitPagingInfo PitPagingInfo { get; set; }
    }
}
