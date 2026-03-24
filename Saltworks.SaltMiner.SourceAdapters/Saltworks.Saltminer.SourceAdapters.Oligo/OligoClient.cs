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

﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.Utility.ApiHelper;

namespace Saltworks.SaltMiner.SourceAdapters.Oligo
{
    public class OligoClient : SourceClient
    {
        private readonly OligoConfig Config;
        public OligoClient(ApiClient client, OligoConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            SetApiClientDefaults(config.BaseAddress, config.Timeout);
            SetToken();
        }

        public void SetToken()
        {
            Dictionary<string, string> data = new()
            {
                {"scope", "openid offline_access" },
                { "client_id", "api"},
                { "client_secret", Config.ClientSecret.ToString()},
                { "grant_type", "client_credentials"}
            };

            var result = ApiClient.ThrowawayPostForm<AuthDto>($"{Config.BaseAddress}/auth/realms/{Config.ClientId}/protocol/openid-connect/token", data);
            var authHdrs = ApiClientHeaders.AuthorizationBearerHeader(result.Content.Token);
            ApiClient.Options.DefaultHeaders.Add(authHdrs);



        }//Here are all the calls to the Source API to get data and massage it into DTOs
        public async Task<List<VulnerabilityDTO>> GetVulnerabilitiesAsync(int limit, int offset)
        {           
            int retryCount = 0;
            while (true)
            {
                try
                {
                    var result = await ApiClient.GetAsync<List<VulnerabilityDTO>>($"/api/v2/vulnerabilities?per-page={limit}&page={offset}");
                    return result.Content;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    var msg = ex.Message.Length > 500 ? ex.Message[..499] : ex.Message;
                    Logger.LogInformation(ex, "[Client] GetVulnerabilitiesAsync failed ([{Type}] {Msg}), retrying", ex.GetType().Name, msg);
                    if (ex.Message == "Unauthorized")
                    {
                        Logger.LogInformation("[Client] Access Token expired, resetting then retrying");
                        retryCount--;
                        SetToken();
                    }
                    if (retryCount >= Config.ClientMaxRetries)
                    {
                        CheckErrorAsync("/api/v1/vulnerabilities");
                        throw new OligoClientException($"[Client] GetVulnerabilitiesAsync failed with message: {ex.Message}", ex);
                    }
                    await Task.Delay(Config.ClientRetryDelay);
                }
            }
        }

        public async Task<CveDto> GetCveAsync(string cve)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    var response = await ApiClient.GetAsync<CveDto>($"api/v1/cves/{cve}");

                    // Extract the CveDto from the ApiClientResponse
                    if (response?.Content == null)
                    {
                        throw new Exception("Response data is null");
                    }

                    return response.Content;
                }
                catch (ApiClientOtherException ex) when (ex.Message.Contains("Too Many Requests"))
                {
                    Logger.LogWarning("[Client] Rate limit exceeded ([{Type}] {Msg}), retrying...", ex.GetType().Name, ex.Message);

                    if (retryCount >= Config.ClientMaxRetries)
                    {
                        throw new OligoClientException($"[Client] Rate limit retry exceeded for {cve}: {ex.Message}", ex);
                    }
                    else
                    {
                        await Task.Delay(Config.ClientRateLimitDelay);
                    }
                }
                catch (Exception ex)
                {
                    retryCount++;
                    var msg = ex.Message.Length > 500 ? ex.Message[..499] : ex.Message;
                    Logger.LogInformation(ex, "[Client] GetCveAsync failed ([{Type}] {Msg}), retrying...", ex.GetType().Name, msg);

                    if (ex.Message == "Unauthorized")
                    {
                        Logger.LogInformation("[Client] Access Token expired, resetting then retrying");
                        retryCount--;
                        SetToken();
                    }

                    if (retryCount >= Config.ClientMaxRetries)
                    {
                        CheckErrorAsync($"api/v1/cves/{cve}");
                        throw new OligoClientException($"[Client] GetCveAsync failed with message: {ex.Message}", ex);
                    }

                    await Task.Delay(Config.ClientRetryDelay);
                }
            }
        }

        public async Task<List<ImageDTO>> GetImages()

        {

            try
            {
                var result = await ApiClient.GetAsync<List<ImageDTO>>("/api/v1/image");
                return result.Content;
            }
            catch (OligoClientException ex)
            {
                CheckErrorAsync("/api/v1/image");
                throw new OligoClientException($"[Client]{ex}");
            }
        }

        public SourceMetric GetSourceMetric(QueueScan queueScan)
        {
            SourceMetric currentMetric = new();
            
            if(Config.EnableDifferentialUpdates)
            {
                currentMetric = new()
                {
                    Instance = Config.Instance,
                    IsSaltminerSource = OligoConfig.IsSaltminerSource,
                    SourceType = Config.SourceType,
                    SourceId = queueScan.Id,
                    VersionId = "",
                    Attributes = new Dictionary<string, string>(),
                    LastScan = queueScan.Entity.Saltminer.Scan.ScanDate
                };
            }
            else
            {
                currentMetric = new()
                {
                    Instance = Config.Instance,
                    IsSaltminerSource = OligoConfig.IsSaltminerSource,
                    SourceType = Config.SourceType,
                    SourceId = queueScan.Id,
                    VersionId = "",
                    IssueCount = queueScan.Entity.Saltminer.Internal.IssueCount,
                    Attributes = new Dictionary<string, string>(),
                    LastScan = queueScan.Entity.Saltminer.Scan.ScanDate
                };
            }
            return currentMetric;

        }

        public async void CheckErrorAsync(string errorCall)
        {
            try
            {
                var result = await ApiClient.GetAsync<ErrorDTO>(errorCall);
                throw new OligoClientException($"{result.Content}");

            }
            catch (Exception ex)
            {
                Logger.LogInformation($"[Client] CheckErrorAsync Failed with {ex}");
                throw new OligoClientException($"{ex}");

            } 
           
        }
    }
}
