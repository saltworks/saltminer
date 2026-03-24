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
    // formerly AssetSummaryDTO
    public class IssuePrimerAssetItem(AssetFull asset)
    {
        public string Name { get; set; } = asset.Name.Value;
        public string Description { get; set; } = asset.Description.Value;
        public string AssetId { get; set; } = asset.AssetId;
        public string ScanId { get; set; } = asset.ScanId;
    }

    public class IssuePrimer(string regex) : UiModelBase
    {
        public List<LookupValue> AddItemDropdown { get; set; }
        public List<LookupValue> SubtypeDropdown { get; set; }
        public List<LookupValue> SeverityDropdown { get; set; }
        public List<LookupValue> TestedDropdown { get; set; }
        public List<LookupValue> IssueStateDropdown { get; set; }
        public List<LookupValue> ReportTemplateDropdown { get; set; }
        public List<AttributeDefinitionValue> AttributeDefinitions { get; set; }
        public List<string> ActionRestrictions { get; set; }
        public List<IssuePrimerAssetItem> AssetDropdown { get; set; }
        public List<FieldFilter> SearchFilters { get; set; }
        public List<FieldFilter> SortFilterOptions { get; set; }
        public List<string> ValidFileExtensions { get; set; }
        public string GuiValidationRegex { get; set; } = regex;
    }

    public class IssueEditPrimer(string regex) : UiModelBase
    {
        public LockInfo LockInfo { get; set; }
        public List<IssuePrimerAssetItem> AssetDropdown { get; set; }
        public List<LookupValue> SeverityDropdown { get; set; }
        public List<LookupValue> EngagementTypeDropdown { get; set; }
        public List<LookupValue> TestedDropdowns { get; set; }
        public List<AttributeDefinitionValue> AttributeDefinitions { get; set; }
        public List<string> ActionRestrictions { get; set; }
        public IssueFull Issue { get; set; }
        public List<UiAttachment> Attachments { get; set; }
        public List<string> ValidFileExtensions { get; set; }
        public List<string> IssueFieldsThatRequireComments { get; set; }
        public string GuiValidationRegex { get; set; } = regex;
        public bool IsTemplate { get; set; }
    }
}
