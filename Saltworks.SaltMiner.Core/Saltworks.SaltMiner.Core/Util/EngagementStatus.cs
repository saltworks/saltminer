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
using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{
    [Serializable]
    public enum EngagementStatus
    {
        [Description("Draft")]
        Draft = 1,
        [Description("Queued")]
        Queued,
        [Description("Processing")]
        Processing,
        [Description("Published")]
        Published,
        [Description("Historical")]
        Historical,
        [Description("Error")]
        Error
    }
}
