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
using System.Globalization;
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
    internal class Step030007 : IUpgradeStep
    {
        internal Step030007(ApiConfig config)
        {
            // config may or may not be needed - in this example it is not needed
        }

        string IUpgradeStep.AppliesToVersion => "3.0.6";

        string IUpgradeStep.CompletedVersion => "3.0.7";

        bool IUpgradeStep.RequiresSchemaUpdate => true; // set to true to use method below

        bool IUpgradeStep.RequiresETL => true; // set to true to use method below

        List<string> IUpgradeStep.UpdatedTemplateNames => ["comment"]; // set to index template name of index (indices) to update

        void IUpgradeStep.UpdateSchema(string indexName, JsonNode indexTemplate)
        {
            var mappings = indexTemplate.AsObject()["index_templates"][0]["index_template"]["template"]["mappings"]["properties"];

            if (indexName.StartsWith("comment"))
            {
                #region Additions

                var myMappings = mappings["saltminer"]["properties"]["comment"]["properties"];
                if (myMappings["added"]?.AsObject() == null)
                {
                    myMappings.AsObject().Add("added", new JsonObject());
                    myMappings["added"].AsObject().Add("type", "date");
                }

                #endregion

                #region Removals

                if (myMappings["unwanted_field"]?.AsObject() != null)
                    myMappings.AsObject().Remove("unwanted_field");

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
            if (first is not Comment) // || first is QueueIssue, etc.
                return;

            // Example - Adjust this to transform data as needed (remove example if RequiresEtl is set to true)
            foreach (var item in batch)
            {
                if (item is Comment entity)
                {
                    var docJson = array[count].AsObject()["_source"].AsObject();
                    if (docJson != null)
                    {
                        entity.Saltminer.Comment.Added = DateTime.Parse(docJson["timestamp"].ToString(), CultureInfo.InvariantCulture);
                    }
                }
                count++;
            }

        }
    }
}
