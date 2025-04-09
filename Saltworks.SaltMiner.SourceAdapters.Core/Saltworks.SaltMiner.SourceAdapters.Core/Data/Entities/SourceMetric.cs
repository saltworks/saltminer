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