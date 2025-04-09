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

﻿using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.Utility.ApiHelper;
using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Linq;
using Saltworks.SaltMiner.SourceAdapters.Core.Helpers;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Text;
using System.Web;
using System.Net;
using System.Collections.Concurrent;
using Org.BouncyCastle.Asn1.Cmp;

namespace Saltworks.SaltMiner.SourceAdapters.Qualys
{
    public class QualysClient : SourceClient, IDisposable
    {
        private readonly ApiClient Client;
        private readonly QualysConfig Config;
        private readonly List<XmlReader> OpenReaders = [];
        private readonly List<StreamReader> OpenStreams = [];
        private readonly ConcurrentQueue<Object> ApiCallers = new();
        private bool disposedValue;
        private const string HDR_RATE_LIMIT_WAIT_SEC = "X-RateLimit-ToWait-Sec";
        private const string HDR_RATE_LIMIT_LIMIT = "X-RateLimit-Limit";
        private const string HDR_RATE_LIMIT_WINDOW_SEC = "X-RateLimit-Window-Sec";
        private const string HDR_RATE_LIMIT_REMAINING = "X-RateLimit-Remaining";
        private const string HDR_RATE_LIMIT_CONCURRENCY = "X-Concurrency-Limit-Limit";

        public int RateLimitCount { get; private set; }
        public int RateLimitRemaining { get; private set; }
        public int RateLimitWindowSec { get; private set; }
        public int TotalApiCallCount { get; private set; } = 0;

        public QualysClient(ApiClient client, QualysConfig config, ILogger logger) : base(client, logger)
        {
            Client = client;
            Config = config;
            var hdrs = ApiClientHeaders.AuthorizationBasicHeader(config.Username, config.Password);
            hdrs.Add("X-Requested-With", "SaltMiner");

            SetApiClientDefaults(config.BaseAddress, config.Timeout, hdrs, true);
            if (Client.Options.LogExtendedErrorInfo)
                Logger.LogInformation("[Client] Extended error logging enabled.");
            if (Config.EnablePostApiCalls)
                Logger.LogInformation("[Client] Using POST API calls.");
        }

        /// <summary>
        /// Verify connection to Qualys is functioning
        /// </summary>
        internal void CheckConnection()
        {
            var response = Client.Get<string>($"/report/?action=list");
            var conLimit = 2;
            if (response.ResponseHeaders.TryGetValues(HDR_RATE_LIMIT_CONCURRENCY, out var conLimitHdrVals))
            {
                conLimit = int.Parse(conLimitHdrVals.First());
            }
            if (conLimit < 1)
                throw new QualysException("Concurrency limit is invalid, check returned API headers");
            Logger.LogInformation("[Client] API concurrency limit: {Limit}", conLimit);
            while (conLimit > 0)
            {
                ApiCallers.Enqueue(new object());
                conLimit--;
            }
            if (response.RawContent.Contains(" is not in the list of secure IPs"))
            {
                throw new QualysException("Failed to connect to Qualys.  Connection rejected by IP filtering (security).");
            }
            if (!response.IsSuccessStatusCode)
            {
                throw new QualysException($"Failed attempt to connect to Qualys.  Status code was {response.StatusCode}.");
            }
        }

