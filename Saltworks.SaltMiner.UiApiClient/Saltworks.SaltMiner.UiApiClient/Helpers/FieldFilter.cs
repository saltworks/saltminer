/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-04-09
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.UiApiClient.Helpers
{
    public class FieldFilter
    {
        public FieldFilter() { }
        public FieldFilter(SearchFilterValue filter)
        {
            Field = filter.Field; 
            Value = filter.Display;
        }
        public string Field { get; set; }

        public string Value { get; set; }
    }
}
