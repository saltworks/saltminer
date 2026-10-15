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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Saltworks.SaltMiner.SourceAdapters.Core.Data;

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
        Entity.Id = Id;
        QueueScanId = Entity?.Saltminer?.QueueScanId;
        QueueAssetId = Entity?.Saltminer?.QueueAssetId;
        FoundDate = Entity?.Vulnerability.FoundDate;
        SerializedEntity = Entity == null ? string.Empty : JsonSerializer.Serialize(Entity);
    }
}
