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
