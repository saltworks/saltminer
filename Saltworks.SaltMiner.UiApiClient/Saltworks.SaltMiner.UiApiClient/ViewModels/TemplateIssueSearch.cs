using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Responses;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class TemplateIssueSearch : UiModelBase
    {
        public List<FieldFilter> SearchFilters { get; set; }
        public UiPager Pager { get; set; }
    }
}
