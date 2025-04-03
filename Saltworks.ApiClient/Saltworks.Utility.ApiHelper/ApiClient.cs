/*
 * Copyright (c) 2025 Saltworks Security
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE.TXT file.
 *
 * Change Date: 2029-01-28
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 */

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Saltworks.Utility.ApiHelper
{
    public class ApiClient : IDisposable
    {
        private readonly HttpClient Client;
        private readonly ILogger Logger;
        private readonly CookieContainer CookieJar;
        private readonly JsonSerializerOptions CamelCaseSerializationPolicy = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Constructor
        internal ApiClient(HttpClient httpClient, ApiClientOptions options, ILogger<ApiClient> logger, CookieContainer cookieJar=null)
        {
            Client = httpClient;
            Options = options;
            Logger = logger;
            CookieJar = cookieJar;
        }

        #region Properties

        /// <summary>
        /// Options used to create this instance.  Read-only, as they have no effect except during instantiation.
        /// </summary>
        public ApiClientOptions Options { get; internal set; }

        /// <summary>
        /// Base address to use for relative urls in action methods.
        /// </summary>
        public string BaseAddress
        {
            get => Client.BaseAddress?.ToString();
            set { Client.BaseAddress = new Uri(value); Options.BaseAddress = value; }
        }

        /// <summary>
        /// Timeout for requests.  Defaults to 5 seconds.  Set to System.Threading.Timeout.InfiniteTimeSpan for unlimited (not recommended for synchronous calls).
        /// </summary>
        public TimeSpan Timeout
        {
            get => Client.Timeout;
            set { Client.Timeout = value; Options.Timeout = value; }
        }

        #endregion

        #region Api Methods

        /// <summary>
        /// Calls the specified api as a GET method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="body">The content of the request that will be serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public ApiClientResponse<T> Get<T>(string url, object body = null, ApiClientHeaders headers = null) where T : class
        {
            return Send<T>(HttpMethod.Get, url, body, headers);
        }

        /// <summary>
        /// Calls the specified api as a GET method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="sBody">The content of the request that has already been serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public ApiClientResponse<T> Get<T>(string url, string sBody, ApiClientHeaders headers = null) where T : class
        {
            return Send<T>(HttpMethod.Get, url, sBody, headers);
        }

        /// <summary>
        /// Calls the specified api as a GET method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="body">The content of the request that will be serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public async Task<ApiClientResponse<T>> GetAsync<T>(string url, object body = null, ApiClientHeaders headers = null) where T : class
        {
            return await SendAsync<T>(HttpMethod.Get, url, body, headers);
        }

        /// <summary>
        /// Calls the specified api as a GET method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="sBody">The content of the request that has already been serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public async Task<ApiClientResponse<T>> GetAsync<T>(string url, string sBody, ApiClientHeaders headers = null) where T : class
        {
            return await SendAsync<T>(HttpMethod.Get, url, sBody, headers);
        }

        public async Task<ApiClientFileResponse> GetFileAsync(string url, ApiClientHeaders headers = null)
        {
            HttpRequestMessage req;
            try
            {
                url = GetUrl(url);
                req = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(url)
                };
                SetHeaders(req.Headers, headers, Options.DefaultHeaders);
            }
            catch (Exception ex)
            {
                throw new ApiClientException("Error preparing API file download request, see inner exception for details.", ex);
            }

            try
            {
                return ApiClientFileResponse.BuildApiClientResponse(await Client.SendAsync(req), Options.ExceptionOnFailure);
            }
            catch (TaskCanceledException ex)
            {
                throw new ApiClientTimeoutException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Calls the specified api as a POST method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="body">The content of the request that will be serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public ApiClientResponse<T> Post<T>(string url, object body, ApiClientHeaders headers = null) where T : class
        {
            return Send<T>(HttpMethod.Post, url, body, headers);
        }

        /// <summary>
        /// Calls the specified api as a POST method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="sBody">The content of the request that has already been serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public ApiClientResponse<T> Post<T>(string url, string sBody, ApiClientHeaders headers = null) where T : class
        {
            return Send<T>(HttpMethod.Post, url, sBody, headers);
        }

        /// <summary>
        /// Calls the specified api as a POST method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="body">The content of the request that will be serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public async Task<ApiClientResponse<T>> PostAsync<T>(string url, object body, ApiClientHeaders headers = null) where T : class
        {
            return await SendAsync<T>(HttpMethod.Post, url, body, headers);
        }

        /// <summary>
        /// Calls the specified api as a POST method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="sBody">The content of the request that has already been serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public async Task<ApiClientResponse<T>> PostAsync<T>(string url, string sBody, ApiClientHeaders headers = null) where T : class
        {
            return await SendAsync<T>(HttpMethod.Post, url, sBody, headers);
        }

        /// <summary>
        /// Calls the specified api as a POST method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="fileStream">The already initialized Stream (or FileStream) content of the request.</param>
        /// <param name="fileName">The file name (and extension) of the posted file content.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientNoContentResponse indicating success or failure.</returns>
        public async Task<ApiClientNoContentResponse> PostFileAsync(string url, Stream fileStream, string fileName, ApiClientHeaders headers = null)
        {
            var status = "preparing";
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                throw new ArgumentException("File name contains one or more invalid characeters.");
            HttpRequestMessage req;
            try
            {
                url = GetUrl(url);
                using var form = new MultipartFormDataContent();
                using var content = new StreamContent(fileStream);
                using var fileContent = new ByteArrayContent(await content.ReadAsByteArrayAsync());
                form.Add(fileContent, "file", fileName);
                req = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Content = form
                };
                SetHeaders(req.Headers, headers, Options.DefaultHeaders);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                Logger.LogInfo($"Api upload file '{fileName}': [Post] {url}");
                return await ApiClientNoContentResponse.BuildApiClientResponseAsync(await Client.SendAsync(req), Options.ExceptionOnFailure);
            }
            catch (TaskCanceledException ex)
            {
                throw new ApiClientTimeoutException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new ApiClientException($"Error {status} API file upload, see inner exception for details.", ex);
            }
        }

        /// <summary>
        /// Calls the specified api as a PUT method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="body">The content of the request that will be serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public ApiClientResponse<T> Put<T>(string url, object body, ApiClientHeaders headers = null) where T : class
        {
            return Send<T>(HttpMethod.Put, url, body, headers);
        }

        /// <summary>
        /// Calls the specified api as a PUT method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="sBody">The content of the request that has already been serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public ApiClientResponse<T> Put<T>(string url, string sBody, ApiClientHeaders headers = null) where T : class
        {
            return Send<T>(HttpMethod.Put, url, sBody, headers);
        }

        /// <summary>
        /// Calls the specified api as a PUT method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="body">The content of the request that will be serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public async Task<ApiClientResponse<T>> PutAsync<T>(string url, object body, ApiClientHeaders headers = null) where T : class
        {
            return await SendAsync<T>(HttpMethod.Put, url, body, headers);
        }

        /// <summary>
        /// Calls the specified api as a PUT method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="sBody">The content of the request that has already been serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public async Task<ApiClientResponse<T>> PutAsync<T>(string url, string sBody, ApiClientHeaders headers = null) where T : class
        {
            return await SendAsync<T>(HttpMethod.Put, url, sBody, headers);
        }

        /// <summary>
        /// Calls the specified api as a DELETE method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="body">The content of the request that will be serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public ApiClientResponse<T> Delete<T>(string url, object body = null, ApiClientHeaders headers = null) where T : class
        {
            return Send<T>(HttpMethod.Delete, url, body, headers);
        }

        /// <summary>
        /// Calls the specified api as a DELETE method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="sBody">The content of the request that has already been serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public ApiClientResponse<T> Delete<T>(string url, string sBody, ApiClientHeaders headers = null) where T : class
        {
            return Send<T>(HttpMethod.Delete, url, sBody, headers);
        }

        /// <summary>
        /// Calls the specified api as a DELETE method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="body">The content of the request that will be serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public async Task<ApiClientResponse<T>> DeleteAsync<T>(string url, object body = null, ApiClientHeaders headers = null) where T : class
        {
            return await SendAsync<T>(HttpMethod.Delete, url, body, headers);
        }

        /// <summary>
        /// Calls the specified api as a DELETE method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="sBody">The content of the request that has already been serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public async Task<ApiClientResponse<T>> DeleteAsync<T>(string url, string sBody, ApiClientHeaders headers = null) where T : class
        {
            return await SendAsync<T>(HttpMethod.Delete, url, sBody, headers);
        }


        /// <summary>
        /// Calls the specified api using parameters to build the request, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="method">HTTP method to use (GET, POST, etc.)</param>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="sBody">The content of the request that has already been serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public ApiClientResponse<T> Send<T>(HttpMethod method, string url, string sBody, ApiClientHeaders headers = null) where T : class
        {
            try
            {
                return SendAsync<T>(method, url, sBody, headers).Result;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.Count == 1)
                {
                    throw ex.InnerExceptions[0];
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Calls the specified api using parameters to build the request, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="method">HTTP method to use (GET, POST, etc.)</param>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="body">The content of the request that will be serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public ApiClientResponse<T> Send<T>(HttpMethod method, string url, object body, ApiClientHeaders headers = null) where T : class
        {
            try
            {
                return SendAsync<T>(method, url, body, headers).Result;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.Count == 1)
                {
                    throw ex.InnerExceptions[0];
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Calls the specified api using parameters to build the request, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="method">HTTP method to use (GET, POST, etc.)</param>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="body">The content of the request that will be serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public async Task<ApiClientResponse<T>> SendAsync<T>(HttpMethod method, string url, object body, ApiClientHeaders headers = null) where T : class
        {
            string sBody;
            if (Options.CamelCaseJsonOutput)
            {
                sBody = JsonSerializer.Serialize(body, CamelCaseSerializationPolicy);
            }
            else
            {
                sBody = JsonSerializer.Serialize(body);
            }
            return await SendAsync<T>(method, url, sBody, headers);
        }

        /// <summary>
        /// Calls the specified api using parameters to build the request, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="method">HTTP method to use (GET, POST, etc.)</param>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="sBody">The content of the request that has already been serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public async Task<ApiClientResponse<T>> SendAsync<T>(HttpMethod method, string url, string sBody, ApiClientHeaders headers = null) where T : class
        {
            HttpRequestMessage req;
            try
            {
                UpdateOptions();
                url = GetUrl(url);

                req = new HttpRequestMessage()
                {
                    Method = method,
                    RequestUri = new Uri(url)
                };
                var ctype = SetHeaders(req.Headers, headers, Options.DefaultHeaders).Item2;
                ctype = string.IsNullOrEmpty(ctype) ? "application/json" : ctype;


                if (!string.IsNullOrEmpty(sBody) && !sBody.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    req.Content = new StringContent(sBody, Encoding.Default, ctype);
                }
                else
                {
                    sBody = "";
                }

                if (Options.OverrideCharset != null)
                {
                    req.Content.Headers.ContentType.CharSet = "";
                }

                if (Options.LogApiCallsAsInfo)
                {
                    Logger.LogInfo("API call: [{method}] {url}", method.Method, url);
                }
                else
                {
                    Logger.LogDebug("API call: [{Method}] {Url}", method.Method, url);
                }
                Logger.LogDebug("Body (first 1000 chars): {Body}", sBody.Length > 999 ? sBody[..999] : sBody);
            }
            catch (Exception ex)
            {
                throw new ApiClientException("Error preparing API call, see inner exception for details.", ex);
            }

            try
            {
                return await ApiClientResponse<T>.BuildApiClientResponseAsync(await Client.SendAsync(req), Options.ExceptionOnFailure);
            }
            catch (ApiClientException ex)
            {
                LogExtendedErrorInfo(ex);
                throw;
            }
            catch (TaskCanceledException ex)
            {
                var nex = new ApiClientTimeoutException(req, ex);
                LogExtendedErrorInfo(nex);
                throw nex;
            }
        }

        private void LogExtendedErrorInfo(ApiClientException ex)
        {
            if (!Options.LogExtendedErrorInfo) return;
            try
            {
                var headers = ex.RequestHeaders?.Select(x => $"{x.Key}: {string.Join(',', x.Value)}").ToList() ?? [];
                var auth = headers.FindIndex(h => h.StartsWith("Authorization", StringComparison.OrdinalIgnoreCase));
                if (auth > -1)
                    headers[auth] = "Authorization: [redacted]";
                Logger.LogInfo("[API Error Info] Request body: {Body}", ex.RequestBody);
                Logger.LogInfo("[API Error Info] Request headers: {Headers}", string.Join(", ", headers));
                Logger.LogInfo("[API Error Info] Request URI: {Uri}", ex.RequestUri);
                Logger.LogInfo("[API Error Info] Response content: {Content}", ex.ResponseContent);
                Logger.LogInfo("[API Error Info] Response headers: {Headers}", string.Join(", ", ex.ResponseHeaders?.Select(x => $"{x.Key}: {string.Join(',', x.Value)}") ?? []));
                Logger.LogInfo("[API Error Info] Response error status: {Status}, error message: {Message}", ex.Status, ex.Message);
            }
            catch (Exception ex1)
            {
                Logger?.LogWarning(ex1, "[API Error Info] Failed to log error info (but I tried!) due to: {ErrMsg}", ex1.Message);
            }
        }

        private void UpdateOptions()
        {
            if (!Options.Dirty)
            {
                return;
            }

            BaseAddress = Options.BaseAddress;

            if (Options.MaxResponseContentBufferSize > 0)
            {
                Client.MaxResponseContentBufferSize = Options.MaxResponseContentBufferSize;
            }

            Client.Timeout = Options.Timeout;
            Options.Dirty = false;
        }

        /// <summary>
        /// Posts form content to the specified api using parameters to build the request, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="form">The content of the request that will be encoded before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public ApiClientResponse<T> PostForm<T>(string url, IEnumerable<KeyValuePair<string, string>> form, ApiClientHeaders headers = null) where T : class
        {
            return PostFormAsync<T>(url, form, headers).Result;
        }

        /// <summary>
        /// Posts form content to the specified api using parameters to build the request, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  If BaseAddress is set, can be a relative url.</param>
        /// <param name="form">The content of the request that will be encoded before sending.</param>
        /// <param name="headers">Headers to use with the call.  If DefaultHeaders are set on the ApiClient, these are merged with them (overwriting if duplicate).</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        public async Task<ApiClientResponse<T>> PostFormAsync<T>(string url, IEnumerable<KeyValuePair<string, string>> form, ApiClientHeaders headers = null) where T : class
        {
            if (!url.StartsWith("http:") && !url.StartsWith("https:"))
            {
                if (BaseAddress.EndsWith('/') || url.StartsWith('/'))
                {
                    url = BaseAddress + url;
                }
                else
                {
                    url = BaseAddress + "/" + url;
                }
            }

            var req = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                Content = new FormUrlEncodedContent(form),
                RequestUri = new Uri(url)
            };

            SetHeaders(req.Headers, headers, Options.DefaultHeaders);

            Logger.LogInfo($"API call: [Form POST] {url}");

            return await ApiClientResponse<T>.BuildApiClientResponseAsync(await Client.SendAsync(req), Options.ExceptionOnFailure);
        }

        #endregion

        #region Static "Throwaway" Methods

        /// <summary>
        /// Calls the specified api as a GET method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  Must be an absolute url/uri.</param>
        /// <param name="body">The content of the request that will be serialized before sending.  Usually body should not be sent in a GET call.</param>
        /// <param name="headers">Headers to use with the call.  If not specified, sends no headers.</param>
        /// <param name="exceptionOnFailure">Whether to throw an exception on failure</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        /// <remarks>Throwaway calls are meant to be used infrequently, as they do not make use of a singleton HttpClient.</remarks>
        public ApiClientResponse<T> ThrowawayGet<T>(string url, object body = null, ApiClientHeaders headers = null, bool exceptionOnFailure = true) where T : class
        {
            return ThrowawaySend<T>(HttpMethod.Get, url, body, headers, exceptionOnFailure);
        }

        /// <summary>
        /// Calls the specified api as a POST method, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="url">Url of the remote api.  Must be an absolute url/uri.</param>
        /// <param name="form">The form name value pairs that will be sent in the body of the request.</param>
        /// <param name="headers">Headers to use with the call.  If not specified, sends no headers.</param>
        /// <param name="exceptionOnFailure">Whether to throw an exception on failure</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        /// <remarks>Throwaway calls are meant to be used infrequently, as they do not make use of a singleton HttpClient.</remarks>
        public ApiClientResponse<T> ThrowawayPostForm<T>(string url, IEnumerable<KeyValuePair<string, string>> form, ApiClientHeaders headers = null, bool exceptionOnFailure = true) where T : class
        {
            var handler = new HttpClientHandler();
            var proxy = Options.Proxy.GetProxy();
            if (proxy != null)
                handler.Proxy = proxy;
            var req = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                Content = new FormUrlEncodedContent(form),
                RequestUri = new Uri(url)
            };

            SetHeaders(req.Headers, headers, null);

            try
            {
                using var client = new HttpClient(handler);
                return ApiClientResponse<T>.BuildApiClientResponse(client.Send(req), exceptionOnFailure);
            }
            catch (TaskCanceledException ex)
            {
                throw new ApiClientTimeoutException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Calls the specified api using parameters to build the request, attempting to return the response body deserialized as type T.
        /// </summary>
        /// <param name="method">HTTP method to use (GET, POST, etc.)</param>
        /// <param name="url">Url of the remote api.  Must be an absolute url/uri.</param>
        /// <param name="body">The content of the request that will be serialized before sending.</param>
        /// <param name="headers">Headers to use with the call.  If not specified, sends no headers.</param>
        /// <param name="exceptionOnFailure">Whether to throw an exception on failure</param>
        /// <returns>ApiClientResponse of the type specified.</returns>
        /// <remarks>Throwaway calls are meant to be used infrequently, as they do not make use of a singleton HttpClient.</remarks>
        public ApiClientResponse<T> ThrowawaySend<T>(HttpMethod method, string url, object body, ApiClientHeaders headers = null, bool exceptionOnFailure = true) where T : class
        {
            HttpRequestMessage req;
            HttpClientHandler handler;
            try
            {
                var sBody = JsonSerializer.Serialize(body);
                handler = new HttpClientHandler();
                var proxy = Options.Proxy.GetProxy();
                if (proxy != null)
                    handler.Proxy = proxy;
                req = new HttpRequestMessage()
                {
                    Method = method,
                    RequestUri = new Uri(url)
                };
                var ctype = SetHeaders(req.Headers, headers, new()).Item2;
                ctype = string.IsNullOrEmpty(ctype) ? "application/json" : ctype;
                if (!string.IsNullOrEmpty(sBody) && sBody.ToLower() != "null")
                {
                    req.Content = new StringContent(sBody, Encoding.Default, ctype);
                }
                
            }
            catch (Exception ex)
            {
                throw new ApiClientException("Error preparing API call, see inner exception for details.", ex);
            }

            try
            {
                using var client = new HttpClient(handler);
                return ApiClientResponse<T>.BuildApiClientResponse(client.Send(req), exceptionOnFailure);
            }
            catch (TaskCanceledException ex)
            {
                throw new ApiClientTimeoutException(ex.Message, ex);
            }
        }

        #endregion

        /// <summary>
        /// Sets cookie.  Cookie no go 'way unless cookie monster eat.  Nom nom nom nom nom!
        /// </summary>
        /// <param name="uri">Domain for cookie</param>
        /// <param name="name">Cookie property name</param>
        /// <param name="value">Cookie property value</param>
        public void SetCookie(Uri uri, string name, string value)
        {
            // Silently eat null reference - maybe taste bad, but was in cookie jar no? Nom nom nom nom nom!
            CookieJar?.Add(uri, new Cookie(name, value));
        }

        private string GetUrl(string url)
        {
            if (!url.StartsWith("http:") && !url.StartsWith("https:"))
            {
                if (BaseAddress.EndsWith('/') || url.StartsWith('/') || string.IsNullOrEmpty(url))
                {
                    url = BaseAddress + url;
                }
                else
                {
                    url = BaseAddress + "/" + url;
                }
            }
            return url;
        }

        internal static Tuple<HttpRequestHeaders, string> SetHeaders(HttpRequestHeaders reqHeaders, ApiClientHeaders inHeaders, ApiClientHeaders defHeaders)
        {
            reqHeaders.Clear();
            const string DISABLE = "@#LEGDISABLE#@";
            var contentType = string.Empty;

            // Add default headers, with special handling to create empty values as needed
            if (defHeaders != null)
            {
                foreach (var h in defHeaders.Headers)
                {
                    if (h.Key.ToLower() == "content-type")
                    {
                        contentType = h.Value[0];
                        continue;
                    }
                    if (h.Value.Exists(v => v.Contains(DISABLE)))
                    {
                        reqHeaders.TryAddWithoutValidation(h.Key, h.Value.Select(v => v.Replace(DISABLE, "")).ToArray());
                    }
                    else
                    {
                        reqHeaders.Add(h.Key, h.Value);
                    }
                }
            }

            // Add passed headers
            if (inHeaders == null)
            {
                return new(reqHeaders, contentType);
            }

            foreach (var h in inHeaders.Headers)
            {
                if (h.Key.ToLower() == "content-type")
                {
                    contentType = h.Value[0];
                    continue;
                }
                if (reqHeaders.Contains(h.Key))
                {
                    reqHeaders.Remove(h.Key);
                }
                reqHeaders.Add(h.Key, h.Value);
            }
            return new(reqHeaders, contentType);
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Client.Dispose();
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
