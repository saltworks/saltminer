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

using Saltworks.SaltMiner.Core.Common;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.Core.Data
{
    /// <summary>
    /// Use to request an operation on a single entity or by its ID (don't have to pass both)
    /// </summary>
    public class DataItemRequest<T> where T : SaltMinerEntity
    {
        public string Id { get; set; }
        public T Entity { get; set; }
    }

    /// <summary>
    /// Use to request an operation over multiple queue entities
    /// </summary>
    public class QueueDataRequest
    {
        public IEnumerable<QueueScan> QueueScans { get; set; }
        public IEnumerable<QueueAsset> QueueAssets { get; set; }
        public IEnumerable<QueueIssue> QueueIssues { get; set; }
    }
    
    public class JsonDataRequest
    {
        public IEnumerable<JsonObject> Documents { get; set; }
        public string TypeName { get; set; }
    }

    /// <summary>
    /// Use to request an operation over multiple entities
    /// </summary>
    public class DataRequest<T> where T : SaltMinerEntity
    {
        public IEnumerable<T> Documents { get; set; }
    }

    /// <summary>
    /// Use to request an operation over multiple entities, supporting concurrency info for "locking" operations
    /// </summary>
    public class DataDtoRequest<T> where T : SaltMinerEntity
    {
        public IEnumerable<DataDto<T>> Documents { get; set; }
    }

    /// <summary>
    /// Use to request an update by query
    /// </summary>
    public class UpdateQueryRequest<T> where T : SaltMinerEntity
    {
        /// <summary>
        /// Required for non-queue requests
        /// </summary>
        public string AssetType { get; set; }

        /// <summary>
        /// Required for non-queue requests
        /// </summary>
        public string SourceType { get; set; }

        /// <summary>
        /// Optional: Used to determine affected records
        /// </summary>
        public Filter Filter { get; set; }

        /// <summary>
        /// Required: This represents a KVP of fields
        /// Dictonary of objectfield/newdata i.e. name/Asset.Name 
        /// </summary>
        [JsonConverter(typeof(DictionaryStringObjectJsonConverter))]
        public Dictionary<string, object> ScriptUpdates { get; set; }
    }


    public class Filter
    {
        /// <summary>
        /// Currently has no affect, all matches are used with AND
        /// </summary>
        public bool AnyMatch { get; set; }

        /// <summary>
        /// <para>Dictionary of field/value pairs to use for searching with logical AND</para>
        /// <para>Supports DataRange, TermRange, MustNot Exists, and Wildcard Queries</para>
        /// <para>See Add*FilterMatch methods</para>
        /// <para>WildCard - Use '*' where you want to place the wildcard</para>
        /// </summary>
        public Dictionary<string, string> FilterMatches { get; set; } = [];

        /// <summary>
        /// Sub Queries 
        /// </summary>
        public Filter SubFilter { get; set; }

        public void RemoveFilterMatchByField(string field)
        {
            if (FilterMatches.TryGetValue(field, out _))
                FilterMatches.Remove(field);
        }


        public void AddSimpleFilterMatch(string field, string value, bool exceptionOnTryFail = false)
        {
            FilterMatches.TryAdd(field, value);
        }

        public void AddDateRangeFilterMatch(string field, DateTime greaterThanOrEqual, DateTime lessThan, bool exceptionOnTryFail = false)
        {
            var didIt = FilterMatches.TryAdd(field, $"{greaterThanOrEqual:yyyy-mm-dd}||{lessThan:yyyy-mm-dd}");
            if (exceptionOnTryFail && !didIt)
                throw new SaltMinerValidationException($"Field '{field}' already added to filters.");
        }

        public void AddGreaterThanOrEqualFilterMatch(string field, string value, bool exceptionOnTryFail = false)
        {
            var didIt = FilterMatches.TryAdd(field, $"{value}>=||");
            if (exceptionOnTryFail && !didIt)
                throw new SaltMinerValidationException($"Field '{field}' already added to filters.");
        }

        public void AddRangeOrEqualFilterMatch(string field, string gteValue, string lteValue, bool exceptionOnTryFail = false)
        {
            var didIt = FilterMatches.TryAdd(field, $"{gteValue}>=||{lteValue}<=||");
            if (exceptionOnTryFail && !didIt)
                throw new SaltMinerValidationException($"Field '{field}' already added to filters.");
        }

        public void AddRangeFilterMatch(string field, string gtValue, string ltValue, bool exceptionOnTryFail = false)
        {
            var didIt = FilterMatches.TryAdd(field, $"{gtValue}>||{ltValue}<||");
            if (exceptionOnTryFail && !didIt)
                throw new SaltMinerValidationException($"Field '{field}' already added to filters.");
        }

        public void AddLessThanOrEqualFilterMatch(string field, string value, bool exceptionOnTryFail = false)
        {
            var didIt = FilterMatches.TryAdd(field, $"{value}<=||");
            if (exceptionOnTryFail && !didIt)
                throw new SaltMinerValidationException($"Field '{field}' already added to filters.");
        }

        public void AddGreaterThanFilterMatch(string field, string value, bool exceptionOnTryFail = false)
        {
            var didIt = FilterMatches.TryAdd(field, $"{value}>||");
            if (exceptionOnTryFail && !didIt)
                throw new SaltMinerValidationException($"Field '{field}' already added to filters.");
        }

        public void AddLessThanFilterMatch(string field, string value, bool exceptionOnTryFail = false)
        {
            var didIt = FilterMatches.TryAdd(field, $"{value}<||");
            if (exceptionOnTryFail && !didIt)
                throw new SaltMinerValidationException($"Field '{field}' already added to filters.");
        }

        public void AddQueryStringFilterMatch(string field, string value, bool exceptionOnTryFail = false)
        {
            var didIt = FilterMatches.TryAdd(field, $"{value}**");
            if (exceptionOnTryFail && !didIt)
                throw new SaltMinerValidationException($"Field '{field}' already added to filters.");
        }

        public void AddMustNotExistsFilterMatch(string field, bool exceptionOnTryFail = false)
        {
            var didIt = FilterMatches.TryAdd(field, "!");
            if (exceptionOnTryFail && !didIt)
                throw new SaltMinerValidationException($"Field '{field}' already added to filters.");
        }

        public void AddMustExistsFilterMatch(string field, bool exceptionOnTryFail = false)
        {
            var didIt = FilterMatches.TryAdd(field, "+!");
            if (exceptionOnTryFail && !didIt)
                throw new SaltMinerValidationException($"Field '{field}' already added to filters.");
        }

        public void AddTermsFilterMatch(string field, List<string> values, bool exceptionOnTryFail = false)
        {
            var didIt = FilterMatches.TryAdd(field, string.Join("||+", values.ToArray()));
            if (exceptionOnTryFail && !didIt)
                throw new SaltMinerValidationException($"Field '{field}' already added to filters.");
        }

        public void AddExcludeTermsFilterMatch(string field, List<string> values, bool exceptionOnTryFail=false)
        {
            bool didIt;
            if (values.Count == 1)
            {
                didIt = FilterMatches.TryAdd(field, $"||~{values[0]}");
            }
            didIt = FilterMatches.TryAdd(field, string.Join("||~", values.ToArray()));
            if (exceptionOnTryFail && !didIt)
                throw new SaltMinerValidationException($"Field '{field}' already added to filters.");
        }

    }

    public abstract class SearchRequestBase
    {
        protected SearchRequestBase() { }
        protected SearchRequestBase(PagingInfo paging)
        {
            PagingInfo = paging;
        }
        protected SearchRequestBase(string filterField, string filterValue, int size = 0)
        {
            Filter = new();
            Filter.AddSimpleFilterMatch(filterField, filterValue);
            if (size > 0)
                PagingInfo = new() { Size = size };
        }

        protected SearchRequestBase(string filterField, string filterValue, PagingInfo pagingInfo)
        {
            Filter = new();
            Filter.AddSimpleFilterMatch(filterField, filterValue);
            PagingInfo = pagingInfo;
        }

        /// <summary>
        /// Filter(s) to apply to search
        /// </summary>
        public Filter Filter { get; set; } = new();

        /// <summary>
        /// Dictionary&lt;sort field, is ascending&gt; for sorting
        /// </summary>
        public Dictionary<string, bool> SortKeys { get; set; }

        /// <summary>
        /// Pagination Information
        /// </summary>
        public PagingInfo PagingInfo { get; set; } = null;

        /// <summary>
        /// If set, includes concurrency information in results (sequence num, primary term)
        /// </summary>
        public bool IncludeConcurrencyInfo { get; set; } = false;
    }
    
    /// <summary>
    /// Used to perform a search (or continue it)
    /// </summary>
    public class SearchRequest : SearchRequestBase
    {
        public SearchRequest() { }
        public SearchRequest(PagingInfo paging) : base(paging) {}
        public SearchRequest(string filterField, string filterValue, int size = 0) : base(filterField, filterValue, size) {}
        public SearchRequest(string filterField, string filterValue, PagingInfo pagingInfo) : base(filterField, filterValue, pagingInfo) {}

        /// <summary>
        /// Possible filter by asset type
        /// </summary>
        public string AssetType { get; set; }
        /// <summary>
        /// Possible filter by instance
        /// </summary>
        public string Instance { get; set; }
        /// <summary>
        /// Possible filter by asset type
        /// </summary>
        public string SourceType { get; set; }
    }

    public class JsonSearchRequest : SearchRequestBase
    {
        public JsonSearchRequest() { }
        public JsonSearchRequest(PagingInfo paging) : base(paging) { }
        public JsonSearchRequest(string filterField, string filterValue, int size = 0) : base(filterField, filterValue, size) { }
        public JsonSearchRequest(string filterField, string filterValue, PagingInfo pagingInfo) : base(filterField, filterValue, pagingInfo) { }

        public SearchRequest ToSearchRequest()
        {
            return new SearchRequest
            {
                Filter = Filter,
                SortKeys = SortKeys,
                PagingInfo = PagingInfo,
                IncludeConcurrencyInfo = IncludeConcurrencyInfo
            };
        }

        /// <summary>
        /// The entity type to return from the search
        /// </summary>
        public string TypeName { get; set; }
    }
}
