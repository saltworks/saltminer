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
using System.Text.Json.Nodes;

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