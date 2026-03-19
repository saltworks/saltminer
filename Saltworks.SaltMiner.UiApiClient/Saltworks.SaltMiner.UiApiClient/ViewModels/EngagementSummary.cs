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