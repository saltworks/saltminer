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

namespace Saltworks.SaltMiner.SourceAdapters.Traceable
{
    public class TraceableClient : SourceClient
    {
        private readonly TraceableConfig Config;
        private int RateLimitWaitSec = 15;

        protected static string StartTime => DateTime.Now.AddDays(-90).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
        protected static string EndTime => DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

        #region Query Constants

        private const string VULN_QUERY = @"
        query Vulnerabilities {
          vulnerabilitiesV3(
            filter: {
              logicalFilter: {
                operator: AND
                filters: [
                  {
                    logicalFilter: {
                      operator: OR
                      filters: [
                        {
                          logicalFilter: {
                            operator: AND
                            filters: [
                              {
                                relationalFilter: {
                                  key: API_DISCOVERY_STATE
                                  operator: IN
                                  value: [""DISCOVERED"", ""UNDER_DISCOVERY""]
                                }
                              }
                              {
                                relationalFilter: {
                                  key: SOURCES
                                  operator: IN
                                  value: [
                                    ""VULNERABILITY_SOURCE_TYPE_RUNTIME_PROTECTION""
                                    ""VULNERABILITY_SOURCE_TYPE_GATEWAY_CONFIGURATION""
                                    ""VULNERABILITY_SOURCE_TYPE_COMPLIANCE""
                                  ]
                                }
                              }
                            ]
                          }
                        }
                        {
                          relationalFilter: {
                            key: SOURCES
                            operator: IN
                            value: [""VULNERABILITY_SOURCE_TYPE_SECURITY_TESTING""]
                          }
                        }
                      ]
                    }
                  }
                  {
                    relationalFilter: {
                      key: ENTITY_ID
                      operator: EQUALS
                      value: ""~~EntityId~~""
                    }
                  }
                ]
              }
            }
            orderBy: { key: LAST_SEEN_TIMESTAMP_MILLIS, direction: DESC }
            limit: ~~Limit~~
            offset: ~~Offset~~
          ) {
            results {
              VulnerabilityId: selection(key: VULNERABILITY_ID) { value }
              EntityId: selection(key: ENTITY_ID) { value }
              VulnerabilityCategory: selection(key: VULNERABILITY_CATEGORY) { value }
              Status: selection(key: STATUS) { value }
              AffectedSpanPath: selection(key: AFFECTED_SPAN_PATH) { value }
              LastSeenTimestampMillis: selection(key: LAST_SEEN_TIMESTAMP_MILLIS) { value }
              CreatedTimestampMillis: selection(key: CREATED_TIMESTAMP_MILLIS) { value }
              ClosedTimestampMillis: selection(key: CLOSED_TIMESTAMP_MILLIS) { value }
              ScanId: selection(key: SCAN_ID) { value }
              Sources: selection(key: SOURCES) { value }
              EnvironmentId: selection(key: ENVIRONMENT_ID) { value }
              Severity: selection(key: SEVERITY) {value }
              CvssScore: selection(key: CVSS_SCORE) {value }
              VulnerabilitySubCategory: selection(key: VULNERABILITY_SUB_CATEGORY) {value }
              DisplayName: selection(key: DISPLAY_NAME) {value }
              OwaspApiTop10: selection(key: OWASP_API_TOP10) {value }
              AffectedDomainIds: selection(key: AFFECTED_DOMAIN_IDS) { value }
              AffectedDomainNames: selection(key: AFFECTED_DOMAIN_NAMES) { value }
              apiEntity: entity(type: ""API"") { 
                id
                id1: attribute(expression: {key: ""id""})
                name: attribute(expression: {key: ""name""})
                serviceId: attribute(expression: {key: ""serviceId""})
                serviceName: attribute(expression: {key: ""serviceName""})
                __typename
              }
              __typename
            }
            count
            total
            __typename
          }
        }";
        private const string ENTITY_QUERY = @"
        query {
          entities(
            scope: ""API""
            limit: ~~Limit~~
            between: {
              startTime: ""~~StartTime~~""
              endTime: ""~~EndTime~~""
            }
            offset: ~~Offset~~
            filterBy: [
              {
                keyExpression: { key: ""isLearnt"" }
                operator: EQUALS
                value: true
                type: ATTRIBUTE
              }
              {
                keyExpression: { key: ""apiDiscoveryState"" }
                operator: IN
                value: [""DISCOVERED"", ""UNDER_DISCOVERY""]
                type: ATTRIBUTE
              }
            ]
            includeInactive: true
          ) {
            results {
              entityId: id
              id: attribute(expression: { key: ""id"" })
              name: attribute(expression: { key: ""name"" })
              isLearnt: attribute(expression: { key: ""isLearnt"" })
              dataTypeIds: attribute(expression: { key: ""dataTypeIds"" })
              riskScore: attribute(expression: { key: ""riskScore"" })
              riskScoreCategory: attribute(expression: { key: ""riskScoreCategory"" })
              numCalls: metric(expression: { key: ""numCalls"" }) {
                sum {
                  value
                  __typename
                }
                __typename
              }
              lastCalledTime: attribute(expression: { key: ""lastCalledTime"" })
              isAuthenticated: attribute(expression: { key: ""isAuthenticated"" })
              serviceId: attribute(expression: { key: ""serviceId"" })
              changeLabel: attribute(expression: { key: ""changeLabel"" })
              changeLabelTimestamp: attribute(
                expression: { key: ""changeLabelTimestamp"" }
              )
              isExternal: attribute(expression: { key: ""isExternal"" })
              labels {
                count
                total
                results {
                  id
                  key
                  description
                  color
                  __typename
                }
                __typename
              }
              isEncrypted: attribute(expression: { key: ""isEncrypted"" })
              lastScanTimestamp: attribute(expression: { key: ""lastScanTimestamp"" })
              environment: attribute(expression: { key: ""environment"" })
              serviceName: attribute(expression: { key: ""serviceName"" })
              __typename
            }
            total
            __typename
          }
        }";

