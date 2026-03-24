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
