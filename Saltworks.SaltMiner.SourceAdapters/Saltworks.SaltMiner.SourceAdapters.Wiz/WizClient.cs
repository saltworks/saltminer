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
using Saltworks.Utility.ApiHelper;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Saltworks.SaltMiner.SourceAdapters.IntegrationTests")]
namespace Saltworks.SaltMiner.SourceAdapters.Wiz
{
    public class WizClient : SourceClient
    {
        private readonly WizConfig Config;
        private WizToken _token = null;
        private int IssueRequestSize = 5000;
        private int VulnRequestSize = 5000;
        internal bool StillLoading = false;
        private bool RateLimited = false;
        private int RateLimitMax;
        private readonly object RateLimitLock = new();
        private readonly List<DateTime> RecentRequestTimes = [];

        private WizToken Token
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

        public WizClient(ApiClient client, WizConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            RateLimitMax = Config.WizRateLimitInitialMaxRequests;
            var hdrs = new ApiClientHeaders();
            hdrs.Add("Accept", "application/json");
            SetApiClientDefaults(config.BaseAddress, config.Timeout, hdrs);
        }

        private void RateLimiter(bool tooManyRequestsError=false)
        {
            lock (RateLimitLock)
            {
                if (tooManyRequestsError)
                {
                    if (RateLimited && RateLimitMax > 1)
                    {
                        RateLimitMax--;
                    }
                    else
                    {
                        if (RateLimitMax <= 1)
                        {
                            Logger.LogError("Rate limit (1/{Sec} sec) minimum reached, but still receiving rate limit error responses from Wiz API.  Cannot continue.", Config.WizRateLimitIntervalSeconds);
                            throw new WizClientException($"Rate limit (1/{Config.WizRateLimitIntervalSeconds} sec) minimum reached, but still receiving rate limit error responses from Wiz API.  Cannot continue.");
                        }
                    }
                    RateLimited = true;
                    Logger.LogWarning("Wiz API call failed due to rate limiting, reducing max API call rate (now set to {Max}/{Secs} sec) ", RateLimitMax, Config.WizRateLimitIntervalSeconds);
                }
                if (!RateLimited) return; // no rate limiting in effect
                RecentRequestTimes.RemoveAll(x => x < DateTime.UtcNow.AddSeconds(-Config.WizRateLimitIntervalSeconds));
                if (RecentRequestTimes.Count >= RateLimitMax)
                {
                    Logger.LogWarning("Rate limiter waiting {Wait} seconds before next Wiz API call.", Config.WizRateLimitDelaySeconds);
                    Task.Delay(TimeSpan.FromSeconds(Config.WizRateLimitDelaySeconds)).Wait();
                }
                RecentRequestTimes.Add(DateTime.UtcNow);
            }
        }

        private void Login()
        {
            var form = new Dictionary<string, string>()
            {
                { "grant_type", "client_credentials" },
                { "audience", "wiz-api" },
                { "client_id", Config.ClientId },
                { "client_secret", Config.ClientSecret }
            };
            try
            {
                var r = ApiClient.ThrowawayPostForm<WizToken>(Config.AuthEndpointAddress, form) ?? throw new WizClientException("Failed to call token endpoint - null response.");
                if (!r.IsSuccessStatusCode)
                    throw new WizClientAuthenticationException($"Failed to obtain auth token. [{r.StatusCode}] {r.HttpResponse.ReasonPhrase}");
                Token = r.Content;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is ApiClientUnauthorizedException)
                {
                    throw new WizClientAuthenticationException("401 Unauthorized, check configured credentials.");
                }
                if (ex.InnerException is ApiClientForbiddenException)
                {
                    throw new WizClientAuthenticationException("403 Forbidden, check configured credentials.");
                }
            }
            catch (ApiClientUnauthorizedException) 
            {
                throw new WizClientAuthenticationException("401 Unauthorized, check configured credentials.");
            }
            catch (ApiClientForbiddenException)
            {
                throw new WizClientAuthenticationException("403 Forbidden, check configured credentials.");
            }
            catch (Exception ex)
            {
                throw new WizClientAuthenticationException($"Token authentication API call failed with error [{ex.GetType().Name}] {ex.Message}");
            }
            Logger.LogDebug("Token authentication complete");
        }