        #endregion

        public TraceableClient(ApiClient client, TraceableConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            var headers = new ApiClientHeaders();
            headers.Add("Content-Type", "application/json");
            headers.Add(ApiClientHeaders.AuthorizationCustomHeader(Config.ApiKey));
            SetApiClientDefaults(config.BaseAddress, config.Timeout, headers);
        }

        public async Task<GraphQLResponse<EntityDataDto>> GetEntitiesAsync(int offset = 0, int batchSize = 500)
        {
            var request = ENTITY_QUERY.Replace("~~StartTime~~", StartTime).Replace("~~EndTime~~", EndTime).Replace("~~Offset~~", offset.ToString()).Replace("~~Limit~~", batchSize.ToString())
                .Replace("\r\n", "").Replace("  ", " ").Replace("  ", " ");
            var body = JsonSerializer.Serialize(new { query = request });
            //var request = $@"{{ ""query"": "" query {{ entities( scope: \""API\"" limit: {batchSize} between: {{startTime: \""{StartTime}\"", endTime: \""{EndTime}\""}} offset: {offset}  filterBy: [{{keyExpression: {{key: \""isLearnt\""}}, operator: EQUALS, value: true, type: ATTRIBUTE}},  {{keyExpression: {{key: \""apiDiscoveryState\""}}, operator: IN, value: [\""DISCOVERED\"", \""UNDER_DISCOVERY\""], type: ATTRIBUTE}}] includeInactive: true ) {{ results {{ entityId: id id: attribute(expression: {{key: \""id\""}}) name: attribute(expression: {{key: \""name\""}}) isLearnt: attribute(expression: {{key: \""isLearnt\""}}) dataTypeIds: attribute(expression: {{key: \""dataTypeIds\""}}) riskScore: attribute(expression: {{key: \""riskScore\""}}) riskScoreCategory: attribute(expression: {{key: \""riskScoreCategory\""}}) numCalls: metric(expression: {{key: \""numCalls\""}}) {{ sum {{ value __typename }} __typename }} lastCalledTime: attribute(expression: {{key: \""lastCalledTime\""}}) isAuthenticated: attribute(expression: {{key: \""isAuthenticated\""}}) serviceId: attribute(expression: {{key: \""serviceId\""}}) changeLabel: attribute(expression: {{key: \""changeLabel\""}}) changeLabelTimestamp: attribute(expression: {{key: \""changeLabelTimestamp\""}}) isExternal: attribute(expression: {{key: \""isExternal\""}}) labels {{ count total results {{ id key description color __typename }} __typename }} isEncrypted: attribute(expression: {{key: \""isEncrypted\""}}) lastScanTimestamp: attribute(expression: {{key: \""lastScanTimestamp\""}}) environment: attribute(expression: {{key: \""environment\""}}) serviceName: attribute(expression: {{key: \""serviceName\""}}) __typename }} total __typename }} }}"" }}";
            var result = await RequestAsync<GraphQLResponse<EntityDataDto>>(body);
            return CheckContent(result);
        }


