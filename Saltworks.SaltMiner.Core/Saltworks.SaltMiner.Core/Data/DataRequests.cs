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

﻿using Org.BouncyCastle.Bcpg;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Extensions;
using System.Collections.Generic;
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
        /// <para>DateRange - To build query string value needed use the DataClient Helper BuildDateRangeFilterValue(DateTime greaterThanOrEqual, DateTime lessThan)</para>
        /// <para>TermRange - To build query string value needed use the DataClient Helpers BuildGreaterThanOrEqualFilterValue(string value), BuildLessThanOrEqualFilterValue(string value), BuildGreaterThanFilterValue(string value), BuildLessThanFilterValue(string value)</para>
        /// <para>MustNot Exists - To build query string value needed use the DataClient Helpers BuildMustNotExistsFilterValue()</para>
        /// <para>Must Exists - To build query string value needed use the DataClient Helpers BuildMustExistsFilterValue()</para>
        /// <para>Terms Query - To build query string value needed use the DataClient Helpers BuildTermsFilterValue(List<string> values)</string>)</para>
        /// <para>QueryString - To build query string value needed use the DataClient Helpers BuildQueryStringFilterValue(string value = "")</para>
        /// <para>WildCard - Use '*' where you want to place the wildcard</para>
        /// </summary>
        public Dictionary<string, string> FilterMatches { get; set; }

        /// <summary>
        /// Sub Quweries 
        /// </summary>
        public Filter SubFilter { get; set; }
    }

    /// <summary>
    /// Used to perform a search (or continue it)
    /// </summary>
    public class SearchRequest
    {
        public SearchRequest() { }

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

        public Filter Filter { get; set; }
        /// <summary>
        /// List of sort values after which the next result set should be produced
        /// </summary>
        public IList<object> AfterKeys { get; set; }

        /// <summary>
        /// Pagination information
        /// </summary>
        public PitPagingInfo PitPagingInfo { get; set; } = null;

        /// <summary>
        /// Pagination information
        /// </summary>
        public UIPagingInfo UIPagingInfo { get; set; } = null;

        /// <summary>
        /// If set, includes concurrency information in results (sequence num, primary term)
        /// </summary>
        public bool IncludeConcurrencyInfo { get; set; } = false;
    }
}
