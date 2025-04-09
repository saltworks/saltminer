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

﻿using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;

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
        public UiPager(UIPagingInfo dataPager, List<SearchFilterValue> sortOptions, bool isQueue = false)
        {
            Total = dataPager?.Total;
            Size = dataPager.Size;
            Page = dataPager.Page;
            sortOptions ??= [];
            foreach (var sort in dataPager.SortFilters)
            {
                SearchFilterValue filter = null;
                if (isQueue)
                    filter = sortOptions.FirstOrDefault(x => x.QueueIndexFieldNames.Any(y => y.ToLower() == sort.Key.ToLower()));
                else
                    filter = sortOptions.FirstOrDefault(x => x.IndexFieldNames.Any(y => y.ToLower() == sort.Key.ToLower()));
                
                if (filter != null)
                    SortFilters.Add(filter.Field, sort.Value);
            }
        }

        public UIPagingInfo ToDataPager()
        {
            return new UIPagingInfo
            {
                Page = Page,
                Size = Size,
                SortFilters = SortFilters
            };
        }
    }
}
