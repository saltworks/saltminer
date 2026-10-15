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

﻿using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.DataApi.Models;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Saltworks.SaltMiner.DataApi.Upgrade.Steps
{
    /*
     * Example step class.  For this upgrade, all the main indices get a new "foo" field.  The name could honestly be arbitrary, as the class knows what version it's looking for.
     * We could potentially split really complex upgrades between multiple "steps" as well - a step could apply to only one index for example and would ignore the rest.
     * Matching the index name within the class can be done as a partial match, as shown, or could be different logic entirely.
     * This sample step wants a default value of "bar" in the new "foo" field, so it will expect a re-index and apply itself to all the 3 main index sets.
     */
    internal class Step030002 : IUpgradeStep
    {
        internal Step030002(ApiConfig config)
        {
            // config may or may not be needed - in this example it is not needed
        }

        string IUpgradeStep.AppliesToVersion => "3.0.1";

        string IUpgradeStep.CompletedVersion => "3.0.2";

        bool IUpgradeStep.RequiresSchemaUpdate => true;

        bool IUpgradeStep.RequiresETL => true;

        List<string> IUpgradeStep.UpdatedTemplateNames => new List<string> {
            "asset",
            "attachment",
            "comment",
            "engagement",
            "inventory_asset",
            "license",
            "queue_asset",
            "queue_scan",
            "sys_attribute_definition",
            "sys_config",
            "sys_custom_importer",
            "sys_custom_issue",
            "sys_index_meta",
            "sys_lookup",
            "sys_search_filter",
            "sys_service_job",
            "job_queue",
            "issue",
            "queue_issue",
            "snapshot",
            "scan"
        };

        void IUpgradeStep.UpdateSchema(string indexName, JsonNode indexTemplate)
        {
            var mappings = indexTemplate.AsObject()["index_templates"][0]["index_template"]["template"]["mappings"]["properties"];

            if (indexName.StartsWith("asset"))
            {
                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                #endregion
            }

            if (indexName.StartsWith("attachment"))
            {
                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                #endregion
            }

            if (indexName.StartsWith("comment"))
            {
                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                #endregion
            }

            if (indexName.StartsWith("engagement"))
            {
                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                #endregion
            }

            if (indexName.StartsWith("inventory_asset"))
            {
                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                #endregion
            }

            if (indexName.StartsWith("license"))
            {
                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                #endregion
            }

            if (indexName.StartsWith("queue_asset"))
            {
                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                #endregion
            }

            if (indexName.StartsWith("queue_scan"))
            {
                var saltminer = mappings["saltminer"]["properties"].AsObject();
                var internals = saltminer["internal"]["properties"].AsObject();

                #region Additions
                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                #endregion

                #region Removal

                internals.Remove("agent_id");

                #endregion
            }

            if (indexName.StartsWith("sys_attribute_definition"))
            {
                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                #endregion
            }

            if (indexName.StartsWith("sys_config"))
            {
                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                #endregion
            }

            if (indexName.StartsWith("sys_custom_importer"))
            {
                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                #endregion
            }

            if (indexName.StartsWith("sys_custom_issue"))
            {
                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                #endregion
            }

            if (indexName.StartsWith("sys_index_meta"))
            {
                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                #endregion
            }

            if (indexName.StartsWith("sys_lookup"))
            {
                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                #endregion
            }

            if (indexName.StartsWith("sys_search_filter"))
            {
                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                #endregion
            }

            if (indexName.StartsWith("sys_service_job"))
            {
                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                #endregion
            }

            if (indexName.StartsWith("job_queue"))
            {
                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                if (mappings["message"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("message", new JsonObject());
                    mappings["message"].AsObject().Add("type", "keyword");
                }

                if (mappings["attributes"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("attributes", new JsonObject());
                    mappings["attributes"].AsObject().Add("type", "object");
                }

                #endregion

                #region Removal

                mappings.AsObject().Remove("engagement_id");
                mappings.AsObject().Remove("report_template");

                #endregion
            }

            if (indexName.StartsWith("issues_"))
            {
                var saltminer = mappings["saltminer"]["properties"].AsObject();
                var vulnerability = mappings["vulnerability"]["properties"].AsObject();
                var scan = saltminer["scan"]["properties"].AsObject();

                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                if (scan["noscan"]?.AsObject() == null)
                {
                    scan.Add("noscan", new JsonObject());
                    scan["noscan"].AsObject().Add("type", "integer");
                }

                if (vulnerability["days_to_close"]?.AsObject() == null)
                {
                    vulnerability.Add("days_to_close", new JsonObject());
                    vulnerability["days_to_close"].AsObject().Add("type", "integer");
                }

                #endregion
            }

            if (indexName.StartsWith("queue_issue"))
            {
                var vulnerability = mappings["vulnerability"]["properties"].AsObject();

                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                if (vulnerability["days_to_close"]?.AsObject() == null)
                {
                    vulnerability.Add("days_to_close", new JsonObject());
                    vulnerability["days_to_close"].AsObject().Add("type", "integer");
                }

                #endregion
            }

            if (indexName.StartsWith("snapshot"))
            {
                var saltminer = mappings["saltminer"]["properties"].AsObject();
                var scan = saltminer["scan"]["properties"].AsObject();

                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                if (scan["noscan"]?.AsObject() == null)
                {
                    scan.Add("noscan", new JsonObject());
                    scan["noscan"].AsObject().Add("type", "integer");
                }

                #endregion
            }

            if (indexName.StartsWith("scan"))
            {
                var saltminer = mappings["saltminer"]["properties"].AsObject();
                var scan = saltminer["scan"]["properties"].AsObject();

                #region Additions

                if (mappings["last_updated"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("last_updated", new JsonObject());
                    mappings["last_updated"].AsObject().Add("type", "date");
                }

                if (scan["noscan"]?.AsObject() == null)
                {
                    scan.Add("noscan", new JsonObject());
                    scan["noscan"].AsObject().Add("type", "integer");
                }

                #endregion

                #region Removal

                saltminer.Remove("internal");

                #endregion
            }
        }

        void IUpgradeStep.StepEtl<T>(SaltMinerIndexData index, string tempIndexName, IEnumerable<T> batch, SearchRequest request, IDataRepo data)
        {
            var doc = JsonNode.Parse(data.SearchForJson(request, tempIndexName));
            var count = 0;
            var array = doc["hits"]["hits"].AsArray();

            foreach (var item in batch)
            {
                if (array[count].AsObject()["_source"].AsObject()["timestamp"] != null && array[count].AsObject()["_source"].AsObject()["last_updated"] == null)
                {
                    item.LastUpdated = DateTime.Parse(array[count].AsObject()["_source"].AsObject()["timestamp"].ToString());
                }
                count++;
            }
        }
    }
}
