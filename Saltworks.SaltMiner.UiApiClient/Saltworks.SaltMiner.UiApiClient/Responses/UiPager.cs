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

﻿using Saltworks.SaltMiner.Core.Data;

namespace Saltworks.SaltMiner.UiApiClient.Responses
{
    public class UiPager
    {
        public int Size { get; set; }
        public int Page { get; set; }
        public int? Total { get; set; }
        public int? TotalPages => (Size != 0 && Total != null) ? ((Total.Value - 1) / Size) + 1 : null;

        public Dictionary<string, bool> SortFilters { get; set; } = [];

        public UiPager()
        {
        }

        /// <summary>
        /// Request constructor
        /// </summary>
        public UiPager(int size, int page)
        {
            Size = size;
            Page = page;
            SortFilters = [];
        }

        /// <summary>
        /// Response constructor
        /// </summary>
        public UiPager(PagingInfo dataPager, Dictionary<string, bool> sortFilters = null)
        {
            Total = Convert.ToInt32(dataPager?.TotalHits);
            Size = dataPager?.Size ?? 0;
            Page = dataPager.Page;
            SortFilters = sortFilters ?? [];
        }

        public PagingInfo ToPagingInfo() => new()
        {
            Page = Page,
            Size = Size
        };
    }
}
