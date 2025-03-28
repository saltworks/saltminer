using Microsoft.Extensions.Configuration;
using Saltworks.SaltMiner.Core.Common;

namespace Saltworks.SaltMiner.JobManager
{
    // ConfigBase class provides decryption support for configuration properties ending in -password, -key, and -secret
    // Encryption support requires the use of an EncryptionKey and EncryptionIV that can be generated along with encrypted data using the SaltMiner CLI.
    // ConfigBase may offer additional config-related features in the near future as well
    public class JobManagerConfig : ConfigBase
    {
        public JobManagerConfig() { }

        /// <summary>
        /// Binds and decrypts in one easy step! Now with all natural grape flavor!
        /// </summary>
        /// <param name="config"></param>
        public JobManagerConfig(IConfiguration config, string filePath)
        {
            config.Bind(this);
            CheckEncryption(this, filePath, "JobManagerConfig");
            DecryptProperties(this);
        }
        public string DataApiBaseUrl { get; set; }
        public bool DataApiVerifySsl { get; set; } = true;
        public string DataApiKey { get; set; } = "managersecret";
        public string DataApiKeyHeader { get; set; } = "Authorization";
        public int DataApiTimeoutSec { get; set; } = 10;
        public string ApiBaseUrl { get; set; }
        public bool ApiVerifySsl { get; set; } = true;
        public int ApiTimeoutSec { get; set; } = 3;
        public string ApiKey { get; set; } = "0f8fad5b-d9cb-469f-a165-70867728950e";
        public string ApiAuthHeader { get; set; } = "ReportingAuthorization";
        public bool ListOnly { get; set; } = false;
        public int IssueImportCSVBatchSize { get; set; }
        public List<string> IssueImportRequiredCsvAssetHeaders { get; set; }
        public List<string> IssueImportRequiredCsvIssueHeaders { get; set; }
        public int EngagementImportCheckoutBatchSize { get; set; }
        public string EngagementReportNameTemplate { get; set; }
        public string FileRepo { get; set; } = "File";
        public string TemplateRepository { get; set; } = "Templates";
        public string ReportTemplateFolderPath { get; set; } = "Template";
        public string ReportOutputFilePath { get; set; } = "Output";
        public int ReportImageMaxWidth { get; set; } = 216;  // In points. 1 inch = 72 pts. 216 = 3 inches in width
        public int ReportImageMaxHeight { get; set; } = 288;  // In points. 1 inch = 72 pts. 288 = 4 inches in height
        public string ReportStaticImageAltText { get; set; } = "StaticImage";
        public string ReportAttachmentType { get; set; } = "Word"; // Options: "Word", "Pdf", "All".  Either word or pdf or both...
        public Dictionary<string, FontInfo> ReportFontSubstitutions { get; set; } = [];
        public bool ReportIncludeSystemComments { get; set; } = false;
        public string AppVersion { get; } = "3.0.1";
        public int ReportRetentionDays { get; set; } = 1;
        public int CleanupQueueAfterDays { get; set; } = 1;
        public int CleanupProcessorBatchSize { get; set; } = 500;
        public int CleanupProcessorBatchDelayMs { get; set; } = 0;
        public Dictionary<string, string> FieldValueColorCustomizations { get; set; } = [];
        public Dictionary<string, int> ServiceProcessorIntervals { get; set; } = new() { { "ImportJobs", 30 } };
        public string ReportCommentTemplate { get; set; } = "[{Date:d}] {User}: {Message}";
        public int ReportMaxIssueComments { get; set; } = 3;
        public bool ReportIssueCommentSortLatestFirst { get; set; } = false;
    }

    public class FontInfo
    {
        public FontInfo(string font, string bold, string italic)
        {
            Bold = bold;
            Italic = italic;
            Font = font;
        }
        public string Bold { get; set; }
        public string Italic { get; set; }
        public string Font { get; set; }
    }
}