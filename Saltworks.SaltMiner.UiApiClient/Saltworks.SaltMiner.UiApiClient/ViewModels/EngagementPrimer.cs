using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.UiApiClient.Helpers;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class EngagementPrimer(string regex) : UiModelBase
    {
        public List<FieldFilter> SearchFilters { get; set; }
        public List<FieldFilter> SortFilterOptions { get; set; }
        public List<LookupValue> SubtypeDropdowns { get; set; }
        public List<string> ActionRestrictions { get; set; }
        public string EngagementHeader { get; set; }
        public string CustomerHeader { get; set; }
        public string CreatedHeader { get; set; }
        public string GuiValidationRegex { get; set; } = regex;
    }
}
