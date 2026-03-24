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

﻿namespace Saltworks.SaltMiner.Core.Entities;

/// <summary>
/// Represents a scan queued to be processed by the SaltMiner "Manager"
/// </summary>
public class QueueScan : SaltMinerEntity
{
    private static string _indexEntity = "queue_scans";

    public static string GenerateIndex()
    {
        return _indexEntity;
    }

    public enum QueueScanStatus
    {
        Loading = 0,
        Pending,
        Processing,
        Cancel,
        Complete,
        Error,
        None
    }

    /// <summary>
    /// Gets or sets Saltminer for this queue scan.  See the object for more details.
    /// </summary>
    /// <seealso cref="SaltMinerQueueScanInfo"/>
    /// <remarks>Spelling is intentional, do not "fix"</remarks>
    public SaltMinerQueueScanInfo Saltminer { get; set; } = new();
}

public class SaltMinerQueueScanInfo
{
    /// <summary>
    /// Gets or sets Internal.
    /// </summary>
    /// <seealso cref="QueueScanInternal"/>
    public QueueScanInternal Internal { get; set; } = new();

    /// <summary>
    /// Gets or sets Scan.
    /// </summary>
    /// <seealso cref="QueueScanInfo"/>
    public QueueScanInfo Scan { get; set; } = new();

    /// <summary>
    /// Gets or sets Engagement.
    /// </summary>
    /// <seealso cref="EngagementInfo"/>
    public EngagementInfo Engagement { get; set; } = new();
}