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

﻿using Microsoft.Extensions.Configuration;
using Saltworks.SaltMiner.Core.Common;
using Saltworks.SaltMiner.DataApi.Authentication;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.DataApi.Models
{
    public class ApiConfig: ConfigBase
    {
        public const string CONFIG_SECRET_DEFAULT = "insecure-change-it";

        public ApiConfig() { }

        public ApiConfig(IConfiguration config, string filePath)
        {
            config.Bind("ApiConfig", this);
            Validate(filePath);
        }

        public void Validate(string filePath)
        {
            CheckEncryption(this, filePath, "ApiConfig");
            DecryptProperties(this);
            if (!ApiKeys.ContainsKey(Role.Config.ToString("g")))
            {
                ApiKeys.Add(CONFIG_SECRET_DEFAULT, Role.Config.ToString("g"));
            }
        }

        public string TemplateToVerify { get; set; } = "queue_asset";
        public string KibanaBaseUrl { get; set; } = "http://localhost:5601";
        public string ElasticHttpScheme { get; set; } = "http";
        public string ElasticHost { get; set; }
        public int ElasticPort { get; set; } = 9200;
        public int ElasticDefaultResultSize { get; set; } = 300;
        public string ElasticDefaultPagingTimeout { get; set; } = "10s";
        public string ElasticUsername { get; set; }
        public string ElasticPassword { get; set; }
        public bool ElasticEnableDiagnosticInfo { get; set; } = false;
        public bool ElasticEnableBulkAddDiagnosticInfo { get; set; } = false;
        public string ElasticBackupRepoName { get; set; } = "saltminer_repo";
        public int ConcurrencyLockTime { get; set; } = 1;
        public string ElasticBackupName { get; set; } = "saltminer_backup";
        public bool ElasticSingleNodeCluster { get; set; } = false;
        public string InventoryAssetEnrichmentPolicy { get; set; } = "inventory-asset-enrich-policy";
        public Dictionary<string, string> ApiKeys { get; set; } = new();  // key, role
        public string AuthHeader { get; set; } = "Authorization";
        public string ElasticAppRolePrefix { get; set; } = "smapp_";
        public string AuthType { get; set; } = "bearer";
        public bool VerifySsl { get; set; } = true;
        public string LicenseFileName { get; set; } = "license.txt";
        public string LicenseProcessedFileName{ get; set; } = "License.processed";
        public int Timeout { get; set; } = 10;
        public bool KestrelAllowRemote { get; set; } = false; // referenced from file directly, so no property references
        public int KestrelPort { get; set; } = 5000; // referenced from file directly, so no property references
        public string KeyPath { get; set; } = "license.lnf";
        public string VersionFileName { get; set; } = "version.txt";
        public string ElasticBackupLocation { get; set; }
        public string TempFileLocation { get; set; }
        public string NginxRoute { get; set; } = "smapi";
        public string NginxScheme { get; set; } = "https";
        public string DataIndexTemplatePath { get; set; } = "./data/index-templates/";
        public string DataSeedPath { get; set; } = "./data/seeds/";
        public string DataKibanaSpacePath { get; set; } = "./data/kibana-spaces";
        public string DataRolesPath { get; set; } = "./data/roles";
        public string DataEnrichmentPath { get; set; } = "./data/enrichments";
        public string DataIngestPipelinePath { get; set; } = "./data/ingest-pipelines";
        public string DataIndexPolicyPath { get; set; } = "./data/index-policies";
        public string DataIssueIndexDefaultAlias { get; set; } = "{ \"actions\": [ { \"add\": { \"index\": \"[indexName]\", \"alias\": \"issues_active_app\", \"filter\": { \"bool\": { \"must\": [ { \"term\": { \"vulnerability.is_active\": true } }, { \"term\": { \"saltminer.is_historical\": false } } ] } } } } ] }";
        public static string IndexVersion => "3.0.8";
        public string IssuesActiveAlias { get; set; } = @"{ ""actions"": [ { ""add"": { ""index"": ""issues_[assetType]_[sourceType]"", ""alias"": ""issues_active_[assetType]"", ""filter"": { ""term"": { ""vulnerability.is_active"": true } } } } ] }";
        public bool DisableUpgradeRunner { get; set; } = true;
        public bool EnableWebhookDebug { get; set; } = false;
        public bool EnableWebhooks { get; set; } = false;
        public bool EnableWebhookSecurity { get; set; } = true;
        public Dictionary<string, string> WebhookSecrets { get; set; } = [];
        public int WebhookBatchSize { get; set; } = 100;
    }
}
