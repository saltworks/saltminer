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
 * internal License.
 *
 * ----
 */

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.Utility.ApiHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.SourceAdapters.Sonatype
{
    internal class SonatypeClient : SourceClient
    {
        private readonly SonatypeConfig Config;
        private List<OrganizationDto> Organizations { get; set; } = [];
        internal SonatypeClient(ApiClient client, SonatypeConfig config, ILogger logger) : base(client, logger)
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
            ApiClient.Options.ExceptionOnFailure = !suppressError;
            try
            {
                if (string.IsNullOrEmpty(jsonRequestBody)) 
                    r = await ApiClient.GetAsync<T>(url, jsonRequestBody);
                else
                    r = await ApiClient.PostAsync<T>(url, jsonRequestBody);

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

        internal async Task<ApplicationCollectionDto> GetAppsAsync()
        {
            var result = await RequestAsync<ApplicationCollectionDto>("applications");
            if (Config.TestingAssetLimit > 0)
                result.Content.Applications = result.Content.Applications.Take(Config.TestingAssetLimit).ToList();
            return result.Content;
        }

        internal async Task<IEnumerable<ComponentDto>> GetAppReportComponentsAsync(string appId, string reportId)
        {
            return (await RequestAsync<ComponentCollectionsDto>($"applications/{appId}/reports/{reportId}/policy")).Content.Components;
        }

        internal async Task<OrganizationDto> GetOrganizationByOrgIdAsync(string orgId)
        {
            // Total organizations shouldn't be huge, so cache them to avoid an API call for org info with each application processed
            if (Organizations.Count == 0)
                Organizations = (await RequestAsync<OrganizationCollectionDto>($"organizations")).Content.Organizations;
            return Organizations.FirstOrDefault(x => x.Id == orgId);
        }

        internal async Task<IEnumerable<Report>> GetReportsAsync(ApplicationDto app)
        {
            return (await RequestAsync<IEnumerable<Report>>($"reports/applications/{app.Id}")).Content;            
        }
    }
}
