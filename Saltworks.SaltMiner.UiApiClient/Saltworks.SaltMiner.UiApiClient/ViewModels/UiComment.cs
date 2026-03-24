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
