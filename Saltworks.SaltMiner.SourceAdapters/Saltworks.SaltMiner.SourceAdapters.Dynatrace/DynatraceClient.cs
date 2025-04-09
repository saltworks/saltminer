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
using System.Text.Json;

namespace Saltworks.SaltMiner.SourceAdapters.Dynatrace
{
    public class DynatraceClient : SourceClient
    {
        private readonly DynatraceConfig Config;
        private int RateLimitWaitSec = 15;
        private Auth _token = null;

        private Auth Token
        {
            get
            {
                if (_token == null) { Login(); }
                return _token;
            }
            set
            {
                _token = value;
            }
        }

        public DynatraceClient(ApiClient client, DynatraceConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            var headers = new ApiClientHeaders();
            headers.Add("Content-Type", "application/json");
            SetApiClientDefaults(config.BaseAddress, config.Timeout, headers);
        }

        private void Login()
        {
            var form = new Dictionary<string, string>()
            {
                { "grant_type", "client_credentials" },
                { "client_id", Config.ClientId },
                { "client_secret", Config.ClientSecret }
            };
            try
            {
                var r = ApiClient.ThrowawayPostForm<Auth>(Config.AuthEndpointAddress, form) ?? throw new DynatraceClientAuthenticationException("Failed to call token endpoint - null response.");
                if (!r.IsSuccessStatusCode)
                    throw new DynatraceClientAuthenticationException($"Failed to obtain auth token. [{r.StatusCode}] {r.HttpResponse.ReasonPhrase}");
                Token = r.Content;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is ApiClientUnauthorizedException)
                {
                    throw new DynatraceClientAuthenticationException("401 Unauthorized, check configured credentials.");
                }
                if (ex.InnerException is ApiClientForbiddenException)
                {
                    throw new DynatraceClientAuthenticationException("403 Forbidden, check configured credentials.");
                }
            }
            catch (ApiClientUnauthorizedException)
            {
                throw new DynatraceClientAuthenticationException("401 Unauthorized, check configured credentials.");
            }
            catch (ApiClientForbiddenException)
            {
                throw new DynatraceClientAuthenticationException("403 Forbidden, check configured credentials.");
            }
            catch (Exception ex)
            {
                throw new DynatraceClientAuthenticationException($"Token authentication API call failed with error [{ex.GetType().Name}] {ex.Message}");
            }
            Logger.LogDebug("Token authentication complete");
        }

        private async Task<ApiClientResponse<T>> RequestAsync<T>(string jsonRequestBody, int retries = 0, bool suppressError = true) where T : class
        {
            ApiClientResponse<T> r;
            ApiClient.Options.ExceptionOnFailure = suppressError;
            try
            {
                r = await ApiClient.PostAsync<T>("query:execute?enrich=metric-metadata", jsonRequestBody, ApiClientHeaders.AuthorizationBearerHeader(Token.AccessToken));

                if (r.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    var rc = r.RawContent.Length > 1000 ? r.RawContent[..999] : r.RawContent;
                    if (retries > Config.ApiRetryCount)
                    {
                        Logger.LogError("API call failure (http 500 response) - first 1000 chars of raw content: {Rc}", rc);
                        throw new DynatraceClientException($"API call failed with 500 server error, max retries of {retries} reached.");
                    }
                    else
                    {
                        Logger.LogWarning("API call failure (http 500 response), will retry in 90 sec - first 1000 chars of raw content: {Rc}", rc);
                        await Task.Delay(90000);
                        return await RequestAsync<T>(jsonRequestBody, retries + 1);
                    }
                }
            }
            catch (ApiClientBadRequestException ex)
            {
                ErrorDto error = null;
                try
                {
                    error = JsonSerializer.Deserialize<ErrorDto>(ex.ResponseContent, JsonSerializerOptions.Web);
                }
                catch (Exception)
                {
                    // ignore, means it wasn't an error response and we don't know how to handle it
                }
                var errMsg = "Bad request.";
                if (error != null)
                    errMsg += $"API response: {error.Error?.Details?.ErrorMessage ?? "[unreadable error response]"}";
                throw new ApiClientException(errMsg); // Logged outside this call so we don't need to
            }
            catch (ApiClientUnauthorizedException apicex)
            {
                if (retries > 0)
                {
                    throw new DynatraceClientAuthenticationException("Failed authentication retry.");
                }
                Logger.LogWarning(apicex, "Token invalid/missing, attempting to obtain new token");
                Login();
                return await RequestAsync<T>(jsonRequestBody, retries + 1);
            }
            catch (ApiClientOtherException ex)
            {
                if (ex.Message != "Too Many Requests" && ex.Status != System.Net.HttpStatusCode.TooManyRequests)
                    throw;
                await HandleRateLimitRetryAsync(retries);
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
                throw new DynatraceClientTimeoutException($"Wiz API retry count ({Config.ApiRetryCount}) reached.");
            }
            Logger.LogWarning("{Name} exception thrown, retrying ({Cur} of {RetryCount} after a 90s delay.)", exceptionName, retries, Config.ApiRetryCount);
            await Task.Delay(90000);
        }

