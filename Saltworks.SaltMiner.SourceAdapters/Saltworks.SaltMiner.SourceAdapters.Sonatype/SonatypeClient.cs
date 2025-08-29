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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.SourceAdapters.Sonatype
{
    public class SonatypeClient : SourceClient
    {
        private readonly SonatypeConfig Config;
        public SonatypeClient(ApiClient client, SonatypeConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            SetApiClientDefaults(config.BaseAddress, config.Timeout, ApiClientHeaders.AuthorizationBasicHeader(config.UserName, config.Password), true);
        }

        private async Task HandleExceptionRetryAsync(int retries, string exceptionName)
        {
            if (retries > Config.ApiRetryCount)
            {
                throw new SonatypeClientException($"Sonatype API retry count ({Config.ApiRetryCount}) reached.");
            }
            Logger.LogWarning("{Name} exception thrown, retrying ({Retries} of {RetryCount} after a 90s delay.)", exceptionName, retries, Config.ApiRetryCount);
            await Task.Delay(90000);
        }

        private async Task<ApiClientResponse<T>> RequestAsync<T>(string url, string jsonRequestBody=null, int retries = 0, bool suppressError = true) where T : class
        {
            ApiClientResponse<T> r;
            ApiClient.Options.ExceptionOnFailure = suppressError;
            try
            {
                if (string.IsNullOrEmpty(jsonRequestBody)) 
                    r = await ApiClient.PostAsync<T>(url, jsonRequestBody);
                else
                    r = await ApiClient.GetAsync<T>(url, jsonRequestBody);

                if (r.StatusCode == System.Net.HttpStatusCode.InternalServerError || r.StatusCode == System.Net.HttpStatusCode.BadGateway)
                {
                    var rc = r.RawContent.Length > 1000 ? r.RawContent[..999] : r.RawContent;
                    if (retries > Config.ApiRetryCount)
                    {
                        Logger.LogError("API call failure (http {Status} response) - first 1000 chars of raw content: {Rc}", r.StatusCode.GetHashCode(), rc);
                        throw new SonatypeClientException($"API call failed with 500 server error, max retries of {retries} reached.");
                    }
                    else
                    {
                        Logger.LogWarning("API call failure (http 500 response), will retry in 90 sec - first 1000 chars of raw content: {Rc}", rc);
                        await Task.Delay(90000);
                        return await RequestAsync<T>(url, jsonRequestBody, retries + 1);
                    }
                }
            }
            catch (TimeoutException)
            {
                await HandleExceptionRetryAsync(retries, "Timeout");
                return await RequestAsync<T>(url, jsonRequestBody, retries + 1);
            }
            catch (ApiClientTimeoutException)
            {
                await HandleExceptionRetryAsync(retries, "ApiClientTimeout");
                return await RequestAsync<T>(url, jsonRequestBody, retries + 1);
            }
            catch (TaskCanceledException)
            {
                await HandleExceptionRetryAsync(retries, "Task canceled / Timeout");
                return await RequestAsync<T>(url, jsonRequestBody, retries + 1);
            }
            return r;
        }

        public async Task<ApplicationCollectionDto> GetAppsAsync()
        {
            var result = await RequestAsync<ApplicationCollectionDto>("applications");

            if (Config.TestingAssetLimit > 0)
            {
                result.Content.Applications = result.Content.Applications.Take(Config.TestingAssetLimit).ToList();
            }

            return result.Content;
        }

        public async Task<IEnumerable<Report>> GetAppReportsAsync(string appId, string stage)
        {
            var reports = (await RequestAsync<IEnumerable<Report>>($"reports/applications/{appId}")).Content;
            return reports.OrderByDescending(x => x.EvaluationDate.ToUniversalTime()).GroupBy(x => x.Stage).SelectMany(g => g).Where(x => x.Stage == stage);
        }

        public async Task<IEnumerable<ComponentDto>> GetAppReportComponentsAsync(string appId, string reportId)
        {
            var result = await RequestAsync<ComponentCollectionsDto>($"applications/{appId}/reports/{reportId}/policy");
            return result.Content.Components;
        }

        public async Task<OrganizationDto> GetOrganizationByOrgIdAsync(string orgId)
        {
            var result = await RequestAsync<OrganizationDto>($"organizations/{orgId}");
            return result.Content;
        }

        public async Task<SourceClientResult<SourceMetric>> SourceMetricsAsync(ApplicationDto app, SonatypeConfig config)
        {
                var results = new List<SourceMetric>();
                var reports = await RequestAsync<IEnumerable<Report>>($"reports/applications/{app.Id}");
                var groupedReports = reports.Content.OrderByDescending(x => x.EvaluationDate.ToUniversalTime()).GroupBy(x => x.Stage).Select(x => x.First());

                if (groupedReports != null && groupedReports.Any())
                {
                    results.AddRange(groupedReports.Select(x => x.ToSourceMetric(app, config)));
                }
                else
                {
                    results.Add(new SourceMetric
                    {
                        LastScan = null,
                        Instance = Config.Instance,
                        IsSaltminerSource = SonatypeConfig.IsSaltminerSource,
                        SourceType = config.SourceType,
                        SourceId = $"{app.Id}|",
                        VersionId = null,
                        Attributes = []
                    });
                }

                return new SourceClientResult<SourceMetric>() { Results = results };
        }
    }
}
