// Copyright (C) Saltworks Security, LLC - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and confidential
// Written by Saltworks Security, LLC (www.saltworks.io), 2021

using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;

namespace Saltworks.Utility.ApiHelper
{
    /// <summary>
    /// ApiClient factory class
    /// </summary>
    /// <typeparam name="T">Any type already added to DI using AddApiClient() - this is used to support multiple instances with separate configs.</typeparam>
    public class ApiClientFactory<T>
    {
        internal IHttpClientFactory DepFactory { get; set; } = null;
        internal ILogger<ApiClient> Logger { get; set; } = null;
        internal CookieContainer CookieJar { get; set; } = null;
        public ApiClientOptions Options { get; private set; } = null;
        public string Name { get => "ApiClient." + typeof(T).FullName; }

        internal ApiClientFactory(ApiClientOptions options, CookieContainer cookieJar = null)
        {
            Options = options;
            CookieJar = cookieJar;
        }

        /// <summary>
        /// Creates an ApiClient from DI configuration
        /// </summary>
        public ApiClient CreateApiClient()
        {
            if (DepFactory == null)
            {
                throw new ApiClientConfigurationException("Dependency failure (missing IHttpClientFactory DepFactory).  Did you remember UseApiClient() in Startup.Configure?");
            }

            return new ApiClient(DepFactory.CreateClient(Name), Options, Logger, CookieJar);
        }
        /// <summary>
        /// Creates an ApiClient from DI configuration
        /// </summary>
        public ApiClient CreateApiClient(Action<ApiClientOptions> configureOptions)
        {
            if (DepFactory == null)
            {
                throw new ApiClientConfigurationException("Dependency failure (missing IHttpClientFactory DepFactory).  Did you remember UseApiClient() in Startup.Configure?");
            }

            configureOptions?.Invoke(Options);
            
            return new ApiClient(DepFactory.CreateClient(Name), Options, Logger, null);
        }
    }
}
