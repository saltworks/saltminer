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
