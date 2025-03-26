using System.Collections.Generic;

namespace Saltworks.Common.Data
{
    /// <summary>
    /// Represents the result of an aggregation query
    /// </summary>
    public interface IDataRepositoryAggregateResult
    {
        /// <summary>
        /// The identifier for the aggregation result
        /// </summary>
        string Key { get; set; }
        /// <summary>
        /// Total count of documents retreived for this aggregation result
        /// </summary>
        long? DocCount { get; set; }
        /// <summary>
        /// Dictionary of keys and aggregation result values for this aggregation result
        /// </summary>
        Dictionary<string, double?> Results { get; set; }
        /// <summary>
        /// Paging information to support batching for this aggregation query (if implemented)
        /// </summary>
        IDataRepositoryPitPagingInfo PitPagingInfo { get; set; }
    }
}
