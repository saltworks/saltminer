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

﻿using Microsoft.AspNetCore.Mvc.RazorPages;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using System.Drawing;
using System.Net.NetworkInformation;

namespace Saltworks.SaltMiner.Ui.Api.Extensions
{
    public static class GeneralExtensions
    {
        public static string SearchUIPagingLoggerMessage(string entity, int count, int size, int page)
        {
            return $"Search {entity}:  count of filters {count}, with size {size} and page '{page}'";
        }

        public static Dictionary<string, bool> GetSortFilters(this UIPagingInfo paging, List<SearchFilterValue> searchFilters, bool isQueue = false)
        {
            var sortFilters = new Dictionary<string, bool>();

            if (searchFilters != null)
            {
                foreach (var sort in paging.SortFilters)
                {
                    SearchFilterValue filter = null;

                    if (isQueue)
                    {
                        filter = searchFilters.FirstOrDefault(x => x.QueueIndexFieldNames.Any(y => y.Equals(sort.Key, StringComparison.CurrentCultureIgnoreCase)));
                    }
                    else
                    {
                        filter = searchFilters.FirstOrDefault(x => x.IndexFieldNames.Any(y => y.Equals(sort.Key, StringComparison.CurrentCultureIgnoreCase)));
                    }

                    if (filter != null)
                    {
                        sortFilters.Add(filter.Field, sort.Value);
                    }
                }
            }
            return sortFilters;
        }
    }
}
