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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Saltworks.SaltMiner.SourceAdapters.Core.Data
{
    [Table("QueueIssues")]
    public class QueueIssue : ILocalDataEntity
    {
        [Key]
        public string Id { get; set; }
        public string QueueScanId { get; set; }
        public string QueueAssetId { get; set; }
        public DateTime? FoundDate { get; set; }
        public string DataIndexName() => "QueueIssues";
        public string SerializedEntity { get; set; }
        private SaltMiner.Core.Entities.QueueIssue _entity;

        [NotMapped]
        public SaltMiner.Core.Entities.QueueIssue Entity
        {
            get
            {
                _entity ??= (string.IsNullOrEmpty(SerializedEntity) ? null : JsonSerializer.Deserialize<SaltMiner.Core.Entities.QueueIssue>(SerializedEntity));
                return _entity;
            }
            set
            {
                _entity = value;
            }
        }

        public void UpdateDtoFields()
        {
            if (string.IsNullOrEmpty(Entity.Id))
                Entity.Id = Guid.NewGuid().ToString();
            Id = Entity?.Id;
            QueueScanId = Entity?.Saltminer?.QueueScanId;
            QueueAssetId = Entity?.Saltminer?.QueueAssetId;
            FoundDate = Entity?.Vulnerability.FoundDate;
            SerializedEntity = Entity == null ? string.Empty : JsonSerializer.Serialize(Entity);
        }
    }
}
