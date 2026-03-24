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
using Newtonsoft.Json.Linq;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.Utility.ApiHelper;

namespace Saltworks.SaltMiner.SourceAdapters.GitLab
{
    public class GitLabClient : SourceClient
    {
        private readonly GitLabConfig Config;
        public GitLabClient(ApiClient client, GitLabConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            var headers = new ApiClientHeaders();
            headers.Add("Content-Type", "application/json");
            headers.Add(ApiClientHeaders.AuthorizationBearerHeader(Config.ApiKey));
            SetApiClientDefaults(config.BaseAddress, config.Timeout, headers);
        }

        //Here are all the calls to the Source API to get data and massage it into DTOs

        public async Task<GraphQLResponse<ProjectGroupDto>> GetProjectsByGroupAsync(string projectGroupFullPath, string nextBatchLink = "", int batchSize = 100)
        {
            var request = @"{""query"": ""query { group(fullPath: \""~3~\"") { projects (first: ~1~ ~2~) { pageInfo { hasNextPage endCursor hasPreviousPage startCursor } nodes { id  name nameWithNamespace description  fullPath openIssuesCount path archived repository { diskPath empty exists rootRef } createdAt group {fullName fullPath name} lastActivityAt topics updatedAt} } } }""}";
            var result = await RequestAsync<GraphQLResponse<ProjectGroupDto>>(SetRequestOptions(request, nextBatchLink, batchSize, projectGroupFullPath));
            return CheckContent(result);
        }

        public async Task<GraphQLResponse<ProjectDataDto>> GetProjectAssetAsync(string projectFullPath)
        {
            var request = @"{""query"": ""query { project(fullPath: \""~1~\"") {id  name nameWithNamespace description  fullPath openIssuesCount path repository { diskPath empty exists rootRef } createdAt group {fullName fullPath name} lastActivityAt topics updatedAt } }""} ";
            var result = await RequestAsync<GraphQLResponse<ProjectDataDto>>(SetRequestOptions(request, projectFullPath));
            return CheckContent(result);
        }

        public async Task<GraphQLResponse<ScanDataDto>> GetLatestProjectScanAsync(string projectFullPath, string nextBatchLink = "", int batchSize = 1)
        {
            var request = @"{""query"": ""{ project(fullPath: \""~3~\"") { pipelines(first: ~1~ ~2~) { pageInfo { hasNextPage endCursor hasPreviousPage startCursor } nodes { id createdAt  updatedAt name path ref refPath commitPath committedAt jobs { nodes { name, status }  }  status } } } }""}";
            var result = await RequestAsync<GraphQLResponse<ScanDataDto>>(SetRequestOptions(request, nextBatchLink, batchSize, projectFullPath));
            return CheckContent(result);
        }

        public async Task<GraphQLResponse<ScanDataDto>> GetProjectScansAsync(string projectFullPath, string nextBatchLink = "", int batchSize = 100)
        {
            var request = @"{""query"": ""{ project(fullPath: \""~3~\"") { pipelines(first: ~1~ ~2~) { pageInfo { hasNextPage endCursor hasPreviousPage startCursor } nodes { id createdAt  updatedAt name path ref refPath commitPath committedAt jobs { nodes { name, status }  }  status } } } }""}";
            var result = await RequestAsync<GraphQLResponse<ScanDataDto>>(SetRequestOptions(request, nextBatchLink, batchSize, projectFullPath));
            return CheckContent(result);
        }

        public async Task<GraphQLResponse<IssueDataDto>> GetProjectVulnerabilitiesAsync(string projectFullPath, string nextBatchLink = "", int batchSize = 100)
        {
            var request = @"{""query"": ""{ project(fullPath: \""~3~\"") { vulnerabilities(first: ~1~ ~2~) { pageInfo { hasNextPage endCursor hasPreviousPage startCursor } nodes { id  title description solution  links { url } severity detectedAt hasRemediations  cvss { baseScore overallScore, severity, vendor, version } dismissalReason dismissedAt falsePositive  identifiers { externalId externalType name url }      location { ... on VulnerabilityLocationDependencyScanning {  file blobPath }     ... on VulnerabilityLocationContainerScanning { image containerRepositoryUrl }     ... on VulnerabilityLocationClusterImageScanning { image }    ... on VulnerabilityLocationSast { file blobPath }   ... on VulnerabilityLocationCoverageFuzzing { file blobPath }   ... on VulnerabilityLocationDast { path } ... on VulnerabilityLocationGeneric { description } ... on VulnerabilityLocationSecretDetection { file blobPath }    },      presentOnDefaultBranch  resolvedAt  resolvedOnDefaultBranch  scanner {id name vendor reportType}  state  stateComment updatedAt  uuid  vulnerabilityPath  webUrl } } } }""}";
            var result = await RequestAsync<GraphQLResponse<IssueDataDto>>(SetRequestOptions(request, nextBatchLink, batchSize, projectFullPath));
            return CheckContent(result);
        }

        public async Task<GraphQLResponse<GroupDataDto>> GetNamespaceGroupsAsync(string groupNamespace, string nextBatchLink = "", int batchSize = 100)
        {
            var request = @"{""query"": ""{ groups(search: \""~3~\"", first: ~1~ ~2~) { pageInfo { hasNextPage endCursor hasPreviousPage startCursor } nodes { id name fullPath projectsCount } } }""}";
            var result = await RequestAsync<GraphQLResponse<GroupDataDto>>(SetRequestOptions(request, nextBatchLink, batchSize, groupNamespace));
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

        public SourceMetric GetSourceMetric(ProjectNodeDto project, DateTime? latestScanDate = null)
        {
            return new SourceMetric
            {
                LastScan = latestScanDate,
                Instance = Config.Instance,
                IsSaltminerSource = GitLabConfig.IsSaltminerSource,
                SourceType = Config.SourceType,
                SourceId = project.Id,
                VersionId = "",
                Attributes = [],
            };
        }

        private static string SetRequestOptions(string request, string nextBatchLink, int batchSize, string projectFullPath = "")
        {
            request = request.Replace("~1~", Convert.ToString(batchSize));
            var afterOption = !string.IsNullOrEmpty(nextBatchLink) ? $@", after: \""{nextBatchLink}\""" : string.Empty;
            request = request.Replace("~2~", afterOption);
            if (!string.IsNullOrEmpty(projectFullPath))
            {
                request = request.Replace("~3~", projectFullPath);
            }
            return request;
        }

        private static string SetRequestOptions(string request, string projectFullPath)
        {
            request = request.Replace("~1~", projectFullPath);
            return request;
        }

        private async Task<ApiClientResponse<T>> RequestAsync<T>(string jsonRequestBody, int retries = 0, bool suppressError = true) where T : class
        {
            ApiClientResponse<T> r;
            ApiClient.Options.ExceptionOnFailure = suppressError;
            try
            {
                r = await ApiClient.PostAsync<T>("", jsonRequestBody, ApiClientHeaders.AuthorizationBearerHeader(Config.ApiKey));

                if (r.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    var rc = r.RawContent.Length > 1000 ? r.RawContent[..999] : r.RawContent;
                    if (retries > Config.ApiRetryCount)
                    {
                        Logger.LogError("API call failure (http 500 response) - first 1000 chars of raw content: {Rc}", rc);
                        throw new GitLabException($"API call failed with 500 server error, max retries of {retries} reached.");
                    }
                    else
                    {
                        Logger.LogWarning("API call failure (http 500 response), will retry in 90 sec - first 1000 chars of raw content: {Rc}", rc);
                        await Task.Delay(90000);
                        return await RequestAsync<T>(jsonRequestBody, retries + 1);
                    }
                }
            }
            catch (ApiClientUnauthorizedException ex)
            {
                if (retries > 0)
                {
                    throw new GitLabClientAuthenticationException("Failed authentication retry.");
                }
                Logger.LogWarning(ex, "Token invalid/missing, attempting to obtain new token");
                return await RequestAsync<T>(jsonRequestBody, retries + 1);
            }
            catch (TimeoutException)
            {
                await HandleExceptionRetryAsync(retries, "Timeout");
                return await RequestAsync<T>(jsonRequestBody, retries + 1);
            }
            catch (ApiClientTimeoutException)
            {
                await HandleExceptionRetryAsync(retries, "ApiClientTimeout");
                return await RequestAsync<T>(jsonRequestBody, retries + 1);
            }
            catch (TaskCanceledException)
            {
                await HandleExceptionRetryAsync(retries, "Task canceled / Timeout");
                return await RequestAsync<T>(jsonRequestBody, retries + 1);
            }
            return r;
        }

        private async Task HandleExceptionRetryAsync(int retries, string exceptionName)
        {
            if (retries > Config.ApiRetryCount)
            {
                throw new GitLabClientTimeoutException($"GitLab API retry count ({Config.ApiRetryCount}) reached.");
            }
            Logger.LogWarning("{name} exception thrown, retrying ({cur} of {retries} after a 90s delay.)", exceptionName, retries, Config.ApiRetryCount);
            await Task.Delay(90000);
        }

    }
}