        // For a performance boost...
        // We can use something like this in the vulns query to pull more than one entity at a time (missing brackets for relationalFilter to appease linter):
        // relationalFilter: 
        //   key: API_ID
        //   operator: IN
        //   value: ["0cd83cf4-39ee-3782-a429-a94d4d4ddf48"]
        // 
        public async Task<GraphQLResponse<VulnerabilityDataDto>> GetAssetVulnerabilitiesAsync(string entityId, int offset = 0, int batchSize = 1000)
        {
            var request = VULN_QUERY.Replace("~~EntityId~~", entityId).Replace("~~Offset~~", offset.ToString()).Replace("~~Limit~~", batchSize.ToString())
                .Replace("\r\n", "").Replace("  ", " ").Replace("  ", " ");
            var body = JsonSerializer.Serialize(new { query = request });
            //var request = $@"{{""query"": "" query Vulnerabilities {{ vulnerabilitiesV3(       filter: {{logicalFilter: {{operator: AND, filters: [{{logicalFilter: {{operator: OR, filters: [{{logicalFilter: {{operator: AND, filters: [{{relationalFilter: {{key: API_DISCOVERY_STATE, operator: IN, value: [\""DISCOVERED\"", \""UNDER_DISCOVERY\""]}}}}, {{relationalFilter: {{key: SOURCES, operator: IN, value: [\""VULNERABILITY_SOURCE_TYPE_RUNTIME_PROTECTION\"", \""VULNERABILITY_SOURCE_TYPE_GATEWAY_CONFIGURATION\""]}}}}]}}}}, {{relationalFilter: {{key: SOURCES, operator: IN, value: [\""VULNERABILITY_SOURCE_TYPE_SECURITY_TESTING\""]}}}}]}}}},    {{ relationalFilter: {{ key: ENTITY_ID, operator: EQUALS, value: \""{entityId}\"" }}}}  ]}}}}      orderBy: {{key: LAST_SEEN_TIMESTAMP_MILLIS, direction: DESC}} limit: {batchSize} offset: {offset} )        {{ results  {{ VulnerabilityId: selection(key: VULNERABILITY_ID) {{ value }} EntityId: selection(key: ENTITY_ID) {{ value }} VulnerabilityCategory: selection(key: VULNERABILITY_CATEGORY) {{ value }} Status: selection(key: STATUS) {{ value }} AffectedSpanPath: selection(key: AFFECTED_SPAN_PATH) {{ value }} LastSeenTimestampMillis: selection(key: LAST_SEEN_TIMESTAMP_MILLIS) {{ value }} CreatedTimestampMillis: selection(key: CREATED_TIMESTAMP_MILLIS) {{ value }} ClosedTimestampMillis: selection(key: CLOSED_TIMESTAMP_MILLIS) {{ value }} ScanId: selection(key: SCAN_ID) {{ value }} Sources: selection(key: SOURCES) {{ value }} EnvironmentId: selection(key: ENVIRONMENT_ID) {{ value }} Severity: selection(key: SEVERITY) {{value }}   CvssScore: selection(key: CVSS_SCORE) {{value }}  VulnerabilitySubCategory: selection(key: VULNERABILITY_SUB_CATEGORY) {{value }}   DisplayName: selection(key: DISPLAY_NAME) {{value }}   OwaspApiTop10: selection(key: OWASP_API_TOP10) {{value }}               apiEntity: entity(type: \""API\"") {{ id id1: attribute(expression: {{key: \""id\""}}) name: attribute(expression: {{key: \""name\""}}) serviceId: attribute(expression: {{key: \""serviceId\""}}) serviceName: attribute(expression: {{key: \""serviceName\""}}) __typename }} __typename }} count total __typename }} }}""}}";
            var result = await RequestAsync<GraphQLResponse<VulnerabilityDataDto>>(body);
            return CheckContent(result);
        }


