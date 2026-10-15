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
     * 
     * NOTE: As written, this example step class will not do anything except serve to increment from AppliesToVersion to CompletedVersion.
     * We can use the example as an "empty" step.
     */
    internal class Step030008 : IUpgradeStep
    {
        internal Step030008(ApiConfig config)
        {
            // config may or may not be needed - in this example it is not needed
        }

        string IUpgradeStep.AppliesToVersion => "3.0.7";

        string IUpgradeStep.CompletedVersion => "3.0.8";

        bool IUpgradeStep.RequiresSchemaUpdate => false; // set to true to use method below

        bool IUpgradeStep.RequiresETL => false; // set to true to use method below

        List<string> IUpgradeStep.UpdatedTemplateNames => []; // set to index template name of index (indices) to update

        void IUpgradeStep.UpdateSchema(string indexName, JsonNode indexTemplate)
        {
            var mappings = indexTemplate.AsObject()["index_templates"][0]["index_template"]["template"]["mappings"]["properties"];

            if (indexName.StartsWith("sys_service_job"))
            {
                #region Additions

                // Example - how to add a new property (remove example if RequiresSchemaUpdate == true)
                if (mappings["name"]?.AsObject() == null)
                {
                    mappings.AsObject().Add("name", new JsonObject());
                    mappings["name"].AsObject().Add("type", "keyword");
                }

                #endregion

                #region Removals

                // Example (remove example if RequiresSchemaUpdate == true)
                mappings.AsObject().Remove("unwanted_field");

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
            // Example - Adjust this to return if we aren't working with desired type(s) (remove example if RequiresEtl is set to true),
            if (first is not ServiceJob) // || first is QueueIssue, etc.
                return;

            // Example - Adjust this to transform data as needed (remove example if RequiresEtl is set to true)
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
