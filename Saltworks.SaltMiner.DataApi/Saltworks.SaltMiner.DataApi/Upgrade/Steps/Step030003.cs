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
    internal class Step030003 : IUpgradeStep
    {
        internal Step030003(ApiConfig config)
        {
            // config may or may not be needed - in this example it is not needed
        }

        string IUpgradeStep.AppliesToVersion => "3.0.2";

        string IUpgradeStep.CompletedVersion => "3.0.3";

        bool IUpgradeStep.RequiresSchemaUpdate => true;

        bool IUpgradeStep.RequiresETL => true;

        List<string> IUpgradeStep.UpdatedTemplateNames => [
            "snapshot",
            "queue_issue",
            "sys_service_job"
        ];

        void IUpgradeStep.UpdateSchema(string indexName, JsonNode indexTemplate)
        {
            var mappings = indexTemplate.AsObject()["index_templates"][0]["index_template"]["template"]["mappings"]["properties"];

            if (indexName.StartsWith("snapshot"))
            {
                var saltminer = mappings["saltminer"]["properties"].AsObject();

                #region Removals

                saltminer.Remove("noscan");

                #endregion
            }

            if (indexName.StartsWith("queue_issue"))
            {
                #region Additions

                if (mappings["is_cloned"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("is_cloned", new JsonObject());
                    mappings["is_cloned"].AsObject().Add("type", "boolean");
                }

                #endregion
            }

            if (indexName.StartsWith("sys_service_job"))
            {
                #region Additions

                if (mappings["name"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("name", new JsonObject());
                    mappings["name"].AsObject().Add("type", "keyword");
                }
                if (mappings["description"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("description", new JsonObject());
                    mappings["description"].AsObject().Add("type", "text");
                }
                if (mappings["disabled"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("disabled", new JsonObject());
                    mappings["disabled"].AsObject().Add("type", "boolean");
                }
                if (mappings["run_now"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("run_now", new JsonObject());
                    mappings["run_now"].AsObject().Add("type", "boolean");
                }
                if (mappings["option"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("option", new JsonObject());
                    mappings["option"].AsObject().Add("type", "keyword");
                }
                if (mappings["type"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("type", new JsonObject());
                    mappings["type"].AsObject().Add("type", "keyword");
                }
                if (mappings["next_run_time"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("next_run_time", new JsonObject());
                    mappings["next_run_time"].AsObject().Add("type", "date");
                }

                #endregion

                #region Removals

                mappings.AsObject().Remove("service_type");
                mappings.AsObject().Remove("service_job_type");

                #endregion
            }
        }

        /// <param name="index">Name/version/template of the affected index</param>
        /// <param name="tempIndexName">Old index data will be in this index</param>
        /// <param name="batch">New index objects to update</param>
        /// <param name="request">Search request used to retrieve the batch</param>
        /// <param name="data">Data repo object for making data calls</param>
        void IUpgradeStep.StepEtl<T>(SaltMinerIndexData index, string tempIndexName, IEnumerable<T> batch, SearchRequest request, IDataRepo data)
        {
            var doc = JsonNode.Parse(data.SearchForJson(request, tempIndexName));
            var count = 0;
            var array = doc["hits"]["hits"].AsArray();

            var first = batch.First();
            if (first is not ServiceJob) // || first is QueueIssue, etc.
                return;
            
            foreach (var item in batch)
            {
                if (item is ServiceJob sj)
                {
                    if (array[count].AsObject()["_source"].AsObject()["service_type"] != null)
                    {
                        sj.Option = array[count].AsObject()["_source"].AsObject()["service_type"].ToString();
                    }
                    if (array[count].AsObject()["_source"].AsObject()["service_job_type"] != null)
                    {
                        sj.Type = array[count].AsObject()["_source"].AsObject()["service_job_type"].ToString();
                    }
                }
                count++;
            }

        }
    }
}
