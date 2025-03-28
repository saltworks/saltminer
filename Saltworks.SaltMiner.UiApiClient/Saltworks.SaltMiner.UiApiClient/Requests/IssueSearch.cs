using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Responses;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class IssueSearch : UiModelBase
    {
        [Required]
        public string EngagementId { get; set; }
        public List<FieldFilter> SearchFilters { get; set; }
        public List<string> StateFilters { get; set; }
        public List<string> TestStatusFilters { get; set; }
        public List<string> SeverityFilters { get; set; }
        public List<string> AssetFilters { get; set; }
        public UiPager Pager { get; set; }
    }
}