        private async Task<T> HandleApiExceptionAsync<T>(Func<Task<T>> function, int reps = 0)
        {
            Exception rex;
            ArgumentNullException.ThrowIfNull(function, nameof(function));
            try
            {
                return await function();
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogError(ex, "[Client] API call timed out.  Please increase the Timeout configuration property for Qualys.");
                throw new QualysClientException("API call timed out.  Please increase the Timeout configuration property for Qualys.", ex);
            }
            catch (ApiClientException ex)
            {
                // Rate limited, wait for unlock
                if (ex.Status == HttpStatusCode.Conflict && ex.ResponseHeaders.TryGetValues(HDR_RATE_LIMIT_WAIT_SEC, out var wait))
                {
                    var limit = ex.ResponseHeaders.GetValues(HDR_RATE_LIMIT_LIMIT);
                    var window = ex.ResponseHeaders.GetValues(HDR_RATE_LIMIT_WINDOW_SEC);
                    var waitsec = int.Parse(wait.First());
                    if (reps >= 4)
                        throw new QualysClientException($"Rate limiting still in effect after {reps} attempts to wait.");
                    Logger.LogDebug(ex, "[Client] Rate limit error from Qualys API");
                    Logger.LogWarning("[Client] Rate limit ({Limit} req / {Window} sec) exceeded, waiting {Wait} sec to continue.", limit.First(), window.First(), waitsec);
                    await Task.Delay(TimeSpan.FromSeconds(waitsec));
                    reps++;
                    return await HandleApiExceptionAsync(function, reps);
                }
                try
                {
                    rex = new QualysApiException(XmlDeserializer<SimpleReturnDto>.Deserialize(ex.ResponseContent));
                }
                catch (Exception ex2)
                {
                    try
                    {
                        var rc = ex.ResponseContent;
                        if (rc.Length > 1000)
                            rc = rc[0..999];
                        if (ex.Status == HttpStatusCode.InternalServerError && reps < 3)
                        {
                            Logger.LogWarning("[Client] API call failure (500), retry in 10 sec...");
                            await Task.Delay(TimeSpan.FromSeconds(10));
                            reps++;
                            return await HandleApiExceptionAsync(function, reps);
                        }
                        Logger.LogInformation("[Client] Response Code: {Code}, Content: '{Content}'", ex.Status.GetHashCode(), rc);
                        Logger.LogInformation("[Client] Request URI: {Uri}", ex.RequestUri);
                        Logger.LogInformation("[Client] Response headers: {Hdrs}", string.Join("; ", ex.ResponseHeaders?.Select(h => $"{h.Key}:{string.Join(',', h.Value)}").ToList() ?? []));
                        Logger.LogError(ex2, "[Client] API call failed / unable to deserialize API error response.");
                        rex = ex;
                    }
                    catch (Exception ex3)
                    {
                        rex = new QualysClientException("API call failed / failed to read response content.", ex3);
                        Logger.LogError(ex3, "[Client] API call failed / unable to read response content while handling ApiClientException.");
                    }
                }
                throw rex;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[Client] API call failed with exception of type {Type}", ex.GetType().Name);
                throw new QualysClientException("API call failed due to unexpected exception", ex);
            }
        }

        private async Task<T> ApiGetDeserializedAsync<T>(string uriStub, Dictionary<string, string> parameters) where T : class, IQualysDto =>
            XmlDeserializer<T>.Deserialize(await ApiGetAsync(uriStub, parameters));

