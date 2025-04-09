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

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class UiComment : UiModelBase
    {
        public string AppVersion { get; set; }

        public string Id { get; set; }

        public string ParentId { get; set; }

        public string Message { get; set; }

        public string User { get; set; }

        public string UserFullName { get; set; }

        public string EngagementId { get; set; }

        public string AssetId { get; set; }

        public string IssueId { get; set; }

        public string ScanId { get; set; }

        public string Type { get; set; }

        public DateTime Added { get; set; }

        public UiComment()
        {
        }

        public UiComment(Comment comment, string appVersion)
        {
            AppVersion = appVersion;
            Id = comment.Id;
            ParentId = comment.Saltminer.Comment.ParentId;
            Message = comment.Saltminer.Comment.Message;
            User = comment.Saltminer.Comment.User;
            UserFullName = comment.Saltminer.Comment.UserFullName;
            EngagementId = comment.Saltminer.Engagement?.Id;
            AssetId = comment.Saltminer.Asset?.Id;
            ScanId = comment.Saltminer.Scan?.Id;
            IssueId = comment.Saltminer.Issue?.Id;
            Added = comment.Saltminer.Comment.Added;
            Type = comment.Saltminer.Comment.Type;
        }
    }
}
