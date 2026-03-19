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
