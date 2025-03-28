using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class AssetNewPrimer(string regex) : UiModelBase
    {
        public List<AttributeDefinitionValue> AttributeDefinitions { get; set; }

        public AssetFull Asset { get; set; }

        public string GuiValidationRegex { get; set; } = regex;
    }
}
