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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.SourceAdapters.BlackDuck
{
    public class BlackDuckClient : SourceClient
    {
        private readonly BlackDuckConfig Config;
        public BlackDuckClient(ApiClient client, ApiClient bearerClient, BlackDuckConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            
            try
            {
                var bearerToken = bearerClient.Post<AuthenticateDTO>(config.BaseAddress + "tokens/authenticate", null, ApiClientHeaders.OneHeader("Authorization", "token " + config.AccessToken)); 
                SetApiClientDefaults(config.BaseAddress, config.Timeout, ApiClientHeaders.AuthorizationBearerHeader(bearerToken.Content.BearerToken), true);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<ProjectDTO>> GetProjects()
        {
            var result = await ApiClient.GetAsync<ResponseDTO<ProjectDTO>>("projects");

            if (Config.TestingAssetLimit > 0)
            {
                result.Content.Items = result.Content.Items.Take(Config.TestingAssetLimit).ToList();
            }

            return result.Content.Items;
        }

        public async Task<ProjectDTO> GetProject(string projectId)
        {
            var result = await ApiClient.GetAsync<ProjectDTO>($"projects/{projectId}");
            return result.Content;
        }

        public async Task<IEnumerable<ProjectVerisonDTO>> GetProjectVerisons(string projectId)
        {
            var result = await ApiClient.GetAsync<ResponseDTO<ProjectVerisonDTO>>($"projects/{projectId}/versions");
            return result.Content.Items;
        }

        public async Task<ProjectVerisonDTO> GetProjectVerison(string projectId, string projectVerisonId)
        {
            var result = await ApiClient.GetAsync<ProjectVerisonDTO>($"projects/{projectId}/versions/{projectVerisonId}");
            return result.Content;
        }


        public async Task<IEnumerable<ProjectVerisonVulnComponentDTO>> GetProjectVerisonsVulnerabileComponents(string projectId, string projectVerisonId)
        {
            var result = await ApiClient.GetAsync<ResponseDTO<ProjectVerisonVulnComponentDTO>>($"projects/{projectId}/versions/{projectVerisonId}/vulnerability-bom");
            return result.Content.Items;
        }

        public async Task<IEnumerable<VulnerabilityDTO>> GetComponentVulnerabilities(string url)
        {
            var result = await ApiClient.GetAsync<ResponseDTO<VulnerabilityDTO>>(url);
            return result.Content.Items;
        }

        public async Task<App> GetAppReportAsync(string projectId, string projectVerisonId)
        {
            var version = projectVerisonId == null ? await GetProjectVerison(projectId, projectVerisonId) : null;
            var result = new App
            {
                EvaluationDate = version?.CreatedAt.ToUniversalTime(),
                ProjectId = projectId,
                Version = version?.VersionName,
                VersionId = projectVerisonId,
                Components = projectVerisonId == null ? (await GetProjectVerisonsVulnerabileComponents(projectId, projectVerisonId)).Select(x => new AppComponents(x)).ToList() : new List<AppComponents>()
            };

            var vulernabilityCount = 0;
            foreach (var component in result.Components)
            {
                if (!string.IsNullOrEmpty(component.VulnerabilitiesUrl))
                {
                    component.Vulnerabilities = await GetComponentVulnerabilities(component.VulnerabilitiesUrl);
                }
                vulernabilityCount += component.Vulnerabilities.Count();
            }

            result.VulernabilityCount = vulernabilityCount;

            return result;
        }
        
        public async Task<SourceClientResult<SourceMetric>> SourceMetricsAsync(List<ProjectDTO> projects)
        {
            try
            {
                var results = new List<SourceMetric>();

                foreach (var app in projects)
                {
                    var projectId = app.GetProjectId();
                    var versions = await GetProjectVerisons(projectId);
                    if (versions.Any())
                    {
                        foreach (var verison in versions)
                        {
                            var versionId = verison.GetProjectVerisonId();

                            results.Add(new SourceMetric
                            {
                                LastScan = verison.CreatedAt.ToUniversalTime(),
                                Instance = "",
                                IsSaltminerSource = BlackDuckConfig.IsSaltminerSource,
                                SourceType = Config.SourceType,
                                SourceId = $"{projectId}|{versionId}",
                                VersionId = versionId,
                                Attributes = new Dictionary<string, string>()
                            });
                        }
                    } else
                    {
                        results.Add(new SourceMetric
                        {
                            LastScan = null,
                            Instance = "",
                            IsSaltminerSource = BlackDuckConfig.IsSaltminerSource,
                            SourceType = Config.SourceType,
                            SourceId = $"{projectId}|",
                            VersionId = null,
                            Attributes = new Dictionary<string, string>()
                        });
                    }
                }

                return new SourceClientResult<SourceMetric>() { Results = results };
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
