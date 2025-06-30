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

﻿using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.Core.Data;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.ElasticClient;
using Saltworks.SaltMiner.Core.Util;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class IndexContext : ContextBase
    {
        public IndexContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<AssetContext> logger) : base(config, dataRepository, factory, logger)
        { }

        /// <summary>
        /// Deletes Index by name
        /// </summary>
        /// <param name="indexName">The indexName of the entity to return</param>
        /// <returns>NoDataResponse with boolean indicating success</returns>
        public virtual NoDataResponse DeleteIndex(string indexName)
        {
            Logger.LogInformation("DeleteIndex: '{indexName}'", indexName);
            var result = ElasticClient.DeleteIndex(indexName);
            return result.ToNoDataResponse();
        }

        /// <summary>
        /// Checks and Adds active_issue_soourcetpye alias
        /// </summary>
        /// <param name="indexName">The index name for which to update alias (i.e. issues_app_saltworks.ssc_ssc1)</param>
        /// <returns>NoDataResponse with boolean indicating success</returns>
        public virtual NoDataResponse ActiveIssueAlias(string indexName)
        {
            Logger.LogInformation("ActiveIssueAlias: '{indexName}'", indexName);
            return DataRepo.ActiveIssueAlias(indexName, Config.DataIssueIndexDefaultAlias.Replace("[indexName]", indexName));
        }

        /// <summary>
        /// Refresh Index by name
        /// </summary>
        /// <param name="indexName">The indexName to refresh</param>
        /// <returns>NoDataResponse with boolean indicating success</returns>
        public virtual NoDataResponse RefreshIndex(string indexName)
        {
            Logger.LogInformation("RefreshIndex: '{indexName}'", indexName);
            var result = ElasticClient.RefreshIndex(indexName);
            return result.ToNoDataResponse();
        }

        /// <summary>
        /// Check for Index by name
        /// </summary>
        /// <param name="indexName">The indexName to refresh</param>
        /// <returns>NoDataResponse with boolean indicating success</returns>
        public virtual NoDataResponse CheckForIndex(string indexName)
        {
            Logger.LogInformation("CheckForIndex: '{indexName}'", indexName);
            var result = ElasticClient.CheckForIndex(indexName);
            return result.ToNoDataResponse();
        }
    }
}
