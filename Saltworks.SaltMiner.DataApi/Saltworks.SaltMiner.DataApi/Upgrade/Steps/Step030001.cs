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

﻿using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.DataApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Saltworks.SaltMiner.DataApi.Upgrade.Steps
{
    /*
     * Example step class.  For this upgrade, all the main indices get a new "foo" field.  The name could honestly be arbitrary, as the class knows what version it's looking for.
     * We could potentially split really complex upgrades between multiple "steps" as well - a step could apply to only one index for example and would ignore the rest.
     * Matching the index name within the class can be done as a partial match, as shown, or could be different logic entirely.
     * This sample step wants a default value of "bar" in the new "foo" field, so it will expect a re-index and apply itself to all the 3 main index sets.
     */
    internal class Step030001 : IUpgradeStep
    {
        internal Step030001(ApiConfig config)
        {
            // config may or may not be needed - in this example it is not needed
        }

        string IUpgradeStep.AppliesToVersion => "3.0.0";

        string IUpgradeStep.CompletedVersion => "3.0.1";

        bool IUpgradeStep.RequiresSchemaUpdate => true;

        bool IUpgradeStep.RequiresETL => true;

        List<string> IUpgradeStep.UpdatedTemplateNames => [
            "asset", 
            "issue", 
            "scan", 
            "queue_asset", 
            "queue_scan", 
            "engagement",
            "attachment", 
            "snapshot" 
        ];

        void IUpgradeStep.UpdateSchema(string indexName, JsonNode indexTemplate)
        {
            var mappings = indexTemplate.AsObject()["index_templates"][0]["index_template"]["template"]["mappings"]["properties"];

            if (indexName.StartsWith("snapshot"))
            {
                var saltminer = mappings["saltminer"]["properties"].AsObject();
                var inventoryAsset = saltminer["inventory_asset"]["properties"].AsObject();
                var scan = saltminer["scan"]["properties"].AsObject();
                var asset = saltminer["asset"]["properties"].AsObject();
                var vulnerability = saltminer["vulnerability"]["properties"].AsObject();
                var vulnerabilityScore = vulnerability["score"]["properties"].AsObject();
                var vulnerabilityScanner = vulnerability["scanner"]["properties"].AsObject();

                #region Removals

                saltminer.Remove("attributes");
                saltminer.Remove("is_vulnerability");
                saltminer.Remove("name");
                saltminer.Remove("source_id");
                saltminer.Remove("attributes");
                saltminer.Remove("asset_type");

                asset.Remove("sub_type");

                inventoryAsset.Remove("is_production");
                inventoryAsset.Remove("name");
                inventoryAsset.Remove("description");
                inventoryAsset.Remove("attributes");
                inventoryAsset.Remove("version");

                #endregion

                #region Additions

                if (saltminer["is_historical"]?.AsObject() == null)
                {
                    saltminer.Add("is_historical", new JsonObject());
                    saltminer["is_historical"].AsObject().Add("type", "boolean");
                }

                if (scan["assessment_type"]?.AsObject() == null)
                {
                    scan.Add("assessment_type", new JsonObject());
                    scan["assessment_type"].AsObject().Add("type", "keyword");
                    scan["assessment_type"].AsObject().Add("fields", new JsonObject());
                    scan["assessment_type"]["fields"].AsObject().Add("text", new JsonObject());
                    scan["assessment_type"]["fields"]["text"].AsObject().Add("type", "text");
                }

                if (scan["product_type"]?.AsObject() == null)
                {
                    scan.Add("product_type", new JsonObject());
                    scan["product_type"].AsObject().Add("type", "keyword");
                    scan["product_type"].AsObject().Add("fields", new JsonObject());
                    scan["product_type"]["fields"].AsObject().Add("text", new JsonObject());
                    scan["product_type"]["fields"]["text"].AsObject().Add("type", "text");
                }

                if (scan["vendor"]?.AsObject() == null)
                {
                    scan.Add("vendor", new JsonObject());
                    scan["vendor"].AsObject().Add("type", "keyword");
                    scan["vendor"].AsObject().Add("fields", new JsonObject());
                    scan["vendor"]["fields"].AsObject().Add("text", new JsonObject());
                    scan["vendor"]["fields"]["text"].AsObject().Add("type", "text");
                }

                if (scan["product"]?.AsObject() == null)
                {
                    scan.Add("product", new JsonObject());
                    scan["product"].AsObject().Add("type", "keyword");
                    scan["product"].AsObject().Add("fields", new JsonObject());
                    scan["product"]["fields"].AsObject().Add("text", new JsonObject());
                    scan["product"]["fields"]["text"].AsObject().Add("type", "text");
                }

                if (vulnerability["severity"]?.AsObject() == null)
                {
                    vulnerability.Add("severity", new JsonObject());
                    vulnerability["severity"].AsObject().Add("type", "keyword");
                }

                if (vulnerability["source_severity"]?.AsObject() == null)
                {
                    vulnerability.Add("source_severity", new JsonObject());
                    vulnerability["source_severity"].AsObject().Add("type", "keyword");
                }

                if (vulnerability["name"]?.AsObject() == null)
                {
                    vulnerability.Add("name", new JsonObject());
                    vulnerability["name"].AsObject().Add("type", "keyword");
                    vulnerability["name"].AsObject().Add("fields", new JsonObject());
                    vulnerability["name"]["fields"].AsObject().Add("text", new JsonObject());
                    vulnerability["name"]["fields"]["text"].AsObject().Add("type", "text");
                }

                if (vulnerability["category"]?.AsObject() == null)
                {
                    vulnerability.Add("category", new JsonObject());
                    vulnerability["category"].AsObject().Add("type", "keyword");
                    vulnerability["category"].AsObject().Add("fields", new JsonObject());
                    vulnerability["category"]["fields"].AsObject().Add("text", new JsonObject());
                    vulnerability["category"]["fields"]["text"].AsObject().Add("type", "text");
                }

                if (vulnerability["classification"]?.AsObject() == null)
                {
                    vulnerability.Add("classification", new JsonObject());
                    vulnerability["classification"].AsObject().Add("type", "keyword");
                    vulnerability["classification"].AsObject().Add("fields", new JsonObject());
                    vulnerability["classification"]["fields"].AsObject().Add("text", new JsonObject());
                    vulnerability["classification"]["fields"]["text"].AsObject().Add("type", "text");
                }

                if (vulnerability["severity_level"]?.AsObject() == null)
                {
                    vulnerability.Add("severity_level", new JsonObject());
                    vulnerability["severity_level"].AsObject().Add("type", "integer");
                }

                if (vulnerabilityScore["base"]?.AsObject() == null)
                {
                    vulnerabilityScore.Add("base", new JsonObject());
                    vulnerabilityScore["base"].AsObject().Add("type", "float");
                }

                if (vulnerabilityScore["environmental"]?.AsObject() == null)
                {
                    vulnerabilityScore.Add("environmental", new JsonObject());
                    vulnerabilityScore["environmental"].AsObject().Add("type", "float");
                }

                if (vulnerabilityScore["temporal"]?.AsObject() == null)
                {
                    vulnerabilityScore.Add("temporal", new JsonObject());
                    vulnerabilityScore["temporal"].AsObject().Add("type", "float");
                }

                if (vulnerabilityScore["version"]?.AsObject() == null)
                {
                    vulnerabilityScore.Add("version", new JsonObject());
                    vulnerabilityScore["version"].AsObject().Add("type", "keyword");
                }

                if (vulnerabilityScanner["assessment_type"]?.AsObject() == null)
                {
                    vulnerabilityScanner.Add("assessment_type", new JsonObject());
                    vulnerabilityScanner["assessment_type"].AsObject().Add("type", "keyword");
                }

                if (vulnerabilityScanner["vendor"]?.AsObject() == null)
                {
                    vulnerabilityScanner.Add("vendor", new JsonObject());
                    vulnerabilityScanner["vendor"].AsObject().Add("type", "keyword");
                    vulnerabilityScanner["vendor"].AsObject().Add("fields", new JsonObject());
                    vulnerabilityScanner["vendor"]["fields"].AsObject().Add("text", new JsonObject());
                    vulnerabilityScanner["vendor"]["fields"]["text"].AsObject().Add("type", "text");
                }

                if (vulnerabilityScanner["product"]?.AsObject() == null)
                {
                    vulnerabilityScanner.Add("product", new JsonObject());
                    vulnerabilityScanner["product"].AsObject().Add("type", "keyword");
                    vulnerabilityScanner["product"].AsObject().Add("fields", new JsonObject());
                    vulnerabilityScanner["product"]["fields"].AsObject().Add("text", new JsonObject());
                    vulnerabilityScanner["product"]["fields"]["text"].AsObject().Add("type", "text");
                }

                #endregion

                #region Name Changes

                var configName = asset["config_name"]?.AsObject();
                if (configName != null)
                {
                    asset.Remove("config_name");
                    asset.Add("instance", configName);
                }

                #endregion
            }

            if (indexName.StartsWith("sys_attribute_definition"))
            {
                #region Name Changes

                var values = mappings["values"]["properties"].AsObject();

                if (values["hidden"]?.AsObject() == null)
                {
                    values.Add("hidden", new JsonObject());
                    values["hidden"].AsObject().Add("type", "boolean");
                }

                #endregion
            }

            if (indexName.StartsWith("queue_asset"))
            {
                var saltminer = mappings["saltminer"]["properties"].AsObject();
                var asset = saltminer["asset"]["properties"].AsObject();

                #region Name Changes

                var configName = asset["config_name"]?.AsObject();
                if (configName != null)
                {
                    asset.Remove("config_name");
                    asset.Add("instance", configName);
                }

                #endregion
            }

            if (indexName.StartsWith("queue_scan"))
            {
                var saltminer = mappings["saltminer"]["properties"].AsObject();
                var scan = saltminer["scan"]["properties"].AsObject();

                #region Name Changes

                var configName = scan["config_name"]?.AsObject();
                if (configName != null)
                {
                    scan.Remove("config_name");
                    scan.Add("instance", configName);
                }

                #endregion

                #region Additions

                if (saltminer["lines_of_code"]?.AsObject() == null)
                {
                    saltminer.Add("lines_of_code", new JsonObject());
                    saltminer["lines_of_code"].AsObject().Add("type", "integer");
                }

                #endregion
            }

            if (indexName.StartsWith("queue_issue"))
            {
                var vulnerability = mappings["vulnerability"]["properties"].AsObject();

                #region Additions
                if (vulnerability["severity_level"]?.AsObject() == null)
                {
                    vulnerability.Add("severity_level", new JsonObject());
                    vulnerability["severity_level"].AsObject().Add("type", "integer");
                }

                #endregion
            }

            if (indexName.StartsWith("attachment"))
            {
                var saltminer = mappings["saltminer"]["properties"].AsObject();
                var attachment = saltminer["attachment"]["properties"].AsObject();

                #region Name Changes

                var uri = attachment["uri"]?.AsObject();
                if (uri != null)
                {
                    attachment.Remove("uri");
                    attachment.Add("file_id", uri);
                }

                #endregion

                #region Additions

                if (attachment["user"]?.AsObject() == null)
                {
                    attachment.Add("user", new JsonObject());
                    attachment["user"].AsObject().Add("type", "keyword");
                    attachment["user"].AsObject().Add("fields", new JsonObject());
                    attachment["user"]["fields"].AsObject().Add("text", new JsonObject());
                    attachment["user"]["fields"]["text"].AsObject().Add("type", "text");
                }

                #endregion
            }

            if (indexName.StartsWith("assets_"))
            {
                var saltminer = mappings["saltminer"]["properties"].AsObject();
                var asset = saltminer["asset"]["properties"].AsObject();

                #region Name Changes

                var configName = asset["config_name"]?.AsObject();
                if (configName != null)
                {
                    asset.Remove("config_name");
                    asset.Add("instance", configName);
                }

                #endregion

                #region Removal

                asset.Remove("scan");

                #endregion
            }

            if (indexName.StartsWith("scans_"))
            {
                var saltminer = mappings["saltminer"]["properties"].AsObject();
                var asset = saltminer["asset"]["properties"].AsObject();

                #region Name Changes

                var configName = asset["config_name"]?.AsObject();
                if (configName != null)
                {
                    asset.Remove("config_name");
                    asset.Add("instance", configName);
                }

                #endregion

                #region Removal

                asset.Remove("scan");

                #endregion

                #region Additions

                if (saltminer["lines_of_code"]?.AsObject() == null)
                {
                    saltminer.Add("lines_of_code", new JsonObject());
                    saltminer["lines_of_code"].AsObject().Add("type", "integer");
                }

                #endregion
            }

            if (indexName.StartsWith("engagement"))
            {
                var saltminer = mappings["saltminer"]["properties"].AsObject();
                var engagement = saltminer["engagement"]["properties"].AsObject();

                #region Name Changes

                var state = engagement["state"]?.AsObject();
                if (state != null)
                {
                    engagement.Remove("state");
                    engagement.Add("status", state);
                }

                #endregion
            }

            if (indexName.StartsWith("issues_"))
            {
                var saltminer = mappings["saltminer"]["properties"].AsObject();
                var scan = saltminer["scan"]["properties"].AsObject();
                var asset = saltminer["asset"]["properties"].AsObject();

                #region Additions

                if (scan["assessment_type"]?.AsObject() == null)
                {
                    scan.Add("assessment_type", new JsonObject());
                    scan["assessment_type"].AsObject().Add("type", "keyword");
                    scan["assessment_type"].AsObject().Add("fields", new JsonObject());
                    scan["assessment_type"]["fields"].AsObject().Add("text", new JsonObject());
                    scan["assessment_type"]["fields"]["text"].AsObject().Add("type", "text");
                }

                if (scan["product_type"]?.AsObject() == null)
                {
                    scan.Add("product_type", new JsonObject());
                    scan["product_type"].AsObject().Add("type", "keyword");
                    scan["product_type"].AsObject().Add("fields", new JsonObject());
                    scan["product_type"]["fields"].AsObject().Add("text", new JsonObject());
                    scan["product_type"]["fields"]["text"].AsObject().Add("type", "text");
                }

                if (scan["vendor"]?.AsObject() == null)
                {
                    scan.Add("vendor", new JsonObject());
                    scan["vendor"].AsObject().Add("type", "keyword");
                    scan["vendor"].AsObject().Add("fields", new JsonObject());
                    scan["vendor"]["fields"].AsObject().Add("text", new JsonObject());
                    scan["vendor"]["fields"]["text"].AsObject().Add("type", "text");
                }

                if (scan["product"]?.AsObject() == null)
                {
                    scan.Add("product", new JsonObject());
                    scan["product"].AsObject().Add("type", "keyword");
                    scan["product"].AsObject().Add("fields", new JsonObject());
                    scan["product"]["fields"].AsObject().Add("text", new JsonObject());
                    scan["product"]["fields"]["text"].AsObject().Add("type", "text");
                }

                var vulnerability = mappings["vulnerability"]["properties"].AsObject();
                if (vulnerability["severity_level"]?.AsObject() == null)
                {
                    vulnerability.Add("severity_level", new JsonObject());
                    vulnerability["severity_level"].AsObject().Add("type", "integer");
                }

                #endregion

                #region Name Changes

                var configName = asset["config_name"]?.AsObject();
                if (configName != null)
                {
                    asset.Remove("config_name");
                    asset.Add("instance", configName);
                }

                #endregion
            }
        }

        void MapAsset(SaltMinerIndexData index, string tempIndexName, IEnumerable<Asset> batch, SearchRequest request, IDataRepo data)
        {
            var doc = JsonNode.Parse(data.SearchForJson(request, tempIndexName));
            var count = 0;
            var array = doc["hits"]["hits"].AsArray();
            foreach (var item in batch)
            {
                if (array[count].AsObject()["_source"]["saltminer"]["asset"].AsObject()["config_name"] != null)
                {
                    item.Saltminer.Asset.Instance = array[count].AsObject()["_source"]["saltminer"]["asset"].AsObject()["config_name"].ToString();
                }
                count++;
            }

            index.Name = Asset.GenerateIndex((batch?.First())?.Saltminer.Asset.AssetType, (batch?.First())?.Saltminer.Asset.SourceType, (batch?.First())?.Saltminer.Asset.Instance);
        }

        void MapIssue(SaltMinerIndexData index, string tempIndexName, IEnumerable<Issue> batch, SearchRequest request, IDataRepo data)
        {
            var doc = JsonNode.Parse(data.SearchForJson(request, tempIndexName));
            var count = 0;
            var array = doc["hits"]["hits"].AsArray();

            foreach (var item in batch)
            {
                if (array[count].AsObject()["_source"]["saltminer"]["asset"].AsObject()["config_name"] != null)
                {
                    item.Saltminer.Asset.Instance = array[count].AsObject()["_source"]["saltminer"]["asset"].AsObject()["config_name"].ToString();
                }
                count++;
            }

            index.Name = Issue.GenerateIndex((batch?.First())?.Saltminer.Asset.AssetType, (batch?.First())?.Saltminer.Asset.SourceType, (batch?.First())?.Saltminer.Asset.Instance);
        }

        void MapScan(SaltMinerIndexData index, string tempIndexName, IEnumerable<Scan> batch, SearchRequest request, IDataRepo data)
        {
            var doc = JsonNode.Parse(data.SearchForJson(request, tempIndexName));
            var count = 0;
            var array = doc["hits"]["hits"].AsArray();

            foreach (var item in batch)
            {
                if (array[count].AsObject()["_source"]["saltminer"]["asset"].AsObject()["config_name"] != null)
                {
                    item.Saltminer.Asset.Instance = array[count].AsObject()["_source"]["saltminer"]["asset"].AsObject()["config_name"].ToString();
                }
                count++;
            }

            index.Name = Scan.GenerateIndex((batch?.First())?.Saltminer.Asset.AssetType, (batch?.First())?.Saltminer.Asset.SourceType, (batch?.First())?.Saltminer.Asset.Instance);
        }

        void MapQueueAsset(string tempIndexName, IEnumerable<QueueAsset> batch, SearchRequest request, IDataRepo data)
        {
            var doc = JsonNode.Parse(data.SearchForJson(request, tempIndexName));
            var count = 0;
            var array = doc["hits"]["hits"].AsArray();

            foreach (var item in batch)
            {
                if (array[count].AsObject()["_source"]["saltminer"]["asset"].AsObject()["config_name"] != null)
                {
                    item.Saltminer.Asset.Instance = array[count].AsObject()["_source"]["saltminer"]["asset"].AsObject()["config_name"].ToString();
                }
                count++;
            }
        }

        void MapQueueScan(string tempIndexName, IEnumerable<QueueScan> batch, SearchRequest request, IDataRepo data)
        {
            var doc = JsonNode.Parse(data.SearchForJson(request, tempIndexName));
            var count = 0;
            var array = doc["hits"]["hits"].AsArray();

            foreach (var item in batch)
            {
                if (array[count].AsObject()["_source"]["saltminer"]["scan"].AsObject()["config_name"] != null)
                {
                    item.Saltminer.Scan.Instance = array[count].AsObject()["_source"]["saltminer"]["scan"]?.AsObject()["config_name"]?.ToString();
                }
                count++;
            }
        }

        void MapEngagement(string tempIndexName, IEnumerable<Engagement> batch, SearchRequest request, IDataRepo data)
        {
            var doc = JsonNode.Parse(data.SearchForJson(request, tempIndexName));
            var count = 0;
            var array = doc["hits"]["hits"].AsArray();

            foreach (var item in batch)
            {
                if (array[count].AsObject()["_source"]["saltminer"]["engagement"].AsObject()["state"] != null)
                {
                    item.Saltminer.Engagement.Status = array[count].AsObject()["_source"]["saltminer"]["engagement"].AsObject()["state"].ToString();
                }
                count++;
            }
        }

        void MapAttachment(string tempIndexName, IEnumerable<Attachment> batch, SearchRequest request, IDataRepo data)
        {
            var doc = JsonNode.Parse(data.SearchForJson(request, tempIndexName));
            var count = 0;
            var array = doc["hits"]["hits"].AsArray();

            foreach (var item in batch)
            {
                if (array[count].AsObject()["_source"]["saltminer"]["attachment"].AsObject()["uri"] != null)
                {
                    item.Saltminer.Attachment.FileId = array[count].AsObject()["_source"]["saltminer"]["attachment"].AsObject()["uri"].ToString();
                }
                count++;
            }
        }

        void MapSnapshot(string tempIndexName, IEnumerable<Snapshot> batch, SearchRequest request, IDataRepo data)
        {
            var doc = JsonNode.Parse(data.SearchForJson(request, tempIndexName));
            var count = 0;
            var array = doc["hits"]["hits"].AsArray();

            foreach (var item in batch)
            {
                if (array[count].AsObject()["_source"]["saltminer"]["asset"].AsObject()["config_name"] != null)
                {
                    item.Saltminer.Asset.Instance = array[count].AsObject()["_source"]["saltminer"]["asset"].AsObject()["config_name"].ToString();
                }
                count++;
            }
        }

        void IUpgradeStep.StepEtl<T>(SaltMinerIndexData index, string tempIndexName, IEnumerable<T> batch, SearchRequest request, IDataRepo data)
        {
            switch (typeof(T))
            {
                case Type type when type == typeof(Issue):
                    MapIssue(index, tempIndexName, batch as IEnumerable<Issue>, request, data);
                    break;
                case Type type when type == typeof(Scan):
                    MapScan(index, tempIndexName, batch as IEnumerable<Scan>, request, data);
                    break;
                case Type type when type == typeof(Asset):
                    MapAsset(index, tempIndexName, batch as IEnumerable<Asset>, request, data);
                    break;
                case Type type when type == typeof(Snapshot):
                    MapSnapshot(tempIndexName, batch as IEnumerable<Snapshot>, request, data);
                    break;
                case Type type when type == typeof(QueueScan):
                    MapQueueScan(tempIndexName, batch as IEnumerable<QueueScan>, request, data);
                    break;
                case Type type when type == typeof(QueueAsset):
                    MapQueueAsset(tempIndexName, batch as IEnumerable<QueueAsset>, request, data);
                    break;
                case Type type when type == typeof(Engagement):
                    MapEngagement(tempIndexName, batch as IEnumerable<Engagement>, request, data);
                    break;
                case Type type when type == typeof(Attachment):
                    MapAttachment(tempIndexName, batch as IEnumerable<Attachment>, request, data);
                    break;
                default:
                    break;
            }
        }
    }
}
