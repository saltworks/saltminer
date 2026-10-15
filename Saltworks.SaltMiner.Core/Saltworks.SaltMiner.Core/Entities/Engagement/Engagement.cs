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

﻿using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.Core.Entities;

/// <summary>
/// Represents a Engagement managed by SaltMiner
/// </summary>
public class Engagement : SaltMinerEntity
{
    private readonly static string _indexEntity = "engagements";

    public static string GenerateIndex()
    {
        return _indexEntity;
    }

    /// <summary>
    /// Gets or sets Saltminer for this asset.  See the object for more details.
    /// </summary>
    /// <seealso cref="SaltMinerEngagementInfo"/>
    /// <remarks>Spelling is intentional, do not "fix"</remarks>
    public SaltMinerEngagementWrapper Saltminer { get; set; } = new();
}

public class SaltMinerEngagementWrapper
{
    public SaltMinerEngagementInfo Engagement { get; set; } = new();
}

public class SaltMinerEngagementInfo : EngagementInfo
{
    /// <summary>
    /// Gets or sets GroupId for this engagement. Group Id
    /// </summary>
    public string GroupId { get; set; }

    /// <summary>
    /// Gets or sets Name for this engagement. Engagement name
    /// </summary>
    [Required]
    public override string Name { get; set; }

    /// <summary>
    /// Gets or sets Customer for this engagement. Customer identifier for engagement
    /// </summary>
    [Required]
    public override string Customer { get; set; }

    /// <summary>
    /// Gets or sets Summary for this engagement. Summary description of the engagement
    /// </summary>
    [Required]
    public override string Summary { get; set; }

    /// <summary>
    /// Gets or sets Subtype. This is the system supported value indicating the source sub-type of the data. EG) Fortify, Sonatype, etc. when using Saltminer Engagements
    /// </summary>
    [Required]
    public override string Subtype { get; set; }

    /// <summary>
    /// Gets Status for this engagement
    /// </summary>
    public string Status { get; set; }
}