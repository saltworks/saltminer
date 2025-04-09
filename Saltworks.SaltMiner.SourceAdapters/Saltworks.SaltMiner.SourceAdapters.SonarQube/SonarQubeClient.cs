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
using Newtonsoft.Json.Linq;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.Utility.ApiHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.SourceAdapters.SonarQube
{
    public class SonarQubeClient : SourceClient
    {
        private readonly SonarQubeConfig Config;

        public SonarQubeClient(ApiClient client, SonarQubeConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            //SetApiClientDefaults(config.BaseAddress, config.Timeout, ApiClientHeaders.OneHeader("Cookie", GetSecurityTokenCookie()));
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Config.UserName}:"));
            SetApiClientDefaults(config.BaseAddress, config.Timeout, ApiClientHeaders.OneHeader("Authorization", $"Basic {authToken} "));
        }

        public async Task<IEnumerable<ComponentDto>> GetComponentsByProjectAsync(string projectKey)
        {
            if (projectKey.Contains(":"))
            {
                var indexOfSplit = projectKey.IndexOf(":");
                projectKey = projectKey.Substring(0, indexOfSplit);
            }

            var project = await ApiClient.GetAsync<ProjectDto>($"components/tree?component={projectKey}");
            var result = new List<ComponentDto>();

            var componentCollection = GetPartialDeserializationForProject(project);
            result.AddRange(componentCollection.Components);
            int pageTotal = componentCollection.Paging.Total / Config.PageSize;
            for (int i = 1; i < pageTotal + 1; i++)
            {
                var nextResults = await ApiClient.GetAsync<ProjectDto>($"components/tree?p={i + 1}&component={projectKey}");
                result.AddRange(nextResults.Content.Components);
            }

            foreach (var component in result)
            {
                component.LastAnalysisDate = component.LastAnalysisDate.ToUniversalTime();
            }

            return result;
        }

        public async IAsyncEnumerable<ComponentDto> GetAllComponentsAsync()
        {
            int currentPage = 1;
            int pageTotal = 0;
            do
            {
                var projects = await ApiClient.GetAsync<ProjectDto>($"projects/search?p={currentPage}");
                var components = projects.Content.Components;

                foreach (var component in components)
                {
                    component.LastAnalysisDate = component.LastAnalysisDate.ToUniversalTime();
                    yield return component;
                }

                if (Config.TestingAssetLimit > 0 && Config.TestingAssetLimit <= components.Count())
                {
                    yield break;
                }

                if (currentPage == 1)
                {
                    pageTotal = (projects.Content.Paging.Total + Config.PageSize - 1) / Config.PageSize;
                }

                currentPage++;
            }
            while (currentPage <= pageTotal);
        }



        public async Task<IEnumerable<IssueDto>> GetIssuesByComponentAsync(string componentKey, DateTime lastAnalysisDate)
        {
            var createdAt = lastAnalysisDate.ToString("yyyy-MM-dd'T'HH:mm:ss+ffff").Replace("+", "%2b");
            var pageIndex = 1;
            var results = new List<IssueDto>();

            IssueCollectionDto issueCollection;
            int pageTotal;

            do
            {
                var issues = await ApiClient.GetAsync<IssueCollectionDto>($"issues/search?p={pageIndex}&components={componentKey}");
                issueCollection = GetPartialDeserializationForIssueCollection(issues);
                results.AddRange(issueCollection.Issues);

                pageTotal = issueCollection.Paging.Total / Config.PageSize;
                pageIndex++;

            } while (pageIndex <= pageTotal);

            foreach (var issue in results)
            {
                issue.CreationDate = issue.CreationDate.ToUniversalTime();
                issue.UpdateDate = issue.UpdateDate.ToUniversalTime();
                if (issue.Comments != null)
                {
                    foreach (var comment in issue.Comments)
                    {
                        comment.CreatedAt = comment.CreatedAt.ToUniversalTime();
                    }
                }
            }

            return results;
        }




        public Task<SourceMetric> SourceMetricAsync(ComponentDto component, SonarQubeConfig config)
        {
            var sourceMetric = new SourceMetric
            {
                LastScan = component.LastAnalysisDate,
                Instance = config.Instance,
                IsSaltminerSource = SonarQubeConfig.IsSaltminerSource,
                SourceType = config.SourceType,
                SourceId = $"{component.Key}|{component.LastAnalysisDate:yyyyMMdd_hh:mm:ss}",
                VersionId = null,
                Attributes = []
            };

            return Task.FromResult(sourceMetric);
        }

        public SourceMetric GetSourceMetric(ComponentDto component, SonarQubeConfig config)
        {
            return new SourceMetric
            {
                LastScan = component.LastAnalysisDate,
                Instance = config.Instance,
                IsSaltminerSource = SonarQubeConfig.IsSaltminerSource,
                SourceType = config.SourceType,
                SourceId = component.SourceId,
                VersionId = null,
                Attributes = new Dictionary<string, string>()
            };
        }
    

    //DEPRECATED JUST SETTING AUTH TOKEN FROM UI GENERATED PAT
    //private string GetSecurityTokenCookie()
    //{
    //    var cookieList = new List<string>();

    //    var form = new Dictionary<string, string>
    //    {
    //        { "login", Config.UserName },
    //        { "password", Config.Password }
    //    };

    //    var loginResponse = ApiClient.ThrowawayPostForm<string>($"{Config.AuthAddress}authentication/login", form);

    //    IEnumerable<string> cookies =
    //        loginResponse.HttpResponse.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;
    //    cookies = cookies.Reverse();
    //    foreach (var cookie in cookies)
    //    {
    //        var stringArray = cookie.Split(";");
    //        var indexOfCookie = Array.FindIndex(stringArray,
    //            s => s.StartsWith("JWT-SESSION=") || s.StartsWith("XSRF-TOKEN="));
    //        if (indexOfCookie != -1)
    //        {
    //            cookieList.Add(stringArray[indexOfCookie]);
    //        }
    //    }

    //    return string.Join(";", cookieList);
    //}

    private IssueCollectionDto GetPartialDeserializationForIssueCollection(ApiClientResponse<IssueCollectionDto> issues)
        {
            JObject iCJObject = JObject.Parse(issues.RawContent);
            JToken p = iCJObject["paging"];
            JToken c = iCJObject["issues"];
            IssueCollectionDto issueCollection = new IssueCollectionDto();
            issueCollection.Paging = p.ToObject<PagingDto>();
            issueCollection.Issues = c.ToObject<List<IssueDto>>();
            return issueCollection;
        }

        private ProjectDto GetPartialDeserializationForProject(ApiClientResponse<ProjectDto> issues)
        {
            JObject iCJObject = JObject.Parse(issues.RawContent);
            JToken p = iCJObject["paging"];
            JToken c = iCJObject["components"];
            ProjectDto project = new ProjectDto();
            project.Paging = p.ToObject<PagingDto>();
            project.Components = c.ToObject<List<ComponentDto>>();
            return project;
        }

    }
}

