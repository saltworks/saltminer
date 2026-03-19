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

﻿using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.Core.Entities
{
    [Serializable]
    public class SearchFilter : SaltMinerEntity
    {
        private static string _indexEntity = "sys_search_filters";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        /// <summary>
        /// Gets or sets Type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets Filters
        /// </summary>
        /// <seealso cref="SearchFilterValue"/>
        public List<SearchFilterValue> Filters { get; set; } = new();
    }

    public class SearchFilterValue
    {
        /// <summary>
        /// Gets or sets Display.
        /// </summary>
        public string Display { get; set; }

        /// <summary>
        /// Gets or sets Field.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets IsTextSearch.
        /// </summary>
        public bool IsTextSearch { get; set; }

        /// <summary>
        /// Gets or sets Order.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets IndexFieldNames. Field name(s) to search in main index.
        /// </summary>
        public List<string> IndexFieldNames { get; set; } = new();

        /// <summary>
        /// Gets or sets QueueIndexFieldNames.  Field name(s) to search in related queue index.
        /// </summary>
        public List<string> QueueIndexFieldNames { get; set; } = new();
    }
}