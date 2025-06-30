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

﻿

using Saltworks.SaltMiner.Core.Util;

namespace Saltworks.SaltMiner.ServiceManager.JobModels
{
    public class JobStatusDto
    {
        public string JobKey { get; set; } = string.Empty;
        public string Status { get; set; } = ServiceJobStatus.Ready.ToString("g");
        public DateTime? LastRunTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
