using System;

namespace Saltworks.SaltMiner.Core.Entities
{
    /// <summary>
    /// Represents a Comment
    /// </summary>
    [Serializable]
    public class Attachment : SaltMinerEntity
    {
        private static string _indexEntity = "attachments";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        /// <summary>
        /// Gets or sets Saltminer for this asset.  See the object for more details.
        /// </summary>
        /// <seealso cref="SaltMinerAttachmentInfo"/>
        /// <remarks>Spelling is intentional, do not "fix"</remarks>
        public SaltMinerAttachmentInfo Saltminer { get; set; } = new();
    }

    public class SaltMinerAttachmentInfo
    {
        /// <summary>
        /// Gets or sets user that added the attachment
        /// </summary>
        public string User { get; set; }
        /// <summary>
        /// Gets or sets user full name that added the attachment
        /// </summary>
        public string UserFullName { get; set; }

        /// <summary>
        /// Gets or sets Attaachment. See the object for more details.
        /// </summary>
        /// <seealso cref="AttachmentInfo"/>
        public AttachmentInfo Attachment { get; set; } = new();

        /// <summary>
        /// Gets or sets IsMarkdown.
        /// </summary>
        public bool IsMarkdown { get; set; }

        /// <summary>
        /// Gets or sets Engagement id for this comment.  See the object for more details.
        /// </summary>
        /// <seealso cref="IdInfo"/>
        public IdInfo Engagement { get; set; } = new();

        /// <summary>
        /// Gets or sets Issue Id for this comment.  See the object for more details.
        /// </summary>
        /// <seealso cref="IdInfo"/>
        public IdInfo Issue { get; set; } = new();
    }
}