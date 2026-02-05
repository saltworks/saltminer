using Microsoft.Extensions.Logging;
using Elastic.Clients.Elasticsearch;
using System.Collections.Generic;
using System;
using Elastic.Transport;

namespace Saltworks.SaltMiner.ElasticClient.EsClient;

/// <summary>
/// EsClient factory class
/// </summary>
public class EsClientFactory : IElasticClientFactory
{
    // Logger is set by "UseEsClient()" extension
    public ILogger<IElasticClient> Logger { get; set; } = null;
    public ClientConfiguration Configuration { get; private set; } = null;
    private ElasticsearchClientSettings ConnectionSettings { get; set; }

    public EsClientFactory(ClientConfiguration configuration)
    {
        Configuration = configuration;
        ConfigureConnection();
    }

    private void ConfigureConnection()
    {
        var uris = BuildUris();

        var nodePool = new StaticNodePool(uris);
        var settings = new ElasticsearchClientSettings(nodePool)
            .Authentication(new BasicAuthentication(Configuration.Username, Configuration.Password))
            .DefaultFieldNameInferrer(p => p.ToSnakeCase());

        if (Configuration.EnableDebugInfoInElasticsearchResponse)
        {
            settings.EnableDebugMode();
        }
        if (!Configuration.VerifySsl)
        {
            settings.ServerCertificateValidationCallback((o, cert, chain, errors) => true)
                .ServerCertificateValidationCallback(CertificateValidations.AllowAll);
        }

        ConnectionSettings = settings;
    }

    private List<Uri> BuildUris()
    {
        var uri = new List<Uri>();
        foreach (var address in Configuration.ElasticSearchHost)
        {
            uri.Add(new Uri($"{Configuration.HttpScheme}://{address}:{Configuration.Port}"));
        }
        return uri;
    }

    /// <summary>
    /// Creates an EsClient from DI configuration
    /// </summary>
    public IElasticClient CreateClient()
    {
        return new EsClient(Configuration, ConnectionSettings, Logger);
    }
}
