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

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Saltworks.Utility.ApiHelper
{
    [Serializable]
    public class ApiClientConfigurationException : Exception
    {
        public ApiClientConfigurationException() { }
        public ApiClientConfigurationException(string message) : base(message) { }
        public ApiClientConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }

    [Serializable]
    public class ApiClientException : Exception
    {
        public HttpStatusCode Status { get; internal set; } = HttpStatusCode.InternalServerError;
        public string ResponseContent { get; internal set; } = "";
        public HttpHeaders ResponseHeaders { get; internal set; } = default;
        public string RequestUri { get; internal set; } = "";
        public string RequestBody { get; internal set; } = "";
        public HttpHeaders RequestHeaders { get; internal set; } = default;
        public ApiClientException() { }
        public ApiClientException(string message, HttpResponseMessage response) : base(message) { SetProperties(response); }
        public ApiClientException(string message) : base(message) { }
        public ApiClientException(string message, Exception innerException) : base(message, innerException) { }
        public ApiClientException(string message, HttpResponseMessage response, Exception innerException) : base(message, innerException) { SetProperties(response); }

        protected static string StringFromHttpContent(HttpContent content, int limit=5000)
        {
            // request body, up to limit chars
            var ret = content?.ReadAsStringAsync()?.Result ?? "";
            if (ret.Length > limit)
                return ret[..(limit - 1)];
            return ret;
        }
        protected void SetProperties(HttpRequestMessage request)
        {
            RequestUri = request?.RequestUri?.ToString();
            RequestBody = StringFromHttpContent(request?.Content);
            RequestHeaders = request?.Headers;
        }
        protected void SetProperties(HttpResponseMessage response)
        {
            SetProperties(response?.RequestMessage);
            Status = response?.StatusCode == null ? Status : response.StatusCode;
            ResponseContent = StringFromHttpContent(response?.Content);
            ResponseHeaders = response?.Headers;
        }
    }


    [Serializable]
    public class ApiClientTimeoutException : ApiClientException
    {
        public ApiClientTimeoutException() { }
        public ApiClientTimeoutException(string message) : base(message) { }
        public ApiClientTimeoutException(string message, Exception innerException) : base(message, innerException) { }
        public ApiClientTimeoutException(HttpRequestMessage request, Exception innerException) : base("API call timed out.", innerException) { SetProperties(request); }
    }

    [Serializable]
    public class ApiClientNotFoundException : ApiClientException
    {
        public ApiClientNotFoundException() { }
        public ApiClientNotFoundException(string message) : base(message) { }
        public ApiClientNotFoundException(string message, Exception innerException) : base(message, innerException) { }
        public ApiClientNotFoundException(string message, HttpResponseMessage response) : base(message, response) { }
        public ApiClientNotFoundException(string message, HttpResponseMessage response, Exception innerException) : base(message, response, innerException) { }
    }

    [Serializable]
    public class ApiClientBadRequestException : ApiClientException
    {
        public ApiClientBadRequestException() : base() {}
        public ApiClientBadRequestException(string message) : base(message) { }
        public ApiClientBadRequestException(string message, Exception innerException) : base(message, innerException) { }
        public ApiClientBadRequestException(string message, HttpResponseMessage response) : base(message, response) { }
        public ApiClientBadRequestException(string message, HttpResponseMessage response, Exception innerException) : base(message, response, innerException) { }
    }


    [Serializable]
    public class ApiClientServiceNotAvailableException : ApiClientException
    {
        public ApiClientServiceNotAvailableException() { }
        public ApiClientServiceNotAvailableException(string message) : base(message) { }
        public ApiClientServiceNotAvailableException(string message, Exception inner) : base(message, inner) { }
        public ApiClientServiceNotAvailableException(string message, HttpResponseMessage response) : base(message, response) { }
        public ApiClientServiceNotAvailableException(string message, HttpResponseMessage response, Exception innerException) : base(message, response, innerException)
        { Status = HttpStatusCode.NotFound; }
    }


    [Serializable]
    public class ApiClientNotImplementedException : ApiClientException
    {
        public ApiClientNotImplementedException() { }
        public ApiClientNotImplementedException(string message) : base(message) { }
        public ApiClientNotImplementedException(string message, Exception inner) : base(message, inner) { }
        public ApiClientNotImplementedException(string message, HttpResponseMessage response) : base(message, response) { }
        public ApiClientNotImplementedException(string message, HttpResponseMessage response, Exception innerException) : base(message, response, innerException) { }
    }


    [Serializable]
    public class ApiClientMethodNotAllowedException : ApiClientException
    {
        public ApiClientMethodNotAllowedException() { }
        public ApiClientMethodNotAllowedException(string message) : base(message) { }
        public ApiClientMethodNotAllowedException(string message, Exception inner) : base(message, inner) { }
        public ApiClientMethodNotAllowedException(string message, HttpResponseMessage response) : base(message, response) { }
        public ApiClientMethodNotAllowedException(string message, HttpResponseMessage response, Exception innerException) : base(message, response, innerException) { }
    }


    [Serializable]
    public class ApiClientGatewayTimeoutException : ApiClientException
    {
        public ApiClientGatewayTimeoutException() { }
        public ApiClientGatewayTimeoutException(string message) : base(message) { }
        public ApiClientGatewayTimeoutException(string message, Exception inner) : base(message, inner) { }
        public ApiClientGatewayTimeoutException(string message, HttpResponseMessage response) : base(message, response) { }
        public ApiClientGatewayTimeoutException(string message, HttpResponseMessage response, Exception innerException) : base(message, response, innerException) { }
    }


    [Serializable]
    public class ApiClientBadGatewayException : ApiClientException
    {
        public ApiClientBadGatewayException() { }
        public ApiClientBadGatewayException(string message) : base(message) { }
        public ApiClientBadGatewayException(string message, Exception inner) : base(message, inner) { }
        public ApiClientBadGatewayException(string message, HttpResponseMessage response) : base(message, response) { }
        public ApiClientBadGatewayException(string message, HttpResponseMessage response, Exception innerException) : base(message, response, innerException) { }
    }


    [Serializable]
    public class ApiClientForbiddenException : ApiClientException
    {
        public ApiClientForbiddenException() { }
        public ApiClientForbiddenException(string message) : base(message) { }
        public ApiClientForbiddenException(string message, Exception inner) : base(message, inner) { }
        public ApiClientForbiddenException(string message, HttpResponseMessage response) : base(message, response) { }
        public ApiClientForbiddenException(string message, HttpResponseMessage response, Exception innerException) : base(message, response, innerException) { }
    }


    [Serializable]
    public class ApiClientUnauthorizedException : ApiClientException
    {
        public ApiClientUnauthorizedException() { }
        public ApiClientUnauthorizedException(string message) : base(message) { }
        public ApiClientUnauthorizedException(string message, Exception inner) : base(message, inner) { }
        public ApiClientUnauthorizedException(string message, HttpResponseMessage response) : base(message, response) { }
        public ApiClientUnauthorizedException(string message, HttpResponseMessage response, Exception innerException) : base(message, response, innerException) { }
    }


    [Serializable]
    public class ApiClientConflictException : ApiClientException
    {
        public ApiClientConflictException() { }
        public ApiClientConflictException(string message) : base(message) { }
        public ApiClientConflictException(string message, Exception inner) : base(message, inner) { }
        public ApiClientConflictException(string message, HttpResponseMessage response) : base(message, response) { }
        public ApiClientConflictException(string message, HttpResponseMessage response, Exception innerException) : base(message, response, innerException) { }
    }

    [Serializable]
    public class ApiClientServerErrorException : ApiClientException
    {
        public ApiClientServerErrorException() { }
        public ApiClientServerErrorException(string message) : base(message) { }
        public ApiClientServerErrorException(string message, Exception inner) : base(message, inner) { }
        public ApiClientServerErrorException(string message, HttpResponseMessage response) : base(message, response) { }
        public ApiClientServerErrorException(string message, HttpResponseMessage response, Exception innerException) : base(message, response, innerException) { }
    }

    [Serializable]
    public class ApiClientSerializationException : Exception
    {
        public ApiClientSerializationException() { }
        public ApiClientSerializationException(string message) : base(message) { }
        public ApiClientSerializationException(string message, Exception inner) : base(message, inner) { }
    }

    [Serializable]
    public class ApiClientOtherException : ApiClientException
    {
        public ApiClientOtherException(HttpStatusCode status) : base() { Status = status; }
        public ApiClientOtherException(HttpStatusCode status, string message) : base(message) { Status = status; }
        public ApiClientOtherException(HttpStatusCode status, string message, Exception innerException) : base(message, innerException) { Status = status; }
        public ApiClientOtherException(string message, HttpResponseMessage response) : base(message, response) { }
        public ApiClientOtherException(string message, HttpResponseMessage response, Exception innerException) : base(message, response, innerException) { }
    }
}
