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
