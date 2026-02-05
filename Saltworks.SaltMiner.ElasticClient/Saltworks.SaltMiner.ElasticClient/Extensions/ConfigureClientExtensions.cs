/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.ElasticClient.EsClient;
using System;

namespace Saltworks.SaltMiner.ElasticClient;
public static class ConfigureClientExtensions
{
    #region EsClient Client...from the dept of redundancy dept
    public static void AddEsClient(this IServiceCollection services)
    {
        AddEsClient(services, null);
    }

    public static void AddEsClient(this IServiceCollection services, Action<ClientConfiguration> configureOptions)
    {

        var options = new ClientConfiguration();
        configureOptions?.Invoke(options);
        services.AddSingleton<IElasticClientFactory>(new EsClientFactory(options));
    }

    /// <summary>
    /// Configures the Elasticsearch EsClient.
    /// </summary>
    /// <param name="services"></param>
    public static void UseEsClient(this IServiceProvider services)
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
        logger?.LogDebug("Registered EsClient for use.");
    }

    public static void UseEsClient(this Microsoft.AspNetCore.Builder.IApplicationBuilder builder)
    {
        builder.ApplicationServices.UseEsClient();
    }

    public static void Configure(Microsoft.AspNetCore.Builder.IApplicationBuilder app, ILogger<EsClient.EsClient> logger)
    {
        app.UseEsClient();
    }

    #endregion
}
