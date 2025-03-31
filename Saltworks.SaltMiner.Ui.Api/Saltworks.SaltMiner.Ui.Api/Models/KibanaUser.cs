using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.Ui.Api.Models
{
    public class KibanaUser
    {
        public KibanaUser() { }
        public KibanaUser(string userName, string fullName) {
            UserName = userName;
            FullName = fullName;
        }

        public const string CookieTag = "sid";
        public string UserName { get; set; }
        public List<string> Roles { get; set; }
        [JsonPropertyName("full_name")]
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool Enabled { get; set; }
        public string Cookie { get; set; }
        public string DateFormat { get; set; }
        public int MaxImportFileSize { get; set; }
    }
}
