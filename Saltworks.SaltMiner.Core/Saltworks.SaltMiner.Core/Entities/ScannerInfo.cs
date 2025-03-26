using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.Core.Entities
{
    public class ScannerInfo : ScannerInfoBase
    {
        /// <summary>
        /// Gets or sets ApiUrl. Source specific API data reference URL, links back to source data.
        /// </summary>
        public string ApiUrl { get; set; }

        /// <summary>
        /// Gets or sets GuiUrl. Source specific reference URL, link back to original record in source system.
        /// </summary>
        public string GuiUrl { get; set; }

        /// <summary>
        /// Gets or sets Id. Unique identifier from source for this issue.
        /// </summary>
        public string Id { get; set; }
    }

    public class ScannerInfoBase
    {
        /// <summary>
        /// Gets or sets AssessmentType. Scan assessment type.  Choose from one of the following values:
        /// SAST / DAST / OSS / PENTEST
        /// Manager: validate this field.  May make allowable values a configuration item.
        /// </summary>
        [Required]
        public string AssessmentType { get; set; }

        /// <summary>
        /// Gets or sets Product. Product used to run the scan.
        /// </summary>
        [Required]
        public string Product { get; set; }

        /// <summary>
        /// Gets or sets Vendor. Vendor for the scanner used to identify this issue.
        /// </summary>
        [Required]
        public string Vendor { get; set; }
    }
}