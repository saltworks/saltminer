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
    public class UiAttachment : UiModelBase
    {
        public UiAttachment(Attachment attachment, string appVersion)
        {
            AppVersion = appVersion;
            Id = attachment.Id;
            Timestamp = attachment.Timestamp;
            EngagementId = attachment.Saltminer.Engagement?.Id;
            IssueId = attachment.Saltminer.Issue?.Id;
            Attachment = new UiAttachmentInfo(attachment.Saltminer.Attachment);
            IsMarkdown = attachment.Saltminer.IsMarkdown;
        }

        public UiAttachment() { }

        public string AppVersion { get; set; }
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string EngagementId { get; set; }
        public string IssueId { get; set; }
        public UiAttachmentInfo Attachment { get; set; }
        public bool IsMarkdown { get; set; }
        public string User { get; set; }
        public string UserFullName { get; set; }

        public Attachment TransformAttachment(UiAttachment attachment)
        {
            return new()
            {
                Id = attachment.Id,
                Timestamp = attachment.Timestamp,
                Saltminer = new()
                {
                    Engagement = attachment.EngagementId == null ? null : new IdInfo { Id = attachment.EngagementId },
                    Issue = attachment.IssueId == null ? null : new IdInfo { Id = attachment.IssueId },
                    Attachment = new()
                    {
                        FileName = attachment.Attachment.FileName,
                        FileId = attachment.Attachment.FileId,
                    },
                    IsMarkdown = attachment.IsMarkdown,
                    User = attachment.User,
                    UserFullName = attachment.UserFullName
                }
            };
        }
    }

    public class UiAttachmentInfo : UiModelBase
    {
        public UiAttachmentInfo() { }
        public UiAttachmentInfo(AttachmentInfo info)
        {
            FileName = info.FileName;
            FileId = info.FileId;
        }
        public string FileName { get; set; }
        public string FileId { get; set; }
    }
}
