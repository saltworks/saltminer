
namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class IssueGetScanner(string regex) : UiModelBase
    {
        public IssueFull Issue { get; set; }
        public string GuiValidationRegex { get; set; } = regex;
    }
}
