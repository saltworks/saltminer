using Saltworks.SaltMiner.UiApiClient.Attributes;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class TextValidation : UiModelBase
    {
        [Markdown]
        public string Markdown { get; set; }
        [InputValidation]
        public string Input { get; set; }
        [SeverityValidation]
        public string Severity { get; set; }
        [TestStatusValidation]
        public string TestStatus { get; set; }
        [AttributesValidation]
        public Dictionary<string, string> EngagementAttributes { get; set; }
        [AttributesValidation]
        public Dictionary<string, string> IssueAttributes { get; set; }
        [AttributesValidation]
        public Dictionary<string, string> InventoryAssetAttributes { get; set; }
        [AttributesValidation]
        public Dictionary<string, string> AssetAttributes { get; set; }
        [SubtypeValidation]
        public string Subtype { get; set; }
        [DateValidation]
        public DateTime Date { get; set; }
    }
}
