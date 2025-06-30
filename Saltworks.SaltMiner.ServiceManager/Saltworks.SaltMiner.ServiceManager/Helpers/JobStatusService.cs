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

using Saltworks.SaltMiner.ServiceManager.JobModels;
using System.Collections.Concurrent;

namespace Saltworks.SaltMiner.ServiceManager.Helpers
{
    public class JobStatusService : IJobStatusService
    {
        private readonly ConcurrentDictionary<string, JobStatusDto> Statuses = new();

        public void SetStatus(string jobKey, JobStatusDto status)
        {
            Statuses[jobKey] = status;
        }

        public JobStatusDto? GetStatus(string jobKey)
        {
            Statuses.TryGetValue(jobKey, out var status);
            return status ?? new();
        }

        public void RemoveStatus(string jobKey)
        {
            Statuses.TryRemove(jobKey, out _);
        }
    }
}
