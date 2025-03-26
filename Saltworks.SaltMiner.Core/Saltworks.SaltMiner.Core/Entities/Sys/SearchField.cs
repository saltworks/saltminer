using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.Core.Entities
{
    [Serializable]
    public class SearchFilter : SaltMinerEntity
    {
        private static string _indexEntity = "sys_search_filters";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        /// <summary>
        /// Gets or sets Type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets Filters
        /// </summary>
        /// <seealso cref="SearchFilterValue"/>
        public List<SearchFilterValue> Filters { get; set; } = new();
    }

    public class SearchFilterValue
    {
        /// <summary>
        /// Gets or sets Display.
        /// </summary>
        public string Display { get; set; }

        /// <summary>
        /// Gets or sets Field.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets IsTextSearch.
        /// </summary>
        public bool IsTextSearch { get; set; }

        /// <summary>
        /// Gets or sets Order.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets IndexFieldNames. Field name(s) to search in main index.
        /// </summary>
        public List<string> IndexFieldNames { get; set; } = new();

        /// <summary>
        /// Gets or sets QueueIndexFieldNames.  Field name(s) to search in related queue index.
        /// </summary>
        public List<string> QueueIndexFieldNames { get; set; } = new();
    }
}