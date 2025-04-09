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

﻿using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.UiApiClient.Responses;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class CommentNew : UiModelBase
    {
        public string ParentId { get; set; }
        [Required]
        public string Message { get; set; }
        [Required]
        public string EngagementId { get; set; }
        public string AssetId { get; set; }
        public string IssueId { get; set; }

        public Comment TransformNewComment(string type, string user, string userFullName)
        {
            IdInfo asset = string.IsNullOrEmpty(AssetId) ? null : new IdInfo { Id = AssetId };
            IdInfo engagement = string.IsNullOrEmpty(EngagementId) ? null : new IdInfo { Id = EngagementId };
            IdInfo issue = string.IsNullOrEmpty(IssueId) ? null : new IdInfo { Id = IssueId };
            var now = DateTime.UtcNow;
            return new Comment
            {
                Saltminer = new()
                {
                    Asset = asset,
                    Scan = null,
                    Engagement = engagement,
                    Issue = issue,
                    Comment = new CommentInfo
                    {
                        Message = Message,
                        User = user,
                        UserFullName = userFullName,
                        ParentId = ParentId,
                        Type = type,
                        Added = now
                    }
                },
                Timestamp = now
            };
        }
    }

    public class CommentNotice : UiModelBase
    {
        [Required]
        public CommentNew Request { get; set; }
        public List<string> MentionAddresses { get; set; }
    }

    public class CommentEdit : UiModelBase
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string Message { get; set; }
        public List<string> MentionAddresses { get; set; }
    }

    public class CommentSearch : UiModelBase
    {
        [Required]
        public string EngagementId { get; set; }
        public string IssueId { get; set; }
        public string ScanId { get; set; }
        public string AssetId { get; set; }
        public bool IncludeSystem { get; set; } = true;
        public UiPager Pager { get; set; }
    }
}
