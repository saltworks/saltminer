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

﻿using Microsoft.Extensions.DependencyInjection;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.Utility.ApiHelper;

namespace Saltworks.SaltMiner.UiApiClient
{
    public static class Extensions
    {
        public static void AddUiApiClientAsSingleton<T>(this IServiceCollection services, Action<UiApiClientOptions> configureOptions) where T : class
        {
            SetupUiApiClientOptions<T>(services, configureOptions);
            services.AddSingleton<UiApiClientFactory<T>>();
        }

        public static void AddUiApiClientAsTransient<T>(this IServiceCollection services, Action<UiApiClientOptions> configureOptions) where T : class
        {
            SetupUiApiClientOptions<T>(services, configureOptions);
            services.AddTransient<UiApiClientFactory<T>>();
        }

        public static void AddUiApiClient<T>(this IServiceCollection services, Action<UiApiClientOptions> configureOptions) where T : class
        {
            AddUiApiClientAsTransient<T>(services, configureOptions);
        }

        private static void SetupUiApiClientOptions<T>(this IServiceCollection services, Action<UiApiClientOptions> configureOptions) where T : class
        {
            var options = new UiApiClientOptions();
           
            configureOptions?.Invoke(options);

            options.RunConfig ??= new();

            services.AddApiClient<T>(o =>
            {
                o.BaseAddress = options.UiApiBaseAddress;
                o.ExceptionOnFailure = false;
                o.Timeout = options.UiApiTimeout;
                o.VerifySsl = options.UiApiVerifySsl;
                o.CamelCaseJsonOutput = false;
                if (!string.IsNullOrEmpty(options.RunConfig.ReportingApiAuthHeader))
                    o.DefaultHeaders.Add(ApiClientHeaders.OneHeader(options.RunConfig.ReportingApiAuthHeader, options.RunConfig.ReportingApiKey));
            });
            
            services.AddSingleton(options.RunConfig);
        }

        public static void UseUiApiClient<T>(this IServiceProvider provider) where T : class
        {
            provider.UseApiClient<T>();
        }

        public static string Left(this string value, int numChars)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            if (value.Length < numChars)
            {
                return value;
            }

            return value[..numChars];
        }

        public static ErrorResponse ToErrorResponse(this Exception ex)
        {
            var status = 500;
            //List of messages
            var msgs = new List<string>();

            if (ex is UiApiClientHttpException apiException2)
            {
                if (apiException2.HttpMessages != null && apiException2.HttpMessages.Count > 0)
                    msgs.AddRange(apiException2.HttpMessages);
                else
                    msgs.Add(apiException2.HttpStatus.ToString());
                status = apiException2.HttpStatus;
            }
            else
            {
                if (!string.IsNullOrEmpty(ex.Message))
                    msgs.Add(ex.Message);
            }

            if (ex.InnerException != null)
                msgs.Add($" (inner exception: {ex.InnerException.Message}");

            return new ErrorResponse(status, ex.GetType().ToString(), msgs);
        }
        public static Dictionary<string, string> ToDictionary(this List<TextField> list) => list.ToDictionary(k => k.Name, v => v.Value);
    }
}
