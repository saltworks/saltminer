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

﻿using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient.ViewModels;

namespace Saltworks.SaltMiner.Ui.Api.Contexts
{
    public class ScanContext(IServiceProvider services, ILogger<ScanContext> logger) : ContextBase(services, logger)
    {
        public UiDataItemResponse<ScanFull> Get(string engagementId)
        {
            General.ValidateIdAndInput(engagementId, Config.ApiFieldRegex, "Id");
            var engagement = DataClient.EngagementGet(engagementId).Data;
            if(engagement.Saltminer.Engagement.Status == EnumExtensions.GetDescription(EngagementStatus.Draft))
            {
                return GetQueueScanByEngagement(engagementId);
            }
            else
            {
                return GetScanByEngagement(engagementId);
            }
        }

        public UiDataItemResponse<ScanFull> GetScanByEngagement(string engagementId)
        {
            General.ValidateIdAndInput(engagementId, Config.ApiFieldRegex, "Id");
            Logger.LogInformation("Get initiated for Scan for engagement '{Id}'", engagementId);
            var scan = DataClient.ScanGetByEngagement(engagementId).Data;
            return new UiDataItemResponse<ScanFull>(new ScanFull(scan, UiApiConfig.AppVersion));
        }

        public UiDataItemResponse<ScanFull> GetQueueScanByEngagement(string engagementId)
        {
            General.ValidateIdAndInput(engagementId, Config.ApiFieldRegex, "Id");
            Logger.LogInformation("Get initiated for Queue Scan for engagement '{Id}'", engagementId);
            var scan = DataClient.QueueScanGetByEngagement(engagementId).Data;
            return new UiDataItemResponse<ScanFull>(new ScanFull(scan, UiApiConfig.AppVersion));
        }
    }
}