        //private ApiClientResponse Request(string jsonRequestBody, int retries = 0)
        //{
        //    ApiClientResponse r;
        //    try
        //    {
        //        r = ApiClient.Post<string>("", jsonRequestBody, ApiClientHeaders.AuthorizationBearerHeader(Token.AccessToken));
        //    }
        //    catch (ApiClientUnauthorizedException ex)
        //    {
        //        if (retries > 0)
        //        {
        //            throw new WizClientAuthenticationException("Failed authentication retry.");
        //        }
        //        Logger.LogWarning(ex, "Token invalid/missing, attempting to obtain new token");
        //        Login();
        //        return Request(jsonRequestBody, retries + 1);
        //    }
        //    catch (TaskCanceledException ex)
        //    {
        //        if (retries > Config.ApiRetryCount)
        //        {
        //            throw new WizClientTimeoutException($"Wiz API retry count ({Config.ApiRetryCount}) reached.");
        //        }
        //        retries++;
        //        Logger.LogWarning(ex, "TaskCanceled exception thrown, treating as timeout and retrying ({Retries} of {RetryCount} after a short delay.)", retries, Config.ApiRetryCount);
        //        Task.Delay(60000).Wait();
        //        return Request(jsonRequestBody, retries);
        //    }
        //    return r;
        //}

