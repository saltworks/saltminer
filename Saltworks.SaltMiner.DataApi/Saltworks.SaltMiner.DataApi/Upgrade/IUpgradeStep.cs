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

﻿using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.DataApi.Models;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Saltworks.SaltMiner.DataApi.Upgrade
{
    internal interface IUpgradeStep
    {
        /// <summary>
        /// The version of Data API this step upgrades from.
        /// </summary>
        internal string AppliesToVersion { get; }
        /// <summary>
        /// The version of Data API this step upgrades to.
        /// </summary>
        internal string CompletedVersion { get; }
        /// <summary>
        /// Whether this step requires a schema update - if so, then the index template should be passed to UpdateSchema().
        /// </summary>
        internal bool RequiresSchemaUpdate { get; }
        /// <summary>
        /// Whether this step requires a reindex of its data (removing fields, renaming fields, etc.).
        /// </summary>
        internal bool RequiresETL { get; }
        /// <summary>
        /// List of index template names that will be affected
        /// </summary>
        List<string> UpdatedTemplateNames { get; }
        /// <summary>
        /// Updates the index template to add/remove/rename fields and their types.
        /// </summary>
        /// <param name="indexName">The index name for the update.</param>
        /// <param name="indexTemplate">The index template to update.</param>
        internal void UpdateSchema(string indexName, JsonNode indexTemplate);

        internal void StepEtl<T>(SaltMinerIndexData index, string tempIndexName, IEnumerable<T> batch, SearchRequest request, IDataRepo data) where T : SaltMinerEntity;
    }
}
