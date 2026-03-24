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

using Elastic.Clients.Elasticsearch.Aggregations;
using Saltworks.SaltMiner.Core.Data;
using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.ElasticClient
{
    public interface IElasticClientResponse
    {
        /// <summary>
        /// Flag indicating success.
        /// </summary>
        public bool IsSuccessful { get; set; }
        /// <summary>
        /// Message about this result.  So far seems to be a one word indicator of operation.  Have fun with that.
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Message about this result.  So far seems to be a one word indicator of operation.  Have fun with that.
        /// </summary>
        public Dictionary<string, string> BulkErrorMessages { get; set; }
        /// <summary>
        /// How many records were affected by this request.  Only set for non-result operations.
        /// </summary>
        public long CountAffected { get; set; }
        /// <summary>
        /// Elastic http status code returned
        /// </summary>
        public int HttpStatus { get; set; }
    }

    public interface IElasticClientAggregateResponse : IElasticClientResponse
    {
        /// <summary>
        /// Aggregation results go here.
        /// </summary>
        public AggregateDictionary Aggregations { get; set; }
    }

    public interface IElasticClientResponse<T> : IElasticClientResponse where T: class
    {
        /// <summary>
        /// Multiple result documents go here, not singles.
        /// </summary>
        public IEnumerable<IElasticClientDto<T>> Results { get; set; }
        /// <summary>
        /// Single result document goes here, not as a loner in the Results field.
        /// </summary>
        public IElasticClientDto<T> Result { get; set; }
        /// <summary>
        /// For multiple result queries, key information needed to return the next (or first) set of results
        /// </summary>
        public PagingInfo PagingInfo { get; set; }
    }
}
