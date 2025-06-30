/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
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
    [Table("QueueAssets")]
    public class QueueAsset : ILocalDataEntity
    {
        [Key]
        public string Id { get; set; }
        public string QueueScanId { get; set; }
        public string DataIndexName() => "QueueAssets";
        public string SourceType { get; set; }
        public string SourceId { get; set; }
        public string SerializedEntity { get; set; }
        private SaltMiner.Core.Entities.QueueAsset _entity;

        [NotMapped]
        public SaltMiner.Core.Entities.QueueAsset Entity
        {
            get
            {
                _entity ??= (string.IsNullOrEmpty(SerializedEntity) ? null : JsonSerializer.Deserialize<SaltMiner.Core.Entities.QueueAsset>(SerializedEntity));
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
            QueueScanId = Entity?.Saltminer?.Internal?.QueueScanId;
            SourceType = Entity?.Saltminer.Asset.SourceType;
            SourceId = Entity?.Saltminer.Asset.SourceId;
            Id = Entity?.Id;
            SerializedEntity = Entity == null ? string.Empty : JsonSerializer.Serialize(Entity);
        }
    }
}