        private async Task HandleRateLimitRetryAsync(int retries)
        {
            if (retries > 1)
                RateLimitWaitSec *= 4;
            if (RateLimitWaitSec > 3840) // give up if still rate limited at 64 min
            {
                Logger.LogError("Rate limiting still encountered after {Secs} delay, giving up.", RateLimitWaitSec);
                throw new DynatraceClientTimeoutException("Rate limiting exceeded configured retries, unable to continue.");
            }
            Logger.LogWarning("Rate limiting encountered, waiting {Secs} before continuing...)", RateLimitWaitSec);
            await Task.Delay(RateLimitWaitSec);
        }

        /// <summary>
        /// Gets Entity info
        /// </summary>
        /// <param name="afterToken">Used for pagination, gets the next set of results by last Id.</param>
        /// <param name="size">Number of results to return at once, defaults to 1000 if null, and scales down automatically if API rejects.</param>
        public async Task<ApiClientResponse<DqlResponse<EntityRecord>>> GetEntitiesAsync(string afterToken = "", int size = 1000, int retryCount = 0)
        {
            var request = @"fetch dt.entity.process_group | filter id > \""{afterToken}\"" | sort id asc | fields Name = entity.name, id";
            if (!string.IsNullOrEmpty(Config.EntityQueryOverride))
                request = Config.EntityQueryOverride;
            request = request.Replace("{afterToken}", afterToken);

            var query = $@"{{""query"": ""{request}"",
                ""timezone"": ""UTC"",
                ""locale"": ""en_US"",
                ""maxResultRecords"": {size},
                ""maxResultBytes"": 1000000,
                ""fetchTimeoutSeconds"": 60,
                ""requestTimeoutMilliseconds"": 1000,
                ""enablePreview"": true,
                ""defaultSamplingRatio"": 1000,
                ""defaultScanLimitGbytes"": 100,
                ""queryOptions"": null
            }}";

            var attempts = 1;
            ApiClientResponse<DqlResponse<EntityRecord>> rsp = null;
            while (attempts < 10)
            {
                rsp = await RequestAsync<DqlResponse<EntityRecord>>(query);
                try
                {
                    CheckContent(rsp);
                }
                catch (DynatraceApiCallException ex)
                {
                    Logger.LogError(ex, "Exception: {Message}", ex.Message);
                    return null;
                }

                if (rsp.Content.State == "SUCCEEDED")
                {
                    return rsp;
                }
                else
                {
                    attempts++;
                }
            }
            Logger.LogError("Get entities failed with max attempts (state: {State}).", rsp?.Content.State);
            return null;
        }

        /// <summary>
        /// Gets Issues info
        /// </summary>
        /// /// <param name="assetId">Used to retrieve issues for a specific asset</param>
        /// <param name="afterToken">Used for pagination, gets the next set of results by last Id.</param>
        /// <param name="size">Number of results to return at once, defaults to 1000 if null, and scales down automatically if API rejects.</param>
        public async Task<ApiClientResponse<DqlResponse<SecurityEventRecord>>> GetIssuesByIdAsync(string assetId, string afterToken = "", int? size = 1000, int retryCount = 10)
        {
            var request = @"fetch events | filter event.type == \""VULNERABILITY_STATE_REPORT_EVENT\"" | filter event.level == \""ENTITY\"" | filter matchesPhrase(dt.entity.process_group, \""{assetId}\"") | filter vulnerability.id > \""{afterToken}\"" | sort timestamp desc | summarize {vulnerability.resolution.status = takeFirst(vulnerability.resolution.status), `InternetExposure` = takeFirst(vulnerability.davis_assessment.exposure_status), `DataExposure` = takeFirst(vulnerability.davis_assessment.data_assets_status), `VulnerableFunction` = takeFirst(vulnerability.davis_assessment.vulnerable_function_status), `PublicExploit` = takeFirst(vulnerability.davis_assessment.exploit_status),   vulnerability.resolution.change_date = takeFirst(vulnerability.resolution.change_date), vulnerability.mute.status = takeFirst(vulnerability.mute.status), vulnerability.davis_assessment.score = takeFirst(vulnerability.davis_assessment.score), vulnerability.davis_assessment.level = takeFirst(vulnerability.davis_assessment.level), vulnerability.title = takeFirst(vulnerability.title), vulnerability.external_id = takeFirst(vulnerability.external_id), vulnerability.references.cve = takeFirst(vulnerability.references.cve), timestamp=takeFirst(timestamp), vulnerability.id = takeFirst(vulnerability.id), related_entities.hosts.ids = takeFirst(related_entities.hosts.ids), related_entities.hosts.names = takeFirst(related_entities.hosts.names), vulnerability.parent.first_seen = takeFirst(vulnerability.parent.first_seen), vulnerability.description = takeFirst(vulnerability.description), vulnerability.external_url = takeFirst(vulnerability.external_url), vulnerability.url = takeFirst(vulnerability.url), affected_entity.vulnerable_component.id = takeFirst(affected_entity.vulnerable_component.id), affected_entity.vulnerable_component.name = takeFirst(affected_entity.vulnerable_component.name), affected_entity.vulnerable_component.package_name = takeFirst(affected_entity.vulnerable_component.package_name), affected_entity.vulnerable_component.short_name = takeFirst(affected_entity.vulnerable_component.short_name), vulnerability.tracking_link.text = takeFirst(vulnerability.tracking_link.text), vulnerability.tracking_link.url = takeFirst(vulnerability.tracking_link.text), vulnerability.remediation.description = takeFirst(vulnerability.remediation.description)}, by:{vulnerability.display_id} | filter vulnerability.resolution.status == \""OPEN\"" | sort vulnerability.id asc | fields DisplayId = vulnerability.display_id, TimeStamp = timestamp, Id = vulnerability.id, Title = vulnerability.title, Description = vulnerability.description, `InternetExposure`,`DataExposure`,`VulnerableFunction`, `PublicExploit`, Score = round(vulnerability.davis_assessment.score,decimals:1), Level = vulnerability.davis_assessment.level, Status = vulnerability.resolution.status, StatusDate = formatTimestamp(toTimestamp(vulnerability.resolution.change_date), format:\""MMM dd, HH:mm:ss\""), Cve = vulnerability.references.cve, ExternalId = vulnerability.external_id, related_entities.hosts.names, related_entities.hosts.ids, FirstSeen = vulnerability.parent.first_seen, ApiUrl = vulnerability.external_url, GuiUrl = vulnerability.url, MuteStatus = vulnerability.mute.status, vulnerability.tracking_link.text, vulnerability.tracking_link.url, vulnerability.remediation.description";
            if (!string.IsNullOrEmpty(Config.VulnQueryOverride))
                request = Config.VulnQueryOverride;
            request = request.Replace("{assetId}", assetId).Replace("{afterToken}", afterToken);
            var query = $@"{{""query"": ""{request}"",
                ""timezone"": ""UTC"",
                ""locale"": ""en_US"",
                ""maxResultRecords"": {size},
                ""maxResultBytes"": 1000000,
                ""fetchTimeoutSeconds"": 60,
                ""requestTimeoutMilliseconds"": 1000,
                ""enablePreview"": true,
                ""defaultSamplingRatio"": 1000,
                ""defaultScanLimitGbytes"": 100,
                ""queryOptions"": null
            }}";

            var attempts = 1;
            ApiClientResponse<DqlResponse<SecurityEventRecord>> rsp = null;
            while (attempts < retryCount)
            {
                rsp = await RequestAsync<DqlResponse<SecurityEventRecord>>(query);
                try
                {
                    CheckContent(rsp);
                }
                catch (DynatraceApiCallException ex)
                {
                    Logger.LogError(ex, "Exception: {Message}", ex.Message);
                    return null;
                }

                if (rsp.Content.State == "SUCCEEDED")
                {
                    return rsp;
                }
                else
                {
                    attempts++;
                }
            }

            Logger.LogError("Get Issues failed with max attempts (state: {State}).", rsp?.Content.State);
            return null;
        }

        public T CheckContent<T>(ApiClientResponse<T> result) where T : class
        {
            if (result.RawContent.Contains("errorCode"))
            {
                throw new DynatraceApiCallException(result.RawContent);
            }

            return result.Content;
        }

        public SourceMetric GetSourceMetric(EntityRecord asset)
        {
            var issueCounts = 0; // project.Meta.LatestIssueCounts;
            return new SourceMetric
            {
                LastScan = null,
                Instance = Config.Instance,
                IsSaltminerSource = DynatraceConfig.IsSaltminerSource,
                SourceType = Config.SourceType,
                SourceId = asset.Id,
                VersionId = "",
                Attributes = new Dictionary<string, string>(),
            };
        }
    }
}
