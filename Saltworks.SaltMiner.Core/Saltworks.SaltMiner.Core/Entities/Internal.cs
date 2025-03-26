using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.Core.Entities
{
    public class QueueScanInternal
    {
        /// <summary>
        /// Gets or sets IssueCount.  Source unique ID for application or application/version
        /// </summary>
        [Required]
        public int IssueCount { get; set; }

        /// <summary>
        /// Gets or sets CurrentQueueScanId.
        /// </summary>
        public string CurrentQueueScanId { get; set; }

        /// <summary>
        /// Gets or sets QueueStatus.
        /// </summary>
        public string QueueStatus { get; set; }
        /// <summary>
        /// Gets or sets ReplaceIssues, determines if all existing issues need to be removed so all incoming queue issues will be used
        /// </summary>
        public bool ReplaceIssues { get; set; } = false;
        /// <summary>
        /// Gets or sets error message last thrown by Manager when processing this item.
        /// </summary>
        public string LastError { get; set; } = "";
        /// <summary>
        /// Lock ID set by Manager to aid with concurrency
        /// </summary>
        public string LockId { get; set; } = "";
    }

    public class QueueAssetInternal
    {
        /// <summary>
        /// [Required] Gets or sets QueueScanId.  Set to "0" to skip check for existing queue scan.
        /// </summary>
        [Required]
        public string QueueScanId { get; set; }

        /// <summary>
        /// Gets or sets NeverScanned, representing whether this asset has been scanned. Set to false for a "null record".
        /// </summary>
        public bool NeverScanned { get; set; } = false;
    }
}
