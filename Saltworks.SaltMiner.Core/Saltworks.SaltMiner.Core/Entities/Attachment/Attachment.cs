/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
*
* ----
*/

﻿using System;

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