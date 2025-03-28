using Microsoft.AspNetCore.Http;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class ReportTemplateImport
    {
        public IFormFile File { get; set; }
        public string FileRepo { get; set; }
        public string UiBaseUrl { get; set; }
        public string TemplateFolder { get; set; }
        public string UserName { get; set; }
        public string UserFullName { get; set; }
        public string JobType { get; set; }
        public bool FromQueue { get; set; } = false;
    }
}
