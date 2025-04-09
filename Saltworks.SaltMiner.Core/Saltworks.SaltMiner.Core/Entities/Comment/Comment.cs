/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-04-09
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
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
    public class Comment : SaltMinerEntity
    {
        private static string _indexEntity = "comments";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        /// <summary>
        /// Gets or sets Saltminer for this asset.  See the object for more details.
        /// </summary>
        /// <seealso cref="SaltMinerCommentInfo"/>
        /// <remarks>Spelling is intentional, do not "fix"</remarks>
        public SaltMinerCommentInfo Saltminer { get; set; } = new();
    }

    public class SaltMinerCommentInfo
    {
        /// <summary>
        /// Gets or sets Comment for this comment. See the object for more details.
        /// </summary>
        /// <seealso cref="CommentInfo"/>
        public CommentInfo Comment { get; set; } = new();

        /// <summary>
        /// Gets or sets Engagement id for this comment.  See the object for more details.
        /// </summary>
        /// <seealso cref="IdInfo"/>
        public IdInfo Engagement { get; set; } = new();

        /// <summary>
        /// Gets or sets Asset id for this comment.  See the object for more details.
        /// </summary>
        /// <seealso cref="IdInfo"/>
        public IdInfo Asset { get; set; } = new();

        /// <summary>
        /// Gets or sets Scan id for this comment.  See the object for more details.
        /// </summary>
        /// <seealso cref="IdInfo"/>
        public IdInfo Scan { get; set; } = new();

        /// <summary>
        /// Gets or sets Issue Id for this comment.  See the object for more details.
        /// </summary>
        /// <seealso cref="IdInfo"/>
        public IdInfo Issue { get; set; } = new();
    }
}