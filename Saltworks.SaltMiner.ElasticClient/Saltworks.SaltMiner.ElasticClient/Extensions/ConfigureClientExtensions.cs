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
