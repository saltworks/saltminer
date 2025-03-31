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
                {
                    requestFilters.Add(filter.Field, filter.Value);
                }
            }
        }

        private static void CreateFilter(Dictionary<string, string> requestFilters, string indexFieldName, SearchFilterValue option, FieldFilter filter)
        {
            if (indexFieldName.Contains("date", StringComparison.OrdinalIgnoreCase) || indexFieldName.Contains("timestamp", StringComparison.OrdinalIgnoreCase) || Regex.Match(filter.Value, @"([012]?\d)[\/. -]([0123]?\d)[\/. -]([012]\d{3})\b").Success)
            {
                filter.Value = $"{DateTime.Parse(filter.Value):yyyy-MM-dd}";
            }

            if (option.IsTextSearch)
            {
                requestFilters.Add(indexFieldName + ".Text", DataClient.Helpers.BuildQueryStringFilterValue(filter.Value));
            }
            else if (indexFieldName == "")
            {
                requestFilters.Add(indexFieldName, DataClient.Helpers.BuildQueryStringFilterValue(filter.Value));
            }
            else
            {
                requestFilters.Add(indexFieldName, filter.Value);
            }
        }

        public static Dictionary<string, bool> MapSortFilters(Dictionary<string, bool> sortFilters, List<SearchFilterValue> sortFilterValues, bool isQueue = false)
        {
            if (sortFilters == null)
            {
                return [];
            }

            var result = new Dictionary<string, bool>();
                       
            foreach (var filter in sortFilters)
            {
                var filterValue = (sortFilterValues?.FirstOrDefault(x => x.Field.Equals(filter.Key, StringComparison.OrdinalIgnoreCase))) ?? throw new UiApiConfigurationException($"'{filter.Key}' is not a valid sort field.");

                if (isQueue)
                {
                    foreach (var indexName in filterValue.QueueIndexFieldNames)
                    {
                        result.Add(indexName, filter.Value);
                    }
                }
                else
                {
                    foreach (var indexName in filterValue.IndexFieldNames)
                    {
                        result.Add(indexName, filter.Value);
                    }
                }
            }

            return result;
        }
    }
}
