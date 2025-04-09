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
using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{
    [Serializable]
    public enum EngagementIssueStatus
    {
        [Description("Not Tested")]
        NotTested = 1,
        [Description("Found")]
        Found = 2,
        [Description("Not Found")]
        NotFound = 3,
        [Description("Out of Scope")]
        OutOfScope = 4,
        [Description("Tested")]
        Tested = 5
    }
}
