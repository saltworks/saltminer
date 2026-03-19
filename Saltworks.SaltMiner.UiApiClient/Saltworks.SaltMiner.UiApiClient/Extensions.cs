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
