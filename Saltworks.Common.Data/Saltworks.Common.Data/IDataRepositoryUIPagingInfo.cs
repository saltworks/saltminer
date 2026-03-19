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
    public interface IDataRepositoryUIPagingInfo
    {
        /// <summary>
        /// Total documents found
        /// </summary>
        int? Total { get; set; }
        /// <summary>
        /// Where to start next resultset in the list, if supported by provider
        /// </summary>
        int Page { get; set; }
        /// <summary>
        /// Size of current (or next) resultset, if supported by provider
        /// </summary>
        int Size { get; set; }

        int From { get; }

        int? TotalPages { get; }
        Dictionary<string, bool> SortFilters { get; set; }
    }
}
