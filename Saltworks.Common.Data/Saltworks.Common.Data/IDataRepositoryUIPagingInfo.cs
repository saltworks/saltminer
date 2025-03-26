using System.Collections.Generic;

namespace Saltworks.Common.Data
{
    public interface IDataRepositoryUIPagingInfo
    {
        /// <summary>
        /// Total documents found
        /// </summary>
        int? Total { get; set; }
        /// <summary>
        /// Where to start next resultset in the list, if supported by provider
        /// </summary>
        int Page { get; set; }
        /// <summary>
        /// Size of current (or next) resultset, if supported by provider
        /// </summary>
        int Size { get; set; }

        int From { get; }

        int? TotalPages { get; }
        Dictionary<string, bool> SortFilters { get; set; }
    }
}
