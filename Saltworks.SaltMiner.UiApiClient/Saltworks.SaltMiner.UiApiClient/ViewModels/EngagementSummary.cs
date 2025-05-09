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
    public class EngagementSummary : UiModelBase
    {
        public EngagementSummary() { }

        public EngagementSummary(Engagement engagement, FieldInfo fieldInfo)
        {
            var attributes = engagement.Saltminer.Engagement.Attributes.ToAttributeFields(fieldInfo);
            if ((attributes ?? []).Count == 0)
                attributes = fieldInfo.AttributeDefinitions.Select(ad => new TextField(default, ad.Name, fieldInfo, true, true)).ToList();
            Name = engagement.Saltminer.Engagement.Name;
            Id = engagement.Id;
            GroupId = engagement.Saltminer.Engagement.GroupId;
            Subtype = engagement.Saltminer.Engagement.Subtype;
            Timestamp = engagement.Timestamp;
            PublishDate = engagement.Saltminer.Engagement.PublishDate;
            Status = engagement.Saltminer.Engagement.Status;
            Summary = engagement.Saltminer.Engagement.Summary;
            Customer = engagement.Saltminer.Engagement.Customer;
            Attributes = attributes;
            IssueCount = new IssueCount();
            ActionRestrictions = fieldInfo.GetActionPermissions(true).ToList();
        }

        public string Customer { get; set; }
        public IssueCount IssueCount { get; set; }
        public string Id { get; set; }
        public string ScanId { get; set; }
        public string Name { get; set; }
        public string Subtype { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime? PublishDate { get; set; }
        public string Status { get; set; }
        public string Summary { get; set; }
        public string GroupId { get; set; }
        public string DraftEngagementId { get; set; }
        public List<UiAttachment> Attachments { get; set; }
        public List<TextField> Attributes { get; set; }
        public List<string> ActionRestrictions { get; set; }
    }
}