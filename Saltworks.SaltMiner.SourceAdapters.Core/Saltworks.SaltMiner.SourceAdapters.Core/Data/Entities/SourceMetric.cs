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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Saltworks.SaltMiner.SourceAdapters.Core.Data
{
    [Table("SourceMetrics")]
    public class SourceMetric : ILocalDataEntity
    {
        public string Id { get; set; }
        public string SourceId { get; set; }
        public string Instance { get; set; }
        public bool IsSaltminerSource { get; set; }
        public bool IsNotScanned { get; set; }
        public string SourceType { get; set; }
        public string VersionId { get; set; }
        public DateTime? LastScan { get; set; }
        public bool IsProcessed { get; set; }
        public bool IsRetired { get; set; }
        public long IssueCount { get; set; }
        public long IssueCountSev1 { get; set; }
        public long IssueCountSev2 { get; set; }
        public long IssueCountSev3 { get; set; }
        public long IssueCountSev4 { get; set; }
        public string SerializedLocalAttributes { get; set; }
        private Dictionary<string, string> _localAttributes;
        [NotMapped]
        public Dictionary<string, string> LocalAttributes
        {
            get
            {
                _localAttributes ??= (string.IsNullOrEmpty(SerializedLocalAttributes) ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(SerializedLocalAttributes));
                return _localAttributes;
            }
            set
            {
                _localAttributes = value;
            }
        }
        public string SerializedAttributes { get; set; }
        private Dictionary<string, string> _attributes;
        [NotMapped]
        public Dictionary<string, string> Attributes
        {
            get
            {
                _attributes ??= (string.IsNullOrEmpty(SerializedAttributes) ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(SerializedAttributes));
                return _attributes;
            }
            set
            {
                _attributes = value;
            }
        }
        public string DataIndexName() => "SourceMetrics";
        public void UpdateDtoFields() {
            SerializedAttributes = Attributes == null ? string.Empty : JsonSerializer.Serialize(Attributes);
            SerializedLocalAttributes = LocalAttributes == null ? string.Empty : JsonSerializer.Serialize(LocalAttributes);
        }
    }
}