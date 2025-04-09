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
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.Utility.ApiHelper;

namespace Saltworks.SaltMiner.SourceAdapters.Polaris
{
    public class PolarisClient : SourceClient
    {
        private readonly PolarisConfig Config;
        public PolarisClient(ApiClient client, PolarisConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            SetApiClientDefaults(config.BaseAddress, config.Timeout, ApiClientHeaders.OneHeader("Api-Token", Config.ApiKey));
        }

        public async Task<ProjectBranchesDto> GetProjectBranchesAsync(string nextBatchLink = "")
        {
            var apiHeader = GetApiHeader("application/vnd.synopsys.pm.branches-1+json");
            var url = nextBatchLink == "" ? $"portfolio/branches?_limit={Config.BatchLimit}" : $"portfolio/{nextBatchLink}";
            var result = await ApiClient.GetAsync<ProjectBranchesDto>(url, headers: apiHeader);
            return CheckContent(result);
        }

        public async Task<PortfolioDto> GetPortfolioItemAsync(string id)
        {
            var apiHeader = GetApiHeader("application/vnd.synopsys.pm.portfolio-items-1+json");
            var result = await ApiClient.GetAsync<PortfolioDto>($"portfolio/portfolio-items/{id}", headers: apiHeader);
            return CheckContent(result);
        }

        public async Task<ProjectDto> GetProjectAsync(string id)
        {
            var apiHeader = GetApiHeader("application/vnd.synopsys.pm.portfolio-subitems-1+json");
            var result = await ApiClient.GetAsync<ProjectDto>($"portfolio/portfolio-sub-items/{id}", headers: apiHeader);
            return CheckContent(result);
        }

        public async Task<ScansDto> GetScansByBranchAsync(string id, string nextBatchLink = "")
        {
            var apiHeader = GetApiHeader("application/vnd.synopsys.tm.tests-1+json");
            var url = nextBatchLink == "" ? $"test-manager/tests?branchId={id}&_include=comments&_limit={Config.BatchLimit}" : $"{nextBatchLink}";
            var result = await ApiClient.GetAsync<ScansDto>(url, headers: apiHeader);
            return CheckContent(result);
        }

        public async Task<IssuesDto> GetIssuesFamiliesByBranchAsync(string projectId, string branchId, string nextBatchLink = "")
        {
            var apiHeader = GetApiHeader("application/vnd.synopsys.polaris-one.issue-management.issue-family-paginated-list-1+json");
            var url = nextBatchLink == "" ? 
                $"specialization-layer-service/issue-families/_actions/list?_includeIssueType=true&_includeTriageProperties=true&_includeIssueProperties=true&portfolioSubItemId={projectId}&_first={Config.BatchLimit}&_includeAttributes=true&branchId={branchId}" : 
                $"{nextBatchLink}";
            var result = await ApiClient.GetAsync<IssuesDto>(url, headers: apiHeader);
            return CheckContent(result);
        }

        public async Task<IssuesDto> GetIssuesByBranchAsync(string projectId, string branchId, string nextBatchLink = "")
        {
            var apiHeader = GetApiHeader("application/vnd.synopsys.polaris-one.issue-management.issue-paginated-list-1+json");
            var url = nextBatchLink == "" ?
                $"specialization-layer-service/issues/_actions/list?portfolioSubItemId={projectId}&_first={Config.BatchLimit}&_includeAttributes=true&branchId={branchId}" :
                $"{nextBatchLink}";
            var result = await ApiClient.GetAsync<IssuesDto>(url, headers: apiHeader);
            return CheckContent(result);
        }

        public T CheckContent<T>(ApiClientResponse<T> result) where T : class
        {
            if (result.RawContent.Contains("errorCode"))
            {
                throw new Exception(result.RawContent);
            }

            return result.Content;
        }

        public SourceMetric GetSourceMetric(ProjectBranchDto project)
        {
            var issueCounts = 0; // project.Meta.LatestIssueCounts;
            return new SourceMetric
            {
                LastScan = DateTime.UtcNow, // issueCounts.UpdatedAt?.ToUniversalTime() ?? DateTime.UtcNow,
                Instance = Config.Instance,
                IsSaltminerSource = PolarisConfig.IsSaltminerSource,
                SourceType = Config.SourceType,
                SourceId = project.Id,
                VersionId = "",
                Attributes = new Dictionary<string, string>(),
                IssueCount = 0, // issueCounts.Critical + issueCounts.High + issueCounts.Medium + issueCounts.Low,
                IssueCountSev1 = 0, // issueCounts.High,
                IssueCountSev2 = 0, // issueCounts.Medium,
                IssueCountSev3 = 0, // issueCounts.Low
            };
        }

        private ApiClientHeaders GetApiHeader(string headerValue)
        {
            var headers = new ApiClientHeaders();
            headers.Add("Accept", headerValue);
            return headers;
        }
    }
}
