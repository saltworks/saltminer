using Saltworks.SaltMiner.UiApiClient.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class EngagementSummaryEdit : UiModelBase
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Summary { get; set; }
        [Required]
        [SubtypeValidation]
        public string Subtype { get; set; }
        [Required]
        public string Customer { get; set; }
        public List<string> OptionalFields { get; set; }
        [AttributesValidation]
        public Dictionary<string, string> Attributes { get; set; }
    }
}