        public static T CheckContent<T>(ApiClientResponse<T> result) where T : class
        {
            ErrorResponseDto rsp;
            try
            {
                // if we can deserialize an error response then throw an error
                rsp = JsonSerializer.Deserialize<ErrorResponseDto>(result.RawContent, JsonSerializerOptions.Web);
                if (rsp?.Errors?[0] == null)
                    rsp = null;
            }
            catch (Exception)
            {
                rsp = null;
            }
            if (rsp != null)
                throw new TraceableClientException($"API call failed with error: {rsp.Errors[0].Message}");
            return result.Content;
        }

        public SourceMetric GetSourceMetric(EntityResultsDto project, DateTime? latestScanDate = null, int issueCount = 0)
        {
            return new SourceMetric
            {
                LastScan = latestScanDate,
                Instance = Config.Instance,
                IsSaltminerSource = TraceableConfig.IsSaltminerSource,
                SourceType = Config.SourceType,
                SourceId = project.Id,
                VersionId = "",
                IssueCount = issueCount,
                Attributes = [],
            };
        }

        private async Task<ApiClientResponse<T>> RequestAsync<T>(string jsonRequestBody, int retries = 0, bool suppressError = true) where T : class
        {
            ApiClientResponse<T> r;
            ApiClient.Options.ExceptionOnFailure = suppressError;
            try
            {
                r = await ApiClient.PostAsync<T>("", jsonRequestBody, ApiClientHeaders.AuthorizationCustomHeader(Config.ApiKey));

                if (r.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    var rc = r.RawContent.Length > 1000 ? r.RawContent[..999] : r.RawContent;
                    if (retries > Config.ApiRetryCount)
                    {
                        Logger.LogError("API call failure (http 500 response) - first 1000 chars of raw content: {Rc}", rc);
                        throw new TraceableException($"API call failed with 500 server error, max retries of {retries} reached.");
                    }
                    else
                    {
                        Logger.LogWarning("API call failure (http 500 response), will retry in 90 sec - first 1000 chars of raw content: {Rc}", rc);
                        await Task.Delay(90000);
                        return await RequestAsync<T>(jsonRequestBody, retries + 1);
                    }
                }
            }
            catch (ApiClientUnauthorizedException apiAuthEx)
            {
                if (retries > 0)
                {
                    throw new TraceableClientAuthenticationException("Failed authentication retry.");
                }
                Logger.LogWarning(apiAuthEx, "Token invalid/missing, attempting to obtain new token");
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
            catch (ApiClientOtherException ex)
            {
                if (ex.Message != "Too Many Requests" && ex.Status != System.Net.HttpStatusCode.TooManyRequests)
                    throw;
                await HandleRateLimitRetryAsync(retries);
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
                throw new TraceableClientTimeoutException($"Traceable API retry count ({Config.ApiRetryCount}) reached.");
            }
            Logger.LogWarning("{Name} exception thrown, retrying ({Cur} of {ApiCount} after a 90s delay.)", exceptionName, retries, Config.ApiRetryCount);
            await Task.Delay(90000);
        }

        private async Task HandleRateLimitRetryAsync(int retries)
        {
            if (retries > 1)
                RateLimitWaitSec *= 4;
            if (RateLimitWaitSec > 3840) // give up if still rate limited at 64 min
            {
                Logger.LogError("Rate limiting still encountered after {Secs} delay, giving up.", RateLimitWaitSec);
                throw new TraceableClientTimeoutException("Rate limiting exceeded configured retries, unable to continue.");
            }
            Logger.LogWarning("Rate limiting encountered, waiting {Secs} before continuing...)", RateLimitWaitSec);
            await Task.Delay(RateLimitWaitSec);
        }
    }
}
