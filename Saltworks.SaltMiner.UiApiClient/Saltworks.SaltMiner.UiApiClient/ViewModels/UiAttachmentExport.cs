namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class UiAttachmentExport : UiModelBase
    {
        public string Id { get; set; }

        public DateTime Timestamp { get; set; }

        public string IssueId { get; set; }

        public UiAttachmentInfo Attachment { get; set; }

        public bool IsMarkdown { get; set; }

        public string User { get; set; }

        public string UserFullName { get; set; }
    }
}
