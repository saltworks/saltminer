using Saltworks.SaltMiner.Core.Common;
using Saltworks.SaltMiner.Core.Extensions;
using System.Configuration;

namespace Saltworks.SaltMiner.Ui.Api.Models
{
    public class UiApiConfig : ConfigBase
    {
        public UiApiConfig() { }

        public UiApiConfig(IConfiguration config, string filePath, bool isConsoleApp = false)
        {
            var replaceDefaultFileExt = config.GetSection("UiApiConfig").GetValue<bool>("ValidFileExtensionsReplaceDefault");
            if (replaceDefaultFileExt)
            {
                ValidFileExtensions = new();
            }

            if (isConsoleApp)
            {
                config.Bind(this);
            }
            else
            {
                config.Bind("UiApiConfig", this);
            }

            Validate(filePath);
        }

        public void Validate(string filePath)
        {
            CheckEncryption(this, filePath, "UiApiConfig");

            DecryptProperties(this);
        }

        public string FullPathSettingsFile { get; set; }
        public string DataApiBaseUrl { get; set; } = "";
        /// <summary>
        /// Don't reference this directly, use BaseContext.UiBaseUrl instead
        /// </summary>
        /// <remarks>Shouldn't need to set this unless exceptional circumstances (i.e. UI is separated from UI API).</remarks>
        public string UiBaseUrl { get; set; } = "";
        public bool DataApiVerifySsl { get; set; } = true;
        public string DataApiKey { get; set; } = "";
        public string DataApiKeyHeader { get; set; } = "Authorization";
        public int DataApiTimeoutSec { get; set; } = 3;
        public string BypassCookie { get; set; } = null;
        public int MaxImportFileSize { get; set; } = 5000;
        public bool TestingEnableCors { get; set; } = false;
        public string[] TestingCorsAllowedOrigins { get; set; } = null;
        public string VersionFileName { get; set; } = "version.txt";
        public static string AppVersion => "3.0.1";
        public bool KestrelAllowRemote { get; set; } = false; // file loaded and referenced manually not refereneced by model
        public int KestrelPort { get; set; } = 5001; // file loaded and referenced manually not refereneced by model
        /// <summary>
        /// Don't reference this directly, use BaseContext.KibanaBaseUrl instead.
        /// </summary>
        /// <remarks>Leave blank to set default, will be set to current host</remarks>
        public string KibanaBaseUrl { get; set; } = "";
        public string FileRepository { get; set; } = "";
        public string NginxRoute { get; set; } = "smuiapi";
        public string NginxScheme { get; set; } = "https";
        public string DisplayDateFormat { get; set; } = "en-US";
        public string TemplateRepository { get; set; } = "Templates";
        public string CsvTemplateFileName { get; set; } = "csv_import_template.csv";
        public string EngagementIssueImportTemplateFileName { get; set; } = "engagement_issue_import.json";
        public string EngagementImportTemplateFileName { get; set; } = "engagement_import.json";
        public bool EngagementCheckoutWithClosedIssues { get; set; } = false;
        public bool EngagementCheckoutWithSystemComments { get; set; } = false;
        public string TemplateImportTemplateFileName { get; set; } = "template_issue_import.json";
        public int ImportBatchSize { get; set; } = 100;
        public bool DetailedErrors { get; set; } = false;
        public int DefaultPageSize { get; set; } = 25;
        public int EmailPort { get; set; } = 587;
        public string EmailHost { get; set; } = "";
        public string EmailFromDisplay { get; set; } = "Saltworks No Reply";
        public string EmailFromAddress { get; set; } = "saltworks-no-reply@saltworks.io";
        public string RequestAccessEmailName { get; set; } = "";
        public string RequestAccessEmail { get; set; } = "";
        public string EmailPassword { get; set; } = "";
        public string EmailUserName { get; set; } = "";
        public string ReportingApiKey { get; set; } = "0f8fad5b-d9cb-469f-a165-70867728950e";
        public string ReportingApiKeyHeader { get; set; } = "ReportingAuthorization";
        public string ReportingOutputDirectory { get; set; } = "Output";
        public string ApiFieldRegex { get; set; } = "[^a-zA-Z\\x20\\d\\.\\-,:/();\\[\\]%_\\n\\?'\\\"]";
        public string GuiFieldRegex { get; set; } = "[^a-zA-Z\\x20\\d\\.\\-,:/();\\[\\]%_\\n\\?'\\\"]";
        public string FailedRegexSplat { get; set; } = "[?]";
        public string MarkdownUrlRegex { get; set; } = @"!\[([^]\n]*)\]\((.*?)\)";
        public static string SourceType => EnumExtensions.GetDescription(SaltMiner.Core.Util.SourceType.Pentest);
        public static string AssetType => SaltMiner.Core.Util.AssessmentType.Pen.ToString();
        public static string Instance => "PenTest";
        public string LastScanDaysPolicy { get; set; } = "60";
        public string EngagementCustomerHeader { get; set; } = "Customer";
        public string EngagementNameHeader { get; set; } = "Engagement";
        public string EngagementCreatedHeader { get; set; } = "Created Date";
        public string InventoryAssetKeyAttribute { get; set; } = string.Empty;
        public List<string> RequiredCsvAssetHeaders { get; set; } = [
            "saltminer.asset.name",
            "saltminer.asset.source_id"
        ];
        public List<string> RequiredCsvIssueHeaders { get; set; } = [
        
            "vulnerability.found_date",
            "vulnerability.location",
            "vulnerability.location_full",
            "vulnerability.report_id",
            "vulnerability.name",
            "vulnerability.severity",
            "vulnerability.scanner.product",
            "vulnerability.scanner.vendor"
        ];
        public List<string> IssueFieldsThatRequireComments { get; set; } = [];
        public List<string> ValidFileExtensions { get; set; } = [ ".zip", ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" ];
        public bool ValidFileExtensionsReplaceDefault { get; set; } = false;
        public string AppRolePrefix { get; set; } = "smapp_";
        public bool DisableDataClientInitializationCall { get; set; } = false;
    }
}
