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

﻿using Saltworks.SaltMiner.Core.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Saltworks.SaltMiner.ElasticClient
{
    public static class DataFilterExtensions
    {
        /// <summary>
        /// Converts search request to data filters to be used with Elasticsearch
        /// </summary>
        /// <param name="searchRequest">Search request to convert</param>
        /// <returns>Search filters that will hopefully be helpful</returns>
        /// <remarks>Warning: this probably doesn't work except maybe on the FilterMatches, and everything is an AND.</remarks>
        public static Dictionary<string, string> ToSearchRequest(this SearchRequest searchRequest)
        {
            var dict = new Dictionary<string, string>();

            if (searchRequest?.Filter?.FilterMatches != null && searchRequest.Filter.FilterMatches.Any())
            {
                searchRequest = searchRequest.ToSnakeCaseFilter();
                foreach (var item in searchRequest.Filter.FilterMatches)
                {
                    dict.Add(item.Key, item.Value);
                }
            }
            
            return dict;
        }

        /// <summary>
        /// Converts data filters to sort filters to be used with Elasticsearch
        /// </summary>
        /// <param name="sort">sort filter to convert</param>
        /// <returns>SortFilters that will hopefully be helpful</returns>
        /// <remarks>Warning: this probably doesn't work except maybe on the FilterMatches, and everything is an AND.</remarks>
        public static Dictionary<string, bool> ToSortFilters(this Dictionary<string, bool> sort)
        {
            Dictionary<string, bool> dict = new();

            if (sort != null && sort.Any())
            {
                foreach (var item in sort)
                {
                    dict.Add(item.Key, item.Value);
                }
            }

            return dict;
        }

        public static string ToSnakeCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            var pHolder = "Supercalifraggleisticexpealidocious";

            if (str.ToLower() == str) // no update needed, all lower case already
            {
                return str;
            }

            Regex pattern = new(@"[A-Z]{2,}(?=[A-Z][a-z]+[0-9]*|\b)|[A-Z]?[a-z]+[0-9]*|[A-Z]|[0-9]+");
            
            str = str.Replace(".", pHolder);
            
            var result = string.Join("_", pattern.Matches(str)).ToLower().Replace("_" + pHolder.ToLower() + "_", ".");
            
            return result;
        }

        public static SearchRequest ToSnakeCaseFilter(this SearchRequest searchRequest)
        {
            var filterDic = new Dictionary<string, string>();

            if (searchRequest == null)
            {
                return searchRequest;
            }

            if (searchRequest?.Filter?.FilterMatches != null && searchRequest.Filter.FilterMatches.Any())
            {
                foreach (var key in searchRequest.Filter.FilterMatches.Keys)
                {
                    filterDic.Add(key.ToSnakeCase(), searchRequest.Filter.FilterMatches[key]);
                }

                searchRequest.Filter.FilterMatches.Clear();

                foreach (var kvp in filterDic)
                {
                    searchRequest.Filter.FilterMatches.Add(kvp.Key, kvp.Value);
                }

                filterDic.Clear();

                var filter = searchRequest.Filter.SubFilter;
                while(filter?.FilterMatches != null && filter.FilterMatches.Any())
                {
                    foreach (var key in filter.FilterMatches.Keys)
                    {
                        filterDic.Add(key.ToSnakeCase(), filter.FilterMatches[key]);
                    }

                    filter.FilterMatches.Clear();

                    foreach (var kvp in filterDic)
                    {
                        filter.FilterMatches.Add(kvp.Key, kvp.Value);
                    }

                    filterDic.Clear();

                    filter = filter.SubFilter;
                }
            }

            return searchRequest;
        }
    }
}
