/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
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