        private async Task<string> ApiGetAsync(string uriStub, Dictionary<string, string> parameters)
        {
            var wcount = 0;
            
            // avoid concurrency limit
            while (!ApiCallers.TryDequeue(out var call))
            {
                Logger.LogDebug("[Client] API concurrency limit reached, waiting for available connection...");
                await Task.Delay(2000);
                wcount++;
                if (wcount > 200)
                    throw new QualysException("API concurrency failure.");
            }
            try
            {
                ArgumentNullException.ThrowIfNullOrEmpty(uriStub, nameof(uriStub));
                if (parameters == null || !parameters.ContainsKey("action"))
                    throw new ArgumentException("Must be non-null and include at least action parameter", nameof(parameters));
                if (!uriStub.StartsWith('/')) uriStub = "/" + uriStub;
                if (!(uriStub.EndsWith('/') || uriStub.EndsWith("/?"))) uriStub += "/";
                if (!uriStub.EndsWith('?')) uriStub += "?";
                var uri = new StringBuilder(uriStub);
                var ander = "";
                if (!Config.EnablePostApiCalls)
                {
                    foreach (var kvp in parameters)
                    {
                        uri.Append($"{ander}{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}");
                        ander = "&";
                    }
                }
                var uristr = uri.ToString();
                if (Config.LogApiCalls)
                    Logger.LogInformation("[Client] API Call: {Uri}", uristr);
                ApiClientResponse<string> response;
                if (Config.EnablePostApiCalls)
                {
                    response = await HandleApiExceptionAsync(async () => await Client.PostFormAsync<string>(uristr, parameters));
                }
                else
                {
                    response = await HandleApiExceptionAsync(async () => await Client.GetAsync<string>(uristr));
                }
                TotalApiCallCount++;
                if (response.ResponseHeaders.TryGetValues(HDR_RATE_LIMIT_REMAINING, out IEnumerable<string> values))
                    RateLimitRemaining = int.Parse(values.First());
                if (response.ResponseHeaders.TryGetValues(HDR_RATE_LIMIT_LIMIT, out values))
                    RateLimitCount = int.Parse(values.First());
                if (response.ResponseHeaders.TryGetValues(HDR_RATE_LIMIT_WINDOW_SEC, out values))
                    RateLimitWindowSec = int.Parse(values.First());
                if (TotalApiCallCount % 25 == 0)
                    Logger.LogInformation("[Client] API call total: {Total}.  Remaining: {Remain} ({Limit}/{Window} sec)", TotalApiCallCount, RateLimitRemaining, RateLimitCount, RateLimitWindowSec);
                return response.RawContent;
            }
            catch (Exception ex)
            {
                throw new QualysException($"[Client] API call failure to uri stub '{uriStub}'", ex);
            }
            finally
            {
                // put back our used "call"
                ApiCallers.Enqueue(new object());
            }
        }

        /// <summary>
        /// Returns a list of scans run by Qualys after the specified date
        /// </summary>
        /// <param name="after">Return scan information for scans run after this date</param>
        internal async Task<IEnumerable<ScanListItem>> ScanListAsync(DateTime after)
        {
            var scanListItems = new List<ScanListItem>();
            var paramList = new Dictionary<string, string>() {
                { "action", "list" },
                { "state", "Finished" },
                { "launched_after_datetime", after.ToString("yyyy-MM-dd") }
            };
            var scanRpt = await ApiGetDeserializedAsync<ScanListOutputDto>("scan", paramList);
            foreach (var scan in scanRpt.Response.Scans)
                scanListItems.Add(ScanListItem.FromScan(scan));
            return scanListItems.OrderByDescending(s => s.ScanDate);
        }

        /// <summary>
        /// Returns list of hosts, sorted by Host.Id
        /// </summary>
        /// <param name="ipList">IPs of hosts to return (can be empty)</param>
        /// <remarks>Pagination is handled automatically.</remarks>
        internal IAsyncEnumerable<HostDto> HostListAsync(IEnumerable<string> ipList, DateTime? scanDateAfter=null)
        {
            ArgumentNullException.ThrowIfNull(ipList, nameof(ipList));
            var paramList = new Dictionary<string, string>() {
                { "action", "list" },
                { "show_asset_id", "1" },
                { "details", "All" },
                { "host_metadata", "all" },
                { "id_min", "0" }
            };
            if (scanDateAfter != null)
                paramList.Add("vm_scan_date_after", scanDateAfter.Value.ToString("yyyy-MM-dd"));
            if (ipList.Any())
                paramList.Add("ips", string.Join(',', ipList));
            return HostListGenerator(paramList);
        }

        // Separated generator from public call so parameter validation will not be lazy
        private async IAsyncEnumerable<HostDto> HostListGenerator(Dictionary<string, string> paramList)
        {
            while (true)
            {
                // API returns hosts in Host.Id order, pagination ("id_min", "truncation_limit") applies to Hosts
                var rsp = await ApiGetDeserializedAsync<HostListOutputDto>("asset/host/", paramList);
                foreach (var host in rsp.Response?.Hosts ?? [])
                    yield return host;
                var idmin = rsp.NextCallMinId;
                if (string.IsNullOrEmpty(idmin))
                {
                    if (rsp.Warning != null)
                        Logger.LogWarning("[Client] Host list API included pagination section, but couldn't find pagination data.  No more calls will be made.");
                    break;  // No more data due to missing pagination parameter, break
                }
                else
                {
                    paramList["id_min"] = idmin;  // More data to be pulled, keep loop going
                }
            }
        }

