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

using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.Core.Data
{
    public class PagingInfo
    {
        public PagingInfo() { }
        public PagingInfo(int? size)
        {
            Size = size;
        }
        /// <summary>
        /// Current page number
        /// </summary>
        public int Page { get; set; } = 1;
        /// <summary>
        /// Total hits as reported by Elasticsearch
        /// </summary>
        public long? TotalHits { get; set; }
        /// <summary>
        /// Total pages of results, computed by math (TotalHits / Size), rounding any remainder up
        /// </summary>
        public long? TotalPages {
            get
            {
                if (TotalHits == null)
                    return 0;
                var res = TotalHits.Value / Size;
                if (TotalHits.Value % Size != 0)
                    return res + 1;
                return res;
            }
        }
        /// <summary>
        /// If false, a separate count query will be run to determine total hits if over max results (10k currently).  If true (default), allow max results for total hits without running the count query.
        /// </summary>
        public bool TotalHitsCanBeTruncated { get; set; } = true;
        /// <summary>
        /// Set by a successful query, if true, TotalHits represents max results from datastore without a count query, but more results exist.
        /// </summary>
        public bool TotalHitsWereTruncated { get; set; } = false;
        /// <summary>
        /// Page size - should not exceed 10,000.  If set to 0 or null, will use default from configuration.
        /// </summary>
        public int? Size { get; set; } = null;
        /// <summary>
        /// Enables Elasticsearch PIT (Point In Time) paging to ensure consistent results over multiple search request pages.
        /// </summary>
        public bool EnablePit { get; set; } = false;
        /// <summary>
        /// If using Elasticsearch PIT (Point In Time), the token representing the search context in Elasticsearch.
        /// </summary>
        public string PitPagingToken { get; set; } = null;
        /// <summary>
        /// Resume paging after these keys - speeds up page requests when available.
        /// </summary>
        public List<object> NextAfterKeys { get; set; }
        /// <summary>
        /// Keys used for current request - speeds up page requests when available.
        /// </summary>
        public List<object> CurrentAfterKeys { get; set; }
        /// <summary>
        /// Keys used for current aggregate request - speeds up page requests when available.
        /// </summary>
        public Dictionary<string, object> AggregateKeys { get; set; }
        /// <summary>
        /// Returns a PagingInfo object representing a next page request, just add to your SearchRequest and fire it off.
        /// </summary>
        public PagingInfo NextPage() => new()
        {
            Page = Page + 1,
            TotalHits = TotalHits,
            TotalHitsCanBeTruncated = TotalHitsCanBeTruncated,
            Size = Size,
            EnablePit = EnablePit,
            PitPagingToken = PitPagingToken,
            NextAfterKeys = [],
            CurrentAfterKeys = NextAfterKeys,
        };
    }
}
