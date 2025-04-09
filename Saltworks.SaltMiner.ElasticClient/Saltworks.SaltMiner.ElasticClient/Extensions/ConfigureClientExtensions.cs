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
using Microsoft.Extensions.Logging;
using System;

namespace Saltworks.SaltMiner.ElasticClient
{
    public static class ConfigureClientExtensions
    {
        public static void AddNestClient(this IServiceCollection services)
        {
            AddNestClient(services, null);
        }

        public static void AddNestClient(this IServiceCollection services, Action<ClientConfiguration> configureOptions)
        {

            var options = new ClientConfiguration();
            configureOptions?.Invoke(options);
            services.AddSingleton<IElasticClientFactory>(new NestClientFactory(options));
        }

        /// <summary>
        /// Configures the Elasticsearch NestClient.
        /// </summary>
        /// <param name="services"></param>
        public static void UseNestClient(this IServiceProvider services)
        {
            ILogger<IElasticClient> logger;
            try
            {
                logger = services.GetRequiredService<ILogger<IElasticClient>>();
            }
            catch (Exception)
            {
                // ignore any problem getting a logger
                logger = null;
            }

            var factory = services.GetRequiredService<IElasticClientFactory>();
            if (!factory.Configuration.VerifySsl)
            {
                logger?.LogWarning("SSL verify is disabled for Elasticsearch connections.  This is insecure and not a recommended configuration.");
            }
            factory.Logger = logger;
            logger?.LogDebug("Registered NestClient for use.");
        }

        public static void UseNestClient(this Microsoft.AspNetCore.Builder.IApplicationBuilder builder)
        {
            builder.ApplicationServices.UseNestClient();
        }

        public static void Configure(Microsoft.AspNetCore.Builder.IApplicationBuilder app, ILogger<NestClient> logger)
        {
            app.UseNestClient();
        }
    }
}
