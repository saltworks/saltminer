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
using Saltworks.SaltMiner.UiApiClient.Helpers;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class EngagementPrimer(string regex) : UiModelBase
    {
        public List<FieldFilter> SearchFilters { get; set; }
        public List<FieldFilter> SortFilterOptions { get; set; }
        public List<LookupValue> SubtypeDropdowns { get; set; }
        public List<string> ActionRestrictions { get; set; }
        public string EngagementHeader { get; set; }
        public string CustomerHeader { get; set; }
        public string CreatedHeader { get; set; }
        public string GuiValidationRegex { get; set; } = regex;
    }
}
