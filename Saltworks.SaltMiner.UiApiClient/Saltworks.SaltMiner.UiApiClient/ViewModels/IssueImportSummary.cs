namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    // formerly EngagementIssueImportDto
    public class IssueImportSummary : UiModelBase
    {
        public IssueImport Issue { get; set; }

        public AssetImport Asset { get; set; }

        public IssueImportSummary()
        {
        }

        public IssueImportSummary(IssueFull issue, AssetFull asset)
        {
            Issue = new IssueImport(issue);
            Asset = new AssetImport(asset);
        }
    }
}
