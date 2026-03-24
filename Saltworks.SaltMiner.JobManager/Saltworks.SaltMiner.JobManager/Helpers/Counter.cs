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

﻿using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;

namespace Saltworks.SaltMiner.JobManager.Helpers
{
    public class Counter
    {
        public Counter()
        {
            Counts["total"] = 0;
        }
        private Dictionary<string, int> Counts { get; } = new();
        
        public void Count(QueueIssue issue)
        {
            if (Counts.ContainsKey(issue.Vulnerability.Severity))
            {
                Counts[issue.Vulnerability.Severity]++;
            }
            else
            {
                Counts[issue.Vulnerability.Severity] = 1;
            }

            Counts["total"]++;
        }

        private int GetCount(string key) => Counts.ContainsKey(key) ? Counts[key] : 0;

        public void SetCounts(SaltMinerScanInfo scanInfo)
        {
            scanInfo.Scan.Critical = GetCount(Severity.Critical.ToString("g"));
            scanInfo.Scan.High = GetCount(Severity.High.ToString("g"));
            scanInfo.Scan.Medium = GetCount(Severity.Medium.ToString("g"));
            scanInfo.Scan.Low = GetCount(Severity.Low.ToString("g"));
            scanInfo.Scan.Info = GetCount(Severity.Info.ToString("g"));
        }

        public int Total { get => GetCount("total"); }
    }
}
