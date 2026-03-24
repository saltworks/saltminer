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

﻿using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.Utility.ApiHelper;
using System.Net;

namespace Saltworks.SaltMiner.SourceAdapters.Debricked
{
    public class DebrickedClient : SourceClient
    {
        private readonly DebrickedConfig Config;
        private ApiClient BearerClient;
        private AuthDto AuthDto;
        public DebrickedClient(ApiClient client, ApiClient bearerClient, DebrickedConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            BearerClient = bearerClient;
            SetAuthToken();
        }

        public void SetAuthToken()
        {
            try
            {
                AuthDto = GetAuthToken();
                SetApiClientDefaults(Config.BaseAddress, Config.Timeout, ApiClientHeaders.AuthorizationBearerHeader(AuthDto.Token), true);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public AuthDto GetAuthToken()
        {
            var data = new
            {
                _username = Config.Username.ToString(),
                _password = Config.Password.ToString()
            };

            var result = BearerClient.Post<AuthDto>($"{Config.BaseAddress}login_check", data);
            return CheckContent(result);
        }

        public async Task<List<RepositoryDto>> GetRepositories()
        {
            var result = await ApiClient.GetAsync<List<RepositoryDto>>($"1.0/open/repositories/get-repositories-names-links");
            return CheckContent(result);
        }

        public async Task<DependencyHierarchyDto> GetDependencies(int repoId)
        {
            var result = await ApiClient.GetAsync<DependencyHierarchyDto>($"1.0/open/dependencies/get-dependencies-hierarchy?repositoryId={repoId}");
            return CheckContent(result);
        }

        public async Task<VulnerabilitiesDto> GetVulnerabilities(int dependencyId)
        {
            var result = await ApiClient.GetAsync<VulnerabilitiesDto>($"1.0/open/vulnerabilities/get-vulnerabilities?dependencyId={dependencyId}");
            return CheckContent(result);
        }

        public async Task<SbomDto> GenerateSbom(SbomRequest requestBody)
        {
            var result = await ApiClient.PostAsync<SbomDto>($"1.0/open/sbom/generate-cyclonedx-sbom", requestBody);
            return CheckContent(result);
        }

        public SbomReportDto GetSbomReport(string reportId)
        {
            var result = ApiClient.Get<SbomReportDto>($"1.0/open/sbom/download-generated-cyclonedx-sbom?reportUuid={reportId}");
            return CheckContent(result);
        }

        public SourceClientResult<SourceMetric> GetSourceMetrics(SbomReportDto sbom, DebrickedConfig config)
        {
            var metrics = new List<SourceMetric>();

            if (sbom.Components != null)
            {
                foreach (var component in sbom.Components)
                {
                    metrics.Add(new SourceMetric
                    {
                        LastScan = DateTime.Now.ToUniversalTime(),
                        Instance = config.Instance,
                        IsSaltminerSource = DebrickedConfig.IsSaltminerSource,
                        SourceType = config.SourceType,
                        SourceId = $"{component.BomRef}",
                        Attributes = new Dictionary<string, string>(),
                        IsNotScanned = false
                    });
                }
            }
            return new SourceClientResult<SourceMetric>() { Results = metrics };
        }

        public T CheckContent<T>(ApiClientResponse<T> result) where T : class
        {
            if (result.RawContent.Contains("errorCode"))
            {
                throw new Exception(result.RawContent);
            }

            return result.Content;
        }
    }
}
