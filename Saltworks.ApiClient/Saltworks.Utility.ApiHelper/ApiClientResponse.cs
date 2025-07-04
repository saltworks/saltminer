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

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Net.Http.Headers;

namespace Saltworks.Utility.ApiHelper
{
    public abstract class ApiClientResponse
    {
        /// <summary>
        /// HttpResponse generated by the underlying HttpClient.
        /// </summary>
        public HttpResponseMessage HttpResponse { get; protected set; }
        /// <summary>
        /// Raw content of the underlying HttpResponse as generated by the underlying HttpClient.
        /// </summary>
        public string RawContent { get; protected set; }
        /// <summary>
        /// Whether the call was successful or not.
        /// </summary>
        public bool IsSuccessStatusCode => HttpResponse.IsSuccessStatusCode;
        /// <summary>
        /// The http status code of the response (i.e. 200, 404, etc.)
        /// </summary>
        public HttpStatusCode StatusCode => HttpResponse.StatusCode;
        /// <summary>
        /// The standard reason text used for the current StatusCode (i.e. "Ok" for 200, "Not Found" for 404, etc.).
        /// </summary>
        public string ReasonPhrase => HttpResponse.ReasonPhrase;
        /// <summary>
        /// Response headers attached to HttpResponse generated by the underlying client.
        /// </summary>
        public HttpHeaders ResponseHeaders => HttpResponse.Headers;

        protected static void ThrowFailureException(HttpResponseMessage response)
        {
            ApiClientException ex = response.StatusCode switch
            {
                HttpStatusCode.BadRequest => new ApiClientBadRequestException(response.ReasonPhrase, response),
                HttpStatusCode.NotFound => new ApiClientNotFoundException(response.ReasonPhrase, response),
                HttpStatusCode.ServiceUnavailable => new ApiClientServiceNotAvailableException(response.ReasonPhrase, response),
                HttpStatusCode.NotImplemented => new ApiClientNotImplementedException(response.ReasonPhrase, response),
                HttpStatusCode.MethodNotAllowed => new ApiClientMethodNotAllowedException(response.ReasonPhrase, response),
                HttpStatusCode.GatewayTimeout => new ApiClientGatewayTimeoutException(response.ReasonPhrase, response),
                HttpStatusCode.Forbidden => new ApiClientForbiddenException(response.ReasonPhrase, response),
                HttpStatusCode.Unauthorized => new ApiClientUnauthorizedException(response.ReasonPhrase, response),
                HttpStatusCode.Conflict => new ApiClientConflictException(response.ReasonPhrase, response),
                HttpStatusCode.InternalServerError => new ApiClientServerErrorException(response.ReasonPhrase, response),
                _ => new ApiClientOtherException(response.ReasonPhrase, response),
            };
            throw ex;
        }
    }

    public class ApiClientNoContentResponse : ApiClientResponse
    {
        private ApiClientNoContentResponse(HttpResponseMessage response)
        {
            HttpResponse = response;
        }

        internal static ApiClientNoContentResponse BuildApiClientResponse(HttpResponseMessage response, bool exceptionOnFailure)
        {
            return BuildApiClientResponseAsync(response, exceptionOnFailure).Result;
        }

        internal static async Task<ApiClientNoContentResponse> BuildApiClientResponseAsync(HttpResponseMessage response, bool exceptionOnFailure)
        {
            var res = new ApiClientNoContentResponse(response);

            if (!res.IsSuccessStatusCode && exceptionOnFailure)
            {
                ThrowFailureException(response);
            }

            res.RawContent = await response.Content.ReadAsStringAsync();

            return res;
        }
    }

    public class ApiClientFileResponse : ApiClientResponse
    {
        private ApiClientFileResponse(HttpResponseMessage response)
        {
            HttpResponse = response;
        }
        public async Task<Stream> GetContentAsync() => await HttpResponse.Content.ReadAsStreamAsync();
        public async Task SaveAsFileAsync(string filePath, bool overwrite = true)
        {
            if (!overwrite && File.Exists(filePath))
            {
                throw new ApiClientException($"File '{filePath}' already exists.");
            }
            using var fs = File.OpenWrite(filePath);
            await (await GetContentAsync()).CopyToAsync(fs);
            fs.Close();
        }
        internal static ApiClientFileResponse BuildApiClientResponse(HttpResponseMessage response, bool exceptionOnFailure)
        {
            var res = new ApiClientFileResponse(response);
            if (!res.IsSuccessStatusCode && exceptionOnFailure)
            {
                ThrowFailureException(response);
            }
            return res;
        }
    }

    public class ApiClientResponse<T> : ApiClientResponse where T: class
    {
        private ApiClientResponse(HttpResponseMessage response)
        {
            HttpResponse = response;
        }

        private T DeserializeContent()
        {
            if (string.IsNullOrEmpty(RawContent))
            {
                return null;
            }

            try
            {
                var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
            
                options.Converters.Add(new ApiClientDateTimeConverter());
             
                return JsonSerializer.Deserialize<T>(RawContent, options);
            }
            catch (JsonException ex)
            {
                var contentType = "unknown";
                var content = RawContent.Length > 100 ? RawContent.Substring(0, 100) : RawContent;
                
                if (content.ToLower().StartsWith("<!doctype html") || content.ToLower().StartsWith("<html"))
                {
                    contentType = "HTML";
                }

                if (content.ToLower().StartsWith("<?xml"))
                {
                    contentType = "XML";
                }

                if (content.StartsWith("{"))
                {
                    contentType = "JSON";
                }

                var msg = $"Failed to deserialize response content into type '{typeof(T).Name}'. Status is {HttpResponse.StatusCode.GetHashCode()}, Content is {contentType}, Raw content starts with (see RawContent for more): '{content}'";
                
                throw new ApiClientSerializationException(msg, ex);
            }
        }

        /// <summary>
        /// Content deserialized into the type specified for this call.
        /// </summary>
        public T Content => DeserializeContent();

        internal static ApiClientResponse<T> BuildApiClientResponse(HttpResponseMessage response, bool exceptionOnFailure)
        {
            return BuildApiClientResponseAsync(response, exceptionOnFailure).Result;
        }

        internal static async Task<ApiClientResponse<T>> BuildApiClientResponseAsync(HttpResponseMessage response, bool exceptionOnFailure)
        {
            var res = new ApiClientResponse<T>(response);

            if (!res.IsSuccessStatusCode && exceptionOnFailure)
            {
                ThrowFailureException(response);
            }
          
            res.RawContent = await response.Content.ReadAsStringAsync();
            
            return res;
        }
    }
}
