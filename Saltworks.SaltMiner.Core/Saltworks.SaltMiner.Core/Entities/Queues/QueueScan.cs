namespace Saltworks.SaltMiner.Core.Entities
{
    /// <summary>
    /// Represents a scan queued to be processed by the SaltMiner "Manager"
    /// </summary>
    public class QueueScan : SaltMinerEntity
    {
        private static string _indexEntity = "queue_scans";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        public enum QueueScanStatus
        {
            Loading = 0,
            Pending = 1,
            Processing = 2,
            Cancel = 3,
            Complete = 4,
            Error = 5
        }

        /// <summary>
        /// Gets or sets Saltminer for this queue scan.  See the object for more details.
        /// </summary>
        /// <seealso cref="SaltMinerQueueScanInfo"/>
        /// <remarks>Spelling is intentional, do not "fix"</remarks>
        public SaltMinerQueueScanInfo Saltminer { get; set; } = new();
    }

    public class SaltMinerQueueScanInfo
    {
        /// <summary>
        /// Gets or sets Internal.
        /// </summary>
        /// <seealso cref="QueueScanInternal"/>
        public QueueScanInternal Internal { get; set; } = new();

        /// <summary>
        /// Gets or sets Scan.
        /// </summary>
        /// <seealso cref="QueueScanInfo"/>
        public QueueScanInfo Scan { get; set; } = new();

        /// <summary>
        /// Gets or sets Engagement.
        /// </summary>
        /// <seealso cref="EngagementInfo"/>
        public EngagementInfo Engagement { get; set; } = new();
    }
}