        /// <summary>
        /// Returns list of hosts (with host info, but less than HostListAsync) with detections attached as a list to each host.
        /// </summary>
        /// <param name="ipList">List of host IPs and IP ranges to query</param>
        /// <param name="idList">List of host IDs to query</param>
        /// <param name="status">Active/New/Re-Opened/Fixed - defaults to all of these if empty</param>
        internal IAsyncEnumerable<HostDetectDto> HostDetectionsAsync(IEnumerable<string> ipList, IEnumerable<string> idList, string status = "")
        {
            string[] statuses = ["Active", "New", "Re-Opened", "Fixed"];
            if (!string.IsNullOrEmpty(status))
            {
                if (statuses.Contains(status))
                    statuses = [status];
                else
                    throw new ArgumentOutOfRangeException($"Unknown status, expected one of [{string.Join(',', statuses)}].");
            }
            var paramList = new Dictionary<string, string>() {
                { "action", "list" },
                { "status", string.Join(',', statuses) },
                { "host_metadata", "all" },
                { "show_asset_id", "1" },
                { "show_igs", Config.IncludeInfoIssues ? "1" : "0" },
                { "include_ignored", "1" },
                { "include_disabled", "1" },
                { "id_min", "0" }
            };

            if ((ipList ?? []).Any())
                paramList.Add("ips", string.Join(',', ipList));
            if ((idList ?? []).Any())
                paramList.Add("ids", string.Join(',', idList));
            return HostDetectionsGeneratorAsync(paramList);
        }

        // Separated generator from public call so parameter validation will not be lazy
        private async IAsyncEnumerable<HostDetectDto> HostDetectionsGeneratorAsync(Dictionary<string, string> paramList)
        {
            while (true)
            {
                // API returns hosts in Host.Id order, pagination ("id_min", "truncation_limit") applies to hosts, not detections
                var rsp = await ApiGetDeserializedAsync<HostListVmDetectionDto>("asset/host/vm/detection", paramList);
                foreach (var host in rsp.Response?.Hosts ?? [])
                    yield return host;
                var idmin = rsp.NextCallMinId;
                if (string.IsNullOrEmpty(idmin))
                {
                    if (rsp.Warning != null)
                        Logger.LogWarning("[Client] Host detection list API included pagination section, but couldn't find pagination data.  No more calls will be made.");
                    break;  // No more data due to missing pagination parameter, break
                }
                else
                {
                    paramList["id_min"] = idmin;  // More data to be pulled, keep loop going
                }
            }
        }

        /// <summary>
        /// Returns list of vulnerability definitions for the QIDs passed
        /// </summary>
        /// <param name="qidList">QIDs to look up (required)</param>
        /// <remarks>If QID list passed is empty, an empty result will be returned.</remarks>
        public async Task<IEnumerable<KnowledgeBaseDto>> KnowledgeBaseAsync(IEnumerable<string> qidList)
        {
            ArgumentNullException.ThrowIfNull(qidList, nameof(qidList));
            if (!qidList.Any())
                return [];
            var kbList = new List<KnowledgeBaseDto>();
            var paramList = new Dictionary<string, string>() {
                { "action", "list" },
                { "ids", string.Join(',', qidList) },
                { "details", "All" }
            };
            // API probably returns QID order, pagination ("id_min", "truncation_limit") should apply
            var rsp = await ApiGetDeserializedAsync<KnowledgeBaseOuputDto>("knowledge_base/vuln", paramList);
            foreach (var kb in rsp.Response.Vulnerabilities)
                kbList.Add(kb);
            return kbList;
        }

        #region IDisposable Interface

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach(var r in OpenReaders)
                    {
                        r.Close();
                        r.Dispose();
                    }
                    foreach (var r in OpenStreams)
                    {
                        r.Close();
                        r.Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
