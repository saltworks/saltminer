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
