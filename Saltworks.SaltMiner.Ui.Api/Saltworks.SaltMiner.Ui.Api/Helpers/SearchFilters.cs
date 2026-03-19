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

using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using System.Text.RegularExpressions;

namespace Saltworks.SaltMiner.Ui.Api.Helpers
{
    public static class SearchFilters
    {
        public static void AddFilters(Dictionary<string, string> requestFilters, List<SearchFilterValue> options, List<FieldFilter> searchFilters, bool isQueue = false)
        {
            foreach (var filter in searchFilters)
            {
                filter.Value = filter.Value.Trim();
                var option = options?.FirstOrDefault(x => x.Field.Equals(filter.Field, StringComparison.OrdinalIgnoreCase));
                if (option != null)
                {
                    if (option.Field.Equals("all", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(filter.Value))
                    {
                        // nothing, just fall out
                    }
                    else if (isQueue)
                    {
                        foreach(var field in option.QueueIndexFieldNames)
                        {
                            CreateFilter(requestFilters, field, option, filter);
                        }
                    }
                    else
                    {
                        foreach (var field in option.IndexFieldNames)
                        {
                            CreateFilter(requestFilters, field, option, filter);
                        }
                    }
                }
            }
        }

        public static void AddNonFilters(Dictionary<string, string> requestFilters, List<SearchFilterValue> options, List<FieldFilter> searchFilters)
        {
            foreach (var filter in searchFilters)
            {
                var option = options?.FirstOrDefault(x => x.Field.Equals(filter.Field, StringComparison.OrdinalIgnoreCase));
                if (option == null)
                    requestFilters.Add(filter.Field, filter.Value);
            }
        }

        private static void CreateFilter(Dictionary<string, string> requestFilters, string indexFieldName, SearchFilterValue option, FieldFilter filter)
        {
            if (indexFieldName.Contains("date", StringComparison.OrdinalIgnoreCase) || indexFieldName.Contains("timestamp", StringComparison.OrdinalIgnoreCase) || Regex.Match(filter.Value, @"([012]?\d)[\/. -]([0123]?\d)[\/. -]([012]\d{3})\b").Success)
                filter.Value = $"{DateTime.Parse(filter.Value):yyyy-MM-dd}";

            var f = new Filter();
            if (option.IsTextSearch)
                f.AddQueryStringFilterMatch(indexFieldName + ".Text", filter.Value);
            else if (indexFieldName == "")
                f.AddQueryStringFilterMatch(indexFieldName, filter.Value);
            else
                f.AddSimpleFilterMatch(indexFieldName, filter.Value);
            requestFilters.Add(f.FilterMatches.First().Key, f.FilterMatches.First().Value);
        }

        public static void AddIssueDefaultSortFilters(Dictionary<string, bool> sortFilters)
        {
            sortFilters ??= [];

            // Include defaults (severity, name)
            if (!sortFilters.Any(x => x.Key.Equals("severity", StringComparison.OrdinalIgnoreCase)))
                sortFilters.Add("severity", true);
            if (!sortFilters.Any(x => x.Key.Equals("name", StringComparison.OrdinalIgnoreCase)))
                sortFilters.Add("name", true);
        }

        public static Dictionary<string, bool> MapSortFilters(Dictionary<string, bool> sortFilters, List<SearchFilterValue> sortFilterValues, bool isQueue = false)
        {
            sortFilters ??= [];
            var result = new Dictionary<string, bool>();
                       
            foreach (var filter in sortFilters)
            {
                var filterValue = (sortFilterValues?.FirstOrDefault(x => x.Field.Equals(filter.Key, StringComparison.OrdinalIgnoreCase))) ?? throw new UiApiConfigurationException($"'{filter.Key}' is not a valid sort field.");

                if (isQueue)
                {
                    foreach (var indexName in filterValue.QueueIndexFieldNames)
                        result.Add(indexName, filter.Value);
                }
                else
                {
                    foreach (var indexName in filterValue.IndexFieldNames)
                        result.Add(indexName, filter.Value);
                }
            }

            return result;
        }
    }
}
