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
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient.ViewModels;

namespace Saltworks.SaltMiner.Ui.Api.Contexts
{
    public class ReportContext(IServiceProvider services, ILogger<ReportContext> logger) : ContextBase(services, logger)
    {
        protected override List<SearchFilterValue> SortFilterValues => SearchFilters?.Find(x => x.Type == SearchFilterType.ReportingQueueSortFilters.ToString())?.Filters ?? new List<SearchFilterValue>();

        public UiDataResponse<LookupValue> Severities()
        {
            return new UiDataResponse<LookupValue>(SeverityDropdowns);
        }
        public UiNoDataResponse UpdateTemplateLookups(List<string> templateNames)
        {
            DataClient.LookupDeleteByType(LookupType.ReportTemplateDropdown.ToString());

            DataClient.LookupAddUpdate(new Lookup
            {
                Type = LookupType.ReportTemplateDropdown.ToString(),
                Values = templateNames.Select((x, index) => new LookupValue { Display = x, Order = (index + 1), Value = x }).ToList()
            });

            return new UiNoDataResponse { Affected = templateNames.Count };
        }

        public UiDataItemResponse<UiAttachmentInfo> GetReportAttachment(string fileName)
        {
            return new UiDataItemResponse<UiAttachmentInfo>(GetAttachmentByFileName(fileName));
        }
    }
}
