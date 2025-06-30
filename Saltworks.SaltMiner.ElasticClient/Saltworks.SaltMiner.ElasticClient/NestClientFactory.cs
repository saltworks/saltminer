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

﻿using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.ElasticClient
{
    /// <summary>
    /// NestClient factory class
    /// </summary>
    public class NestClientFactory : IElasticClientFactory
    {
        // Logger is set by "UseNestClient()" extension
        public ILogger<IElasticClient> Logger { get; set; } = null;
        public ClientConfiguration Configuration { get; private set; } = null;
        private ConnectionSettings ConnectionSettings { get; set; }

        internal NestClientFactory(ClientConfiguration configuration)
        {
            Configuration = configuration;
            ConfigureConnection();
        }

        private void ConfigureConnection()
        {
            var uris = BuildUris();
            if (Configuration.SingleNodeCluster)
            {
                ConnectionSettings = new(new StaticConnectionPool(uris), sourceSerializer: (builtin, settings) => new SnakeCustomJsonNetSerializer(builtin, settings));
            }
            else
            {
                ConnectionSettings = new(new SniffingConnectionPool(uris), sourceSerializer: (builtin, settings) => new SnakeCustomJsonNetSerializer(builtin, settings));
            }
            ConnectionSettings.DisableDirectStreaming(Configuration.EnableDebugInfoInElasticsearchResponse)
                .BasicAuthentication(Configuration.Username, Configuration.Password)
                .ThrowExceptions();
            if (!Configuration.VerifySsl)
            {
                ConnectionSettings.ServerCertificateValidationCallback((o, cert, chain, errors) => true)
                    .ServerCertificateValidationCallback(CertificateValidations.AllowAll);
            }
        }

        private IEnumerable<Uri> BuildUris()
        {
            var uri = new List<Uri>();
            foreach (var address in Configuration.ElasticSearchHost)
            {
                uri.Add(new Uri($"{Configuration.HttpScheme}://{address}:{Configuration.Port}"));
            }
            return uri;
        }

        /// <summary>
        /// Creates a NestClient from DI configuration
        /// </summary>
        public IElasticClient CreateClient()
        {
            return new NestClient(Configuration, ConnectionSettings, Logger);
        }
    }
}
