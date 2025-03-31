using Microsoft.AspNetCore.Mvc.RazorPages;
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
