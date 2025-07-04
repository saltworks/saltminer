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