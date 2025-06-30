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

﻿namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class IssueCount : UiModelBase
    {
        public long TotalIssues => Critical + High + Medium + Low + Info;
        public int Critical { get; set; }
        public int High { get; set; }
        public int Medium { get; set; }
        public int Low { get; set; }
        public int Info { get; set; }
        public int CriticalBar => TotalIssues == 0 ? 0 : (int)(Critical / (decimal)TotalIssues * 100);
        public int HighBar => TotalIssues == 0 ? 0 : (int)(High / (decimal)TotalIssues * 100);
        public int MediumBar => TotalIssues == 0 ? 0 : (int)(Medium / (decimal)TotalIssues * 100);
        public int LowBar => TotalIssues == 0 ? 0 : (int)(Low / (decimal)TotalIssues * 100);
        public int InfoBar => TotalIssues == 0 ? 0 : (int)(Info / (decimal)TotalIssues * 100);
        public string Id { get; set; }
    }
}
