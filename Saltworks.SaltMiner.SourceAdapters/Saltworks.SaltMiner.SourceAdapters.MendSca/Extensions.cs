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

﻿using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.SourceAdapters.MendSca
{
    internal static class Extensions
    {
        public static Dictionary<string, string> ToDictionary(this List<ProjectTagDto> tags)
        {
            if (tags.Count == 0)
            {
                return new Dictionary<string, string>();
            }

            return tags[0].Tags
                .Select(x => new KeyValuePair<string, string>(x.Key, x.Value[0]))
                .ToList()
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }
}
