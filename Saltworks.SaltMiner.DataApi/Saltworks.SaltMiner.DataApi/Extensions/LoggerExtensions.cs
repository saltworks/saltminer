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
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.DataApi.Extensions
{
    public static class LoggerExtensions
    {
        public static string SearchPagingLoggerMessage(string methodName, SearchRequest request)
        {
            if (request.PitPagingInfo != null)
            {
                var keyString = request.AfterKeys == null ? null : String.Join(",", request.AfterKeys.ToArray());
                var result = $"{methodName}:  count of filters {request?.Filter?.FilterMatches?.Count ?? 0}, with scroll size {request?.PitPagingInfo?.Size ?? 0} and scroll token '{request?.PitPagingInfo?.PagingToken ?? ""}'";
                if (keyString != null)
                {
                    result = result + $" and after keys '{keyString}'";
                }
                return result;
            }
            else if(request.UIPagingInfo != null)
            {
                var keyString = request.AfterKeys == null ? null : String.Join(",", request.AfterKeys.ToArray());
                var result = $"{methodName}:  count of filters {request?.Filter?.FilterMatches?.Count ?? 0}, with size {request?.UIPagingInfo?.Size ?? 0} and page '{request?.UIPagingInfo?.Page ?? 0}'";
                if (keyString != null)
                {
                    result = result + $" and after keys '{keyString}'";
                }
                return result;
            } 
            else
            {
                return $"{methodName}:  count of filters {request?.Filter?.FilterMatches?.Count ?? 0} without any PagingIfo";
            }
        }

        public static string GetPagingLoggerMessage(string methodName, string name, PitPagingInfo scrollInfo, IList<object> afterKeys)
        {
            var keyString = afterKeys == null ? null : String.Join(",", afterKeys.ToArray());
            var result = $"{methodName} called for type {name} with scroll size {scrollInfo.Size} and scroll token '{scrollInfo.PagingToken}'";
            if (keyString != null)
            {
                result = result + $" and after keys '{keyString}'";
            }

            return result;
        }

        public static string GetPagingLoggerMessage(string methodName, string name, UIPagingInfo scrollInfo, IList<object> afterKeys)
        {
            var keyString = afterKeys == null ? null : String.Join(",", afterKeys.ToArray());
            var result = $"{methodName} called for type {name} with size {scrollInfo.Size} and page '{scrollInfo.Page}'";
            if (keyString != null)
            {
                result = result + $" and after keys '{keyString}'";
            }

            return result;
        }

        public static string GetNoPagingLoggerMessage(string methodName, string name)
        {
            return $"{methodName} called for type {name} with no PagingInfo'";
        }
    }
}
