using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.UiApiClient.Helpers
{
    public class FieldFilter
    {
        public FieldFilter() { }
        public FieldFilter(SearchFilterValue filter)
        {
            Field = filter.Field; 
            Value = filter.Display;
        }
        public string Field { get; set; }

        public string Value { get; set; }
    }
}
