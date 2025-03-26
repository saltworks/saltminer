using System.Collections.Generic;

namespace Saltworks.Common.Data
{
    public interface IDataRepositoryPitPagingInfo
    {
        /// <summary>
        /// Total documents found
        /// </summary>
        int? Total { get; set; }
        /// <summary>
        /// Size of current (or next) resultset, if supported by provider
        /// </summary>
        int? Size { get; set; }
        /// <summary>
        /// Implementation-specific data used to re-call a previous query
        /// </summary>
        string PagingToken { get; set; }
        /// <summary>
        /// If supported by provider, flag indicating whether or not to enable pagination
        /// </summary>
        bool Enabled { get; set; }
        /// <summary>
        /// If supported by provider, dictionary of keys used to produce the next aggregate result from a previous aggregate query
        /// </summary>
        Dictionary<string, object> AggregateKeys { get; set; }
        Dictionary<string, bool> SortFilters { get; set; }
        /// <summary>
        /// If supported by provider, list of sort values after which the next result set should be produced
        /// </summary>
        IList<object> AfterKeys { get; set; }
    }
}
