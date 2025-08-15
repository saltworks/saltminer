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
using System;

namespace Saltworks.SaltMiner.DataApi.Extensions
{
    public static class LoggerExtensions
    {
        public static string SearchPagingLoggerMessage(string methodName, SearchRequest request)
        {
            if (request.PagingInfo != null)
            {
                var keyString = request.PagingInfo.CurrentAfterKeys == null ? null : String.Join(",", request.PagingInfo.CurrentAfterKeys.ToArray());
                var result = $"{methodName}:  count of filters {request?.Filter?.FilterMatches?.Count ?? 0}, with size {request?.PagingInfo?.Size ?? 0} and page '{request?.PagingInfo?.Page ?? 0}'";
                if (keyString != null)
                {
                    result += $" and after keys '{keyString}'";
                }
                return result;
            } 
            else
            {
                return $"{methodName}:  count of filters {request?.Filter?.FilterMatches?.Count ?? 0} without any PagingInfo";
            }
        }
    }
}
