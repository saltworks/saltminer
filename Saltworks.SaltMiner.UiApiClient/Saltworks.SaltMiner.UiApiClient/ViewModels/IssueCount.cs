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
