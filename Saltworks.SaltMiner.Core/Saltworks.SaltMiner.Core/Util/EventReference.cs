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
    public enum EventReference
    {
        [Description("Engagement")]
        Engagement = 0,
        [Description("queue_scan")]
        QueueScan,
        [Description("queue_issue")]
        QueueIssue,
        [Description("queue_asset")]
        QueueAsset,
        [Description("scan")]
        Scan,
        [Description("issue")]
        Issue,
        [Description("asset")]
        Asset,

    }
}