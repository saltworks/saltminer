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

﻿using System.Collections.Generic;

namespace Saltworks.Common.Data
{
    public interface IDataRepositoryQueryResult<out T> where T : class
    {
        /// <summary>
        /// Information object used to facilitate multiple "paged" calls to the same query
        /// </summary>
        IDataRepositoryPitPagingInfo PitPagingInfo { get; }
        /// <summary>
        /// Information object used to facilitate multiple "paged" calls to the same query
        /// </summary>
        IDataRepositoryUIPagingInfo UIPagingInfo { get; }
        /// <summary>
        /// Result set produced by the query
        /// </summary>
        IEnumerable<T> Results { get; }
    }
}
