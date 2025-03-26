using System;

namespace Saltworks.SaltMiner.Core.Entities
{
    public class AuditInfo
    {
        /// <summary>
        /// Gets or sets Audited.
        /// </summary>
        public bool Audited { get; set; }

        /// <summary>
        /// Gets or sets Auditor.
        /// </summary>
        public string Auditor { get; set; }

        /// <summary>
        /// Gets or sets LastAudit.
        /// </summary>
        public DateTime? LastAudit { get; set; }
    }
}