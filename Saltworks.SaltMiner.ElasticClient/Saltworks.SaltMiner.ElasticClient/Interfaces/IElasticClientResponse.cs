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

﻿using Saltworks.SaltMiner.Core.Data;
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
        /// Single result document goes here, not as a loner in the Results field.
        /// </summary>
        public IList<object> AfterKeys { get; set; }
        /// <summary>
        /// For multiple result queries, key information needed to return the next (or first) set of results
        /// </summary>
        public PitPagingInfo PitPagingInfo { get; set; }
        /// <summary>
        /// For multiple result queries, key information needed to return the next (or first) set of results
        /// </summary>
        public UIPagingInfo UIPagingInfo { get; set; }
    }
}
