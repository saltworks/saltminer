using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.UiApiClient.Helpers;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class InventoryAssetPrimer(string regex) : UiModelBase
    {
        public List<FieldFilter> SearchFilters { get; set; }
        public List<FieldFilter> SortFilterOptions { get; set; }
        public List<AttributeDefinitionValue> AttributeDefinitions { get; set; }
        public InventoryAssetFull InventoryAsset { get; set; }
        public string GuiValidationRegex { get; set; } = regex;
    }
}
