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

namespace Saltworks.SaltMiner.Core.Data
{
    public class PitPagingInfo
    {
        public PitPagingInfo() { }

        public PitPagingInfo(int? size, bool enabled = false, Dictionary<string, bool> sortFilters = null)
        {
            Size = size;
            Enabled = enabled;
            SortFilters = sortFilters;
        }

        public PitPagingInfo(int? size, bool enabled, string token, Dictionary<string, bool> sortFilters = null)
        {
            Size = size;
            Enabled = enabled;
            PagingToken = token;
            SortFilters = sortFilters;
        }

        public PitPagingInfo(int? size, bool enabled, Dictionary<string, object> aggregateKeys, Dictionary<string, bool> sortFilters = null)
        {
            Size = size;
            Enabled = enabled;
            AggregateKeys = aggregateKeys;
            SortFilters = sortFilters;
        }

        public int? Total { get; set; }
        public int? Size { get; set; }
        public bool Enabled { get; set; } = false;
        public string PagingToken { get; set; } = null;
        public Dictionary<string, object> AggregateKeys { get; set; }
        public Dictionary<string, bool> SortFilters { get; set; }
    }

    public class UIPagingInfo
    {
        public UIPagingInfo() { }
        public UIPagingInfo(int size, int page = 1, Dictionary<string, bool> sortFilters = null)
        {
            Size = size;
            Page = page;
            SortFilters = sortFilters;
        }

        public int? Total { get; set; }
        public int Size { get; set; }
        public int Page { get; set; }
        public int? TotalPages => (Size != 0 && Total != null) ? (int) ((Total - 1) / Size) + 1 : null;
        public Dictionary<string, bool> SortFilters { get; set; }
    }
}
