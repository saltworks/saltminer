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

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.Utility.ApiHelper;

namespace Saltworks.SaltMiner.SourceAdapters.Contrast
{
    public class ContrastClient : SourceClient
    {
        private readonly ContrastConfig Config;

        public ContrastClient(ApiClient client, ContrastConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            var headers = ApiClientHeaders.AuthorizationCustomHeader(Config.AuthorizationHeader, true);
            headers.Add(ApiClientHeaders.OneHeader("api-key", config.ApiKey));

            SetApiClientDefaults(config.BaseAddress, config.Timeout, headers, true);
        }

        public async Task<List<OrganizationResourceDTO>> GetOrgsAsync()
        {
            var result = new List<OrganizationResourceDTO>();

            var response = await ApiClient.GetAsync<AllowedOrganizationsResponseDTO>("profile/organizations");
            foreach (var org in response.Content.Organizations)
            {
                if (Config.TestingAssetLimit == 0 || result.Count < Config.TestingAssetLimit)
                {
                    if (Config.OrgIds.Contains(org.OrganizationUuid))
                    {
                        result.Add(org);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return result;
        }

        public async Task<List<ApplicationResourceDTO>> GetApplicationsAsync(string orgId, int offset)
        {
            var result = new List<ApplicationResourceDTO>();
            var response = await ApiClient.GetAsync<ApplicationsResponseDTO>($"{orgId}/applications?includeOnlyLicensed=true&includeMerged=true&expand=trace_breakdown&limit={Config.ApplicationBatchSize}&offset={offset}");

            foreach (var app in response.Content.Applications)
            {
                if (Config.TestingAssetLimit == 0 || (result.Count < Config.TestingAssetLimit && offset < Config.TestingAssetLimit))
                {
                    result.Add(app);
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        public async Task<ApplicationResourceDTO> GetApplicationAsync(string orgId, string appId)
        {
            var response = await ApiClient.GetAsync<ApplicationResponseDTO>($"{orgId}/applications{appId}");
            return response.Content.Application;
        }

        public async Task<TraceBreakdownResourceDTO> GetTraceCountsAsync(string orgId, string appId)
        {
            var response = await ApiClient.GetAsync<TraceBreakdownResponseDTO>($"{orgId}/applications/{appId}/breakdown/trace");
            return response.Content.TraceBreakdown;
        }

        public async Task<List<TraceResourceDTO>> GetTracesAsync(string orgId, string appId, int offset)
        {
            var response = await ApiClient.GetAsync<TraceFilterResponseDTO>($"{orgId}/traces/{appId}/filter?limit={Config.ApplicationBatchSize}&offset={offset}");
            return response.Content.Traces;
        }
    }
}
