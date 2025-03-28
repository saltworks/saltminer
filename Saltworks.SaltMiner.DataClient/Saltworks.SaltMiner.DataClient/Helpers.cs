using Saltworks.SaltMiner.Core.Data;
using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.DataClient
{
    public static class Helpers
    {
        public static SearchRequest SearchRequest(string field, string value)
        {
            return new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = new()
                    {
                        { field, value }
                    }
                }
            };
        }

        public static string BuildDateRangeFilterValue(DateTime greaterThanOrEqual, DateTime lessThan) {
            return $"{greaterThanOrEqual.ToString("yyyy-mm-dd")}||{lessThan.ToString("yyyy-mm-dd")}";
        }

        public static string BuildGreaterThanOrEqualFilterValue(string value)
        {
            return $"{value}>=||";
        }

        public static string BuildLessThanOrEqualFilterValue(string value)
        {
            return $"{value}<=||";
        }

        public static string BuildGreaterThanFilterValue(string value)
        {
            return $"{value}>||";
        }

        public static string BuildLessThanFilterValue(string value)
        {
            return $"{value}<||";
        }

        public static string BuildQueryStringFilterValue(string value)
        {
            return $"{value}**";
        }

        public static string BuildMustNotExistsFilterValue()
        {
            return "!";
        }

        public static string BuildMustExistsFilterValue()
        {
            return "+!";
        }

        public static string BuildTermsFilterValue(List<string> values)
        {
            return string.Join("||+", values.ToArray());
        }

        public static string BuildExcludeTermsFilterValue(List<string> values)
        {
            if (values.Count == 1)
            {
                return $"||~{values[0]}";
            }
            return string.Join("||~", values.ToArray());
        }
    }
}
