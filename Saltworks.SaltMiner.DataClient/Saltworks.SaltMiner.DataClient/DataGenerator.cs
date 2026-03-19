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

﻿using Microsoft.AspNetCore.Razor.Hosting;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.DataClient;

public static class DataGenerator
{
    public static IEnumerable<T> Generate<T>(SearchRequest searchRequest, Func<SearchRequest, DataResponse<T>> dataClientCall) where T: SaltMinerEntity
    {
        Validate(searchRequest, dataClientCall);
        string curFirstId = null;
        while (true)
        {
            var rsp = dataClientCall.Invoke(searchRequest);
            if (!rsp.Success)
                throw new DataClientException("Search failed in DataGenerator");
            var first = rsp.Data.FirstOrDefault();
            if (first == null)
                break;
            if (first.Id == curFirstId)
                throw new DataClientException("Repeating first ID, pagination error.");
            curFirstId = first.Id;
            foreach (var item in rsp.Data)
                yield return item;
            if (rsp.PagingInfo.IsLastPage())
                break;
            searchRequest.PagingInfo = rsp.PagingInfo.NextPage();
        }
    }

    private static void Validate<T>(SearchRequest searchRequest, Func<SearchRequest, DataResponse<T>> dataClientCall) where T : SaltMinerEntity
    {
        ArgumentNullException.ThrowIfNull(searchRequest);
        ArgumentNullException.ThrowIfNull(dataClientCall);
    }
}
