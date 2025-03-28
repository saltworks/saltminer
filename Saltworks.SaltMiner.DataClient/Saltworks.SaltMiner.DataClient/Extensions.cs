using Microsoft.Extensions.DependencyInjection;
using Saltworks.Utility.ApiHelper;
using System;

namespace Saltworks.SaltMiner.DataClient
{
    public static class Extensions
    {
        public static void AddDataClientAsSingleton<T>(this IServiceCollection services, Action<DataClientOptions> configureOptions) where T : class
        {
            SetupDataClientOptions<T>(services, configureOptions);
            services.AddSingleton<DataClientFactory<T>>();
        }

        public static void AddDataClientAsTransient<T>(this IServiceCollection services, Action<DataClientOptions> configureOptions) where T : class
        {
            SetupDataClientOptions<T>(services, configureOptions);
            services.AddTransient<DataClientFactory<T>>();
        }

        public static void AddDataClient<T>(this IServiceCollection services, Action<DataClientOptions> configureOptions) where T : class
        {
            AddDataClientAsTransient<T>(services, configureOptions);
        }

        private static void SetupDataClientOptions<T>(this IServiceCollection services, Action<DataClientOptions> configureOptions) where T : class
        {
            var options = new DataClientOptions();
            configureOptions?.Invoke(options);
            options.RunConfig ??= new();

            services.AddApiClient<T>(o =>
            {
                o.BaseAddress = options.ApiBaseAddress;
                o.DefaultHeaders.Add(options.ApiKeyHeader, options.ApiKey);
                o.ExceptionOnFailure = false;
                o.Timeout = options.Timeout;
                o.VerifySsl = options.VerifySsl;
                o.CamelCaseJsonOutput = false;
                o.LogExtendedErrorInfo = options.LogExtendedErrorInfo;
                o.LogApiCallsAsInfo = options.LogApiCallsAsInfo;
            });           
            services.AddSingleton(options.RunConfig);
        }

        public static void UseDataClient<T>(this IServiceProvider provider) where T : class
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

            return value.Substring(0, numChars);
        }
    }
}
