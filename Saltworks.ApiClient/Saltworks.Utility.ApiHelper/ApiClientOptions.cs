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

namespace Saltworks.Utility.ApiHelper
{
    public class ApiClientOptions
    {
        private string baseAddress = "";
        private bool verifySsl = true;
        private long maxResponseContentBufferSize = -1;
        private TimeSpan timeout = TimeSpan.FromSeconds(5);

        internal bool Dirty { get; set; } = true;
        /// <summary>
        /// Base address to use for relative urls in action methods.
        /// </summary>
        public string BaseAddress { get => baseAddress; set { baseAddress = value; Dirty = true; } }
        /// <summary>
        /// Whether to validate SSL certs - don't clear this unless in development environments.
        /// </summary>
        public bool VerifySsl { get => verifySsl; set { verifySsl = value; Dirty = true; } }
        /// <summary>
        /// Defines proxy configuration if needed.
        /// </summary>
        public ApiClientProxyOptions Proxy { get; internal set; } = new();
        /// <summary>
        /// If set, causes an exception to be thrown on an unsuccessful response (api call failure/timeout always throws an exception).
        /// </summary>
        public bool ExceptionOnFailure { get; set; }
        /// <summary>
        /// Max response content buffer size in bytes.  If &lt;= 0 then default of 2 GB is used.
        /// </summary>
        public long MaxResponseContentBufferSize { get => maxResponseContentBufferSize; set { maxResponseContentBufferSize = value; Dirty = true; } }
        /// <summary>
        /// Timeout for requests.  Defaults to 5 seconds.  Set to System.Threading.Timeout.InfiniteTimeSpan for unlimited (not recommended for synchronous calls).
        /// </summary>
        public TimeSpan Timeout { get => timeout; set { timeout = value; Dirty = true; } }
        /// <summary>
        /// Headers to use with each request (overridden by headers specified with a request).
        /// </summary>
        public ApiClientHeaders DefaultHeaders { get; } = new ApiClientHeaders();
        /// <summary>
        /// Set to non-null value to override charset in Content-Type header for calls.
        /// </summary>
        public string OverrideCharset { get; set; }
        /// <summary>
        /// Determines whether JSON bodies (POST/PUT) are set to camelCase or PascalCase (i.e. thisAttribute vs ThisAttribute).
        /// </summary>
        public bool CamelCaseJsonOutput { get; set; }
        /// <summary>
        /// If set, logs basic information about API calls as Information messages; otherwise logs these as Debug.  Defaults to true (Information).
        /// </summary>
        public bool LogApiCallsAsInfo { get; set; } = true;
        /// <summary>
        /// If set, logs extended information about API calls that fail (400+ status).  Defaults to false.
        /// </summary>
        public bool LogExtendedErrorInfo { get; set; } = false;
    }

    public class ApiClientProxyOptions
    {
        /// <summary>
        /// If this is not set, no proxy is used.
        /// </summary>
        public string Uri { get; set; } = "";
        public bool BypassOnLocal { get; set; } = false;
        public bool UseDefaultCredentials { get; set; } = false;
        /// <summary>
        /// If this is not set, no credentials are used (unauthenticated proxy).
        /// </summary>
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        internal NetworkCredential Credentials => string.IsNullOrEmpty(Username) ? null : new NetworkCredential(Username, Password);
        internal WebProxy GetProxy()
        {
            if (string.IsNullOrEmpty(Uri))
                return null;
            var proxy = new WebProxy
            {
                Address = new Uri(Uri),
                BypassProxyOnLocal = BypassOnLocal,
                UseDefaultCredentials = UseDefaultCredentials
            };
            if (Credentials != null)
                proxy.Credentials = Credentials;
            return proxy;
        }
    }
}
