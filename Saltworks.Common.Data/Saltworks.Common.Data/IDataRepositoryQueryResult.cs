using System.Collections.Generic;

namespace Saltworks.Common.Data
{
    public interface IDataRepositoryQueryResult<out T> where T : class
    {
        /// <summary>
        /// Information object used to facilitate multiple "paged" calls to the same query
        /// </summary>
        IDataRepositoryPitPagingInfo PitPagingInfo { get; }
        /// <summary>
        /// Information object used to facilitate multiple "paged" calls to the same query
        /// </summary>
        IDataRepositoryUIPagingInfo UIPagingInfo { get; }
        /// <summary>
        /// Result set produced by the query
        /// </summary>
        IEnumerable<T> Results { get; }
    }
}
