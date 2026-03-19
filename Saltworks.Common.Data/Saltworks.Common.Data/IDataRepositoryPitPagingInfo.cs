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
    public interface IDataRepositoryPitPagingInfo
    {
        /// <summary>
        /// Total documents found
        /// </summary>
        int? Total { get; set; }
        /// <summary>
        /// Size of current (or next) resultset, if supported by provider
        /// </summary>
        int? Size { get; set; }
        /// <summary>
        /// Implementation-specific data used to re-call a previous query
        /// </summary>
        string PagingToken { get; set; }
        /// <summary>
        /// If supported by provider, flag indicating whether or not to enable pagination
        /// </summary>
        bool Enabled { get; set; }
        /// <summary>
        /// If supported by provider, dictionary of keys used to produce the next aggregate result from a previous aggregate query
        /// </summary>
        Dictionary<string, object> AggregateKeys { get; set; }
        Dictionary<string, bool> SortFilters { get; set; }
        /// <summary>
        /// If supported by provider, list of sort values after which the next result set should be produced
        /// </summary>
        IList<object> AfterKeys { get; set; }
    }
}
