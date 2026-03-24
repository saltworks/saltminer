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

namespace Saltworks.SaltMiner.Core.Entities;

public class EngagementInfo : IdInfo
{
    /// <summary>
    /// Gets or sets Name for this engagement.  Name of engagement that created this issue.
    /// </summary>
    public virtual string Name { get; set; }

    /// <summary>
    /// Gets or sets Summary for this engagement. Summary description of the engagement
    /// </summary>
    public virtual string Summary { get; set; }

    /// <summary>
    /// Gets or sets Subtype. This is the system supported value indicating the source sub-type of the data. EG) Fortify, Sonatype, etc. when using Saltminer Engagements
    /// </summary>
    public virtual string Subtype { get; set; }
    /// <summary>
    /// Gets or sets PublishDate for this engagement.  Date engagement was published (can be null).
    /// </summary>
    public virtual DateTime? PublishDate { get; set; }

    /// <summary>
    /// Gets or sets Customer for this engagement.  Customer for whom the engagement is made
    /// </summary>
    public virtual string Customer { get; set; }

    /// <summary>
    /// Gets or sets attributes. Attributes are custom values allowed by some sources that apply at the Engagement level and which are used for reporting.
    /// </summary>
    public virtual Dictionary<string, string> Attributes { get; set; } = [];
}