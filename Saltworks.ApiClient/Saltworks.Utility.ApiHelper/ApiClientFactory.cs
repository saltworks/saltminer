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
