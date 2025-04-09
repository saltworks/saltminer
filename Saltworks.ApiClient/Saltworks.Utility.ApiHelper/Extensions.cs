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

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Saltworks.Utility.ApiHelper
{
    public static class Extensions
    {
        /// <summary>
        /// Adds ApiClient support to DI for the specified type with a default set of options.
        /// </summary>
        /// <typeparam name="T">Any type - this is used to support multiple instances with separate configs.</typeparam>
        /// <remarks>Using type T in this way is as close as .NET Core's DI lets use get to named instances.</remarks>
        public static void AddApiClient<T>(this IServiceCollection services)
        {
            AddApiClient<T>(services, null);
        }

        /// <summary>
        /// Adds ApiClientFactory support to DI - use this version if only implementing one instance of ApiClient.  Uses default set of options.
        /// </summary>
        public static void AddApiClient(this IServiceCollection services)
        {
            AddApiClient<ApiClient>(services, null);
        }

        /// <summary>
        /// Adds ApiClientFactory support to DI for the specified type.
        /// </summary>
        /// <typeparam name="T">Any type - this is used to support multiple instances with separate configs.</typeparam>
        /// <param name="services">Target service collection</param>
        /// <param name="configureOptions">Action used to configure options for the resulting ApiClientFactory then ApiClient.</param>
        /// <remarks>Using type T in this way is as close as .NET Core's DI lets use get to named instances.</remarks>
        public static void AddApiClient<T>(this IServiceCollection services, Action<ApiClientOptions> configureOptions)
        {
            var options = new ApiClientOptions();
            var cookieJar = new CookieContainer();

            configureOptions?.Invoke(options);

            var acc = new ApiClientFactory<T>(options, cookieJar);
            services.AddHttpClient(acc.Name, c => c.BaseAddress = new Uri(options.BaseAddress))
                .ConfigurePrimaryHttpMessageHandler(sp => new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    ServerCertificateCustomValidationCallback = (message, certificate2, chain, errors) =>
                    {
                        var f = sp.GetRequiredService<ApiClientFactory<T>>();
                    
                        if (!f.Options.VerifySsl)
                        {
                            return true;
                        }

                        return errors == System.Net.Security.SslPolicyErrors.None;
                    },
                    CookieContainer = cookieJar,
                    Proxy = options.Proxy.GetProxy()
                });
 
            services.AddSingleton(acc);
        }

        /// <summary>
        /// Configures the typed ApiClient.
        /// </summary>
        /// <typeparam name="T">Any type already added to DI using AddApiClient() - this is used to support multiple instances with separate configs.</typeparam>
        /// <remarks>Using type T in this way is as close as .NET Core's DI lets use get to named instances.</remarks>
        public static void UseApiClient<T>(this IServiceProvider services, Action<ApiClientOptions> configureOptions)
        {
            var depFactory = services.GetRequiredService<IHttpClientFactory>();

            ILogger<ApiClient> logger;

            try
            {
                logger = services.GetRequiredService<ILogger<ApiClient>>();
            }
            catch
            {
                // ignore any problem getting a logger
                logger = null;
            }

            var config = services.GetRequiredService<ApiClientFactory<T>>();
            configureOptions?.Invoke(config.Options);
            config.DepFactory = depFactory;
            config.Logger = logger;

            logger.LogDebg($"Registered ApiClient for type [{typeof(T).Name}].");
        }

        /// <summary>
        /// Configures the typed ApiClient.
        /// </summary>
        /// <typeparam name="T">Any type already added to DI using AddApiClient() - this is used to support multiple instances with separate configs.</typeparam>
        /// <remarks>Using type T in this way is as close as .NET Core's DI lets use get to named instances.</remarks>
        public static void UseApiClient<T>(this IServiceProvider services)
        {
            services.UseApiClient<T>(null);
        }

        /// <summary>
        /// Configures the typed ApiClient.
        /// </summary>
        /// <typeparam name="T">Any type already added to DI using AddApiClient() - this is used to support multiple instances with separate configs.</typeparam>
        /// <remarks>Using type T in this way is as close as .NET Core's DI lets use get to named instances.</remarks>
        public static void UseApiClient<T>(this IApplicationBuilder builder, Action<ApiClientOptions> configureOptions = null)
        {
            builder.ApplicationServices.UseApiClient<T>(configureOptions);
        }

        // See https://geeklearning.io/serialize-an-object-to-an-url-encoded-string-in-csharp/ for reference on this extension.
        /// <summary>
        /// Converts an object into a dictionary suitable for submission as form content.
        /// </summary>
        public static IDictionary<string, string> ToFormContentDictionary(this object metaToken)
        {
            if (metaToken == null)
                return new Dictionary<string, string>();
            
            if (metaToken is not JToken token)
            {
                return ToFormContentDictionary(JObject.FromObject(metaToken));
            }

            if (token.HasValues)
            {
                var contentData = new Dictionary<string, string>();
                foreach (var child in token.Children().ToList())
                {
                    var childContent = child.ToFormContentDictionary();
                    if (childContent != null)
                    {
                        contentData = contentData.Concat(childContent).ToDictionary(k => k.Key, v => v.Value);
                    }
                }
                return contentData;
            }

            var jValue = token as JValue;
            if (jValue?.Value == null)
            {
                return new Dictionary<string, string>();
            }

            var value = jValue.Type == JTokenType.Date ?
                jValue.ToString("o", CultureInfo.InvariantCulture) :
                jValue.ToString(CultureInfo.InvariantCulture);

            return new Dictionary<string, string> { { token.Path, value } };
        }

        internal static void LogInfo(this ILogger logger, string msg, params object[] args)
        {
            if (logger != null)
            {
                logger.LogInformation(msg, args);
            }
        }

        internal static void LogErr(this ILogger logger, string msg, params object[] args)
        {
            logger.LogErr(null, msg, args);
        }

        internal static void LogErr(this ILogger logger, Exception ex, string msg, params object[] args)
        {
            if (logger != null)
            {
                logger.LogError(ex, msg, args);
            }
        }

        internal static void LogDebg(this ILogger logger, string msg, params object[] args)
        {
            if (logger != null)
            {
                logger.LogDebug(msg, args);
            }
        }
    }
}
