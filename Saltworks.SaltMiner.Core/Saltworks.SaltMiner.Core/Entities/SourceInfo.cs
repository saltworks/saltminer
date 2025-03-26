namespace Saltworks.SaltMiner.Core.Entities
{
    /// <summary>
    /// Encapsulates source information about an issue.
    /// </summary>
    public class SourceInfo
    {
        /// <summary>
        /// Gets or sets Analyzer. This is a source-specific analyzer (in this case, Fortify SCA would use a SQL_Injection analyzer).
        /// </summary>
        public string Analyzer { get; set; }

        /// <summary>
        /// Gets or sets Confidence. This is a source-specific confidence score (fortify-specific in this case).
        /// </summary>
        public float? Confidence { get; set; }

        /// <summary>
        /// Gets or sets Impact. This is a source-specific impact score (fortify-specific in this case).
        /// </summary>
        public float? Impact { get; set; }

        /// <summary>
        /// Gets or sets IssueStatus. This is a source-specific issue status (in this case, Fortify).
        /// </summary>
        public string IssueStatus { get; set; }

        /// <summary>
        /// Gets or sets Kingdom. This is a source-specific kingdom identifier for this issue.
        /// </summary>
        public string Kingdom { get; set; }

        /// <summary>
        /// Gets or sets Lilkelihood. This is a source-specific likelihood score (fortify-specific in this case).
        /// </summary>
        public float? Likelihood { get; set; }
    }
}