        private async Task<ApiClientResponse<T>> RequestAsync<T>(string jsonRequestBody, int retries=0, bool suppressError=true) where T : class
        {
            RateLimiter();
            ApiClientResponse<T> r;
            ApiClient.Options.ExceptionOnFailure = suppressError;
            try
            {
                r = await ApiClient.PostAsync<T>("", jsonRequestBody, ApiClientHeaders.AuthorizationBearerHeader(Token.AccessToken));

                if (r.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    var rc = r.RawContent.Length > 1000 ? r.RawContent[..999] : r.RawContent;
                    if (retries > Config.ApiRetryCount)
                    {
                        Logger.LogError("API call failure (http 500 response) - first 1000 chars of raw content: {Rc}", rc);
                        throw new WizClientException($"API call failed with 500 server error, max retries of {retries} reached.");
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
                    throw new WizClientAuthenticationException("Failed authentication retry.");
                }
                Logger.LogWarning(ex, "Token invalid/missing, attempting to obtain new token");
                Login();
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
            catch (ApiClientException ex)
            {
                // ApiClient doesn't throw a too many requests exception...
                if (ex.Status == System.Net.HttpStatusCode.TooManyRequests)
                {
                    RateLimiter(true);
                    return await RequestAsync<T>(jsonRequestBody, retries);
                }
                throw; // rethrow other ApiClientExceptions
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
                throw new WizClientTimeoutException($"Wiz API retry count ({Config.ApiRetryCount}) reached.");
            }
            Logger.LogWarning("{Name} exception thrown, retrying ({Retries} of {RetryCount} after a 90s delay.)", exceptionName, retries, Config.ApiRetryCount);
            await Task.Delay(90000);
        }

        /// <summary>
        /// Gets updated assets based on issues starting at passed date
        /// </summary>
        /// <param name="after">Gets results where status changed after the passed datetime.</param>
        /// <param name="afterToken">Used for pagination, gets the next set of results.</param>
        /// <param name="size">Number of results to return at once, defaults to 5000 if null, and scales down automatically if API rejects.</param>
        public async Task<ApiClientResponse<IssueAssetDataDto>> IssuesGetUpdatedAssetsAsync(DateTime after, string afterToken, int? size) =>
            await IssuesGetUpdatedAssetsAsync(after, afterToken, size, 0);
        private async Task<ApiClientResponse<IssueAssetDataDto>> IssuesGetUpdatedAssetsAsync(DateTime after, string afterToken, int? size, int retryCount)
        {
            if (size != null)
                IssueRequestSize = size.Value;
            var query = @"{
                 ""query"": ""query IssuesTable($filterBy: IssueFilters $first: Int $after: String $orderBy: IssueOrder) {     issuesV2(filterBy: $filterBy first: $first after: $after orderBy: $orderBy) {       nodes {         id         createdAt         updatedAt         dueAt         projects {           id           name           slug           businessUnit           riskProfile {             businessImpact           }         }         status         severity         entitySnapshot {           id           type           nativeType           name           status           cloudPlatform           cloudProviderURL           providerId           region           resourceGroupExternalId           subscriptionExternalId           subscriptionName           subscriptionTags           tags           externalId         }         note         serviceTickets {           externalId           name           url         }       }       pageInfo {         hasNextPage         endCursor       }     } }"",
                 ""variables"": {
                     ""first"": ~1~, ~2~
                     ""filterBy"": {
                          ""statusChangedAt"": {
                               ""after"": ""~3~""
                          }
                     },
                    ""orderBy"": {
                        ""field"": ""STATUS_CHANGED_AT"",
                        ""direction"": ""ASC""
                    }
                 }
            }".Replace("~1~", IssueRequestSize.ToString())
            .Replace("~2~", string.IsNullOrEmpty(afterToken) ? "" : $"\"after\": \"{afterToken}\",")
            .Replace("~3~", after.ToString("o"));
            ApiClientResponse<IssueAssetDataDto> rsp = null;
            var retries = 3;
            var curTry = 1;
            while (curTry <= retries)
            {
                try
                {
                    rsp = await RequestAsync<IssueAssetDataDto>(query);
                    break; // it worked, break out of retry loop
                }
                catch (Exception ex)
                {
                    if (curTry <= retries)
                    {
                        Logger.LogError(ex, "Get assets via issues API call failed due to error, retrying in 60 sec...");
                        curTry++;
                        await Task.Delay(TimeSpan.FromSeconds(60));
                    }
                    else
                    {
                        Logger.LogError(ex, "Get assets via issues API call failed due to error (after {Retries} retries): [{Type}] {Msg}", retries, ex.GetType().Name, ex.Message);
                        throw new WizClientException($"Get assets via issues API call failed due to error (after {retries} retries.", ex);
                    }
                }
            }
            try
            {
                WizResponseErrorCheck(rsp);  // throws exception if response has error
            }
            catch (WizApiCallException ex)
            {
                var firstErr = "'first' must be less than or equal to ";
                if (ex.Response.Message.StartsWith(firstErr) && retryCount == 0)
                {
                    IssueRequestSize = int.Parse(ex.Response.Message.Replace(firstErr, ""));
                    Logger.LogWarning(ex, "API call rejected due to first parameter, retrying with indicated first max value ({Val})...", IssueRequestSize);
                    return await IssuesGetUpdatedAssetsAsync(after, afterToken, null, 1);
                }
                else
                {
                    Logger.LogWarning("API call rejected - see earlier logging for details.");
                }
            }
            return rsp;
        }

        /// <summary>
        /// Gets updated issues based on passed date
        /// </summary>
        /// <param name="assetId">Asset ID for which to retrieve issues.</param>
        /// <param name="afterToken">Used for pagination, gets the next set of results.  Null is ok for starting at the beginning.</param>
        /// <param name="size">Number of results to return at once, defaults to 5000 if null, and scales down automatically if API rejects.</param>
        public async Task<ApiClientResponse<IssueDataDto>> IssuesGetAsync(string assetId, string afterToken, int? size) =>
            await IssuesGetAsync(assetId, afterToken, size, 0);
        private async Task<ApiClientResponse<IssueDataDto>> IssuesGetAsync(string assetId, string afterToken, int? size, int retryCount)
        {
            if (string.IsNullOrEmpty(assetId))
                throw new ArgumentNullException(nameof(assetId));
            if (size != null)
                IssueRequestSize = size.Value;
            var query = @"{
                 ""query"": ""query IssuesTable($filterBy: IssueFilters $first: Int $after: String $orderBy: IssueOrder) {     issuesV2(filterBy: $filterBy first: $first after: $after orderBy: $orderBy) {       nodes {         id         sourceRules {           id           name           description   }         createdAt         updatedAt         resolvedAt         dueAt         projects {           id           name           slug           businessUnit           riskProfile {             businessImpact           }         }         status         severity         entitySnapshot {           id           type           nativeType           name           status           cloudPlatform           cloudProviderURL           providerId           region           resourceGroupExternalId           subscriptionExternalId           subscriptionName           subscriptionTags           tags           externalId         }         note         serviceTickets {           externalId           name           url         }       }       pageInfo {         hasNextPage         endCursor       }     } }"",
                 ""variables"": {
                    ""first"": ~1~, ~2~
                    ""filterBy"": {
                        ""relatedEntity"": {
                            ""id"": ""~3~""
                        }
                    }
                 }
            }".Replace("~1~", IssueRequestSize.ToString())
            .Replace("~2~", string.IsNullOrEmpty(afterToken) ? "" : $"\"after\": \"{afterToken}\",")
            .Replace("~3~", assetId);
            var rsp = await RequestAsync<IssueDataDto>(query);
            try
            {
                WizResponseErrorCheck(rsp);  // throws exception if response has error
            }
            catch (WizApiCallException ex)
            {
                var firstErr = "'first' must be less than or equal to ";
                if (ex.Response.Message.StartsWith(firstErr) && retryCount == 0)
                {
                    IssueRequestSize = int.Parse(ex.Response.Message.Replace(firstErr, ""));
                    Logger.LogWarning(ex, "API call rejected due to first parameter, retrying with indicated first max value ({Val})...", IssueRequestSize);
                    return await IssuesGetAsync(assetId, afterToken, null, 1);
                }
                else
                {
                    Logger.LogWarning("API call rejected - see earlier logging for details.");
                }
            }
            return rsp;
        }


