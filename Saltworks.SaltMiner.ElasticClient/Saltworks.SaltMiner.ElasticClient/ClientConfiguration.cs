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

﻿namespace Saltworks.SaltMiner.ElasticClient
{
    // This is a sub config, meaning not loaded directly from a settings file.  
    // As such, there is no need to worry about encrypted settings - we assume all are unencrypted.
    public class ClientConfiguration
    {
        
        private int _DefaultPageSize = 1000;
        /// <summary>
        /// Host for ElasticSearch server (just the host, not a URL / URI)
        /// </summary>
        public string[] ElasticSearchHost { get; set; } = new[] { "localhost"};
        /// <summary>
        /// Http scheme for calling ElasticSearch, should be "http" or "https"
        /// </summary>
        public string HttpScheme { get; set; } = "http";
        /// <summary>
        /// ElasticSearch server http port number
        /// </summary>
        public int Port { get; set; } = 9200;
        /// <summary>
        /// Username for basic authentication
        /// </summary>
        public string Username { get; set; } = "elastic";
        /// <summary>
        /// Password for basic authentication
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// Set to true to verify SSL certificates, false to disable (not recommended)
        /// </summary>
        public bool VerifySsl { get; set; } = true;
        /// <summary>
        /// Timeout in seconds before request fails
        /// </summary>
        public int RequestTimeout { get; set; } = 60;
        /// <summary>
        /// Default number of documents to return in a single scrollable query.  Defaults to 1000 if not between 10 and 5000
        /// </summary>
        public int DefaultPageSize { get => _DefaultPageSize; set => _DefaultPageSize = (value < 10 || value > 5000) ? 1000 : value; }

        /// <summary>
        /// Default index used to initialize elasticsearch
        /// </summary>
        public string DefaultIndex { get; set; } = "assets_inventory";

        /// <summary>
        /// Timeout in minutes the scroll stays alive
        /// </summary>
        public string DefaultPagingTimeout { get; set; } = "2m";

        /// <summary>
        /// If set, configures the client to throw a ElasticClientException if the response is invalid
        /// </summary>
        public bool ExceptionOnInvalidResponse { get; set; } = true;

        /// <summary>
        /// Enables direct streaming in client, increasing DebugInformation available in Elastic responses and decreasing performance
        /// </summary>
        public bool EnableDebugInfoInElasticsearchResponse { get; set; } = false;

        /// <summary>
        /// Enables error diagnostics when a bulk add failure occurs, greatly slowing performance during errors
        /// </summary>
        public bool EnableBulkAddErrorDiagnostics { get; set; } = false;

        /// <summary>
        /// Set this to true to disable sniffing behaviors in the Nest connection pool
        /// </summary>
        public bool SingleNodeCluster { get; set; } = false;
    }
}
