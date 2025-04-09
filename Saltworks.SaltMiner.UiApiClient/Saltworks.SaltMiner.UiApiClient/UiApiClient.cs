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
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.ViewModels;
using Saltworks.Utility.ApiHelper;
using System.Net;

namespace Saltworks.SaltMiner.UiApiClient
{
    public class UiApiClient : IDisposable
    {
        public ApiClient UiApi { get; }
        private ILogger Logger { get; }
        public UiApiClientConfig Config { get; internal set; }

        #region Ctor

        internal UiApiClient(ApiClient client, ILogger logger)
        {
            Logger = logger;
            UiApi = client;
            Logger.LogDebug("UiApiClient initialization starting");

            Logger.LogInformation("UiApiClient init");
               
            Logger.LogDebug("UiApiClient initialization complete");
        }

        #endregion

        #region Report Engagement

        public DataItemResponse<EngagementSummary> EngagementSummaryGet(string id)
        {
            return CheckRetry(() => UiApi.Get<DataItemResponse<EngagementSummary>>($"report/summary/{id}")).Content;
        }

        public NoDataResponse AddEngagementAttachment(string id, UiAttachmentInfo attachment)
        {
            return CheckRetry(() => UiApi.Post<NoDataResponse>($"report/engagement/{id}/attachment", attachment)).Content;
        }

        public DataItemResponse<UiAttachmentInfo> GetEngagementAttachment(string fileName)
        {
            return CheckRetry(() => UiApi.Get<DataItemResponse<UiAttachmentInfo>>($"report/attachment/{fileName}")).Content;
        }

        #endregion

        #region Report Scan

        public DataItemResponse<ScanFull> EngagementScanGet(string engagementId)
        {
            return CheckRetry(() => UiApi.Get<DataItemResponse<ScanFull>>($"report/scan/{engagementId}")).Content;
        }

        #endregion

        #region Report Asset

        public DataResponse<AssetFull> EngagementAssetsGet(string engagementId)
        {
            return CheckRetry(() => UiApi.Get<DataResponse<AssetFull>>($"report/assets/{engagementId}")).Content;
        }

        #endregion

        #region Report Issues
        public DataResponse<IssueFull> EngagementIssueSearch(IssueSearch search)
        {
            return CheckRetry(() => UiApi.Post<DataResponse<IssueFull>>($"report/issues", search), true).Content;
        }

        public DataResponse<LookupValue> IssueSeverities()
        {
            return CheckRetry(() => UiApi.Get<DataResponse<LookupValue>>($"report/issue/severities")).Content;
        }

        #endregion

        #region Job

        public DataItemResponse<Job> PollPendingJob(string type = null)
        {
            var url = "job/pending";
            if (!string.IsNullOrEmpty(type))
            {
                url = $"{url}?type={type}";
            }

            var pagingInfo = new UIPagingInfo 
            { 
                Size = 1,
                SortFilters = new Dictionary<string, bool> { { "Timestamp", true } }
            };

            return new DataItemResponse<Job>(CheckRetry(() => UiApi.Post<DataResponse<Job>>(url, pagingInfo)).Content.Data.FirstOrDefault());
        }

        public DataResponse<Job> PendingJobCount(string type = null)
        {
            var url = "job/pending";
            if (!string.IsNullOrEmpty(type))
            {
                url = $"{url}?type={type}";
            }

            return CheckRetry(() => UiApi.Get<DataResponse<Job>>(url, new UIPagingInfo { Size = 100 })).Content;
        }

        public DataItemResponse<Job> UpdateJobQueue(Job queue)
        {
            return CheckRetry(() => UiApi.Post<DataItemResponse<Job>>($"job", queue)).Content;
        }

        public NoDataResponse DeleteJobQueue(string id)
        {
            return CheckRetry(() => UiApi.Delete<NoDataResponse>($"job/{id}")).Content;
        }

        #endregion

        #region Report File

        public void UploadFile(Stream file, string fileName)
        {
            UiApi.PostFileAsync($"report/file/upload", file, fileName).Wait();
        }

        public byte[] DownloadFile(string url)
        {
            ApiClientFileResponse response = UiApi.GetFileAsync(url).Result;
            using var stream = new MemoryStream();
            response.GetContentAsync().Result.CopyTo(stream);
            return stream.ToArray();
        }

        public void DeleteFile(string fileId)
        {
            UiApi.Delete<NoDataResponse>($"file/{fileId}");
        }

        #endregion

        #region Report Template Lookup

        public NoDataResponse UpdateTemplateLookups(List<string> templates)
        {
            return CheckRetry(() => UiApi.Post<NoDataResponse>($"report/templates", templates)).Content;
        }

        #endregion
        
        #region Support

        private ApiClientResponse<T> CheckRetry<T>(Func<ApiClientResponse<T>> retry, bool skipRetry = false) where T : Response
        {
            ApiClientResponse<T> response = null;
            var msg = "";
            // Stay in retry loop until retries exhausted or good response or bad response that isn't a 5xx error
            for (int i = 0; i < (skipRetry ? 1 : Config.UiApiApiRetryCount); i++)
            {
                try
                {
                    response = retry();
                }
                catch (Exception ex)
                {
                    throw new UiApiClientException($"UiApiClient API call failed: [{ex.GetType().Name}] {ex.Message}", ex);
                }

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                try
                {
                    msg = $"UiApiClient API call unsuccessful: ({response.StatusCode.GetHashCode()}) {response.Content.Message}";
                }
                catch (ApiClientSerializationException)
                {
                    msg = $"UiApiClient API call unsuccessful: ({response.StatusCode.GetHashCode()}) {response.HttpResponse.ReasonPhrase}";
                }

                if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge)
                {
                    msg = $"{msg}. Consider reducing the batch size if this request contains a batch of entities.";
                }

                if (response.StatusCode < HttpStatusCode.InternalServerError)
                {
                    throw new UiApiClientResponseException(msg, response);
                }

                Logger.LogError("{Msg}", msg);
                Logger.LogInformation("Retry {Cur}/{RetryCount} after {Sec} sec delay...", i + 1, Config.UiApiApiRetryCount, Config.UiApiApiDelaySec);
                Thread.Sleep(Config.UiApiApiDelaySec * 1000);
            }

            Logger.LogDebug("Response raw content (up to 300 chars): {Msg}", response?.RawContent?.Left(300));
            throw new UiApiClientResponseException(msg, response);
        }

        #endregion

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    UiApi.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}