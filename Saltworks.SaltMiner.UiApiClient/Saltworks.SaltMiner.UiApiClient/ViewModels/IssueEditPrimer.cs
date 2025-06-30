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
