using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class AdminPrimer(string regex) : UiModelBase
    {
        public List<LookupValue> SystemDropdownOptions { get; set; }
        public string GuiValidationRegex { get; set; } = regex;
    }
}
