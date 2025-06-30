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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Saltworks.SaltMiner.SourceAdapters.Core.Data
{
    [Table("QueueScans")]
    public class QueueScan : ILocalDataEntity
    {
        public string Id { get; set; }
        public string DataIndexName() => "QueueScans";
        public bool Loading { get; set; }
        /// <summary>
        /// Currently only used for TwistLock source as a special case and should not be considered storage for history in general
        /// </summary>
        public List<QueueScan> History { get; set; }
        public int FailureCount { get; set; }
        public string Instance { get; set; }
        public string SourceType { get; set; }
        public string QueueStatus { get; set; }
        public string ReportId { get; set; }
        public string SerializedEntity { get; set; }
        public DateTime Timestamp { get; set; }

        private SaltMiner.Core.Entities.QueueScan _entity;

        [NotMapped]
        public SaltMiner.Core.Entities.QueueScan Entity {
            get
            {
                _entity ??= (string.IsNullOrEmpty(SerializedEntity) ? null : JsonSerializer.Deserialize<SaltMiner.Core.Entities.QueueScan>(SerializedEntity));
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
            Instance = Entity?.Saltminer.Scan.Instance;
            SourceType = Entity?.Saltminer.Scan.SourceType;
            QueueStatus = Entity?.Saltminer.Internal.QueueStatus;
            ReportId = Entity?.Saltminer.Scan.ReportId;
            Id = Entity?.Id;
            SerializedEntity = Entity == null ? string.Empty : JsonSerializer.Serialize(Entity);
        }

        public QueueScan Clone() => new()
        {
            Id = Id,
            //History = new() {  }, // skipping this because not necessary for duplicating scan history
            Loading = Loading,
            Timestamp = DateTime.UtcNow,
            SerializedEntity = SerializedEntity
        };
    }
}