        /// <summary>
        /// Gets recently updated vuln assets (only) based on passed date
        /// </summary>
        /// <param name="after">Gets results where update occurred after the passed datetime.</param>
        /// <param name="afterToken">Include this to continue the query results at the next page (batch).  Null is ok for starting at the beginning.</param>
        /// <param name="size">Number of results to return at once - defaults to 5000 but may be limited by API to 500.</param>
        public async Task<ApiClientResponse<VulnerabilityFindingsAssetsDataDto>> VulnsGetUpdatedAssetsAsync(DateTime? after, string afterToken, int? size)
        {
            return await VulnsGetUpdatedAssetsAsync(after, afterToken, size, 0);
        }
        private async Task<ApiClientResponse<VulnerabilityFindingsAssetsDataDto>> VulnsGetUpdatedAssetsAsync(DateTime? after, string afterToken, int? size, int retryCount)
        {
            if (size != null)
                VulnRequestSize = size.Value;
            // set query to full set of fields only if asset passed; otherwise, set to just asset id and firstDetectedAt
            var query = @"query VulnerabilityFindingsPage($filterBy: VulnerabilityFindingFilters $first: Int $after: String $orderBy: VulnerabilityFindingOrder) {  vulnerabilityFindings(filterBy: $filterBy first: $first after: $after orderBy: $orderBy) {  nodes  {  id  firstDetectedAt  vulnerableAsset { ... on VulnerableAssetBase { id  name } } }  pageInfo {  hasNextPage  endCursor  } } }";
            var body = @"{                
                ""query"": """ + query + @""",
                ""variables"": {
                    ""first"": ~1~, ~2~
                    ""orderBy"": {
                        ""direction"": ""ASC"",
                        ""field"": ""CREATED_AT""
                    },
                    ""filterBy"": {
                         ""firstSeenAt"": { ""after"": ""~3~"" }
                    }
                }
            }".Replace("~1~", VulnRequestSize.ToString())
            .Replace("~2~", string.IsNullOrEmpty(afterToken) ? "" : $"\"after\": \"{afterToken}\",")
            .Replace("~3~", after?.ToString("o"));
            ApiClientResponse<VulnerabilityFindingsAssetsDataDto> rsp = null;
            var retries = 3;
            var curTry = 1;
            while (curTry <= retries)
            {
                try
                {
                    rsp = await RequestAsync<VulnerabilityFindingsAssetsDataDto>(body);
                    break; // it worked, break out of retry loop
                }
                catch (Exception ex)
                {
                    if (curTry <= retries)
                    {
                        Logger.LogError(ex, "Get assets via vulns API call failed due to error, retrying in 60 sec...");
                        curTry++;
                        await Task.Delay(TimeSpan.FromSeconds(60));
                    }
                    else
                    {
                        Logger.LogError(ex, "Get assets via vulns API call failed due to error (after {Retries} retries): [{Type}] {Msg}", retries, ex.GetType().Name, ex.Message);
                        throw new WizClientException($"Get assets via vulns API call failed due to error (after {retries} retries.", ex);
                    }
                }
            }
            try
            {
                WizResponseErrorCheck(rsp);  // throws exception if response has error
            }
            catch (WizApiCallException ex)
            {
                var firstErr = "'first' must be less than or equal to ";
                if (ex.Response.Message.StartsWith(firstErr) && retryCount == 0)
                {
                    VulnRequestSize = int.Parse(ex.Response.Message.Replace(firstErr, ""));
                    Logger.LogWarning(ex, "API call rejected due to first parameter, retrying with indicated first max value ({Val})...", VulnRequestSize);
                    return await VulnsGetUpdatedAssetsAsync(after, afterToken, null, 1);
                }
                else
                {
                    Logger.LogWarning("API call rejected - see earlier logging for details.");
                    throw;
                }
            }
            return rsp;
        }

        /// <summary>
        /// Gets recently updated vulns based on passed date
        /// </summary>
        /// <param name="assetId">Asset ID for which to pull vulns.</param>
        /// <param name="afterToken">Include this to continue the query results at the next page (batch).  Null is ok for starting at the beginning.</param>
        /// <param name="size">Number of results to return at once - defaults to 5000 but may be limited by API to 500.</param>
        public async Task<ApiClientResponse<VulnerabilityFindingsDataDto>> VulnsGetAsync(string assetId, string afterToken, int? size) =>
            await VulnsGetAsync(assetId, afterToken, size, 0);
        private async Task<ApiClientResponse<VulnerabilityFindingsDataDto>> VulnsGetAsync(string assetId, string afterToken, int? size, int retryCount)
        {
            if (string.IsNullOrEmpty(assetId))
                throw new ArgumentNullException(nameof(assetId));
            if (size != null)
                VulnRequestSize = size.Value;

            // set query to full set of fields only if asset passed; otherwise, set to just asset id and firstDetectedAt
            var body = @"{                
                ""query"": ""query VulnerabilityFindingsPage($filterBy: VulnerabilityFindingFilters $first: Int $after: String $orderBy: VulnerabilityFindingOrder) { vulnerabilityFindings(filterBy: $filterBy first: $first after: $after orderBy: $orderBy) {     nodes {  id  portalUrl  name  CVEDescription  CVSSSeverity  score  exploitabilityScore  impactScore  dataSourceName  hasExploit  hasCisaKevExploit  status  vendorSeverity  firstDetectedAt  lastDetectedAt  resolvedAt  description  remediation  detailedName  version  fixedVersion  detectionMethod  link  locationPath  resolutionReason  epssSeverity  epssPercentile  epssProbability  validatedInRuntime  layerMetadata { id  details  isBaseLayer }  projects { id  name  slug  businessUnit  riskProfile {  businessImpact } }  ignoreRules { id  name  enabled  expiredAt  }  vulnerableAsset {  ... on VulnerableAssetBase {  id  type  name  region  providerUniqueId  cloudProviderURL  cloudPlatform  status  subscriptionName  subscriptionExternalId  subscriptionId  tags  hasLimitedInternetExposure  hasWideInternetExposure  isAccessibleFromVPN  isAccessibleFromOtherVnets  isAccessibleFromOtherSubscriptions  }  ... on VulnerableAssetVirtualMachine {  operatingSystem  ipAddresses  }  ... on VulnerableAssetServerless {  runtime }  ... on VulnerableAssetContainerImage {  imageId  }    ... on VulnerableAssetContainer {  ImageExternalId  VmExternalId  ServerlessContainer  PodNamespace  PodName  NodeName  }  }  }  pageInfo {  hasNextPage  endCursor } } }"",
                ""variables"": {
                    ""first"": ~1~, ~2~
                    ""orderBy"": { 
                        ""direction"": ""DESC"",
                        ""field"": ""CREATED_AT""
                    },
                    ""filterBy"": {
                        ""assetId"": ""~3~""
                    }
                }
            }".Replace("~1~", VulnRequestSize.ToString())
            .Replace("~2~", string.IsNullOrEmpty(afterToken) ? "" : $"\"after\": \"{afterToken}\",")
            .Replace("~3~", assetId);
            var rsp = await RequestAsync<VulnerabilityFindingsDataDto>(body);
            try
            {
                WizResponseErrorCheck(rsp);  // throws exception if response has error
            }
            catch (WizApiCallException ex)
            {
                var firstErr = "'first' must be less than or equal to ";
                if (ex.Response.Message.StartsWith(firstErr) && retryCount == 0)
                {
                    VulnRequestSize = int.Parse(ex.Response.Message.Replace(firstErr, ""));
                    Logger.LogWarning(ex, "API call rejected due to first parameter, retrying with indicated first max value ({Val})...", VulnRequestSize);
                    return await VulnsGetAsync(assetId, afterToken, null, VulnRequestSize);
                }
                else
                {
                    Logger.LogWarning("API call rejected - see earlier logging for details.");
                }
            }
            return rsp;
        }

        private void WizResponseErrorCheck<T>(ApiClientResponse<T> response) where T : DataDto
        {
            if ((response.Content.Errors?.Count ?? 0) > 0)
            {
                var i = 0;
                ResponseError err = null;
                foreach (var error in response.Content.Errors)
                {
                    Logger.LogInformation("Wiz API call error details ({Idx}): '{Msg}', line {Line}, col {Col}, code '{Code}', path '{Path}'",
                        i, error.Message, error.Locations?[0]?.Line ?? 0, error.Locations?[0]?.Column ?? 0, error.Extensions?.Code, error.Path?[0]);
                    if (i == 0)
                        err = error;  // throw first error info after logging all
                    i++;
                }
                throw new WizApiCallException($"Wiz API call failure", err);
            }
            else
            {
                if (!response.IsSuccessStatusCode)
                    throw new WizClientException($"Wiz API call failure - [{response.StatusCode}] {response.ReasonPhrase}");
            }
        }
    }
}