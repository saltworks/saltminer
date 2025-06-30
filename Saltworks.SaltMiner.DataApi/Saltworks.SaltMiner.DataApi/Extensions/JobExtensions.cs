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

﻿using static Saltworks.SaltMiner.Core.Entities.Job;
using System;

namespace Saltworks.SaltMiner.DataApi.Extensions
{
    public static class JobExtensions
    {
        public static JobStatus ToJobStatus(this string status)
        {
            if (Enum.TryParse<JobStatus>(status, out var parsed))
            {
                return parsed;
            }
            else
            {
                throw new ApiValidationQueueStateException($"Invalid status '{status}'");
            }
        }
    }
}
