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
