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

using System;

namespace Saltworks.SaltMiner.ElasticClient;
// This is a sub config, meaning not loaded directly from a settings file.  
// As such, there is no need to worry about encrypted settings - we assume all are unencrypted.
public class ClientConfiguration
{
    
    private int _DefaultPageSize = 1000;
    /// <summary>
    /// Connection string, if supplied, overrides other properties.
    /// Format: "Key=Value;Key=Value;..." with keys: Host, Port, Scheme, Username, Password, SslVerify, CloudID, ApiKeyId, ApiKeyValue, UseAuth
    /// Examples: "Host=localhost;Port=9200;Scheme=https;Username=elastic;Password=changeme"
    ///           "CloudID=my-deployment:hexcode;Username=elastic;Password=changeme"
    ///           "CloudID=my-deployment:hexcode;ApiKeyId=key-id;ApiKeyValue=key-value"
    /// </summary>
    public string ElasticConnectionString { get; set; } = "";
    /// <summary>
    /// Cloud ID for Elastic Cloud connections; if set, overrides host-based configuration
    /// </summary>
    public string CloudId { get; set; } = "";
    /// <summary>
    /// API key ID for API key authentication (use with ApiKeyValue)
    /// </summary>
    public string ApiKeyId { get; set; } = "";
    /// <summary>
    /// API key value/secret for API key authentication (use with ApiKeyId)
    /// </summary>
    public string ApiKeyValue { get; set; } = "";
    /// <summary>
    /// Set to false to connect without authentication (anonymous). Defaults to true.
    /// </summary>
    public bool UseAuth { get; set; } = true;
    /// <summary>
    /// Host for ElasticSearch server (just the host, not a URL / URI)
    /// </summary>
    public string[] ElasticSearchHost { get; set; } = [ "localhost" ];
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
    /// Sets the maximum number of documents to retrieve from Elasticsearch for a "cold" search (one that has no AfterKeys, but is past page 1).
    /// Defaults to 10,000 which is also the maximum number of documents Elasticsearch will return in a single query.
    /// </summary>
    public int MaxIndexDocsForPaging { get; set; } = 10000;
    /// <summary>
    /// In future versions this could change in Elasticsearch, so we will make a wee property that can give the number to us.
    /// </summary>
    internal static int MaxDocsInOneQuery => 10000;
    /// <summary>
    /// Maximum number of documents to send in a single bulk add/update request
    /// </summary>
    public int MaxBulkDocsPerRequest { get; set; } = 10000;
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
    /// <summary>
    /// List of indices known to have inconsistent IDs (internal id might not match doc _id).  Some operations will be slower for these.
    /// </summary>
    public string[] IndicesWithInconsistentIds { get; set; } = [];
}
