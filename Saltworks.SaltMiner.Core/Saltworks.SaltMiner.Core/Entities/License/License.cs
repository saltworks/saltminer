using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.Core.Entities
{
    [Serializable]
    public class License : SaltMinerEntity
    {
        private static string _indexEntity = "licenses";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        public string Hash { get; set; }
        public LicenseInfo LicenseInfo { get; set; } = new();
    }

    [Serializable]
    public class LicenseInfo
    {
        public string Name { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool EnableUnknownAssessmentTypes { get; set; }
        public List<LicenseType> LicenseAssessmentTypes { get; set; }
        public bool EnableUnknownSources { get; set; }
        public List<LicenseType> LicenseSourceTypes { get; set; }
        public int AssetInventoryLimit { get; set; }
    }
}