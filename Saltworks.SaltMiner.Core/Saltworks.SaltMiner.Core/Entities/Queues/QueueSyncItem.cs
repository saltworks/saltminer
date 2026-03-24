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

namespace Saltworks.SaltMiner.Core.Entities;

/// <summary>
/// Represents a source queue that pushes and pops queue items
/// </summary>
public class QueueSyncItem : SaltMinerEntity
{
    private static string _indexEntity = "queue_sync_items";

    public static string GenerateIndex() => GenerateIndex(false);
    public static string GenerateIndex(bool forSearch)
    {
        return forSearch ? $"{_indexEntity}_*" : $"{_indexEntity}_{DateTime.UtcNow:yyyy_MM_dd}";
    }

    public QueueSyncItemSaltminerInfo Saltminer { get; set; }

    /// <summary>
    /// [Required] Indicates operation to take on the indicated item (updated or removed).  Defaults to updated.
    /// </summary>
    [Required]
    public string Action { get; set; } = QueueSyncAction.Updated.ToString("g").ToLower();

    /// <summary>
    /// [Required] Indicates the relative priority of this sync item.  Lower priority values are processed first.  Defaults to 5.  Accepted values are 1-9.
    /// </summary>
    [Required]
    public int Priority { get; set; } = 5;

    /// <summary>
    /// Type of the webhook request (can be the same name as the source)
    /// </summary>
    [Required]
    public string Type { get; set; }

    /// <summary>
    /// Json payload of the webhook request.  If setting saltminer fields directly, this does not need to be passed.
    /// </summary>
    [Required]
    public string Payload { get; set; }

    /// <summary>
    /// State of the sync item (new or deleted)
    /// </summary>
    [Required]
    public string State { get; set; }
}

public class QueueSyncItemSaltminerInfo
{
    /// <summary>
    /// [Required] Gets or sets SourceType. This is the system supported value indicating the source of the data. EG) Fortify, Sonatype, etc. 
    /// This value combined with the SourceId field should uniquely identify any asset for a customer.
    /// </summary>
    [Required]
    public string SourceType { get; set; }

    /// <summary>
    /// [Required] Gets or sets Instance. 
    /// </summary>
    [Required]
    public string Instance { get; set; }

    /// <summary>
    /// [Required] Gets or sets SourceId. This is the unique identifier of the asset from the source system.
    /// </summary>
    [Required]
    public string SourceId { get; set; }
}

public enum QueueSyncAction { Updated, Removed }