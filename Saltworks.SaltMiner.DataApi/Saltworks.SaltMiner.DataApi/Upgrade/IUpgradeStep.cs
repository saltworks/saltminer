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
