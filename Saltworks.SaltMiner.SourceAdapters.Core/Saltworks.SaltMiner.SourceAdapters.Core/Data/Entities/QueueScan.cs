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

namespace Saltworks.SaltMiner.SourceAdapters.Core.Data;

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
        Entity.Id = Id;
        Instance = Entity?.Saltminer.Scan.Instance;
        SourceType = Entity?.Saltminer.Scan.SourceType;
        QueueStatus = Entity?.Saltminer.Internal.QueueStatus;
        ReportId = Entity?.Saltminer.Scan.ReportId;
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
