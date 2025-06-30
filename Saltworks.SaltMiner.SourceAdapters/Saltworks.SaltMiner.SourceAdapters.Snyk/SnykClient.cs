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

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.Utility.ApiHelper;

namespace Saltworks.SaltMiner.SourceAdapters.Snyk
{
    public class SnykClient : SourceClient
    {
        private readonly SnykConfig Config;
        public SnykClient(ApiClient client, SnykConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            SetApiClientDefaults(config.BaseAddress, config.Timeout, ApiClientHeaders.AuthorizationCustomHeader($"token {Config.ApiKey}"), true);
        }

        //Here are all the calls to the Source API to get data and massage it into DTOs

        public async Task<OrgCollectionDto> GetAllOrganizationsAsync(string nextBatchLink = null)
        {
            ApiClientResponse<OrgCollectionDto> result = null;
            if (nextBatchLink == null)
            {
                result = await ApiClient.GetAsync<OrgCollectionDto>($"{Config.RestApiUrl}orgs?version={Config.RestApiVersion}");
            }
            else
            {
                nextBatchLink = nextBatchLink.Replace("/rest/", Config.RestApiUrl);
                result = await ApiClient.GetAsync<OrgCollectionDto>(nextBatchLink);
            }
            return CheckContent(result);
        }

        public async Task<ProjectCollectionDto> GetAllProjectsAsync(string orgId, string nextBatchLink = null)
        {
            ApiClientResponse<ProjectCollectionDto> result = null;
            if (nextBatchLink == null)
            {
                result = await ApiClient.GetAsync<ProjectCollectionDto>($"{Config.RestApiUrl}orgs/{orgId}/projects?version={Config.RestApiVersion}&limit={Config.ApplicationBatchSize}&meta.latest_issue_counts=true");
            }
            else
            {
                nextBatchLink = nextBatchLink.Replace("/rest/", Config.RestApiUrl);
                result = await ApiClient.GetAsync<ProjectCollectionDto>(nextBatchLink);
            }
            return CheckContent(result);
        }

        public async Task<IssueCollectionDto> GetIssuesByOrgAsync(string orgId, string scanItemId, string scanItemType, string nextBatchLink = null)
        {
            ApiClientResponse<IssueCollectionDto> result = null;
            if (nextBatchLink == null)
            {
                result = await ApiClient.GetAsync<IssueCollectionDto>($"{Config.RestApiUrl}orgs/{orgId}/issues?version={Config.RestApiVersion}&limit={Config.ApplicationBatchSize}&scan_item.id={scanItemId}&scan_item.type={scanItemType}");
            }
            else
            {
                nextBatchLink = nextBatchLink.Replace("/rest/", Config.RestApiUrl);
                result = await ApiClient.GetAsync<IssueCollectionDto>(nextBatchLink);
            }
            return CheckContent(result);
        }

        public async Task<Issue1CollectionDto> GetProjectIssuesAsync(string orgId, string projectId)
        {
            var result = await ApiClient.PostAsync<Issue1CollectionDto>($"org/{orgId}/project/{projectId}/aggregated-issues", null);
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

        public SourceMetric GetSourceMetric(ProjectDto project)
        {
            var issueCounts = project.Meta.LatestIssueCounts;
            return new SourceMetric
            {
                LastScan = issueCounts.UpdatedAt?.ToUniversalTime() ?? DateTime.UtcNow,
                Instance = Config.Instance,
                IsSaltminerSource = SnykConfig.IsSaltminerSource,
                SourceType = Config.SourceType,
                SourceId = project.Id,
                VersionId = "",
                Attributes = new Dictionary<string, string>(),
                IssueCount = issueCounts.Critical + issueCounts.High + issueCounts.Medium + issueCounts.Low,
                IssueCountSev1 = issueCounts.High,
                IssueCountSev2 = issueCounts.Medium,
                IssueCountSev3 = issueCounts.Low
            };
        }

        
    }
}
