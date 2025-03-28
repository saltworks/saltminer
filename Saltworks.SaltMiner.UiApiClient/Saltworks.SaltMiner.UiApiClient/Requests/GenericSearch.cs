using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Responses;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class GenericSearch : UiModelBase
    {
        public UiPager Pager { get; set; }
        public List<FieldFilter> SearchFilters { get; set; }
    }
}
