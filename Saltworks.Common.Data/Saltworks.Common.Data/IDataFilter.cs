using System.Collections.Generic;

namespace Saltworks.Common.Data
{
    public interface IDataFilter
    {
        bool AnyMatch { get; set; }
        Dictionary<string, string> FilterMatches { get; }
        string Index { get; set; }
        Dictionary<string, bool> SortFilters { get; }
    }
}
