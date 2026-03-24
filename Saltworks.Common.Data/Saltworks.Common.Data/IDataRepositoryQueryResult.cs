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
    public interface IDataRepositoryQueryResult<out T> where T : class
    {
        /// <summary>
        /// Information object used to facilitate multiple "paged" calls to the same query
        /// </summary>
        IDataRepositoryPitPagingInfo PitPagingInfo { get; }
        /// <summary>
        /// Information object used to facilitate multiple "paged" calls to the same query
        /// </summary>
        IDataRepositoryUIPagingInfo UIPagingInfo { get; }
        /// <summary>
        /// Result set produced by the query
        /// </summary>
        IEnumerable<T> Results { get; }
    }
}
