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

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.Utility.ApiHelper;



namespace Saltworks.SaltMiner.SourceAdapters.CheckmarxOne
{
    public class CheckmarxOneClient : SourceClient
    {
        private readonly CheckmarxOneConfig Config;
        private AuthDto AuthDto;
        private ApiClient Client;
        public CheckmarxOneClient(ApiClient client, CheckmarxOneConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            SetApiClientDefaults(Config.BaseAddress, Config.Timeout);
            SetToken(Config.IsOAuth2);
        }

        public void SetToken(bool isOAuth2)
        {           
            Dictionary<string, string> data;
      
            if (isOAuth2)
            {
                data = new()
                {
                    { "client_id", Config.ClientId.ToString()},
                    { "client_secret", Config.ClientSecret.ToString()}
                };
            }
            else
            {
                data = new ()
                {
                    {"grant_type","refresh_token" },
                    {"client_id", "ast-app" },
                    {"refresh_token", Config.ApiKey.ToString() }

                };

            }
            var result = ApiClient.ThrowawayPostForm<AuthDto>($"{Config.AuthBaseAddress}/realms/{Config.TenantName}/protocol/openid-connect/token", data);
            var authHdrs = ApiClientHeaders.AuthorizationBearerHeader(result.Content.Token);
            authHdrs.Add(ApiClientHeaders.OneHeader("accept", "application/json"));
            ApiClient.Options.DefaultHeaders.Headers.Clear();
            ApiClient.Options.DefaultHeaders.Add(authHdrs);

        }
        public async Task<TotalProjectsDto> GetProjectsAsync(int Limit, int Offset)
        {

           
           var result = await ApiClient.GetAsync<TotalProjectsDto>($"/projects?limit={Limit}&offset={Offset}");
           return CheckContent(result);

            
        }
        public async Task<List<ResultsOverviewDTO>> GetResultsOverviewAsync(string projectId)
        {

            var result = await ApiClient.GetAsync<List<ResultsOverviewDTO>>($"/results-overview/projects?projectIds={projectId}");
            
            return CheckContent(result);
        }

        public async Task<ScansDTO> GetScansAsync(string projectId)
        {
      
            string scansString = $"/scans?project-ids={projectId}";
           
            try
            {
                var result = await ApiClient.GetAsync<ScansDTO>(scansString);
                return result.Content; 
                
            }
            catch(Exception ex)
            {
                CheckErrorAsync(scansString);
                throw new Exception($"{ex}");
            }
            
        }

        public async void CheckErrorAsync(string errorCall)
        {
            try
            {
                var result = await ApiClient.GetAsync<ErrorDTO>(errorCall);
                throw new Exception($"{result.Content}");

            }
            catch(Exception ex)
            {
                
                throw new Exception($"{ex}");
                
            }
        }

        public async Task<ScanResultsDTO> GetScanResultsAsync(string ScanId, int Limit, int Offset)
        {
        
            var result = await ApiClient.GetAsync<ScanResultsDTO>($"/results?scan-id={ScanId}&limit={Limit}&offset={Offset}&sort=-type");
            return CheckContent(result);
        }

        public async Task<ScanDetailsDTO> GetScanDetailsAsync(string ScanId)
        {

            var result = await ApiClient.GetAsync<ScanDetailsDTO>($"/scans/{ScanId}");
            return CheckContent(result);
        }

        public async Task<ScanSummariesDTO> GetScanSummaryAsync(string ScanId)
        {

            var result = await ApiClient.GetAsync<ScanSummariesDTO>($"/scan-summary?scan-ids={ScanId}");
            return CheckContent(result);
        }

        public async Task<ApplicationsDTO> GetApplicationsAsync()
        {

            var result = await ApiClient.GetAsync<ApplicationsDTO>($"/applications");
            return CheckContent(result);
        }

        public async Task<ApplicationDetailsDTO> GetApplicationDetailsAsync(string ApplicationId)
        {

            var result = await ApiClient.GetAsync<ApplicationDetailsDTO>($"/applications/{ApplicationId}");
            return CheckContent(result);
        }

        public async Task<SourceMetric> GetSourceMetric(ProjectDto project)
        {
            var resultsOverview = await GetResultsOverviewAsync(project.ID);
           
            var result = resultsOverview.FirstOrDefault(); 
             
            int criticalCount = 0;
            int highCount = 0;
            int mediumCount = 0;
            int lowCount = 0;

            foreach (ResultsOverviewSeverityCounterDTO counter in result.SeverityCounters)

                if (counter.Severity == "Critical")
                {
                    criticalCount = counter.Counter;
                }
                else if (counter.Severity == "High")
                {
                    highCount = counter.Counter;
                }
                else if (counter.Severity == "Medium")
                {
                    mediumCount = counter.Counter;
                }
                else if (counter.Severity == "Low")
                {
                    lowCount = counter.Counter;
                }

            return new SourceMetric
            {
                Instance = Config.Instance,
                IsSaltminerSource = CheckmarxOneConfig.IsSaltminerSource,
                SourceType = Config.SourceType,
                SourceId = project.ID,
                VersionId = "",
                Attributes = new Dictionary<string, string>(),
                IssueCount = result.TotalCounter, 
                IssueCountSev1 = criticalCount, 
                IssueCountSev2 = highCount, 
                IssueCountSev3 = mediumCount, 
                IssueCountSev4 = lowCount 
            };
        }


        public T CheckContent<T>(ApiClientResponse<T> result) where T : class
        {
            
            if (result.RawContent.Contains("errorCode"))
            {
                throw new CheckmarxOneClientException(result.RawContent);
            }

            return result.Content;
        }
    }
}
