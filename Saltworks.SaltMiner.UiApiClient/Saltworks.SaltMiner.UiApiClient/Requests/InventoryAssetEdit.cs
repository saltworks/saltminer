using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class InventoryAssetAddUpdateRequest : UiModelBase
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public bool IsProduction { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public Dictionary<string, Dictionary<string, string>> Attributes { get; set; }
        public string Name { get; set; }

        public InventoryAsset TransformInventoryAsset()
        {
            return new InventoryAsset()
            {
                Id = Id,
                Timestamp = DateTime.UtcNow,
                Key = Key,
                IsProduction = IsProduction,
                Description = Description,
                Version = Version,
                Attributes = Attributes ?? new Dictionary<string, Dictionary<string, string>>(),
                Name = Name
            };
        }
    }
}
