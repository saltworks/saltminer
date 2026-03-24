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

﻿using Saltworks.SaltMiner.Core.Util;
using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.SourceAdapters.Core.Helpers
{
    public abstract class SeverityHelper
    {
        public static string ValidSeverity(Dictionary<string, string> map, string severity)
        {
            if (map.ContainsKey(severity.ToLower()))
            {
                severity = map[severity.ToLower()];
            }

            if (Enum.TryParse(severity, out Severity sourceEnum))
            {
                return sourceEnum.ToString("g");
            }
            else
            {
                return Severity.Info.ToString("g");
            }
        }
    }
}
